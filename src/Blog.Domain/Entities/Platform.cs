using Blog.Domain.Common;
using Blog.Domain.Constants;
using Blog.Domain.Enums;

namespace Blog.Domain.Entities;

public sealed class MediaAsset : Entity
{
    public string FileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string? AltText { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public Guid? UploadedByUserId { get; private set; }
    public Guid? UploadedByAgentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private MediaAsset()
    {
    }

    public static MediaAsset Create(
        string fileName,
        string storageKey,
        string url,
        string mimeType,
        long fileSize,
        int? width,
        int? height,
        string? altText,
        string? title,
        string? description,
        DateTimeOffset now,
        Guid? uploadedByUserId = null,
        Guid? uploadedByAgentId = null)
    {
        return new MediaAsset
        {
            FileName = fileName,
            StorageKey = storageKey,
            Url = url,
            MimeType = mimeType,
            FileSize = fileSize,
            Width = width,
            Height = height,
            AltText = altText,
            Title = title,
            Description = description,
            UploadedByUserId = uploadedByUserId,
            UploadedByAgentId = uploadedByAgentId,
            CreatedAt = now
        };
    }

    public void UpdateMetadata(string? altText, string? title, string? description)
    {
        AltText = altText;
        Title = title;
        Description = description;
    }
}

public sealed class AgentCredential : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string Scopes { get; private set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public string? LastAction { get; private set; }
    public bool IsRevoked { get; private set; }
    public bool IsDisabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private AgentCredential()
    {
    }

    public static AgentCredential Create(
        string name,
        string keyPrefix,
        string keyHash,
        IEnumerable<string> scopes,
        DateTimeOffset now,
        Guid createdByUserId,
        DateTimeOffset? expiresAt = null)
    {
        var normalized = scopes
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var scope in normalized)
        {
            if (!AgentScopes.All.Contains(scope, StringComparer.OrdinalIgnoreCase))
            {
                throw new DomainException("INVALID_SCOPE", $"Scope '{scope}' is not recognized.");
            }
        }

        return new AgentCredential
        {
            Name = name.Trim(),
            KeyPrefix = keyPrefix,
            KeyHash = keyHash,
            Scopes = string.Join(' ', normalized),
            CreatedAt = now,
            CreatedByUserId = createdByUserId,
            ExpiresAt = expiresAt
        };
    }

    public AgentCredentialStatus Status(DateTimeOffset now)
    {
        if (IsRevoked)
        {
            return AgentCredentialStatus.Revoked;
        }

        if (IsDisabled)
        {
            return AgentCredentialStatus.Disabled;
        }

        if (ExpiresAt is not null && ExpiresAt <= now)
        {
            return AgentCredentialStatus.Expired;
        }

        return AgentCredentialStatus.Active;
    }

    public bool HasScope(string scope)
        => Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(scope, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> ScopeList
        => Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public void Revoke()
    {
        IsRevoked = true;
        IsDisabled = true;
    }

    public void Disable() => IsDisabled = true;

    public void Enable()
    {
        if (IsRevoked)
        {
            throw new DomainException("AGENT_REVOKED", "A revoked credential cannot be re-enabled. Create a new credential.");
        }

        IsDisabled = false;
    }

    public void Rotate(string keyPrefix, string keyHash, DateTimeOffset now)
    {
        if (IsRevoked)
        {
            throw new DomainException("AGENT_REVOKED", "A revoked credential cannot be rotated.");
        }

        KeyPrefix = keyPrefix;
        KeyHash = keyHash;
        LastUsedAt = null;
        LastAction = "rotated";
    }

    public void UpdateScopes(IEnumerable<string> scopes)
    {
        if (IsRevoked)
        {
            throw new DomainException("AGENT_REVOKED", "A revoked credential cannot be modified.");
        }

        var normalized = scopes
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var scope in normalized)
        {
            if (!AgentScopes.All.Contains(scope, StringComparer.OrdinalIgnoreCase))
            {
                throw new DomainException("INVALID_SCOPE", $"Scope '{scope}' is not recognized.");
            }
        }

        Scopes = string.Join(' ', normalized);
    }

    public void MarkUsed(string action, DateTimeOffset now)
    {
        LastUsedAt = now;
        LastAction = action;
    }
}

public sealed class AuditLog : Entity
{
    public string ActorId { get; private set; } = string.Empty;
    public ActorType ActorType { get; private set; }
    public string ActorName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string? ResourceId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string? IpAddress { get; private set; }
    public string? RequestId { get; private set; }
    public string? MetadataJson { get; private set; }

    private AuditLog()
    {
    }

    public static AuditLog Create(
        string actorId,
        ActorType actorType,
        string actorName,
        string action,
        string resourceType,
        string? resourceId,
        DateTimeOffset timestamp,
        string? ipAddress,
        string? requestId,
        string? metadataJson)
    {
        return new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            ActorName = actorName,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Timestamp = timestamp,
            IpAddress = ipAddress,
            RequestId = requestId,
            MetadataJson = metadataJson
        };
    }
}

public sealed class NewsletterSubscriber : Entity
{
    public string Email { get; private set; } = string.Empty;
    public NewsletterStatus Status { get; private set; }
    public DateTimeOffset SubscribedAt { get; private set; }
    public DateTimeOffset? UnsubscribedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string ConfirmTokenHash { get; private set; } = string.Empty;

    private NewsletterSubscriber()
    {
    }

    public static NewsletterSubscriber Create(string email, string confirmTokenHash, DateTimeOffset now)
    {
        return new NewsletterSubscriber
        {
            Email = email.Trim().ToLowerInvariant(),
            Status = NewsletterStatus.PendingConfirmation,
            SubscribedAt = now,
            ConfirmTokenHash = confirmTokenHash
        };
    }

    public void Confirm(DateTimeOffset now)
    {
        Status = NewsletterStatus.Subscribed;
        ConfirmedAt = now;
    }

    public void Unsubscribe(DateTimeOffset now)
    {
        Status = NewsletterStatus.Unsubscribed;
        UnsubscribedAt = now;
    }
}

public sealed class AnalyticsEvent : Entity
{
    public string EventType { get; private set; } = string.Empty;
    public Guid? ArticleId { get; private set; }
    public string Path { get; private set; } = string.Empty;
    public string? Referrer { get; private set; }
    public string? DeviceCategory { get; private set; }
    public string? Country { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AnalyticsEvent()
    {
    }

    public static AnalyticsEvent PageView(
        string path,
        Guid? articleId,
        string? referrer,
        string? deviceCategory,
        string? country,
        DateTimeOffset now)
    {
        return new AnalyticsEvent
        {
            EventType = "page_view",
            Path = path,
            ArticleId = articleId,
            Referrer = referrer,
            DeviceCategory = deviceCategory,
            Country = country,
            CreatedAt = now
        };
    }
}

public sealed class IdempotencyRecord : Entity
{
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid AgentId { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public string Method { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public string ResponseBody { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private IdempotencyRecord()
    {
    }

    public static IdempotencyRecord Create(
        string key,
        Guid agentId,
        string requestHash,
        string method,
        string path,
        int statusCode,
        string responseBody,
        DateTimeOffset now,
        TimeSpan ttl)
    {
        return new IdempotencyRecord
        {
            IdempotencyKey = key,
            AgentId = agentId,
            RequestHash = requestHash,
            Method = method,
            Path = path,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            CreatedAt = now,
            ExpiresAt = now.Add(ttl)
        };
    }
}

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime)
        };
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now) => RevokedAt = now;
}

public sealed class SiteSetting
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }

    private SiteSetting()
    {
    }

    public SiteSetting(string key, string value, DateTimeOffset now)
    {
        Key = key;
        Value = value;
        UpdatedAt = now;
    }

    public void SetValue(string value, DateTimeOffset now)
    {
        Value = value;
        UpdatedAt = now;
    }
}
