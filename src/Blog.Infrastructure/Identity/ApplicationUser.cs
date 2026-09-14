using Microsoft.AspNetCore.Identity;

namespace Blog.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? AuthorId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? TotpSecret { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
