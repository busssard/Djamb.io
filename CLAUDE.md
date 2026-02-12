# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Djambi-N - Online Multiplayer Strategy Board Game

Djambi-N is an online multiplayer strategy board game in curved space, based on [Djambi](https://en.wikipedia.org/wiki/Djambi). It supports 3-8 players on dynamically sized hexagonal boards. The game features real-time updates via WebSockets, session-based authentication, and full deployment on AWS.

**Live site**: [djambi-n.com](https://djambi-n.com/)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | F# / ASP.NET Core 3.1 |
| ORM | Entity Framework Core with MySQL |
| Frontend (current) | TypeScript / React 16 / Redux / Material-UI / Konva.js (`web2/`) |
| Frontend (legacy) | TypeScript / React / Redux / Webpack (`web/`) |
| Database | MySQL 5.7+ (production: AWS RDS, local: MSSQL via Docker) |
| Infrastructure | AWS (Elastic Beanstalk, S3, CloudFront, ECR, RDS) |
| CI/CD | GitHub Actions |
| Build orchestration | FAKE (F# Make) |
| API docs | Swagger/OpenAPI at `/swagger` |

## Key Development Commands

### Docker Compose (Full Stack Local)
```bash
# Start everything (API + DB + web)
docker-compose up

# API at http://localhost:5100, Web at http://localhost:8080, DB at localhost:1434
```

### Backend (F# API)
```bash
# Build
dotnet build api/api.host/api.host.fsproj

# Run
dotnet run --project api/api.host/api.host.fsproj

# Unit tests
dotnet test api/tests/api.unitTests/api.unitTests.fsproj

# Integration tests (uses SQLite in-memory)
DJAMBI_Sql__UseSqliteForTesting=true dotnet test api/tests/api.integrationTests/api.integrationTests.fsproj
```

### Frontend (web2 - Current)
```bash
cd web2
npm install
npm start          # Dev server at http://localhost:3000
npm run build      # Production build
npm run lint       # ESLint
npm test           # Jest tests
```

### FAKE Build System
```bash
# List all targets
fake build --list

# Build
fake build -t build-api
fake build -t build-web

# Test
fake build -t test-api-unit
fake build -t test-api-int
fake build -t test-web-unit
fake build -t test-all

# Lint
fake build -t fs-lint       # F# linting
fake build -t es-lint       # TypeScript linting
fake build -t lint-all

# Run
fake build -t run-api
fake build -t run-web
fake build -t run-all

# Everything
fake build -t all
```

## Project Architecture

### Backend Layered Architecture (`api/`)

```
Controllers (HTTP) → Managers (orchestration) → Services (business logic) → Repositories (data access)
```

| Module | Purpose |
|--------|---------|
| `api.host` | ASP.NET startup, DI configuration, middleware |
| `api.web` | Controllers, cookie/session providers, middleware |
| `api.logic` | Managers, services, game rules, security |
| `api.db` | Repository interfaces |
| `api.db.model` | EF Core context, migrations, repository implementations |
| `api.model` | DTOs, configuration models, request/response types |
| `api.enums` | Shared enums (PlayerKind, GameStatus, EventKind, Privilege) |

### API Endpoints

| Route | Controller | Operations |
|-------|-----------|------------|
| `/api/users` | UserController | Create, get, get current, delete |
| `/api/sessions` | SessionController | Login (POST), logout (DELETE) |
| `/api/games` | GameController | Create, get, update parameters, start |
| `/api/players` | PlayerController | Join, leave, manage players |
| `/api/turns` | TurnController | Select cell, commit turn, reset turn |
| `/api/events` | EventController | Game event history |
| `/api/boards` | BoardManager | Board configuration lookup |
| `/api/notifications` | NotificationController | Real-time notifications (WebSocket) |
| `/api/search` | SearchController | Search games |
| `/api/snapshots` | SnapshotController | Game state snapshots |
| `/status` | Health check | ASP.NET health endpoint |
| `/swagger` | Swagger UI | API documentation |

### Frontend Architecture (`web2/`)

- **React 16** with function components (not class-based)
- **Redux** with thunk middleware for async actions
- **Konva.js** for canvas-based board rendering
- **Material-UI** for UI components
- **API client auto-generated** from OpenAPI spec (`web2/src/api-client/`)
- **Runtime config** via `web2/public/env.json` (API URL, etc.)

Redux state slices: `activeGame`, `apiClient`, `boards`, `config`, `images`, `navigation`, `notifications`, `session`

### Frontend (Legacy) (`web/`)

Webpack-based React/Redux app with Konva for rendering. Mocha/Chai for testing. Still builds but `web2/` is the active frontend.

## Data Models

### Core Entities (EF Core, `api.db.model/Model/`)
- **User** - Account with encrypted password, privilege level
- **Session** - Auth sessions with token, stored in DB
- **Game** - Game instance with status, parameters, board region count
- **Player** - Player in a game (user or neutral AI), with status and color
- **Event** - Game events (moves, eliminations, etc.)
- **Snapshot** - Point-in-time game state captures
- **NeutralPlayerName** - Names for AI players

### Key Enums
- **GameStatus**: Pending, InProgress, Canceled, Over
- **PlayerStatus**: Pending, Alive, Eliminated, Conceded, WillConcede, AcceptsDraw, Victorious
- **PlayerKind**: User, Neutral (AI)
- **Privilege**: Normal user, EditPendingGames, OpenParticipation, Snapshots, ViewUsers, EditUsers
- **EventKind**: GameParametersChanged, GameCanceled, GameStarted, TurnCommitted, TurnReset, PlayerStatusChanged, etc.

## Configuration

### Environment Variables (DJAMBI_ prefix)

All environment variables use the `DJAMBI_` prefix with double-underscore for nesting:

```bash
# Database
DJAMBI_Sql__ConnectionString="Data Source=...;Initial Catalog=Apex2;..."
DJAMBI_Sql__UseSqliteForTesting=true   # Integration tests only

# API
DJAMBI_Api__apiAddress=http://*:5100
DJAMBI_Api__cookieDomain=localhost
DJAMBI_Api__webAddress=http://localhost:8080
DJAMBI_Api__allowedOrigins=http://localhost:3000,http://localhost:8080
DJAMBI_Api__cookieName=DjambiSession     # Set in appsettings.json

# Logging
DJAMBI_Log__directory=/var/log/djambi
DJAMBI_Log__levels__microsoft=Warning
DJAMBI_Log__levels__aspnetcore=Warning
DJAMBI_Log__levels__efcore=Information

# Web server (static file serving from API)
DJAMBI_WebServer__Enable=false
DJAMBI_WebServer__EnableDevelopmentMode=false
DJAMBI_WebServer__WebRoot=/path/to/web/dist
```

### Configuration Hierarchy
1. `api/api.host/appsettings.json` (base defaults)
2. Environment variables with `DJAMBI_` prefix (override)
3. Configuration builder in `api/api.host/Config.fs`

### Frontend Runtime Config
`web2/public/env.json` - API URL and other runtime settings loaded at startup.

## Authentication & Security

### Session-Based Auth
- Login: `POST /api/sessions` → creates session in DB, returns HTTP-only cookie (`DjambiSession`)
- Logout: `DELETE /api/sessions`
- Every request validated via `SessionContextProvider` middleware
- Cookie is HTTP-only, secure, with configurable domain

### Authorization
- Privilege-based system (not role-based)
- Key security functions in `api.logic/Security.fs`:
  - `ensureHas` - Check single privilege
  - `ensureCreatorOrEditPendingGames` - Game creator or privileged users
  - `ensurePlayerOrHas` - Game participant or privileged users
  - `ensureCurrentPlayerOrOpenParticipation` - Turn-based access control
  - `ensureSelfOrHas` - Self-service or privileged users

### Middleware
- `LoggingMiddleware` - Request/response logging via Serilog
- `ErrorHandlingMiddleware` - Global exception handling

## Testing

### Backend Tests
- **Framework**: xUnit (via `dotnet test`)
- **Unit tests**: `api/tests/api.unitTests/` - Pure logic tests
- **Integration tests**: `api/tests/api.integrationTests/` - Uses SQLite in-memory DB (`DJAMBI_Sql__UseSqliteForTesting=true`)

### Frontend Tests (web2)
- **Framework**: Jest + React Testing Library
- **Run**: `cd web2 && npm test`

### Frontend Tests (legacy web)
- **Framework**: Mocha + Chai
- **Run**: `cd web && npm test`

### CI/CD Quality Gates
- `.github/workflows/api-quality-gates.yml` - Build, unit tests, integration tests, Docker build
- `.github/workflows/web2-quality-gates.yml` - Build and lint
- `.github/workflows/check-for-api-contract-changes.yml` - API contract validation
- Triggered on push/PR to `master`, `develop`, `release/**`

## Branching Strategy (GitFlow)

- **`develop`** - Base branch for all feature work. PRs target here.
- **`master`** - Production. Merges to master trigger deployments.
- **`release/{date-created}`** - Release candidates, branched from develop.
- **`feature/{description}`** - Feature branches, merged into develop.

## Deployment (AWS)

### Architecture
- **Web client**: S3 bucket → CloudFront CDN
- **API**: Docker image → ECR → Elastic Beanstalk (port 8081)
- **Database**: AWS RDS MySQL

### Deployment Triggers (GitHub Actions)
- Changes to `web2/*` → S3 + CloudFront deployment (`.github/workflows/web2-deploy.yml`)
- Changes to `api/*` → ECR + Elastic Beanstalk deployment (`.github/workflows/api-deploy.yml`)
- Changes to `api/api.db.model/**/*` → RDS migration (`.github/workflows/sql-migration.yml`)

### What CI/CD Does NOT Do
- Provision new AWS resources
- Clean obsolete files from S3

## Common Gotchas

1. **MySQL is required** - The API uses `UseMySql()` in EF Core. SQLite is only for integration tests via the `DJAMBI_Sql__UseSqliteForTesting` flag.
2. **DJAMBI_ prefix on all env vars** - Configuration uses double-underscore for nesting: `DJAMBI_Sql__ConnectionString`, not `DJAMBI_Sql_ConnectionString`.
3. **Docker Compose uses MSSQL, not MySQL** - Local dev via Docker uses `mcr.microsoft.com/mssql/server:2017-latest`. Production uses MySQL on RDS. Be aware of SQL dialect differences.
4. **CORS must match your frontend URL** - Set `DJAMBI_Api__allowedOrigins` to match where the frontend is running (comma-separated).
5. **Cookie domain matters** - `DJAMBI_Api__cookieDomain` must match the domain where the frontend is served, or auth cookies won't be sent.
6. **Two frontend apps exist** - `web2/` is the active frontend (CRA + Material-UI). `web/` is legacy (Webpack). Work in `web2/`.
7. **API client is auto-generated** - `web2/src/api-client/` is generated from the OpenAPI spec. Don't edit these files by hand.
8. **F# project structure** - `.fsproj` files list source files in compilation order. Order matters in F# -- files can only reference things defined above them.
9. **Integration tests need the env var** - Without `DJAMBI_Sql__UseSqliteForTesting=true`, integration tests will try to connect to a real MySQL instance.
10. **FAKE build requires .NET tool restore** - Run `dotnet tool restore` in `api/` before using `fake build` commands.

## File Structure Key Points

```
Djamb.io/
├── api/                          # F# backend
│   ├── api.host/                 # ASP.NET startup, config, Dockerfile
│   ├── api.web/                  # Controllers, middleware, cookie handling
│   ├── api.logic/                # Managers, services, game rules, security
│   ├── api.db/                   # Repository interfaces
│   ├── api.db.model/             # EF Core context, migrations, repos
│   ├── api.model/                # DTOs and config models
│   ├── api.enums/                # Shared enums
│   └── tests/                    # Unit + integration tests
├── web2/                         # Current React frontend (CRA)
│   ├── src/api-client/           # Auto-generated API client
│   ├── src/redux/                # Redux store, slices, middleware
│   ├── src/components/           # React components
│   └── public/env.json           # Runtime API configuration
├── web/                          # Legacy frontend (Webpack)
├── build.fsx                     # FAKE build script
├── docker-compose.yml            # Local dev stack
├── .github/workflows/            # CI/CD pipelines
├── CONTRIBUTING.md               # Contribution guidelines
├── Deployment.md                 # AWS architecture docs
└── readme.md                     # Project overview
```

## When Working on This Project

1. **Check which frontend you're editing** - `web2/` is current, `web/` is legacy
2. **Run quality gates before pushing** - `dotnet test` for backend, `npm run lint && npm test` for frontend
3. **Use the FAKE build system** for orchestrated builds: `fake build -t test-all`
4. **Don't edit `web2/src/api-client/`** - These files are auto-generated from the OpenAPI spec
5. **Respect F# file ordering** - Files in `.fsproj` are compiled top-to-bottom; dependencies must come first
6. **Set environment variables** before running locally outside Docker - especially `DJAMBI_Sql__ConnectionString` and `DJAMBI_Api__allowedOrigins`
7. **Test with Docker Compose first** if unsure about local setup - `docker-compose up` gets everything running
