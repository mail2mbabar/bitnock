using Blog.Application.Common;
using Blog.Domain.Enums;

namespace Blog.Application.Agents;

public sealed record AgentCredentialDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    IReadOnlyList<string> Scopes,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastUsedAt,
    string? LastAction);

public sealed record CreatedAgentCredentialDto(
    Guid Id,
    string Name,
    string ApiKey,
    IReadOnlyList<string> Scopes,
    DateTimeOffset? ExpiresAt);

public sealed record CreateAgentRequest(
    string Name,
    IReadOnlyList<string> Scopes,
    DateTimeOffset? ExpiresAt);

public sealed record UpdateAgentRequest(IReadOnlyList<string>? Scopes, bool? Disabled);

public sealed record AuditLogDto(
    Guid Id,
    string ActorId,
    string ActorType,
    string ActorName,
    string Action,
    string ResourceType,
    string? ResourceId,
    DateTimeOffset Timestamp,
    string? IpAddress,
    string? RequestId,
    string? MetadataJson);

public interface IAgentCredentialService
{
    Task<PagedResult<AgentCredentialDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<CreatedAgentCredentialDto> CreateAsync(CreateAgentRequest request, Guid createdByUserId, CancellationToken cancellationToken);
    Task<AgentCredentialDto> UpdateAsync(Guid id, UpdateAgentRequest request, CancellationToken cancellationToken);
    Task RevokeAsync(Guid id, CancellationToken cancellationToken);
    Task<CreatedAgentCredentialDto> RotateAsync(Guid id, CancellationToken cancellationToken);
    Task<AgentCredential?> AuthenticateAsync(string apiKey, CancellationToken cancellationToken);
}

public sealed record AgentCredential(
    Guid Id,
    string Name,
    IReadOnlyList<string> Scopes,
    AgentCredentialStatus Status);

public interface IAuditService
{
    Task RecordAsync(
        ActorContext actor,
        string action,
        string resourceType,
        string? resourceId,
        object? metadata,
        CancellationToken cancellationToken);

    Task<PagedResult<AuditLogDto>> ListAsync(
        int page,
        int pageSize,
        string? actor,
        string? action,
        string? resourceType,
        CancellationToken cancellationToken);
}
