# Transit Analytics API

ASP.NET Core backend for an Auckland Transport map application.

The backend:
- fetches realtime vehicle positions from Auckland Transport
- stores historical snapshots in PostgreSQL
- exposes backend-owned APIs for the frontend
- imports GTFS static route and trip data for enrichment

## Stack

- C#
- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core

## Current Features

### Public functionality

- health endpoint with maintenance mode visibility
- live vehicle snapshot endpoint backed by persisted data
- historical vehicle playback for a single vehicle in a bounded time window
- range playback for all vehicles in a bounded time window
- optional route-scoped range playback filtering
- route listing enriched with current active vehicle counts
- route-scoped live vehicle snapshots
- route shape and stop endpoints based on imported GTFS data
- websocket live snapshot delivery for all vehicles and route-scoped subscriptions

### Admin functionality

- password-protected admin area
- maintenance mode toggle for public HTTP and websocket access
- polling enable/disable control
- GTFS static `.zip` upload through the admin UI
- background GTFS import with persisted status reporting

### Background functionality

- hosted polling worker that ingests Auckland Transport vehicle positions every 30 seconds
- background GTFS import processing
- daily vehicle history cleanup at `03:00` Auckland time

## Data Sources

Realtime:
- Auckland Transport `vehiclelocations` feed

Static GTFS:
- uploaded through the admin interface as a `.zip` archive and imported into PostgreSQL

## Local Setup

### 1. Database

The app expects a local PostgreSQL database.

Default local database name:
- `transit_analytics`

### 2. Secrets

Use .NET user secrets for local development.

Initialize:

```bash
dotnet user-secrets init
```

Set AT subscription key:

```bash
dotnet user-secrets set "AucklandTransport:SubscriptionKey" "YOUR_KEY"
```

Set database connection string:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=transit_analytics;Username=postgres"
```

Set admin password hash:

```bash
dotnet user-secrets set "Admin:PasswordHash" "YOUR_HASH"
```

### 3. Run the app

Apply migrations first:

```bash
dotnet ef database update
```

Then start the app:

```bash
dotnet run
```

## Migrations

Create a migration:

```bash
dotnet ef migrations add MigrationName
```

Apply migrations:

```bash
dotnet ef database update
```

## GTFS Static Import

Routes, trips, stops, shapes, and stop times are imported through the protected admin UI.

Use:
- `/admin/login`
- `/admin/settings`

Upload a GTFS `.zip` archive there and the backend will validate it and run the import in the background.

## Realtime Ingestion

The app runs a hosted background worker that polls Auckland Transport every 30 seconds and saves vehicle positions into PostgreSQL.

Vehicles in `GET /vehicles/latest` and websocket snapshots are filtered to recent positions only.
The default freshness window is 5 minutes and can be changed with `Vehicles:LatestPositionMaxAgeMinutes`.
Saved vehicle history is retained for 7 days by default and cleaned up at `03:00` Auckland time by a daily background job using `Vehicles:HistoryRetentionDays`.

## Route and Playback Data

Imported GTFS static data is used to enrich realtime vehicle responses and to provide route context for the client.

Current route-related functionality:
- route list retrieval
- live vehicle counts per route
- route shape retrieval
- route stop retrieval

Current playback functionality:
- single-vehicle history in a bounded time window
- range playback in a bounded time window
- optional route-scoped range playback filtering

Playback endpoints enforce server-side time-window limits to keep result sizes bounded.

## Admin Area

The backend includes a small built-in admin area under `/admin/*`.

Current admin capabilities:
- sign in with a configured password hash
- enable or disable maintenance mode
- enable or disable polling
- upload a GTFS `.zip` archive
- monitor GTFS import status from the settings page

When maintenance mode is enabled:
- `/health` remains available
- admin routes remain available
- public API endpoints return `503 Service Unavailable`
- `/ws/vehicles` rejects websocket upgrades with `503`

## Notes

- The frontend should consume this backend, not Auckland Transport directly.
- `GET /vehicles/latest` returns backend DTOs, not raw AT payloads.
- Not all realtime vehicle records include enough trip/route metadata to be fully enriched.
