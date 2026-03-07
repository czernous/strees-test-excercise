# Stress Test Application

A full-stack web application for performing stress test calculations on loan portfolios under various house price change scenarios.

## Overview

This application allows you to:
- Define house price change scenarios for different countries
- Calculate expected losses across loan portfolios
- View detailed calculation results including portfolio-level breakdowns
- Track calculation history

**Tech Stack:**
- **Backend**: ASP.NET Core 10 with minimal APIs (Carter), Entity Framework Core, SQLite
- **Frontend**: React 19, TypeScript, Vite, Tailwind CSS v4, DaisyUI

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) (for frontend)

## Getting Started

### 1. Clone the Repository

```bash
cd c:\development\personal\StressTestApp
```

### 2. Run the Backend

```bash
cd StressTestApp.Server
dotnet run
```

The backend server will start on:
- HTTPS: `https://localhost:7044`
- HTTP: `http://localhost:5101`

The database will be automatically created at `StressTestApp.Server/Data/Db/stresstest.db` on first run.

### 3. Run the Frontend

Open a new terminal:

```bash
cd stresstestapp.client
npm install
npm run dev
```

Alternatively, you can press 'dubug' in Visual Studio to start both frontend and backend.

The frontend development server will start on `https://localhost:59564`

### 4. Open the Application

Navigate to https://localhost:59564 in your browser.

## Features

### Backend Features

- **RESTful API** with minimal APIs using Carter
- **Feature-based structure** (vertical slices)
- **Entity Framework Core** with SQLite
- **CSV data loading** for loans, portfolios, and ratings
- **In-memory caching** of market data
- **Health checks** at `/health`
- **Duplicate calculation prevention** (HTTP 409 on identical inputs)
- **OpenAPI/Swagger** in development mode

### Frontend Features

- **React 19** with modern patterns:
  - `use()` hook for data fetching (no `useEffect`)
  - Suspense for loading states
  - Error boundaries for error handling
- **Component-controller pattern** with separated business logic
- **Request caching** with automatic retry on error boundary reset
- **Server health check** before app loads
- **DaisyUI components** for consistent UI
- **Responsive design** with Tailwind CSS

## API Endpoints

### Calculations

- `POST /api/calculations` - Create a new calculation
- `GET /api/calculations` - List all calculations (summary)
- `GET /api/calculations/{id}` - Get detailed calculation results

### Countries

- `GET /api/countries` - List available countries

### Health

- `GET /health` - Health check endpoint

## Example Calculation

**Request:**
```json
POST /api/calculations
{
  "housePriceChanges": {
    "GB": -5.12,
    "US": -4.34,
    "FR": -3.87,
    "DE": -1.23,
    "SG": -5.5,
    "GR": -5.68
  }
}
```

**Response:**
```json
{
  "calculationId": "01945c2e-3456-7890-abcd-ef1234567890",
  "createdAtUtc": "2026-03-07T13:00:00Z",
  "durationMs": 45,
  "housePriceChanges": { ... },
  "portfolioCount": 6,
  "loanCount": 500,
  "totalExpectedLoss": 1234567.89
}
```

## Running Tests

```bash
cd StressTestApp.Tests
dotnet test
```

## Development

### Backend

- The backend uses **hot reload** - changes to C# files will automatically rebuild
- Database is created automatically on first run
- CSV data is loaded from `Data/Csv/` on startup

### Frontend

- Vite provides **hot module replacement** - changes apply instantly
- API requests are proxied to the backend (configured in `vite.config.ts`)
- Health check runs before app initialization

## Troubleshooting

### Database Issues

If you encounter database issues, delete the database and restart:

```bash
Remove-Item StressTestApp.Server/Data/Db/stresstest.db*
```

The database will be recreated on next run.

### Port Conflicts

If ports are in use, update:
- Backend: `StressTestApp.Server/Properties/launchSettings.json`
- Frontend: `stresstestapp.client/vite.config.ts` (DEV_SERVER_PORT)

### Certificate Issues

If you see HTTPS certificate warnings:

```bash
dotnet dev-certs https --trust
```

## Architecture Decisions

- **React 19 `use()` hook** instead of `useEffect` for data fetching
- **Feature-based organization** for better cohesion and maintainability
- **Component-controller pattern** to separate presentation from business logic
- **Vertical slice architecture** on the backend (features contain all layers)
- **In-memory market data cache** for performance (CSV loaded on startup)
- **No migrations** - using EnsureCreated() for simplicity in this exercise

## License

This is an exercise project for demonstration purposes.
