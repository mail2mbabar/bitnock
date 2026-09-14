namespace Blog.Application.Options;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string Name { get; set; } = "Bitnock";
    public string Description { get; set; } = "Bitnock is Muhammad Babar’s .NET engineering publication. C#, ASP.NET Core, Azure, tutorials and interview preparations.";
    public string Url { get; set; } = "http://localhost:3000";
    public string LogoUrl { get; set; } = "/logo.png";
    public string FaviconUrl { get; set; } = "/favicon.ico";
    public string DefaultAuthorSlug { get; set; } = "muhammad-babar";
    public string Locale { get; set; } = "en-GB";
    public SocialLinkOptions Social { get; set; } = new();
}

public sealed class SocialLinkOptions
{
    public string? GitHub { get; set; } = "https://github.com/mail2mbabar";
    public string? LinkedIn { get; set; }
    public string? X { get; set; }
    public string? YouTube { get; set; } = "https://www.youtube.com/@ABiHelpline";
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "nexus";
    public string Audience { get; set; } = "nexus-web";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 20;
    public int RefreshTokenDays { get; set; } = 14;
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";
    public string LocalRoot { get; set; } = "storage";
    public string PublicBaseUrl { get; set; } = "http://localhost:5080/media";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml"
    ];
}

public sealed class PublishingOptions
{
    public const string SectionName = "Publishing";

    public string Mode { get; set; } = "RequireApproval";
    public int MinimumContentLength { get; set; } = 400;
    public int MinimumTagCount { get; set; } = 1;
    public bool RequireFeaturedImage { get; set; } = true;
    public bool RequireSeoTitle { get; set; } = true;
    public bool RequireMetaDescription { get; set; } = true;
    public int MetaDescriptionMinLength { get; set; } = 70;
    public int MetaDescriptionMaxLength { get; set; } = 160;
    public int SeoTitleMaxLength { get; set; } = 70;
    public bool AllowHtmlContent { get; set; } = false;
}

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    public int PublicPermitLimit { get; set; } = 120;
    public int AdminPermitLimit { get; set; } = 300;
    public int AgentPermitLimit { get; set; } = 60;
    public int AuthPermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = ["http://localhost:3000"];
}

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; } = true;
    public string DevAdminEmail { get; set; } = "admin@localhost";
    public string DevAdminPassword { get; set; } = "DevOnly!Nexus2026";
}
