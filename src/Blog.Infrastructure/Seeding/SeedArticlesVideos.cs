using Blog.Domain.Enums;

namespace Blog.Infrastructure.Seeding;

internal static class SeedArticlesVideos
{
    public static IEnumerable<SeedArticleSpec> Items =>
        AbiHelplineVideos.All.Select(video => SeedArticles.Create(
            video.ArticleTitle,
            video.ArticleSlug,
            video.Subtitle,
            video.Excerpt,
            video.CategorySlug,
            video.Tags,
            video.Technologies,
            video.Difficulty,
            video.ContentType,
            video.Featured,
            video.SeriesPart,
            Materialize(Body(video), video),
            AbiHelplineVideos.SeriesSlug));

    private static string Body(AbiHelplineVideo video) => video.Id switch
    {
        "6Rpto5pUxpw" => DreamJob,
        "0J_T5qRynSI" => CleanArch,
        "aVsG9TydyzE" => Jwt,
        "xUqjEzRbOAw" => Microservices,
        "Ct6WgZx8_B4" => Refactor,
        "ivJYcCelZ8A" => EfCore,
        "NaGwNiUQRzI" => Testing,
        "h4SvC_9_lL0" => Middleware,
        "D1AJJ8PrZr0" => AbstractVsInterface,
        "vACP_yp_qW4" => Solid,
        "834ybOzNhmw" => TopTenCsharp,
        "UtdyV98Grko" => FullStack,
        _ => video.Excerpt
    };

    private static string Watch(string id, string title, string duration) =>
        $"""
        This is the written companion to my ABi Helpline video **{title}** ({duration}). Watch it first, then use this page as the notes you would take in a notebook.

        {AbiHelplineVideos.Embed(id, title)}

        [Watch on YouTube]({AbiHelplineVideos.WatchUrl(id)}) · [All ABi Helpline videos](/videos)

        """;

    private const string DreamJob = """
WATCH_PLACEHOLDER

## Interviews are a product demo of you

I started ABi Helpline to give people the same briefing I wish I had before a .NET interview: not a dump of trivia, but the stories, trade-offs, and code instincts that hiring managers actually remember.

A dream-job interview is rarely won by reciting a definition of SOLID. It is won when you can talk about a system you shipped, the failure that taught you something, and the next decision you would make with today's constraints.

## What I practise the night before

**One production story, three layers deep.** Pick a service you actually own. Be able to explain: the HTTP contract, the data model, and one operational incident. If you cannot talk about logs, retries, or a migration, you are still at the tutorial layer.

**One piece of code you can write from a blank file.** A small ASP.NET Core endpoint with validation, a test, and a SQL query. I keep a console Task Manager and a generic `IRepository<T>` in my interview folder for exactly this — prove you can model state without a framework hugging you.

```csharp
public interface IRepository<T> where T : Entity
{
    T Get(IComparable id);
    void Add(T item);
    IEnumerable<T> FindAll(Func<T, bool> where);
}
```

That is not an architecture religion. It is a whiteboard that fits in ten minutes.

**The stack they wrote on the job spec, in their words.** If they said Azure Service Bus, do not answer with Kafka unless they ask for a comparison. Map your experience onto their nouns.

## Questions I want them to ask me

- Walk me through an API you would not change, and one you would.
- Where did EF Core hurt you, and what did you do instead of rewriting the ORM?
- How do you test a handler that calls a database and a message bus?
- Tell me about a time a retry made things worse.

If they only ask "what is the difference between an abstract class and an interface", they are screening juniors. Answer cleanly, then offer a production example so they can raise the ceiling.

## How I talk about seniority

Senior is not years. Senior is: you can name the cost of a choice. Microservices cost operational complexity. JWT cost revocation. Blazor cost hiring. Say the cost out loud. Interviewers who ship will lean in.

## After the call

Write down every question you missed the same day. That list is the next ABi Helpline video. It is also the next article on this site.

If you want the technical drills that sit under this advice, start with the [top 10 C# questions](/articles/top-10-csharp-interview-questions-and-answers) and the [full-stack mock](/articles/fullstack-dotnet-interview-csharp-frontend-sql).
""";

    private const string CleanArch = """
WATCH_PLACEHOLDER

## What this tutorial is for

The video is a live build: solution setup, folders, a Domain that does not know ASP.NET, an Application layer with use cases, Infrastructure with EF Core, and an API that only translates HTTP. This article is the map I want you to keep after the recording stops.

Bitnock — the site you are reading — is the same shape. `Blog.Domain` has entities and rules. `Blog.Application` has ports. `Blog.Infrastructure` has EF, markdown, and storage. `Blog.Api` hosts. The frontend is a client, not the business.

## The folders I actually create

```text
src/
  Domain/          entities, value rules, domain exceptions
  Application/     commands, queries, interfaces (ports)
  Infrastructure/  EF Core, Identity, file storage, email
  Api/             controllers, auth, middleware, Program.cs
tests/
  UnitTests/
  IntegrationTests/
```

Names can vary. The rule cannot: **inner layers do not reference outer layers.** Domain never takes a `DbContext`. Application never returns `IActionResult`.

## Domain first

Start with the thing the business would recognise if the web vanished:

```csharp
public sealed class Article : Entity
{
    public string Title { get; private set; }
    public ArticleStatus Status { get; private set; }

    public void Publish(DateTimeOffset now)
    {
        if (Status is ArticleStatus.Archived)
        {
            throw new DomainException("ARTICLE_ARCHIVED", "Archived articles cannot be published.");
        }

        Status = ArticleStatus.Published;
        PublishedAt = now;
    }
}
```

Private setters are not ceremony. They stop a controller from writing `article.Status = Published` and skipping the invariant.

## Application: use cases, not "services that do everything"

A create-article handler takes a DTO, talks to `IArticleRepository` and `IClock`, and returns an id. It does not know JWT. It does not know PostgreSQL.

```csharp
public sealed class CreateArticleHandler
{
    public async Task<Guid> Handle(CreateArticleCommand command, CancellationToken ct)
    {
        var article = Article.Create(command.Title, command.Slug, command.Excerpt, command.Content, command.AuthorId, command.CategoryId, _clock.UtcNow);
        await _articles.AddAsync(article, ct);
        return article.Id;
    }
}
```

CRUD is allowed. Clean Architecture is not "never write a repository". It is "the repository is a port".

## Infrastructure: EF Core stays at the edge

`BlogDbContext` implements the port. Fluent mappings live here. Migrations live here. If PostgreSQL is replaced with SQL Server, Domain does not move.

## API: thin

Controllers bind, authorize, call the handler, map errors to ProblemDetails or your envelope. That is the whole job.

```csharp
[HttpPost]
public async Task<ActionResult> Create(CreateArticleRequest request, CancellationToken ct)
{
    var id = await _handler.Handle(new CreateArticleCommand(request.Title, request.Slug, request.Excerpt, request.Content, User.GetAuthorId(), request.CategoryId), ct);
    return Created($"/api/v1/articles/{id}", new { id });
}
```

## CRUD without drowning

For interview projects I implement:

1. Create / get by id / list with paging
2. Update with a row version or `updatedAt`
3. Soft rules: you cannot publish an empty body
4. One integration test that hits `WebApplicationFactory`

You do not need twenty endpoints. You need a loop you can explain: HTTP → handler → domain → EF → HTTP.

## What I skip on purpose

I skip MediatR until there are enough handlers to justify a bus. I skip a generic repository that hides `IQueryable` and then surprises you with N+1. I skip putting DTOs in Domain.

## How I answer "is this overkill?"

For a weekend todo app, yes. For a CMS that will grow agents, media, and publishing workflows — like Bitnock — no. The cost of the extra projects is paid the first time you test a domain rule without spinning Kestrel.

Watch the [hour-long build](/videos/0J_T5qRynSI), then compare the folders here. If they match in spirit, you understood the tutorial.
""";

    private const string Jwt = """
WATCH_PLACEHOLDER

## JWT is a letter, not a session

A JSON Web Token is three Base64url parts: header, payload, signature. ASP.NET Core does not "log you in" with JWT. It **validates a signature** and then **maps claims to `HttpContext.User`**. If you treat it like a server session, you will invent a revocation story you did not plan for.

## The flow I draw on a whiteboard

1. Client posts credentials to `/api/v1/auth/login`.
2. Server checks the password hash, never the raw password.
3. Server issues a short-lived **access token** (I use ~15–20 minutes) and a longer **refresh token** stored hashed.
4. Client sends `Authorization: Bearer <access>`.
5. Middleware validates issuer, audience, lifetime, and signing key.
6. `[Authorize]` and policies decide if this user may hit this endpoint.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
```

## Claims I actually put in the token

- `sub` — user id
- `email`
- roles or a single `role` claim
- a custom `authorId` if the product needs it

I do not put permissions for every feature. Tokens get fat, leak, and go stale. Prefer a policy that reads a lean identity.

## Authorization is not authentication

Authentication answers "who". Authorization answers "may they". In interviews I want this sentence: **401 means we do not know you; 403 means we know you and you still cannot.**

```csharp
[Authorize(Roles = "Admin")]
[HttpPost("articles/{id:guid}/publish")]
public Task Publish(Guid id, CancellationToken ct) => _articles.PublishAsync(id, ct);
```

For Bitnock I also use scoped API keys for agents. Same idea: a credential with an allow-list, not a god token.

## Middleware order — the bug that looks like "JWT is broken"

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

If you authorize before you authenticate, every request is anonymous. If you put CORS in the wrong place, browsers lie to you. Recite the pipeline in order: exception handler, HTTPS, CORS, authn, authz, endpoints.

## Refresh tokens and logout

Access tokens cannot be edited once issued. Logout is: delete the refresh token (and optionally blacklist `jti` in Redis until expiry). Telling an interviewer "we just wait for expiry" is honest only if expiry is short.

## What I do not do

- Store JWT in localStorage for a high-value cookie-less SPA without talking about XSS.
- Use a signing key shorter than 32 bytes for HMAC.
- Put the signing key in source control. Development keys stay in Development config and never in a screenshot.

If you can explain issuer, audience, lifetime, and refresh, you have already beaten most JWT interview answers. Then watch the [seven-minute video](/videos/aVsG9TydyzE) for the demo rhythm.
""";

    private const string Microservices = """
WATCH_PLACEHOLDER

## The only definition I accept

A microservice is a **independently deployable** unit with its **own data** and a **clear reason to fail alone**. If two "services" share a database and a release train, you have a distributed monolith with extra latency.

I have shipped both: a trade platform with CQRS and many services, and ecommerce/healthcare systems that stayed a modular monolith on purpose. Interviews should hear that I can choose.

## Concepts I want on the whiteboard

**Bounded context.** Billing does not own Customer the way CRM does. Duplicate the name, do not duplicate the writes.

**Database per service.** Cross-service joins become APIs or projections. That pain is the point — it stops accidental coupling.

**Synchronous vs asynchronous.** User clicks "place order" might be HTTP. "Send the invoice PDF" is a queue. Mixing them without saying why is how you timeout the homepage.

**API gateway.** One public door: auth, rate limits, routing. Not a second business layer.

**Observability.** Correlation ids, metrics, and traces are part of the architecture. Without them you cannot operate twenty processes.

```csharp
public sealed class PlaceOrderHandler
{
    public async Task Handle(PlaceOrder command, CancellationToken ct)
    {
        var order = Order.Place(command.CustomerId, command.Lines, _clock.UtcNow);
        await _orders.SaveAsync(order, ct);
        await _bus.PublishAsync(new OrderPlaced(order.Id, order.Total), ct);
    }
}
```

The HTTP API commits its own store, then publishes. The invoice service listens. If the bus is down, you have an outbox — that is the next sentence in a senior interview.

## Benefits that are real

- You can scale the hot path without scaling the report job.
- A team can release billing without freezing catalog.
- A fault in notifications should not take down checkout — if you designed the isolation.

## Costs I always name

- Distributed transactions do not exist in the way juniors hope. You get sagas and idempotency.
- Local debugging becomes docker compose.
- Versioning HTTP between services is a product.
- "We will just add a service" is how you get twenty repositories and one on-call rota.

## When I say no

A ten-person team with one product and one database is usually better as a modular monolith: folders that *could* become services later. Azure Service Bus and Functions can wait until a boundary is proven.

On Azure I pick Service Bus when I need competing consumers, sessions, or dead-letter. Event Grid when I need fan-out of facts. That comparison is its own article on this site; in an interview I only need the heuristic.

## How I answer "have you done microservices?"

I describe a concrete boundary (forms vs workflow vs search), the message contract, and a failure. If you cannot name a failure, you have not operated them.

Watch the [concepts video](/videos/xUqjEzRbOAw), then be ready to argue *against* microservices. That is the senior move.
""";

    private const string Refactor = """
WATCH_PLACEHOLDER

## Refactoring is a test of taste

Interviewers who put a messy controller on the screen are not asking you to rewrite the product. They are asking: can you make the next change safer without boiling the ocean?

These are the ten moves I actually use on ASP.NET Core code.

## 1. Extract method until the name is the comment

If you need a comment, the block wanted a name. `ValidateSlug`, `MapToDto`, `EnsureNotArchived`.

## 2. Invert ifs, return early

```csharp
public void Publish(DateTimeOffset now)
{
    if (Status is ArticleStatus.Archived)
    {
        throw new DomainException("ARTICLE_ARCHIVED", "Archived articles cannot be published.");
    }

    Status = ArticleStatus.Published;
    PublishedAt = now;
}
```

Happy path stays left-aligned. Nested `else` is where bugs hide.

## 3. Replace magic numbers and strings

`"PendingReview"` as a string becomes `ArticleStatus.PendingReview`. `220` words per minute becomes a named constant. Interviewers notice.

## 4. Introduce a parameter object

When a method takes eight arguments, two of them are a concept. `Paging`, `DateRange`, `Money`.

## 5. Replace switch-on-type with polymorphism or a dictionary of handlers

Not everywhere. When you add a seventh `if (type == ...)` you have a visitor or a handler map.

## 6. Split fat controllers

Controllers bind and call. Domain does not live in `HomeController`. If you cannot test it without HTTP, extract.

## 7. Replace inheritance with composition when the hierarchy is a lie

`BaseService` that everything extends is usually a bag of helpers. A sealed class plus injected dependencies is easier to test.

## 8. Encapsulate collections

Do not return `List<OrderLine>` for callers to mutate. Return `IReadOnlyList<OrderLine>` and an `AddLine` method with rules.

## 9. Delete dead abstractions

A one-implementation interface that never got a fake is noise. xUnit can construct the real class. Keep ports where you have two implementations or a test double that earned it.

## 10. Make the change that enables the next change

Renaming `UserManagerEx` to `UserDirectory` is a refactor. Adding Kafka "while we are here" is a rewrite. Say which one you are doing.

## A before/after I like in interviews

Before: a 200-line action that queries EF, maps, sends email, and writes a log.

After: `CreateOrderHandler` with three dependencies, a domain `Order.Place`, and a test that never opens a port.

That is enough to show you can touch production code.

Practise on a small console app — add, list, complete, remove — until the commands are boring. Then apply the same extraction to a controller. The [video](/videos/Ct6WgZx8_B4) walks the rhythm; this page is the checklist.
""";

    private const string EfCore = """
WATCH_PLACEHOLDER

## What EF Core is

Entity Framework Core is an object-relational mapper: you write C#, it writes SQL (usually). In interviews I say that in one breath, then I say **you are still responsible for the SQL**.

## The pieces

- **DbContext** — the unit of work and the gateway to sets.
- **DbSet<T>** — a table (or query) of entities.
- **Change tracker** — remembers what you loaded and mutated.
- **Migrations** — version the schema with the code.

```csharp
public sealed class BlogDbContext : DbContext
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlogDbContext).Assembly);
    }
}
```

## Tracking vs no-tracking

Default queries track. That is correct for an update. It is waste for a list page.

```csharp
var list = await db.Articles
    .AsNoTracking()
    .Where(a => a.Status == ArticleStatus.Published)
    .Select(a => new ArticleListItemDto(a.Id, a.Title, a.Slug, a.Excerpt))
    .ToListAsync(ct);
```

**Project in the database.** `Select` into a DTO. `Include` of entire graphs is how list endpoints die.

## LINQ is not always LINQ-to-Objects

`IQueryable` composes a tree that becomes SQL. `IEnumerable` runs in memory. Calling `ToList()` too early is the classic bug. That is the same story as my [C# top 10](/articles/top-10-csharp-interview-questions-and-answers) `IEnumerable` vs `IQueryable` answer.

## Migrations I trust

- One migration per intentional schema change.
- Never edit a migration that already ran in production. Add a new one.
- Backup before applying to shared environments.

## Relationships

I prefer explicit configuration over a pile of conventions you cannot see:

```csharp
builder.HasOne(a => a.Author)
    .WithMany()
    .HasForeignKey(a => a.AuthorId)
    .OnDelete(DeleteBehavior.Restrict);
```

Restrict beats cascade until you can prove cascade is safe.

## Performance interview pack

- N+1: a query in a loop. Fix with a join, a projection, or a compiled query — not with more `Include` by panic.
- Missing index on the filter you use every time (`Status`, `Slug`).
- Tracking on a 10k row export.

## When I do not use EF

Bulk updates, reporting SQL the DBA already wrote, and hot paths where I measured. Dapper or a raw `SqlQuery` is not a failure of Clean Architecture. It is honesty.

The [15-minute video](/videos/ivJYcCelZ8A) is the tour. This page is what I want you to be able to teach back.
""";

    private const string Testing = """
WATCH_PLACEHOLDER

## The point of a unit test

A unit test pins a **behaviour** so a refactor cannot silently break it. It is not a second compiler. If the test is a copy of the implementation line-by-line, delete it.

## xUnit, NUnit, MSTest — what I pick

| Runner | Where I see it | Notes |
| --- | --- | --- |
| **xUnit** | Most new ASP.NET Core | Constructor injection per test class, `[Fact]` / `[Theory]`, default in `dotnet new` |
| **NUnit** | Older suites, `[TestFixture]` | Fine. `[SetUp]` is familiar to people coming from other languages |
| **MSTest** | Teams already in Visual Studio Test Explorer culture | Also fine. Do not rewrite a green suite to switch logos |

I default to **xUnit** on greenfield. I do not migrate a thousand MSTest methods for a blog post.

```csharp
public sealed class ArticleTests
{
    [Fact]
    public void Publish_sets_status_and_timestamp()
    {
        var article = Article.Create("Title", "title", "excerpt", new string('a', 400), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var now = DateTimeOffset.Parse("2026-09-14T00:00:00Z");
        article.Publish(now);
        Assert.Equal(ArticleStatus.Published, article.Status);
        Assert.Equal(now, article.PublishedAt);
    }
}
```

## What belongs in a unit test vs an API test

- **Unit:** domain rules, mapping, a pure handler with fakes.
- **API / integration:** routing, auth, EF against a test database, `WebApplicationFactory`.

If you mock `DbSet`, you are testing your mock. Prefer a real database for repository tests, or test the domain without EF.

## Interview kata: a generic repository

In my interview practice folder I keep a tiny in-memory repository. It is enough to talk about `Add`, `Get`, and `FindAll` without dragging in EF.

```csharp
public interface IRepository<T> where T : Entity
{
    T Get(IComparable id);
    void Add(T item);
    IEnumerable<T> FindAll(Func<T, bool> where);
}
```

The test then says: adding a person and finding by a predicate returns them. That is a unit test. Wiring EF is an integration test.

## Arrange, Act, Assert

Say it. Do it. One behaviour per test. `[Theory]` for input tables (empty slug, too-short body, archived publish).

## What I do not test

Getters. Frameworks. Third-party JWT libraries. I test *my* mapping of claims to a policy.

Watch the [essentials video](/videos/NaGwNiUQRzI) for the runner tour, then write five tests against a domain entity today. That beats a tutorial binge.
""";

    private const string Middleware = """
WATCH_PLACEHOLDER

## The pipeline is a chain

Every ASP.NET Core request walks a list of delegates. Each can work, call `next`, or stop. That is middleware. Filters, MVC, and Minimal APIs sit on top of that chain — they are not a different universe.

```csharp
app.Use(async (context, next) =>
{
    var started = Stopwatch.GetTimestamp();
    await next();
    var elapsed = Stopwatch.GetElapsedTime(started);
    context.Response.Headers["X-Elapsed-Ms"] = ((int)elapsed.TotalMilliseconds).ToString();
});
```

## Use, Run, Map

- **`Use`** — do work, then usually call `next`. Classic middleware.
- **`Run`** — terminal. Does not call `next`. Use for a health endpoint or a short-circuit.
- **`Map` / `MapWhen`** — branch the pipeline (for example `/healthz` without auth).

```csharp
app.Map("/healthz", branch =>
{
    branch.Run(async context =>
    {
        await context.Response.WriteAsync("ok");
    });
});
```

## Order is a production bug

A useful recitation:

1. Exception handler
2. HTTPS redirection
3. Static files (if any)
4. Routing
5. CORS
6. Authentication
7. Authorization
8. Endpoints

Swap authn and authz and JWT "does not work". Put CORS after endpoints and the browser fails with a lie. I have seen both in interviews and in production.

## Custom middleware as a class

When the lambda grows, promote it:

```csharp
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers["X-Request-Id"].FirstOrDefault()
                 ?? Guid.NewGuid().ToString("N");
        context.Items["RequestId"] = id;
        context.Response.Headers["X-Request-Id"] = id;
        await next(context);
    }
}
```

Register with `app.UseMiddleware<CorrelationIdMiddleware>()`. Inject scoped services via `InvokeAsync` parameters, not the constructor — constructors are singleton with the app.

## Short-circuit on purpose

Idempotency middleware can return a cached response and skip the controller. Rate limiting can return 429. That is `return` without `next`. Say it explicitly so the next reader knows it is not a missing `await next()`.

## Interview question I like

"Where would you put tenant resolution?" Early enough that logging and EF interceptors can see the tenant, late enough that you have routing. There is no single holy line — there is a reason.

The [video](/videos/h4SvC_9_lL0) is eight minutes. Draw the chain once on paper. You will not forget it.
""";

    private const string AbstractVsInterface = """
WATCH_PLACEHOLDER

## The one-sentence rule

**An interface is a capability. An abstract class is a kind.** You implement `IDisposable` because you can be disposed. You extend `Stream` because you *are* a stream with shared behaviour.

## Interface

- No object identity of its own (you cannot `new IRepository()`).
- A type can implement many.
- From C# 8, default method bodies exist — use them sparingly, they are not a replacement for a base class.
- Perfect for ports: `IStorageService`, `IClock`, `IArticleRepository`.

```csharp
public interface IClock
{
    DateTimeOffset GetUtcNow();
}
```

## Abstract class

- Can hold fields, constructors, protected helpers, and some implemented methods.
- A type can extend only one class (plus interfaces).
- Use when subclasses share real state or a template algorithm.

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}

public abstract class Animal
{
    public abstract string Speak();
    protected void Breathe() { }
}
```

## Side-by-side

| | Interface | Abstract class |
| --- | --- | --- |
| Multiple | Yes | No (single inheritance) |
| Fields | No (until rare edge cases) | Yes |
| Constructor | No | Yes |
| Access modifiers on members | Public (mostly) | Full control |
| IS-A vs CAN-DO | CAN-DO | IS-A |

## The interview trap: "which is better?"

Neither. I wire application ports as interfaces so Infrastructure can provide EF or a fake. I use an abstract `Entity` when every aggregate shares `Id` and equality. If you only ever have one implementation and no test double, an interface is optional.

## Default interface methods

They let you add a member without breaking implementers. They also hide logic where you cannot store state cleanly. If you need state, you wanted an abstract class (or a dedicated helper).

## Production example from this site

`IStorageService` is an interface: local disk today, blob tomorrow. `Article` is a concrete entity with rules, not an interface soup. That split is the whole lesson.

Watch the [11-minute comparison](/videos/D1AJJ8PrZr0), then explain it back with *your* codebase's types, not `IFlyable` and `Bird`.
""";

    private const string Solid = """
WATCH_PLACEHOLDER

## SOLID is a code review language

If you cannot point at a class, you do not know SOLID yet. Here is how I point at ASP.NET Core.

## S — Single Responsibility

A class should have one reason to change. `ArticlesController` should not also hash passwords and render emails. `DatabaseSeeder` seeds. `MarkdownService` turns markdown into safe HTML.

Violation: a 1,200-line `Utils` class.

## O — Open/Closed

Open for extension, closed for modification. New payment provider? Add a class that implements `IPaymentGateway`. Do not add another `if (provider == "stripe")` in a switch you promised was finished.

```csharp
public interface IPaymentGateway
{
    Task ChargeAsync(Money amount, PaymentMethod method, CancellationToken ct);
}
```

## L — Liskov Substitution

Subtypes must honour the parent contract. If `ReadOnlyRepository.Add` throws `NotSupportedException`, it is not a repository — it is a different port (`IReadRepository`).

## I — Interface Segregation

Do not force a client to depend on methods it does not use. A webhook handler should not take `IArticleAdminService` with 40 members. Give it `IPublishArticle`.

## D — Dependency Inversion

High-level policy depends on abstractions. Domain does not depend on EF. Application depends on `IArticleRepository`; Infrastructure implements it.

```csharp
public sealed class PublishArticleHandler(IArticleRepository articles, IClock clock)
{
    public async Task Handle(Guid id, CancellationToken ct)
    {
        var article = await articles.GetAsync(id, ct)
            ?? throw new DomainException("ARTICLE_NOT_FOUND", "Article was not found.");
        article.Publish(clock.GetUtcNow());
        await articles.SaveAsync(article, ct);
    }
}
```

The handler does not new up `BlogDbContext`. That is D in one gist.

## How I answer in interviews

I pick **one** letter and walk a file. "This controller used to send email; we inverted it behind `IEmailSender` — that's D and S." Five letters recited in 20 seconds scores lower than one letter with a diff.

## What SOLID is not

It is not "more interfaces". It is not "never use a concrete class". It is not Clean Architecture by itself — CA is a packaging of D plus folder policy.

The [ASP.NET SOLID video](/videos/vACP_yp_qW4) is the spoken version. Use this page as the crib sheet while you watch.
""";

    private const string TopTenCsharp = """
WATCH_PLACEHOLDER

These are the ten questions from the video, written so you can answer them out loud with code.

## 1. Key features of C#

I list the ones that change how I write production code: **OOP**, **strong typing**, **GC**, **LINQ**, **async/await**, **properties and indexers**, **exception handling**, and a large BCL. Platform: .NET (including modern .NET on Linux), not "only Windows" anymore.

## 2. `ref` vs `out`

Both pass by reference. **`ref` is bidirectional** — the variable must be assigned before the call, and the method may read and write it. **`out` is write-from-the-method** — the caller need not assign first; the method must assign before returning.

```csharp
void AddOne(ref int n) => n++;
void TryParseId(string text, out int id) => id = int.Parse(text);
```

`TryParse` is the cultural example of `out`. In modern C# I often return a tuple instead of a forest of `out` parameters.

## 3. `IEnumerable<T>` vs `IQueryable<T>`

`IEnumerable<T>` is in-memory iteration. LINQ methods run in the process. `IQueryable<T>` is an expression tree. EF Core turns it into SQL.

If you call `.ToList()` and then `.Where(...)`, you filtered in memory. If you keep `IQueryable` until the database, you filtered in SQL. That is the interview. That is also how list endpoints stay alive.

## 4. `==` vs `Equals`

`==` can be a reference comparison or an overloaded operator. `Equals` is virtual and types can override it for value equality. For strings, `Equals` compares content; `==` is overloaded for content too — which is why people get confused. For your own classes, **override `Equals` and `GetHashCode` together**, or use records.

## 5. Delegates and events

A **delegate** is a type-safe method pointer (`Action`, `Func`, custom). An **event** wraps a delegate with a publisher/subscriber protocol: outsiders can `+=` / `-=` but cannot assign or invoke freely. Events are how you stop a subscriber from clearing everyone else.

Multicast: one delegate instance can point at many methods.

## 6. Boxing and unboxing

**Boxing** copies a value type onto the heap as `object`. **Unboxing** copies it back with a cast. It allocates. `ArrayList` of `int` boxed; `List<int>` does not. That is why generics mattered.

```csharp
int n = 42;
object boxed = n;
int copy = (int)boxed;
```

## 7. `using`

Two jobs people mix up: **`using` directives** import namespaces. **`using` statements** (and `using` declarations) call `Dispose` even when an exception flies — files, `HttpClient` *factory-created* handlers, EF contexts.

```csharp
using var stream = File.OpenRead(path);
```

## 8. Exceptions: try / catch / finally

`try` the dangerous work. `catch` the exceptions you can recover from, **specific first**. `finally` always runs (cleanup). Do not catch `Exception` and swallow. Custom exceptions carry a stable `code` for APIs.

## 9. Access modifiers

- **public** — any assembly
- **private** — this type
- **protected** — this type and subclasses
- **internal** — this assembly
- **protected internal** — subclass *or* same assembly
- **private protected** — subclass *and* same assembly

I design entities with private setters and public methods. That is an access-modifier decision.

## 10. `struct` vs `class`

**struct** is a value type (usually stack or inline), copied by value, cannot inherit other structs/classes (except interfaces), default non-null. Use for small immutable data (`DateTime`, `Guid`, a `Point`). **class** is a reference type on the heap, identity, inheritance. Mutating a struct you accidentally copied is a classic bug — prefer `readonly record struct` when you need a value.

---

Learn these until you can teach them. Then watch the [25-minute session](/videos/834ybOzNhmw) again and pause after each question to answer before I do.
""";

    private const string FullStack = """
WATCH_PLACEHOLDER

## Four piles, one interview

A "full stack .NET" screen usually rotates through **C#**, **ASP.NET / .NET**, **SQL**, and **enough frontend** to not freeze. This is the crib sheet I want next to the [mock video](/videos/UtdyV98Grko).

## C#

Be ready for: types vs references, async/await (what `ConfigureAwait` meant on old ASP.NET, what it means on modern ASP.NET Core), LINQ deferred execution, and the [top 10](/articles/top-10-csharp-interview-questions-and-answers). If they ask you to write a small app, a command loop is enough:

```text
add <title>
list
done <id>
remove <id>
```

That is a Task Manager. It shows parsing, a service, and errors without a UI framework.

## ASP.NET Core / .NET

- Kestrel hosts. Middleware is the pipeline. Controllers or Minimal APIs bind HTTP.
- DI lifetimes: transient, scoped, singleton — and the captive dependency bug (singleton capturing scoped `DbContext`).
- Configuration: `appsettings`, environment, secrets. Never commit production connection strings.
- Auth: cookie vs JWT. Know which the job uses.

## SQL

I expect:

```sql
SELECT a.Title, COUNT(c.Id) AS Comments
FROM Articles a
LEFT JOIN Comments c ON c.ArticleId = a.Id
WHERE a.Status = 'Published'
GROUP BY a.Title
HAVING COUNT(c.Id) > 3
ORDER BY Comments DESC;
```

Know clustered vs nonclustered at a high level, why `SELECT *` hurts, parameterization vs concatenation (injection), and that EF still emits SQL you can read in logs.

Normalization: 3NF as a default; denormalize when a profiler says so, not as a personality.

## Frontend (enough)

They will ask: how does the browser call your API (CORS, JSON, `Authorization` header)? What is a component? How do you keep a token? For Angular shops: services, interceptors, route guards. For React: hooks and that you would not store secrets in the bundle.

I ship Angular on most product UIs and say so. Honesty about Blazor vs Angular is an article on this site; in a junior full-stack screen, "I have consumed REST from a SPA" is the bar.

## How I structure a 25-minute mock

1. Warm-up C# (5 min)
2. A SQL query on a board (5)
3. "Design a login API" (8) — JWT or cookies, hash, 401 vs 403
4. Frontend: "after login, how does the list page get data?" (5)
5. Questions from them (2)

If you stall, narrate. Silence is harder to hire than a wrong first answer you correct.

## After you watch

Do not binge ten more videos. Write the Task Manager. Write the SQL. Sketch the login sequence. Then subscribe on [ABi Helpline](https://www.youtube.com/@ABiHelpline) and come back to Bitnock for the long notes.
""";

    public static string Materialize(string template, AbiHelplineVideo video) =>
        template.Replace("WATCH_PLACEHOLDER", Watch(video.Id, video.Title, video.Duration), StringComparison.Ordinal);
}
