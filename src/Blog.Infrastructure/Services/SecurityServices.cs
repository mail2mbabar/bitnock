using Blog.Application.Agents;
using Blog.Application.Common;
using Blog.Application.Media;
using Blog.Application.Options;
using Blog.Domain.Constants;
using Blog.Domain.Entities;
using Blog.Infrastructure.Persistence;
using Blog.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Blog.Infrastructure.Services;

public sealed class MediaService : IMediaService
{
    private readonly BlogDbContext _db;
    private readonly IStorageService _storage;
    private readonly IAuditService _audit;
    private readonly StorageOptions _options;
    private readonly TimeProvider _clock;

    public MediaService(BlogDbContext db, IStorageService storage, IAuditService audit, IOptions<StorageOptions> options, TimeProvider clock)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<PagedResult<MediaAssetDto>> ListAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _db.MediaAssets.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search}%";
            query = query.Where(m => EF.Functions.ILike(m.FileName, like) || (m.AltText != null && EF.Functions.ILike(m.AltText, like)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => ToDto(m))
            .ToListAsync(cancellationToken);
        return new PagedResult<MediaAssetDto>(items, page, pageSize, total);
    }

    public async Task<MediaAssetDto?> GetAsync(Guid id, CancellationToken cancellationToken)
        => await _db.MediaAssets.AsNoTracking().Where(m => m.Id == id).Select(m => ToDto(m)).FirstOrDefaultAsync(cancellationToken);

    public async Task<MediaAssetDto> UploadAsync(MediaUploadCommand command, ActorContext actor, CancellationToken cancellationToken)
    {
        if (!_options.AllowedContentTypes.Contains(command.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw AppException.Validation("FILE_TYPE_NOT_ALLOWED", "This file type is not allowed.");
        }

        if (command.Content.CanSeek)
        {
            command.Content.Position = 0;
        }

        await using var buffer = new MemoryStream();
        await command.Content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > _options.MaxFileSizeBytes)
        {
            throw AppException.Validation("FILE_TOO_LARGE", $"File exceeds the {_options.MaxFileSizeBytes} byte limit.");
        }

        buffer.Position = 0;
        int? width = null;
        int? height = null;
        if (command.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            && !command.ContentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
        {
            using var image = await Image.LoadAsync(buffer, cancellationToken);
            width = image.Width;
            height = image.Height;
            buffer.Position = 0;
        }

        var stored = await _storage.SaveAsync(buffer, command.FileName, command.ContentType, cancellationToken);
        var asset = MediaAsset.Create(
            Path.GetFileName(command.FileName),
            stored.StorageKey,
            stored.Url,
            command.ContentType,
            stored.FileSize,
            width,
            height,
            command.AltText,
            command.Title,
            command.Description,
            _clock.GetUtcNow(),
            actor.UserId,
            actor.AgentId);
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(actor, AuditActions.MediaUploaded, "media", asset.Id.ToString(), new { asset.FileName, asset.MimeType }, cancellationToken);
        return ToDto(asset);
    }

    public async Task<MediaAssetDto> UpdateAsync(Guid id, UpdateMediaRequest request, CancellationToken cancellationToken)
    {
        var asset = await _db.MediaAssets.FirstOrDefaultAsync(m => m.Id == id, cancellationToken) ?? throw AppException.NotFound("Media");
        asset.UpdateMetadata(request.AltText, request.Title, request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(asset);
    }

    public async Task DeleteAsync(Guid id, ActorContext actor, CancellationToken cancellationToken)
    {
        var asset = await _db.MediaAssets.FirstOrDefaultAsync(m => m.Id == id, cancellationToken) ?? throw AppException.NotFound("Media");
        await _storage.DeleteAsync(asset.StorageKey, cancellationToken);
        _db.MediaAssets.Remove(asset);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(actor, AuditActions.MediaDeleted, "media", id.ToString(), new { asset.FileName }, cancellationToken);
    }

    private static MediaAssetDto ToDto(MediaAsset m)
        => new(m.Id, m.FileName, m.Url, m.MimeType, m.FileSize, m.Width, m.Height, m.AltText, m.Title, m.Description, m.CreatedAt);
}

public sealed class LocalStorageService : IStorageService
{
    private readonly StorageOptions _options;
    private readonly IHostEnvironment _env;

    public LocalStorageService(IOptions<StorageOptions> options, IHostEnvironment env)
    {
        _options = options.Value;
        _env = env;
    }

    public async Task<StoredObject> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                "image/svg+xml" => ".svg",
                _ => ".bin"
            };
        }

        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var full = Path.Combine(Root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await content.CopyToAsync(fs, cancellationToken);
        var size = fs.Length;
        return new StoredObject(key, BuildPublicUrl(key), size);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var full = Path.Combine(Root, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(full))
        {
            File.Delete(full);
        }

        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var full = Path.Combine(Root, storageKey.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = File.OpenRead(full);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken)
        => Task.FromResult(File.Exists(Path.Combine(Root, storageKey.Replace('/', Path.DirectorySeparatorChar))));

    private string BuildPublicUrl(string storageKey)
    {
        var root = (_options.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        if (!root.Contains("://", StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(root) && !root.Contains('.'))
            {
                root = $"{root}.onrender.com";
            }

            root = $"https://{root}";
        }

        if (!root.EndsWith("/media", StringComparison.OrdinalIgnoreCase))
        {
            root += "/media";
        }

        return $"{root}/{storageKey.Replace('\\', '/')}";
    }

    private string Root => Path.IsPathRooted(_options.LocalRoot)
        ? _options.LocalRoot
        : Path.Combine(_env.ContentRootPath, _options.LocalRoot);
}

public sealed class AuditService : IAuditService
{
    private readonly BlogDbContext _db;
    private readonly TimeProvider _clock;

    public AuditService(BlogDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task RecordAsync(ActorContext actor, string action, string resourceType, string? resourceId, object? metadata, CancellationToken cancellationToken)
    {
        var json = metadata is null ? null : System.Text.Json.JsonSerializer.Serialize(metadata);
        _db.AuditLogs.Add(AuditLog.Create(
            actor.ActorId,
            actor.ActorType,
            actor.ActorName,
            action,
            resourceType,
            resourceId,
            _clock.GetUtcNow(),
            actor.IpAddress,
            actor.RequestId,
            json));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>> ListAsync(int page, int pageSize, string? actor, string? action, string? resourceType, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(actor))
        {
            query = query.Where(a => a.ActorName.Contains(actor) || a.ActorId == actor);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            query = query.Where(a => a.ResourceType == resourceType);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.ActorId, a.ActorType.ToString(), a.ActorName, a.Action, a.ResourceType, a.ResourceId, a.Timestamp, a.IpAddress, a.RequestId, a.MetadataJson))
            .ToListAsync(cancellationToken);
        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }
}

public sealed class AgentCredentialService : IAgentCredentialService
{
    private readonly BlogDbContext _db;
    private readonly TimeProvider _clock;

    public AgentCredentialService(BlogDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<AgentCredentialDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var total = await _db.AgentCredentials.CountAsync(cancellationToken);
        var now = _clock.GetUtcNow();
        var items = await _db.AgentCredentials.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AgentCredentialDto>(items.Select(a => ToDto(a, now)).ToList(), page, pageSize, total);
    }

    public async Task<CreatedAgentCredentialDto> CreateAsync(CreateAgentRequest request, Guid createdByUserId, CancellationToken cancellationToken)
    {
        var key = SecretHasher.GenerateApiKey(out var prefix);
        var entity = Domain.Entities.AgentCredential.Create(request.Name, prefix, SecretHasher.Hash(key), request.Scopes, _clock.GetUtcNow(), createdByUserId, request.ExpiresAt);
        _db.AgentCredentials.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new CreatedAgentCredentialDto(entity.Id, entity.Name, key, entity.ScopeList, entity.ExpiresAt);
    }

    public async Task<AgentCredentialDto> UpdateAsync(Guid id, UpdateAgentRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.AgentCredentials.FirstOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw AppException.NotFound("Agent");
        if (request.Scopes is not null)
        {
            entity.UpdateScopes(request.Scopes);
        }

        if (request.Disabled is true)
        {
            entity.Disable();
        }
        else if (request.Disabled is false)
        {
            entity.Enable();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity, _clock.GetUtcNow());
    }

    public async Task RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.AgentCredentials.FirstOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw AppException.NotFound("Agent");
        entity.Revoke();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CreatedAgentCredentialDto> RotateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.AgentCredentials.FirstOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw AppException.NotFound("Agent");
        var key = SecretHasher.GenerateApiKey(out var prefix);
        entity.Rotate(prefix, SecretHasher.Hash(key), _clock.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);
        return new CreatedAgentCredentialDto(entity.Id, entity.Name, key, entity.ScopeList, entity.ExpiresAt);
    }

    public async Task<Application.Agents.AgentCredential?> AuthenticateAsync(string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("nxa_", StringComparison.Ordinal))
        {
            return null;
        }

        var prefix = apiKey.Length >= 12 ? apiKey[..12] : apiKey;
        var entity = await _db.AgentCredentials.FirstOrDefaultAsync(a => a.KeyPrefix == prefix, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var now = _clock.GetUtcNow();
        if (entity.Status(now) != Domain.Enums.AgentCredentialStatus.Active)
        {
            return null;
        }

        var hash = SecretHasher.Hash(apiKey);
        if (!CryptographicEquals(hash, entity.KeyHash))
        {
            return null;
        }

        entity.MarkUsed("authenticated", now);
        await _db.SaveChangesAsync(cancellationToken);
        return new Application.Agents.AgentCredential(entity.Id, entity.Name, entity.ScopeList, entity.Status(now));
    }

    private static AgentCredentialDto ToDto(Domain.Entities.AgentCredential entity, DateTimeOffset now)
        => new(entity.Id, entity.Name, entity.KeyPrefix, entity.ScopeList, entity.Status(now).ToString(), entity.CreatedAt, entity.ExpiresAt, entity.LastUsedAt, entity.LastAction);

    private static bool CryptographicEquals(string a, string b)
        => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(a),
            Convert.FromHexString(b));
}
