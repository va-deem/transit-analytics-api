# Transit Analytics API

ASP.NET Core backend for ingesting Auckland Transport vehicle data, storing historical snapshots, and exposing playback-oriented APIs.

## What This App Does

The app:
- polls Auckland Transport realtime vehicle positions
- stores vehicle history in PostgreSQL
- imports GTFS static data for route/trip enrichment
- exposes HTTP APIs for live and historical map views
- exposes a WebSocket endpoint for live snapshots
- includes a small built-in admin area for operational controls

## Stack

- C#
- ASP.NET Core
- PostgreSQL
- Entity Framework Core
- Hosted background services

## Features

### Public/backend API

- `GET /health`
- `GET /vehicles/latest`
- `GET /vehicles/{id}/history?start=&end=`
- `GET /vehicles/range?start=&end=`
- `GET /vehicles/range?start=&end=&routeId=`
- `GET /routes`
- `GET /routes/{id}/vehicles/latest`
- `GET /routes/{id}/shape`
- `GET /routes/{id}/stops`
- `/ws/vehicles` for live snapshots and route-scoped subscriptions

### Admin

- `/admin/login`
- `/admin/settings`
- maintenance mode toggle
- polling enable/disable toggle
- GTFS `.zip` upload and background import
- GTFS import status visibility

### Background services

- vehicle polling every 30 seconds
- GTFS upload/import worker
- daily history retention cleanup
- startup EF Core migration application

## Local Development

### Prerequisites

- .NET SDK 9+
- PostgreSQL

### Database

Create a local PostgreSQL database, for example:

- database: `transit_analytics`
- user: `postgres`

### User Secrets

Initialize user secrets if needed:

```bash
dotnet user-secrets init
```

Set required values:

```bash
dotnet user-secrets set "AucklandTransport:SubscriptionKey" "YOUR_KEY"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=transit_analytics;Username=postgres"
dotnet user-secrets set "Admin:PasswordHash" "YOUR_HASH"
```

Optional local WebSocket origin allowlist:

```bash
dotnet user-secrets set "VehicleWebSocket:AllowedOrigins:0" "http://localhost:5193"
```

### Apply Migrations

```bash
dotnet ef database update
```

### Run the App

```bash
dotnet run
```

By default, local development runs without production-only internal secret enforcement.

## Local URLs

Typical local URLs:

- `http://localhost:5193/health`
- `http://localhost:5193/admin/login`
- `http://localhost:5193/admin/settings`
- `ws://localhost:5193/ws/vehicles`

If your local launch profile uses a different port, use that port instead.

## Configuration

Important configuration sections:

- `ConnectionStrings:DefaultConnection`
- `AucklandTransport:BaseUrl`
- `AucklandTransport:SubscriptionKey`
- `Admin:PasswordHash`
- `Admin:CookieName`
- `Vehicles:LatestPositionMaxAgeMinutes`
- `Vehicles:HistoryRetentionDays`
- `InternalApi:Secret`
- `InternalApi:HeaderName`
- `VehicleWebSocket:MaxConcurrentConnections`
- `VehicleWebSocket:AllowedOrigins`

## Security Notes

Current behavior:

- admin is protected with cookie authentication
- maintenance mode keeps `/health` and `/admin/*` reachable while blocking public traffic
- WebSocket origin allowlisting and total connection caps are configurable

For local development:

- internal secret enforcement is disabled in `Development`
- startup migrations are applied automatically when the app starts

## Data Behavior

- latest vehicle responses are built from persisted data, not raw AT payloads
- GTFS static imports enrich route and trip metadata
- latest vehicle visibility uses a freshness window controlled by `Vehicles:LatestPositionMaxAgeMinutes`
- saved vehicle history retention uses `Vehicles:HistoryRetentionDays`
- history and range endpoints enforce bounded time windows

## Admin Area

The built-in admin area is intended for operational control of the backend.

Current capabilities:
- sign in with a configured password hash
- enable or disable maintenance mode
- enable or disable polling
- upload a GTFS `.zip` archive
- monitor GTFS import status

## Useful Commands

Create a migration:

```bash
dotnet ef migrations add MigrationName
```

Apply migrations:

```bash
dotnet ef database update
```

Run tests:

```bash
dotnet test
```
