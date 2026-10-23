# Kandy Day Trip Planner — Web API

ASP.NET Core Web API for the [Kandy Day Trip Planner](../kandy-tourism) Angular site. It serves
the attraction catalogue, stores guest itineraries (no tourist account needed) and protects the
admin attraction management with JWT bearer authentication.

| | |
|---|---|
| Runtime | .NET 10 (SDK 10.0.401 tested), ASP.NET Core 10, EF Core 10.0.13 |
| Database | SQL Server — LocalDB for development (SQL Server 2025 LocalDB tested) |
| API docs | OpenAPI at `/openapi/v1.json`, Swagger UI at `/swagger` (Development only) |
## Project layout

```
kandy-tourism-backend.slnx
kandy-tourism-backend/              API project (namespace KandyTourism.Api)
├── Controllers/                    HTTP only: routing, status codes, DTOs in/out
├── Application/
│   ├── DTOs/                       Request/response contracts (+ validation attributes)
│   ├── Interfaces/                 Service and repository abstractions, IUnitOfWork
│   ├── Services/                   Business rules (attractions, itineraries, contact messages, auth, dashboard)
│   ├── Mapping/                    Entity → DTO mapping
│   └── Common/                     Search normalisation, guest tokens, geo distance
├── Domain/                         Entities, enums, application exceptions
├── Infrastructure/
│   ├── Persistence/                DbContext, entity configurations, unit of work
│   ├── Repositories/               EF Core data access
│   ├── Migrations/                 EF Core migrations
│   ├── Seed/                       SeedData (single source), runtime seeder, SQL generator, admin bootstrap
│   ├── SqlScripts/                 SeedKandyAttractions.sql (generated)
│   └── Security/                   JWT token creation
├── Middleware/                     Exception → problem details
├── Extensions/                     Service registration, startup database initialisation
└── Configuration/                  Typed options
```

## Database schema

| Table | Purpose / key rules |
|---|---|
| `AttractionCategories` | 6 categories; unique `Slug`. |
| `Attractions` | Catalogue. `Slug` unique among **active** rows (filtered index). Description, visitor tips and tags are JSON arrays. `SearchText` holds the normalised name/area/category/tags for keyword search. Deleting sets `IsActive = 0`. FK to category is `RESTRICT`. |
| `AttractionImages` | Gallery photos (URL, alt text, credit, sort order); cascade with the attraction. Images are hotlinked from Wikimedia Commons. |
| `StartPresets` | Predefined starting points (one `IsDefault`). |
| `Itineraries` | Guest plans: public `Guid`, SHA-256 hash of the access token, start (preset / attraction / typed text), optional `TripDate` and optional `ShareId` (unique while set) for a read-only share link. |
| `ItineraryStops` | Ordered stops. Unique `(ItineraryId, AttractionId)` and `(ItineraryId, Sequence)`, `Sequence > 0`. FK to attraction is `RESTRICT`. |
| `AdminUsers` | Admin accounts with ASP.NET Core Identity password hashes; unique normalised email. |
| `ContactSenders` | One row per contact-form email address: unique normalised (upper-case) email, latest name, first/last contact time. |
| `ContactMessages` | Contact-form messages (subject, body, received time, `ReadAtUtc` when an admin has read it). Cascade with the sender. |

A deleted (deactivated) attraction disappears from listings and from guest plans, but its row and
any `ItineraryStops` that reference it are kept, so no plan data is destroyed.

## Setup

Prerequisites: .NET 10 SDK, SQL Server LocalDB (installed with Visual Studio or SQL Server
Express) and the EF Core CLI (`dotnet tool install --global dotnet-ef`).

### 1. Connection string

`appsettings.json` points at LocalDB with Windows authentication (no password):

```
Server=(localdb)\MSSQLLocalDB;Database=KandyTourism;Trusted_Connection=True;TrustServerCertificate=True
```

To use another SQL Server, override `ConnectionStrings:KandyTourism` with user-secrets or the
`ConnectionStrings__KandyTourism` environment variable. Don't commit connection strings that
contain passwords.

### 2. Secrets

The JWT signing key (`Jwt:SigningKey`) is set in `appsettings.json`, so no setup is needed for it
locally. The API refuses to start if it is missing or shorter than 32 characters.

Optional: create the first admin on the next start (only if no admin with that email exists).
Run these in `kandy-tourism-backend/` (values are stored outside the repository):

```powershell
dotnet user-secrets set "BootstrapAdmin:Email" "you@example.org"
dotnet user-secrets set "BootstrapAdmin:DisplayName" "Your Name"
dotnet user-secrets set "BootstrapAdmin:Password" "<at least 12 characters>"
```

The bootstrap admin is created once; the
password is hashed and never logged. After the first start you can remove the
`BootstrapAdmin:Password` secret (`dotnet user-secrets remove "BootstrapAdmin:Password"`). No admin
credentials exist in source code or seed data. Once signed in, an admin can add further admins
with `POST /api/v1/admin/users`; there is no public sign-up.

### 3. Database

In Development, startup applies pending migrations and inserts missing seed data automatically
(`Database:ApplyMigrationsOnStartup` and `Database:SeedOnStartup` in `appsettings.Development.json`).
To do it by hand instead:

```powershell
dotnet ef database update          # create/upgrade the schema
```

### 4. Run

```powershell
cd kandy-tourism-backend
dotnet run --launch-profile http   # http://localhost:5201  (Swagger: http://localhost:5201/swagger)
```

The Angular app expects the API at `http://localhost:5201/api/v1` (see its
`src/environments/environment.ts`).

## Seed data

`Infrastructure/Seed/SeedData.cs` is the single source of the content extracted from the Angular
prototype: **6 categories, 2 start points and 15 attractions with 10 gallery images**. Opening
hours, fees and visit durations are unverified demo estimates; four coordinates are flagged as
approximate.

There are two equivalent ways to load it:

- **Runtime seeder (recommended for development).** Runs on startup when
  `Database:SeedOnStartup` is true. Inserts any record whose slug is missing; never updates,
  deletes or re-activates existing rows, so admin edits are safe.
- **Standalone SQL script:** `kandy-tourism-backend/Infrastructure/SqlScripts/SeedKandyAttractions.sql`.
  Apply the migrations first, then:

  ```powershell
  sqlcmd -S "(localdb)\MSSQLLocalDB" -d KandyTourism -f 65001 -b -i Infrastructure\SqlScripts\SeedKandyAttractions.sql
  ```

  `-f 65001` makes sqlcmd read the file as UTF-8. The script is idempotent (slug-guarded), runs in
  one transaction and resolves foreign keys by slug, so it is safe to run repeatedly.

The SQL file is generated from `SeedData.cs`. After changing seed data, regenerate it with
`dotnet run -- generate-seed-sql` (from `kandy-tourism-backend/`) so the two stay identical.

## API (v1)

All routes are under `/api/v1`. Errors use RFC 7807 problem details with a machine-readable
`code` and, for validation, an `errors` object keyed by camelCase field name.

### Public

| Method & route | Purpose | Notes |
|---|---|---|
| `GET /attractions` | List / search | Query: `q` (≤100 chars; all words must match name, area, category or tags), `category` (slug), `duration` (`any\|short\|medium\|long`), `featured`, `sort` (`recommended\|name\|duration-asc\|duration-desc`), `page` (≥1), `pageSize` (1–100, default 24). Returns `{ items, page, pageSize, totalCount, totalPages, hasNextPage }`. 400 for invalid values. |
| `GET /attractions/{id}` | Details by ID | 404 if missing or deleted. |
| `GET /attractions/by-slug/{slug}` | Details by slug | Used by `/attractions/:slug`. |
| `GET /attractions/{id}/nearby?limit=3` | Closest attractions | Straight-line km, `limit` 1–12. |
| `GET /categories` | Categories | `[{ slug, name, description }]` |
| `GET /start-points` | Start presets | `[{ slug, label, description, latitude, longitude, coordinatesApproximate, isDefault }]` |
| `POST /contact-messages` | Contact form | `{ name, email, subject, message }` (name 2–80, subject 3–120, message 10–2000 chars) → 201 `{ id, receivedAtUtc }`. Messages from the same email (any letter case) are grouped under one contact. 400 for invalid fields, 429 after 5 messages per IP in 10 minutes (`RateLimiting:ContactPermitLimit` / `ContactWindowMinutes`). |

### Guest itineraries

Creation returns an `accessToken` once. Every other call must send it in the
`X-Itinerary-Token` header (never in the URL, so it doesn't end up in logs). Missing/malformed
token → 401; unknown plan or wrong token → 404. Each mutation returns the full updated
`ItineraryDto`: `{ id, start: { type, presetSlug?, attractionId?, query?, label, latitude?, longitude?, coordinatesApproximate }, stops: [{ order, attraction }], maxStops, tripDate?, shareId?, updatedAtUtc }`.

| Method & route | Body | Rules / errors |
|---|---|---|
| `POST /itineraries` | – | 201 `{ accessToken, itinerary }`, default start point. |
| `GET /itineraries/{id}` | – | |
| `PUT /itineraries/{id}/start` | `{ type: preset\|attraction\|custom, presetSlug?, attractionId?, query? }` | 400 `invalid-start`. A device location is never sent. |
| `POST /itineraries/{id}/stops` | `{ attractionId, position? }` | 409 `duplicate-stop`, 409 `itinerary-full` (10 stops), 400 `attraction-unavailable`. `position` (0-based) supports undo. |
| `POST /itineraries/{id}/stops/bulk` | `{ attractionSlugs: [] }` | Appends in order, skipping duplicates/unknown slugs (sample day). |
| `PUT /itineraries/{id}/stops/order` | `{ attractionIds: [] }` | Must list every current stop exactly once, else 400 `invalid-order`. Transactional. |
| `DELETE /itineraries/{id}/stops/{attractionId}` | – | Renumbers the remaining stops. |
| `DELETE /itineraries/{id}/stops` | – | Clears all stops; keeps the start. |
| `PUT /itineraries/{id}/trip-date` | `{ tripDate: "yyyy-MM-dd" \| null }` | Today (Sri Lanka time) up to a year ahead, else 400 `invalid-trip-date`. `null` clears it. |
| `POST /itineraries/{id}/share` | – | Turns on a read-only share link; the plan's `shareId` is returned. Idempotent. |
| `DELETE /itineraries/{id}/share` | – | Turns the share link off; the old link returns 404. |
| `GET /shared-itineraries/{shareId}` | – | **No token.** Read-only `{ shareId, start, stops, tripDate?, updatedAtUtc }`; 404 `shared-itinerary-not-found` when sharing is off. |

Road routing is not done by the API: it returns ordered coordinates and the frontend builds the
Google Maps directions link.

### Admin (bearer token, role `Admin`)

| Method & route | Purpose | Status codes |
|---|---|---|
| `POST /auth/login` | `{ email, password }` → `{ accessToken, tokenType, expiresAtUtc, admin }` | 200, 400, 401 (generic message), 429 (5 attempts/min/IP) |
| `GET /auth/me` | Current admin | 200, 401 |
| `POST /admin/users` | Create another admin: `{ email, displayName, password }` (password ≥ 12 chars) → `{ id, email, displayName, role }` | 201, 400, 401, 403, 409 `admin-email-taken` |
| `GET /admin/dashboard` | Totals, per-category counts, guest plan counts, contact/message/unread counts and the 5 latest messages | 200, 401, 403 |
| `GET /admin/contacts` | Contacts (one per email) with `messageCount`, `unreadCount`, `lastSubject`, newest first. Query: `q` (name or email), `status` (`all\|unread`), `page`, `pageSize` (1–100, default 20) | 200, 400 |
| `GET /admin/contacts/{id}` | Contact with all messages, newest first | 200, 404 `contact-not-found` |
| `PUT /admin/contacts/{id}/read` | `{ isRead }` for every message from the contact → updated contact | 200, 400, 404 |
| `PUT /admin/contacts/{id}/messages/{messageId}/read` | `{ isRead }` for one message → updated contact | 200, 400, 404 |
| `DELETE /admin/contacts/{id}` | Permanently delete the contact and their messages | 204, 404 |
| `GET /admin/attractions/{id}` | Record for the edit form | 200, 404 |
| `POST /admin/attractions` | Create (`AttractionUpsertRequest`) | 201, 400, 409 `slug-taken` |
| `PUT /admin/attractions/{id}` | Update | 200, 400, 404, 409 |
| `DELETE /admin/attractions/{id}` | Delete (deactivate) | 204, 404 |

Tokens expire after `Jwt:AccessTokenMinutes` (60). Each authenticated request re-checks that the
admin account is still active.

## Deployment notes

- Generate a reviewed, idempotent schema script with `dotnet ef migrations script --idempotent -o migrate.sql`
  and apply it before deploying; keep `Database:ApplyMigrationsOnStartup` false in production.
  Then run the SQL seed script once (or enable `Database:SeedOnStartup`).
- Provide `ConnectionStrings__KandyTourism`, `Jwt__SigningKey` and `Cors__AllowedOrigins__0`
  (the site's origin) as environment variables or a secret store. Replace the committed
  `Jwt:SigningKey` with a new random value, since anyone with the repository can read it.
- Outside Development the API uses HTTPS redirection and HSTS, and Swagger is disabled.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Startup error about `Jwt:SigningKey` | Check `Jwt:SigningKey` in `appsettings.json` is present and at least 32 characters. |
| `Cannot open database` / LocalDB errors | `sqllocaldb start MSSQLLocalDB`; check the connection string. |
| Browser shows "couldn’t reach the server" | Is the API running on port 5201? Is the site's origin in `Cors:AllowedOrigins`? |
| `Msg 1934 … QUOTED_IDENTIFIER` when running SQL by hand | Use the provided script (it sets the option) or `SET QUOTED_IDENTIFIER ON` first. |
| Garbled ’ or “ characters after running the SQL script | Run sqlcmd with `-f 65001`. |

## Known limitations

- Guest itineraries are never expired or cleaned up automatically.
- Deleted attractions can't be restored from the admin UI (set `IsActive = 1` in the database).
- Admin tokens are short-lived bearer tokens with no refresh token; the Angular app keeps them in
  memory, so reloading the admin page requires signing in again.
- Contact-form messages are only shown in the admin area; no email notification is sent and
  admins reply from their own mail client.
- Rate limiting is in-memory per instance.
