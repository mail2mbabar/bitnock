using Blog.Application.Common;

namespace Blog.Application.Media;

public sealed record MediaAssetDto(
    Guid Id,
    string FileName,
    string Url,
    string MimeType,
    long FileSize,
    int? Width,
    int? Height,
    string? AltText,
    string? Title,
    string? Description,
    DateTimeOffset CreatedAt);

public sealed record MediaUploadCommand(
    Stream Content,
    string FileName,
    string ContentType,
    string? AltText,
    string? Title,
    string? Description);

public sealed record UpdateMediaRequest(string? AltText, string? Title, string? Description);

public interface IMediaService
{
    Task<PagedResult<MediaAssetDto>> ListAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);
    Task<MediaAssetDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<MediaAssetDto> UploadAsync(MediaUploadCommand command, ActorContext actor, CancellationToken cancellationToken);
    Task<MediaAssetDto> UpdateAsync(Guid id, UpdateMediaRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);
}

public interface IStorageService
{
    Task<StoredObject> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed record StoredObject(string StorageKey, string Url, long FileSize);
