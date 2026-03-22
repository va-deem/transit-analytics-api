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

- `GET /health`
- `GET /vehicles/latest`
- hosted polling worker that ingests vehicle positions every 30 seconds
- manual GTFS routes/trips import endpoint for development

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

### 3. Run the app

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

## Notes

- The frontend should consume this backend, not Auckland Transport directly.
- `GET /vehicles/latest` returns backend DTOs, not raw AT payloads.
- Not all realtime vehicle records include enough trip/route metadata to be fully enriched.
