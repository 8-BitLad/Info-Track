# InfoTrack: Solicitor Data Scraper & Dashboard

A .NET Aspire + React application for solicitor listing data scraping and reporting across multiple UK locations.
The App scrapes solicitor data from public sources, stores it in an in-memory database, and provides a React dashboard for visualization and reporting.
Primarliy **solicitors.com** site is used for this exercise. 
The project is architected using **Clean Architecture** using **CQRS**, **Dependency Inversion** and **SOLID** principles.

## Disclaimer
This is development-focused setup. It is not intended for production use or commercial deployment.
Also please note that scraping websites may violate their terms of service. Always check the website's **`robots.txt`** and terms before scraping.
since **solicitors.com** has no restrictions, it was used for scraping in this exercise.
**LawSociety.org.uk** has restrictions in place(throws **403 forbidden** http error on any attempt), so scraping was **attempted** but wasn't possible.

## How to Run

### Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 20.18.1+** - [Download](https://nodejs.org/)
- **npm 10.8.2+** - Comes with Node.js

### Installation

```bash
# Navigate to solution directory
cd Info-Track

# Restore .NET dependencies
dotnet restore InfoTrack.sln

# Install frontend dependencies
cd InfoTrack.Web
npm install
```
## Build & Development

### Build the Solution

```bash
dotnet build InfoTrack.sln
```
**Note: `dotnet build` will restore the .NET project dependencies automatically, but the React frontend dependencies must be installed separately with `npm install` before running or building the web app.**

### Start the Application

```bash
# Run Aspire AppHost (orchestrates API + frontend + database)
dotnet run --project InfoTrack.AppHost/InfoTrack.AppHost.csproj
```
## Running Application
1. Aspire Dashboard launches at: **https://localhost:17264** would ask for token, which could be found in the console
2. ASP.NET Core API starts on: **https://localhost:7000** (configurable, check Aspire Dashboard)
3. React dev server starts on: **http://localhost:5173** (with hot-reload enabled)
4. In-memory database initializes
5. Services communicate via service discovery (configured via Aspire)

### Access the Application

- **Frontend Dashboard:** http://localhost:5173
- **API OpenAPI Docs:** https://localhost:7000/openapi/v1.json
- **Aspire Dashboard:** https://localhost:17264 (service health, logs, traces)

### Hot Reload

- **Backend:** Modify C# code → AppHost auto-restarts API service (within 5 sec)
- **Frontend:** Modify React/TypeScript → Vite HMR auto-updates browser (instant)

## Notes

- This is a development-focused setup; production requires SQL Server, redis cache, external telemetry, and authentication
- Scraping uses only .NET standard libraries (Http)
- Aspire Dashboard requires browser JavaScript enabled (security token validation)