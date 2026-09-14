# Bitnock

Bitnock is an API-first developer publishing platform for .NET content. Humans use the editorial site and admin CMS. Autonomous agents use a scoped, versioned, idempotent HTTP API. Site name, description, logo, and social links are configuration — change `Site__Name` without rewriting the app.

This is not a generic CMS theme. It is a publishing system with a domain model for drafts, review, scheduling, and audit.

## Folder structure

```text
src/Blog.Domain            Entities, statuses, publishing invariants
src/Blog.Application       DTOs, validators, ports, AI extension points
src/Blog.Infrastructure    EF Core, Identity, storage, search, jobs
src/Blog.Api               HTTP, auth, agent API, OpenAPI
tests/Blog.UnitTests
tests/Blog.IntegrationTests
frontend                   Next.js App Router public site + admin
```

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- PostgreSQL 16 (or Docker)

## Environment variables

Copy `.env.example`. Never commit secrets.

| Variable | Purpose |
| --- | --- |
| `ConnectionStrings__Database` | PostgreSQL |
| `Jwt__SigningKey` | 32+ character signing key |
| `Site__Name`, `Site__Url` | Brand and canonical origin |
| `Publishing__Mode` | `RequireApproval` or `AutoPublish` |
| `Storage__*` | Local disk now; swap `IStorageService` for S3/R2/Azure |
| `Seed__*` | Development-only admin. Disable in production. |
| `API_URL` / `NEXT_PUBLIC_API_URL` | Frontend → API |

Development admin (local only):

- Email: `admin@localhost`
- Password: `DevOnly!Nexus2026`

Do not use these credentials in production. `Seed__Enabled` must be `false` outside Development.

## Database and migrations

```bash
dotnet ef database update --project src/Blog.Infrastructure --startup-project src/Blog.Api
```

Startup also runs migrations and seeds categories, tags, a default author, ten technical articles, and the development admin when seeding is enabled.

### Schema (high level)

- `articles` unique slug; indexes on status, published_at, created_at, updated_at, content_hash
- `categories`, `tags`, `technologies`, `series`, `authors` unique slugs
- `article_tags`, `article_technologies`, `related_articles`
- `media_assets`, `agent_credentials` (hashed keys), `audit_logs`
- `idempotency_records`, `refresh_tokens`, `newsletter_subscribers`, `analytics_events`
- ASP.NET Identity tables for human users

## Run locally (without Docker)

Start PostgreSQL, then:

```bash
dotnet run --project src/Blog.Api
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

- Public site: http://localhost:3000
- Admin: http://localhost:3000/admin
- API / OpenAPI: http://localhost:5080/swagger
- OpenAPI document: http://localhost:5080/swagger/v1/swagger.json
- Health: http://localhost:5080/health and `/health/ready`

## Docker

```bash
docker compose up --build
```

Postgres, API, and the Next.js frontend start together. The API applies migrations and seeds demo articles on boot. Open http://localhost:3000.

## Public demo (Render)

The repo includes `render.yaml` (API + web + PostgreSQL). After the code is on GitHub:

1. Open [Render Dashboard](https://dashboard.render.com/) and sign in with GitHub.
2. **New → Blueprint** and select the `bitnock` repository, or use:

   [Deploy to Render](https://render.com/deploy?repo=https://github.com/mail2mbabar/bitnock)

3. Wait for the first deploy (seed runs on API boot; free instances can take a few minutes and spin down when idle).

The public site is the `bitnock-web` URL. Admin seed login is local-only by default; for a live demo, set `Seed__DevAdminPassword` in Render to a password you control.

## Production deployment

### Humans (admin)

`POST /api/v1/auth/login` returns a JWT access token and refresh token. Send `Authorization: Bearer <access>`. Roles: Admin, Editor, Author, Agent. Policies include `CanPublishArticle`, `CanManageAgentKeys`, `CanManageUsers`. Agents never receive Admin.

Optional 2FA is modeled on `ApplicationUser.TotpSecret`; enable a TOTP provider before production if you need it.

### Agents (not a password)

Create a credential in `/admin/agents`. The plaintext key is shown **once**. Format: `nxa_...`. Send `X-Api-Key` or `Authorization: Bearer nxa_...`.

Keys are SHA-256 hashed, prefix-indexed, scoped, expirable, rotatable, and revocable. They are never logged.

Default publisher scopes: `articles.read/create/update/publish/schedule/validate`, `media.upload/read`, `categories.read`, `tags.read`, `series.read`, `rules.read`.

Agents cannot create admins, read secrets, change other agent credentials, or reach infrastructure.

## Agent API

Base path: `/api/v1/agent`

| Method | Path |
| --- | --- |
| GET | `/publishing-rules` |
| GET/POST | `/articles` |
| GET/PUT/DELETE | `/articles/{id}` |
| POST | `/articles/{id}/publish` `/unpublish` `/schedule` `/preview` `/validate` |
| GET | `/articles/search` `/articles/{id}/duplicates` `/articles/{id}/related` |
| GET | `/categories` `/tags` `/series` `/media` `/analytics` |
| POST | `/media` |

List endpoints return `{ success, data, page, pageSize, total, requestId }`. Errors:

```json
{
  "success": false,
  "error": { "code": "ARTICLE_NOT_FOUND", "message": "Article was not found." },
  "requestId": "..."
}
```

Publishing operations honor `Idempotency-Key`. Retries with the same key and body return the original result. A reused key with a different body returns `409 IDEMPOTENCY_KEY_REUSE`.

`Publishing__Mode=RequireApproval` turns agent publish into `PendingReview`. Editors approve or reject (with a reason). `AutoPublish` lets a scoped agent publish after validation.

## Example agent workflow

```bash
KEY="nxa_..."
API=http://localhost:5080

curl -H "X-Api-Key: $KEY" $API/api/v1/agent/publishing-rules
curl -H "X-Api-Key: $KEY" $API/api/v1/agent/categories
curl -H "X-Api-Key: $KEY" "$API/api/v1/agent/articles/search?q=aspnet-core"

curl -H "X-Api-Key: $KEY" -H "Idempotency-Key: draft-1" \
  -H "Content-Type: application/json" \
  -d '{"title":"DI lifetimes","excerpt":"...at least seventy characters of summary for SEO...","content":"...long markdown...","categorySlug":"aspnet-core","tags":["ASP.NET Core","DI"],"metaTitle":"DI lifetimes","metaDescription":"Understand ASP.NET Core DI lifetimes before you ship."}' \
  $API/api/v1/agent/articles

curl -H "X-Api-Key: $KEY" -X POST $API/api/v1/agent/articles/{id}/validate
curl -H "X-Api-Key: $KEY" -H "Idempotency-Key: sched-1" \
  -H "Content-Type: application/json" \
  -d '{"scheduledAt":"2026-09-15T08:00:00Z"}' \
  $API/api/v1/agent/articles/{id}/schedule
```

The OpenAPI document at `/swagger/v1/swagger.json` is the machine-readable contract. Future agents should ingest it rather than scraping the admin UI.

## Scheduling

`ScheduledPublishingService` polls PostgreSQL every 30 seconds. Schedules survive process restarts. Publishing is transactional and skips articles that are no longer `Scheduled`.

## Testing

```bash
dotnet test tests/Blog.UnitTests/Blog.UnitTests.csproj
dotnet test tests/Blog.IntegrationTests/Blog.IntegrationTests.csproj
```

Unit tests cover publishing transitions, slug generation, API key hashing, Markdown sanitization, and validators. Integration tests hit HTTP when PostgreSQL is available and no-op otherwise.

```bash
cd frontend && npm run build
```

## Production deployment

- **Azure**: App Service or Container Apps for API and web; Azure Database for PostgreSQL; Blob Storage behind `IStorageService`; Key Vault for `Jwt__SigningKey`.
- **Docker**: `docker compose` as a starting topology. Split frontend, API, database, and object storage in production.
- **CDN**: Put Cloudflare (or similar) in front of the Next.js origin. Cache public GET HTML. Bypass `/admin` and `/api`.
- Set `ASPNETCORE_ENVIRONMENT=Production`, disable seed, rotate JWT and DB passwords, restrict CORS, and terminate TLS at the edge.

## Security considerations

- Markdown is sanitized with HtmlSanitizer before HTML is returned.
- Uploads are type- and size-limited.
- Agent keys are hashed; plaintext is shown once.
- Rate limits differ for public, admin, agent, and auth endpoints.
- Structured logs include request IDs and omit passwords, tokens, and `nxa_` keys.
- Optimistic concurrency uses PostgreSQL `xmin`.
- Preview URLs require an unguessable hashed token.

## Future AI integration

Do not couple the platform to a vendor. Implement:

- `IAiContentService`
- `IAiSeoService`
- `IAiImageService`
- `IAiContentReviewService`

Semantic search can replace `ISearchService` (currently PostgreSQL `ILIKE`) with OpenSearch/Meilisearch/Algolia. Duplicate detection can add embeddings beside exact title/slug/content-hash matching. Social images can be generated later behind `IAiImageService`.

## Explicit future work

- TOTP enrollment UI (data model is present)
- External email provider (development uses `LoggingEmailService`)
- Object storage providers other than local disk
- Comment provider (architecture placeholder on the article page)
- Automatic OG image renderer
- Semantic recommendations

The agent does not need those to run the full draft → validate → schedule/publish loop.
