namespace Blog.Infrastructure.Seeding;

internal static class SeedArticlesExpand
{
    public static string Apply(string slug, string content)
    {
        var extra = For(slug);
        return string.IsNullOrEmpty(extra) ? content : content.TrimEnd() + "\n\n" + extra;
    }

    private static string For(string slug) => slug switch
    {
        "building-http-apis-that-age-well-in-aspnet-core" => Http,
        "modern-csharp-patterns-i-actually-use-in-production" => Csharp,
        "entity-framework-core-performance-the-80-20-checklist" => Ef,
        "clean-architecture-in-aspnet-core-without-the-ceremony" => Clean,
        "api-versioning-that-clients-can-live-with" => Versioning,
        "dependency-injection-in-aspnet-core-lifetimes-pitfalls-and-tests" => Di,
        "dockerizing-a-dotnet-api-for-local-development-and-ci" => Docker,
        "azure-app-service-vs-container-apps-for-a-dotnet-api" => Azure,
        "measuring-aspnet-core-performance-before-you-rewrite-anything" => Perf,
        "testing-aspnet-core-what-belongs-in-a-unit-test-vs-an-api-test" => Test,
        _ => string.Empty
    };

    private const string Http = """
## Idempotency in practice

I store the key, a hash of the body, and the serialized response. If the same key arrives with a different body, that is a client bug — I return 409, not the old result.

```csharp
public sealed class IdempotencyRecord
{
    public required string Key { get; init; }
    public required string RequestHash { get; init; }
    public required int StatusCode { get; init; }
    public required string ResponseBody { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

TTL of 24 hours is enough for payment-like retries. Do not make the table a second database of every GET.

## Pagination and filters as a contract

Cursor pagination ages better than `page=47` on a live table. If you must use offset, document that page 10 can skip or duplicate under writes. Always cap `pageSize`.

## OpenAPI is the product

If the swagger file and the runtime disagree, agents and partner teams will fail in ways your UI never will. Generate the document in CI and fail the build on a breaking diff. That is how APIs age well.

Related: [API versioning](/articles/api-versioning-that-clients-can-live-with) and the [JWT guide](/articles/jwt-authentication-and-authorization-in-aspnet) from ABi Helpline.
""";

    private const string Csharp = """
## Records vs classes in domain models

I use records for commands, queries, and events. I use classes (often sealed) for aggregates that mutate. A record article with `with` updates looks cute until you have invariants that must run on every change.

## Required and init

`required` on DTO properties catches missing JSON at bind time. Combine with nullable reference types. Do not mark a domain `Title` as `string?` because the database column was poorly migrated — fix the column.

## LINQ that stays honest

I still write `FirstOrDefault` and then throw a domain exception with a stable code. `Single` is for uniqueness you would treat as a data incident. Interviews like this distinction; production likes it more.

If you want the interview set, read [Top 10 C# questions](/articles/top-10-csharp-interview-questions-and-answers).
""";

    private const string Ef = """
## Compiled queries and split queries

For a hot lookup by slug I will compile the query. For graphs that cartesian-explode, `AsSplitQuery` is a tool, not a default. Measure both.

## Indexes that match the filter

If every public list is `Status = Published ORDER BY PublishedAt DESC`, that pair should be in an index. EF will not invent it from hope.

## Concurrency

A `xmin` or `RowVersion` on articles saved from admin stops lost updates. Return 409 when the token mismatches. Clients retry with the new representation.

See also [EF Core in 15 minutes](/articles/ef-core-in-15-minutes-all-in-one-guide) on ABi Helpline.
""";

    private const string Clean = """
## Mapping at the edges

I map HTTP DTOs in the API or Application, never inside the entity. I map EF configuration in Infrastructure, never with data annotations that leak column names into Domain.

## Testing the architecture

A unit test that `Publish` throws for archived articles does not need a web host. An integration test that `POST /api/v1/articles` returns 201 does. If both tests go through `HttpClient`, you do not have a domain layer — you have a slower UI.

The full tutorial video is [Clean Architecture in .NET 8](/videos/0J_T5qRynSI).
""";

    private const string Versioning = """
## Header vs URL

I still prefer `/api/v1` in the URL for public APIs. Humans, gateways, and logs can see it. Header versioning is fine behind a gateway that already rewrites. Do not support three styles at once.

## Deprecation

Add `Sunset` and a `Link` to the successor. Give clients a date. Removing v1 without a metric of remaining traffic is how you learn who your real customers were.
""";

    private const string Di = """
## The captive dependency

A singleton that takes a scoped `DbContext` will use one context for the life of the process. That is data corruption with extra steps. The analyzer will shout. Listen.

```csharp
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<PublishArticleHandler>();
```

## Tests

Replace `IClock` and `IArticleRepository`. Do not replace `ILogger` unless it is noisy. The composition root in `WebApplicationFactory` can swap Infrastructure for fakes when an integration test should not hit Azure.
""";

    private const string Docker = """
## The Dockerfile I keep copying

Multi-stage: restore and publish in SDK, run on `aspnet` with a non-root user. Copy only the published output. Set `ASPNETCORE_HTTP_PORTS`. Do not run as root because "it was easier locally".

Compose: API + PostgreSQL + a volume. Healthchecks so CI does not hit the port before Kestrel is up.

Related: [App Service vs Container Apps](/articles/azure-app-service-vs-container-apps-for-a-dotnet-api).
""";

    private const string Azure = """
## Operations questions that decide it

- Do we already know App Service slots and Easy Auth?
- Do we need scale-to-zero on a bursty worker?
- Who debugs a container at 2am?

If the team has never shipped a container, App Service is not a moral failure. If we already build images in CI, Container Apps (or AKS, later) stops being a science project.

This sits in the [Azure for .NET teams](/series/azure-for-dotnet-teams) series.
""";

    private const string Perf = """
## Counters I look at first

Request duration p95, thread pool starvation, GC pause, EF command duration. If you cannot see them, you are guessing. Application Insights or OpenTelemetry — pick one and keep it.

## The rewrite trap

"Let us move to Minimal APIs for performance" is almost never the first win. Serialization, N+1, and an unbounded `Include` beat framework folklore. Measure, then change the hot 5%.
""";

    private const string Test = """
## WebApplicationFactory

Use it to assert status codes, auth, and the JSON envelope. Do not use it to assert that a private mapper mapped two fields — that is a unit test.

```csharp
[Fact]
public async Task Published_article_is_public()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/v1/public/articles/top-10-csharp-interview-questions-and-answers");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

Pair this with the [unit testing video](/videos/NaGwNiUQRzI).
""";
}
