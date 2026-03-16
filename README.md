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
- local files under `data/gtfs-static/`

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

Routes and trips can be imported from the local GTFS static files with:

Use the HTTP base URL printed by `dotnet run`.

```bash
curl -X POST http://localhost:<port>/debug/gtfs/import-routes-trips
```

This imports:
- `routes.txt`
- `trips.txt`
- `feed_info.txt`

## Realtime Ingestion

The app runs a hosted background worker that polls Auckland Transport every 30 seconds and saves vehicle positions into PostgreSQL.

## Notes

- The frontend should consume this backend, not Auckland Transport directly.
- `GET /vehicles/latest` returns backend DTOs, not raw AT payloads.
- Not all realtime vehicle records include enough trip/route metadata to be fully enriched.
