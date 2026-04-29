# Github-Analyzer

Base project for a thesis web application that analyzes GitHub repository AST structures and visualizes the result in the frontend.

## Stack

- .NET 10
- .NET Aspire
- ASP.NET Core Minimal API
- Vertical Slice Architecture on backend
- PostgreSQL
- EF Core
- Vue 3 + Vite + TypeScript
- Pinia + Vue Router
- Nuxt UI

## Solution Structure

- `src/Github-Analyzer.AppHost`
- `src/Github-Analyzer.ServiceDefaults`
- `src/Github-Analyzer.WebApi`
- `src/Github-Analyzer.WebApp`
- `src/Github-Analyzer.Analysis`
- `test/Github-Analyzer.WebApi.Tests`
- `test/Github-Analyzer.Analysis.Tests`

## Run

1. Start Docker Desktop.
2. Set Google OAuth values in `src/Github-Analyzer.WebApi/appsettings.json` or user secrets.
3. Run `dotnet restore`.
4. Run `dotnet build`.
5. Run `dotnet run --project src/Github-Analyzer.AppHost`.
