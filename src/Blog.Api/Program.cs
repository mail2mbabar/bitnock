using System.Text;
using System.Threading.RateLimiting;
using Blog.Api.Auth;
using Blog.Api.Middleware;
using Blog.Application;
using Blog.Application.Auth;
using Blog.Application.Common;
using Blog.Application.Options;
using Blog.Domain.Constants;
using Blog.Infrastructure;
using Blog.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

static string? WithHttps(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    return value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? value.TrimEnd('/') : $"https://{value.TrimEnd('/')}";
}

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Site:Url"] = WithHttps(builder.Configuration["Site:Url"]),
    ["Storage:PublicBaseUrl"] = WithHttps(builder.Configuration["Storage:PublicBaseUrl"])
});

builder.Host.UseSerilog((ctx, services, config) =>
{
    config.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Bitnock")
        .WriteTo.Console()
        .WriteTo.File("logs/nexus-.log", rollingInterval: RollingInterval.Day)
        .Filter.ByExcluding(log =>
        {
            var rendered = log.RenderMessage();
            return rendered.Contains("nxa_", StringComparison.OrdinalIgnoreCase)
                   || rendered.Contains("password", StringComparison.OrdinalIgnoreCase)
                   || rendered.Contains("Authorization", StringComparison.OrdinalIgnoreCase);
        });
});

builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<PublishingOptions>(builder.Configuration.GetSection(PublishingOptions.SectionName));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.Configure<Blog.Application.Options.CorsOptions>(builder.Configuration.GetSection(Blog.Application.Options.CorsOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
}

var cors = builder.Configuration.GetSection(Blog.Application.Options.CorsOptions.SectionName).Get<Blog.Application.Options.CorsOptions>() ?? new();
var rate = builder.Configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddOutputCache();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "Smart", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (context.Request.Headers.ContainsKey("X-Api-Key")
                || authorization.StartsWith("Bearer nxa_", StringComparison.Ordinal))
            {
                return AgentAuthenticationHandler.SchemeName;
            }

            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    Envelope<object>.Fail(new ApiError("UNAUTHORIZED", "Authentication is required."), context.HttpContext.TraceIdentifier));
            }
        };
    })
    .AddScheme<AgentAuthenticationOptions, AgentAuthenticationHandler>(AgentAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Permissions.CanCreateArticle, p => p.RequireRole(Roles.Admin, Roles.Editor, Roles.Author));
    options.AddPolicy(Permissions.CanEditArticle, p => p.RequireRole(Roles.Admin, Roles.Editor, Roles.Author));
    options.AddPolicy(Permissions.CanPublishArticle, p => p.RequireRole(Roles.Admin, Roles.Editor));
    options.AddPolicy(Permissions.CanApproveArticles, p => p.RequireRole(Roles.Admin, Roles.Editor));
    options.AddPolicy(Permissions.CanManageMedia, p => p.RequireRole(Roles.Admin, Roles.Editor, Roles.Author));
    options.AddPolicy(Permissions.CanManageUsers, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Permissions.CanManageAgentKeys, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Permissions.CanManageTaxonomy, p => p.RequireRole(Roles.Admin, Roles.Editor));
    options.AddPolicy(Permissions.CanViewAuditLogs, p => p.RequireRole(Roles.Admin, Roles.Editor));
    options.AddPolicy(Permissions.CanManageSettings, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Permissions.CanViewAnalytics, p => p.RequireRole(Roles.Admin, Roles.Editor));
    options.AddPolicy(Permissions.CanImportExport, p => p.RequireRole(Roles.Admin, Roles.Editor));
    options.AddPolicy("AgentOnly", p =>
    {
        p.AddAuthenticationSchemes(AgentAuthenticationHandler.SchemeName);
        p.RequireAuthenticatedUser();
        p.RequireRole(Roles.Agent);
    });
});

builder.Services.AddCors(options =>
{
    var origins = cors.AllowedOrigins
        .Select(origin => origin.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? origin.TrimEnd('/') : $"https://{origin.TrimEnd('/')}")
        .ToArray();
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = rate.PublicPermitLimit, Window = TimeSpan.FromSeconds(rate.WindowSeconds) }));
    options.AddPolicy("admin", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = rate.AdminPermitLimit, Window = TimeSpan.FromSeconds(rate.WindowSeconds) }));
    options.AddPolicy("agent", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = rate.AgentPermitLimit, Window = TimeSpan.FromSeconds(rate.WindowSeconds) }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = rate.AuthPermitLimit, Window = TimeSpan.FromSeconds(rate.WindowSeconds) }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bitnock Publishing API",
        Version = "v1",
        Description = "Human CMS and machine-first agent publishing API for a .NET developer publication."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Admin JWT access token"
    });
    options.AddSecurityDefinition("AgentKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "Agent API key. Shown only once at creation. Format: nxa_..."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT")))
{
    app.UseHttpsRedirection();
}
app.UseCors("frontend");
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
app.MapGet("/health/ready", async (Blog.Infrastructure.Persistence.BlogDbContext db) =>
{
    var can = await db.Database.CanConnectAsync();
    return can ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503);
}).AllowAnonymous();

app.MapGet("/sitemap.xml", async (ISeoDocumentService seo) =>
{
    var xml = await seo.GetSitemapAsync(CancellationToken.None);
    return Results.Content(xml, "application/xml");
}).AllowAnonymous();
app.MapGet("/rss.xml", async (ISeoDocumentService seo) =>
{
    var xml = await seo.GetRssAsync(CancellationToken.None);
    return Results.Content(xml, "application/rss+xml");
}).AllowAnonymous();
app.MapGet("/robots.txt", (ISeoDocumentService seo) => Results.Text(seo.GetRobots(), "text/plain")).AllowAnonymous();

app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Bitnock API v1"));

app.Run();

public partial class Program;
