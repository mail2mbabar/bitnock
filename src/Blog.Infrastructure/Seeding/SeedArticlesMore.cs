using Blog.Domain.Enums;

namespace Blog.Infrastructure.Seeding;

internal static class SeedArticlesMore
{
    public static SeedArticleSpec[] Items =>
    [
        SeedArticles.Create("What .NET 10 changes for ASP.NET Core teams", "what-dotnet-10-changes-for-aspnet-core-teams",
            "The SDK, hosting, and API surface I would actually turn on in a production shop.",
            "A field guide to .NET 10 for teams already shipping ASP.NET Core, EF Core, and Azure — what is worth adopting this quarter, and what can wait.",
            "dotnet", [".NET 10", "ASP.NET Core", "C#"], [".NET 10", "ASP.NET Core", "C#"], Difficulty.Intermediate, ArticleContentType.Guide, true, null, Dotnet10),
        SeedArticles.Create("C# 14 features I would actually ship", "csharp-14-features-i-would-actually-ship",
            "Language features that reduce noise in production code, not conference slides.",
            "A practitioner's take on C# 14 with .NET 10: which features I would enable in a brownfield ASP.NET Core codebase this year.",
            "csharp", ["C#", ".NET 10"], ["C#", ".NET 10"], Difficulty.Intermediate, ArticleContentType.DeepDive, false, null, Csharp14),
        SeedArticles.Create("Minimal APIs vs controllers in ASP.NET Core", "minimal-apis-vs-controllers-in-aspnet-core",
            "A comparison based on team size, OpenAPI, and how long the API has to live.",
            "When I choose Minimal APIs, when I keep MVC controllers, and how to stop treating the choice as a personality test.",
            "comparisons", ["ASP.NET Core", "Comparisons", "REST API"], ["ASP.NET Core", "C#"], Difficulty.Intermediate, ArticleContentType.Opinion, false, 5, MinimalVsControllers, "mastering-aspnet-core"),
        SeedArticles.Create("EF Core vs Dapper: choosing a data access style", "ef-core-vs-dapper-choosing-a-data-access-style",
            "Use the ORM until it hurts, then be honest about why it hurts.",
            "A side-by-side of EF Core and Dapper for .NET backends, with the hybrid approach I use on high-read services.",
            "comparisons", ["EF Core", "Dapper", "Comparisons", "Performance"], ["EF Core", "Dapper", "SQL Server"], Difficulty.Advanced, ArticleContentType.Opinion, false, null, EfVsDapper),
        SeedArticles.Create("Azure Functions vs Worker Services for .NET", "azure-functions-vs-worker-services-for-dotnet",
            "Serverless is a billing and operations decision, not a default architecture.",
            "How I choose between Azure Functions, .NET Worker Services, and the older WebJobs model for claims, messages, and background work.",
            "azure", ["Azure Functions", "Comparisons", "Azure"], ["Azure Functions", ".NET", "Azure Service Bus"], Difficulty.Intermediate, ArticleContentType.Opinion, false, 2, FunctionsVsWorkers, "azure-for-dotnet-teams"),
        SeedArticles.Create("Angular vs Blazor for a .NET engineering team", "angular-vs-blazor-for-a-dotnet-engineering-team",
            "The frontend choice is a hiring and product-velocity choice, not a religious one.",
            "I ship Angular on most product surfaces. Here is when I would still pick Blazor, and when I would not.",
            "comparisons", ["Angular", "Blazor", "Comparisons", "ASP.NET Core"], ["Angular", "Blazor", "ASP.NET Core"], Difficulty.Intermediate, ArticleContentType.Opinion, false, null, AngularVsBlazor),
        SeedArticles.Create("Azure Service Bus vs Event Grid for .NET", "azure-service-bus-vs-event-grid-for-dotnet",
            "Queues, sessions, and fan-out: pick the primitive that matches the failure mode.",
            "A comparison of Azure Service Bus and Event Grid from the perspective of ASP.NET Core services that cannot lose a message.",
            "azure", ["Azure Service Bus", "Event Grid", "Comparisons"], ["Azure Service Bus", "Event Grid", "ASP.NET Core"], Difficulty.Advanced, ArticleContentType.Architecture, false, 3, ServiceBusVsEventGrid, "azure-for-dotnet-teams"),
        SeedArticles.Create("GitHub Actions vs Azure DevOps for .NET CI", "github-actions-vs-azure-devops-for-dotnet-ci",
            "Pipelines are product infrastructure. Choose the one your pull requests already live in.",
            "How I compare GitHub Actions and Azure DevOps for .NET 8/10 builds, test gates, and Azure deployments.",
            "devops", ["GitHub Actions", "Azure DevOps", "Comparisons", "CI/CD"], ["GitHub Actions", "Azure DevOps", ".NET"], Difficulty.Intermediate, ArticleContentType.Opinion, false, 5, ActionsVsAzdo, "azure-for-dotnet-teams"),
        SeedArticles.Create("Azure API Management in front of .NET APIs", "azure-api-management-in-front-of-dotnet-apis",
            "APIM is not a substitute for a good API. It is a control plane for the ones you already have.",
            "How I put Azure API Management in front of ASP.NET Core APIs for partner access, versioning, and rate limits without hiding bad contracts.",
            "azure", ["Azure API Management", "REST API", "Azure"], ["Azure API Management", "ASP.NET Core"], Difficulty.Intermediate, ArticleContentType.Guide, false, 4, Apim, "azure-for-dotnet-teams"),
        SeedArticles.Create("Multi-tenant ASP.NET Core: lessons from ecommerce", "multi-tenant-aspnet-core-lessons-from-ecommerce",
            "One codebase, many storefronts, and the isolation mistakes that show up in production.",
            "Patterns I use for multi-tenant ASP.NET Core and Angular ecommerce: tenant resolution, data isolation, and the configuration that must not leak.",
            "architecture", ["ASP.NET Core", "Multi-tenant", "Ecommerce", "Azure"], ["ASP.NET Core", "Angular", "Azure SQL"], Difficulty.Advanced, ArticleContentType.CaseStudy, false, null, MultiTenant)
    ];

    private const string Dotnet10 = """
## Start from the LTS you are actually on

.NET 10 is the current long-term support train. If you are still on .NET 8, the useful question is not "what is new in the keynote" but "what reduces operational cost when we move the API, the workers, and the test suite together."

I treat the upgrade as a platform change, not a language tour. The SDK, the ASP.NET Core hosting model, EF Core, and the container images have to move as a set. Partial upgrades — API on 10, Functions still on 8 — are how you invent two support matrices.

## What I turn on first

**SDK and containers.** Pin `net10.0` in every `csproj`, rebuild the Docker images from `mcr.microsoft.com/dotnet/aspnet:10.0`, and make CI fail if a project silently stays on 8. The first win is a single restore graph.

**ASP.NET Core request pipeline.** Most APIs I maintain are already using endpoint routing, minimal hosting, and `WebApplicationFactory`. .NET 10 continues that direction. I do not rewrite controllers into Minimal APIs as part of the upgrade. I do re-run the performance baseline: allocations in JSON serialization and Kestrel still move between major versions.

**OpenAPI.** If you are still hand-maintaining Swashbuckle attributes as the source of truth, this is a good moment to make the document a build artifact and treat breaking schema diffs as CI failures. Agents and partner teams consume the contract more than they consume your wiki.

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

## What I do not rush

I do not enable every new C# 14 syntax in a 200-project solution on day one. I do not migrate Identity stores "because the template changed." I do not move from Azure App Service to Container Apps as a side effect of the SDK bump.

The boring checklist is the one that pays: nullable reference types already on, analyzers treated as errors in CI, integration tests green on `WebApplicationFactory`, and a rollback plan that is a previous container image, not a hope.

## Azure pairing

On Azure I keep App Service and Functions on the same runtime family. Mixing .NET 8 Functions with a .NET 10 API is survivable for a sprint and expensive after that — especially once shared libraries start taking `TimeProvider`, new BCL types, or JSON options that only exist on one side.

If you only do one thing after reading this: upgrade a vertical slice (API + worker + tests + image) and measure. .NET 10 is ready. Your process has to be ready with it.
""";

    private const string Csharp14 = """
## Language features are product features only if the team can read them

C# 14 shipped with .NET 10. I evaluate new syntax with one test: will a reviewer who did not watch the announcement still understand the pull request in six months?

I adopt features that delete boilerplate without hiding control flow. I postpone features that make a file look clever.

## What I would enable in a brownfield API

**Clearer collection and parameter stories.** Params collections and collection expressions already reduced ceremony in .NET 8/9 code. I keep using them for `params ReadOnlySpan<string>` style helpers and for test data. They make test setup shorter without changing runtime behaviour.

**Field-backed properties, used sparingly.** When a property needs a tiny amount of logic — trim, canonicalize a slug, reject empty — a field-backed property is cleaner than a full backing field plus two accessors. I do not convert every DTO.

```csharp
public sealed class Article
{
    public string Slug
    {
        get;
        set => field = value.Trim().ToLowerInvariant();
    }
}
```

**Null-conditional assignment** for optional wiring:

```csharp
article.FeaturedImage?.AltText = request.AltText;
```

Useful. Not a reason to rewrite a module.

## What I still write the long way

I still write explicit switch expressions for domain status machines. I still keep constructors for entities that have invariants. I still avoid turning every helper into an extension member just because the language now allows richer extensions.

The goal is a codebase a contractor can navigate on Monday. C# 14 helps when it removes noise. It hurts when it becomes the subject of the file.

## Analyzers are the adoption mechanism

Turn the new language version on in the `csproj`, keep nullable enabled, and let the compiler show you the safe conversions. Do not "modernize" 400 files in one PR. I take the files I already have open for a feature and leave the rest until they move for a real reason.

That is how language upgrades survive contact with production.
""";

    private const string MinimalVsControllers = """
## This is not a purity contest

Minimal APIs and MVC controllers both run on the same endpoint routing infrastructure. The interesting differences are discovery, OpenAPI, filters, and how a team of more than three people finds the handler.

## When I choose Minimal APIs

- Internal APIs with a handful of endpoints.
- BFF slices that will not grow a resource model.
- Prototypes where the entire contract fits in one file.

```csharp
app.MapPost("/api/v1/newsletter/subscribe", async (SubscribeRequest request, INewsletterService news) =>
{
    await news.SubscribeAsync(request.Email);
    return Results.Accepted();
});
```

That is honest. It stays honest until the tenth endpoint, when you start inventing your own grouping conventions.

## When I keep controllers

- Public product APIs with versioning, filters, and a stable resource model.
- Teams that already think in `ArticlesController`.
- Anything an AI agent or partner integrates with, where OpenAPI metadata, problem details, and action conventions matter.

Controllers give you a place for `[Authorize]`, model binding, and `ActionResult<T>` that new engineers already understand. I still keep them thin: bind, call a service, return.

```csharp
[ApiController]
[Route("api/v1/articles")]
public sealed class ArticlesController(IArticleService articles) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Get(string slug, CancellationToken ct)
        => Ok(await articles.GetPublicBySlugAsync(slug, ct));
}
```

## Hybrid is allowed

This publication's own API uses controllers for the public/admin/agent surface. I would not rewrite it into Minimal APIs for fashion. I would add a Minimal endpoint for a single-purpose webhook without apology.

## Decision rule

If the API is a product, start with controllers. If the API is a function with a URL, Minimal is fine. If you cannot explain the grouping rule to a new teammate in one sentence, you picked the wrong default.
""";

    private const string EfVsDapper = """
## They solve different pain

EF Core optimizes for change tracking, compositions, and the 80% of queries that look like the model. Dapper optimizes for the query you can already write in SQL and do not want an ORM to reinterpret.

I have shipped both in the same service. That is not indecision. That is refusing to make the hot path pay for the admin path.

## Where EF Core wins

- Aggregates you mutate: articles, orders, claims headers.
- Navigations you actually need, with `AsSplitQuery` when the Cartesian blow-up is real.
- Migrations as the source of truth for schema in app-owned databases.

```csharp
var article = await db.Articles
    .AsNoTracking()
    .Where(a => a.Slug == slug && a.Status == ArticleStatus.Published)
    .Select(a => new ArticleListItem(a.Id, a.Title, a.Slug, a.Excerpt))
    .FirstOrDefaultAsync(ct);
```

Projections. Always projections for reads. If you are loading a tracked graph to return JSON, the ORM is not the problem — the query is.

## Where Dapper wins

- Reporting queries that join five tables and return a DTO.
- Stored procedures you do not own.
- The 5% of endpoints where the profiler showed EF generating something you would never write.

```csharp
var rows = await connection.QueryAsync<ClaimRow>(
    "SELECT c.Id, c.Status, c.ReceivedAt FROM Claims c WHERE c.TenantId = @tenantId AND c.ReceivedAt >= @from ORDER BY c.ReceivedAt DESC",
    new { tenantId, from });
```

## The hybrid I recommend

EF Core is the default. Dapper is an escape hatch behind an interface (`IClaimReadStore`) so the rest of the application does not know. Do not sprinkle `QueryAsync` in controllers. Do not start a new product on Dapper "for speed" before you have a baseline.

If you need a one-line comparison: **EF Core for writes and everyday reads; Dapper for queries you can already see in SSMS.**
""";

    private const string FunctionsVsWorkers = """
## The question is "who restarts this process?"

Azure Functions, .NET Worker Services, and the older WebJobs model all run C#. They differ in scaling, cold start, local debugging, and how you host a long-running message pump.

I spent a year on healthcare claims pipelines. The work is bursty, must not double-process a file, and has to be traceable in Application Insights. That is the lens I use.

## Azure Functions

Choose Functions when:

- The work is event-shaped: blob created, Service Bus message, timer.
- You want Azure to scale out to zero between spikes.
- Each invocation is short and idempotent.

```csharp
[Function("ProcessClaimFile")]
public async Task Run(
    [BlobTrigger("claims/{name}")] Stream blob,
    string name,
    FunctionContext context)
{
    await _processor.HandleAsync(name, blob, context.CancellationToken);
}
```

Pay the cold-start tax honestly. Premium plans reduce it; they also reduce the "serverless is cheaper" story. For claims, I prefer predictable plans over a surprise bill from a retry storm.

## Worker Services

Choose a Worker (`BackgroundService` in a container or App Service) when:

- You need a continuous pump, sessions, or a dedicated concurrency cap.
- The process holds sockets, channels, or a heavy SDK that should not JIT on every message.
- You already run the API in containers and want the same image family.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var message in _receiver.ReadAllAsync(stoppingToken))
    {
        await _processor.HandleAsync(message, stoppingToken);
    }
}
```

Workers are boring. Boring is a compliment in operations.

## WebJobs

I only keep WebJobs on existing App Service plans that already host them. I do not start new work there.

## Decision rule

If the trigger is a first-class Azure event and the function is short, use Functions. If you are building a message-driven subsystem that has to run 24/7 with tight control of prefetch and retries, use a Worker. If someone says "just put it in a Function" because the portal wizard exists, ask them who owns poison-message handling at 2am.
""";

    private const string AngularVsBlazor = """
## I ship Angular. I am not anti-Blazor.

Most of the product surfaces I have owned in the last four years — ecommerce admin, trade officer workflows, healthcare operations — are Angular talking to ASP.NET Core APIs. That is a hiring market, a design-system investment, and a set of muscles the team already has.

Blazor is a good product. It is not a free frontend.

## Where Angular still wins for me

- Large admin applications: grids, filters, role-based actions, Figma-driven Angular Material.
- Teams that already include TypeScript specialists, or will hire them.
- Multi-year products where the UI will outlive two backend rewrites.

RxJS and a component library are not decoration. They are how a 30-form government suite stays consistent.

## Where I would pick Blazor

- Internal tools used by the same engineers who write the API.
- Forms that are essentially the domain model with authorization.
- Teams with no frontend hiring pipeline and a hard deadline.

Blazor Server needs a real story for SignalR, sticky sessions or Azure SignalR, and circuit failure. Blazor WebAssembly needs a real story for download size and auth. Neither is "HTML with C# and nothing else."

## The split I actually use

Public and partner UIs: Angular (or occasionally Vue/React when the product already started there). Internal ops tools: I will consider Blazor if the team is 100% .NET and the UX is form-heavy. I will not rewrite a working Angular admin into Blazor to "stay in C#."

## Comparison in one table (in words)

Angular costs you a second language and pays you an ecosystem. Blazor costs you hosting and payload decisions and pays you shared models. Pick the cost you are staffed to pay.

If your designers live in Figma and your users live in a browser all day, Angular is still the default I recommend to a .NET shop that already has it.
""";

    private const string ServiceBusVsEventGrid = """
## Messaging vs notification

Azure Service Bus is a broker. Event Grid is a distribution grid. I have seen teams put claims payloads on Event Grid because "events" sounded modern, then spend a quarter reinventing sessions, duplicate detection, and dead-lettering.

## Service Bus when you cannot lose the work

- Commands and work items: "process this claim file."
- Competing consumers with prefetch and lock duration you control.
- Sessions when order per claim or per tenant matters.
- Duplicate detection when the producer retries.

```csharp
var processor = client.CreateProcessor("claims", new ServiceBusProcessorOptions
{
    MaxConcurrentCalls = 8,
    AutoCompleteMessages = false
});
processor.ProcessMessageAsync += async args =>
{
    await _handler.HandleAsync(args.Message, args.CancellationToken);
    await args.CompleteMessageAsync(args.Message);
};
```

This is the backbone I used for healthcare claims stages. The message *is* the unit of work.

## Event Grid when many subscribers need a fact

- "Claim completed" as a fact that search, email, and reporting may each handle.
- Blob created notifications that should fan out.
- Subscribers you do not want coupled to a queue contract.

Event Grid is excellent at "something happened." It is a poor command bus. Delivery, retries, and payload size limits are different. Put a small event (id, type, timestamp) on the grid; put the blob or the bus message behind it.

## Combining them

A Function receives the blob, writes the durable work to Service Bus, and publishes a lightweight Event Grid event for interested bystanders. That split has saved me more incidents than any diagram with only one Azure hexagon.

## Decision rule

If a failure must retry until success or land in a dead-letter queue a human can inspect, Service Bus. If many independent systems should wake up because a fact occurred, Event Grid. If you need both, you probably *do* need both — not a compromise primitive.
""";

    private const string ActionsVsAzdo = """
## Pipelines should live next to the pull request

I have run .NET CI on both Azure DevOps and GitHub Actions. The better system is the one where reviewers already look.

## Azure DevOps

Wins when:

- Repos, boards, artifacts, and environments are already in Azure DevOps.
- You need approval gates, protected environments, and service connections that your platform team already standardized.
- The organization is not moving source to GitHub.

YAML pipelines, templates, and self-hosted agents are mature. SonarQube quality gates and Azure subscription deployments are a well-trodden path. I have used that path on government and enterprise work and it is fine.

## GitHub Actions

Wins when:

- The code is already on GitHub.
- You want PR checks, Copilot, and Dependabot in the same place as the workflow.
- The pipeline is a few jobs: restore, test, publish, push image.

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - run: dotnet test --configuration Release
      - run: dotnet publish src/Blog.Api/Blog.Api.csproj -c Release -o out
```

Actions is faster to start. It gets messy when you invent a private template ecosystem that Azure DevOps already had.

## What I refuse to compare on

"Which logo is more modern" is not a criterion. Neither is "serverless runners are the future" if your tests need a SQL container and 20 minutes.

## Decision rule

If your source of truth is Azure DevOps, stay. If your source of truth is GitHub, use Actions and deploy to Azure with OIDC. Do not run both for the same repo unless you are in a migration with an end date. Dual CI is how you get green on the pipeline nobody looks at.
""";

    private const string Apim = """
## APIM sits in front. It does not invent the contract.

Azure API Management is how I expose ASP.NET Core APIs to partners, other business units, and sometimes mobile clients that should not see the raw App Service. It is a control plane: keys, rate limits, versioning, and a developer portal. It is not an excuse for an inconsistent payload.

## What I put in APIM

- Subscription keys or OAuth against Azure AD / B2C.
- Rate limits per product.
- A rewrite from `/external/v1/claims` to the internal `/api/v1/claims`.
- Request IDs and correlation headers so Application Insights still stitches.

## What I keep in ASP.NET Core

- Authorization that depends on the domain (this user may not see that tenant).
- Validation and ProblemDetails.
- Idempotency keys.
- The OpenAPI document that APIM imports — generated from the app, not redrawn in the portal.

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

If APIM allows a call that the API should reject, the API still rejects it. Defense in depth is not optional because a gateway exists.

## Versioning

I version the ASP.NET Core route (`/api/v1`) and map APIM products onto those versions. Sunset happens in both places. Partners should not discover a breaking change because someone deleted an APIM operation by hand.

## When I skip APIM

Internal APIs called only by our Angular admin behind Azure AD B2C can wait. Adding APIM on day one of a three-endpoint prototype is ceremony. Adding it the week a partner asks for a key is too late. I put it in when the second consumer appears.

That is the whole product lesson: **gateways amplify a good API and advertise a bad one.**
""";

    private const string MultiTenant = """
## One application, many stores

I have built US ecommerce on a single ASP.NET Core + Angular application that serves multiple storefronts: different branding, catalogues, and checkout, without a codebase per tenant. Healthcare and trade products have the same shape even when they are not shops — the tenant is the hospital, the department, or the exporter.

The failure mode is always the same: a query that forgot `TenantId`.

## Resolve the tenant early

Host header, path, or an authenticated claim. Pick one primary and stick to it.

```csharp
public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext http, ITenantContext tenant)
    {
        var host = http.Request.Host.Host;
        tenant.ResolveFromHost(host);
        await next(http);
    }
}
```

Put `ITenantContext` into DI as scoped. Everything downstream — EF interceptors, blob prefixes, cache keys — reads it. Nothing downstream accepts a tenant id from the client body without matching the resolved one.

## Data isolation

I prefer a shared database with a mandatory `TenantId` on every tenant-owned table and a global query filter in EF Core. Separate databases per tenant are right for isolation-sensitive healthcare when the contract requires it; they are expensive for 50 similar stores.

```csharp
modelBuilder.Entity<Order>().HasQueryFilter(o => o.TenantId == _tenant.Id);
```

Filters are not a substitute for tests that try to cross the boundary. I write one integration test per bounded context that authenticates as tenant A and asserts 404 for tenant B's ids.

## Configuration that must not leak

Feature flags, price lists, payment accounts, and blob containers are tenant-scoped. Redis keys are prefixed. Application Insights custom dimensions include tenant so a noisy neighbour is visible.

The Angular admin I built sits above the stores: one control plane, role-based access, and no "just add a query string" to switch tenant in production.

## The lesson

Multi-tenant is not a library. It is a discipline: resolve once, filter always, test the negative path, and assume a missing `WHERE TenantId = @id` is a security bug, not a performance bug.
""";
}
