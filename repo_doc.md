# repo_doc.md — Technical Documentation

All technical details about the Djamb.io codebase. For project vision and Claude guidelines, see `CLAUDE.md`. For task tracking, see `tasks.md`.

## Tech Stack

| Layer | Technology | Version | Status |
|-------|-----------|---------|--------|
| Backend | F# / ASP.NET Core | .NET 8.0 | **Current LTS** |
| ORM | Entity Framework Core + Pomelo MySQL | 8.0 | Current |
| Frontend | TypeScript / React / Redux / MUI / Konva.js | React 18, TS 5.7, MUI 7, Vite 5 | **Current** |
| Frontend (legacy) | TypeScript / React / Redux / Webpack (`web/`) | — | To be removed |
| Database | MySQL 8.0 (local: Docker, prod: AWS RDS) | — | OK |
| Infrastructure | AWS (EB, S3, CloudFront, ECR, RDS) | — | OK |
| CI/CD | GitHub Actions | Node 10.x in workflows | **Outdated** |
| Build orchestration | FAKE (F# Make) | — | OK |
| API docs | Swagger/OpenAPI at `/swagger` | Swashbuckle 5.5 | Outdated |

## Key Development Commands

### Docker Compose (Full Stack Local)
```bash
docker-compose up
# API at http://localhost:5100, Web at http://localhost:8080, DB at localhost:1434
```

### Backend (F# API)
```bash
dotnet build api/api.host/api.host.fsproj
dotnet run --project api/api.host/api.host.fsproj
dotnet test api/tests/api.unitTests/api.unitTests.fsproj
DJAMBI_Sql__UseSqliteForTesting=true dotnet test api/tests/api.integrationTests/api.integrationTests.fsproj
```

### Frontend (web2 — Current)
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
fake build --list          # List all targets
fake build -t build-api    # Build API
fake build -t build-web    # Build frontend
fake build -t test-all     # All tests
fake build -t lint-all     # All linting
fake build -t run-all      # Run everything
fake build -t all          # Everything
```
Note: Run `dotnet tool restore` in `api/` before first use.

## Backend Architecture (`api/`)

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

### Key Architecture Patterns
- **Event sourcing**: Game state changes are modeled as effects, logged, and applied in sequence
- **Strategy pattern**: Each of 7 piece types has a `PieceStrategy` defining movement/capture rules
- **Privilege-based auth**: Not role-based; fine-grained `ensureHas`, `ensurePlayerOrHas`, etc.
- **Known tech debt**: `GameManager` implements 4 interfaces (god object) — has a `TODO: Break up` comment

### API Endpoints

| Route | Controller | Operations |
|-------|-----------|------------|
| `/api/users` | UserController | Create, get, get current, delete |
| `/api/users/quick` | UserController | Quick-register (POST, AllowAnonymous): `{ name, email? }` → creates user + session, sets cookie |
| `/api/sessions` | SessionController | Login (POST), logout (DELETE), restore (GET) |
| `/api/sessions/magic-link` | SessionController | Request magic link (POST, AllowAnonymous): `{ email }` → sends login email (always 200) |
| `/api/sessions/magic-link/verify` | SessionController | Verify magic link (POST, AllowAnonymous): `{ token }` → creates session, sets cookie |
| `/api/games` | GameController | Create, get, update parameters, start |
| `/api/games/invite/{code}` | GameController | Get game by invite code (GET, AllowAnonymous) |
| `/api/games/invite/{code}/join` | GameController | Join game via invite code (POST, requires auth) |
| `/api/players` | PlayerController | Join, leave, manage players |
| `/api/turns` | TurnController | Select cell, commit turn, reset turn |
| `/api/events` | EventController | Game event history |
| `/api/boards` | BoardManager | Board configuration lookup |
| `/api/notifications` | NotificationController | Real-time notifications (WebSocket + SSE) |
| `/api/search` | SearchController | Search games |
| `/api/snapshots` | SnapshotController | Game state snapshots |
| `/status` | Health check | ASP.NET health endpoint |
| `/swagger` | Swagger UI | API documentation |

## Frontend Architecture (`web2/`)

- **React 18** with function components and hooks (no class components)
- **Redux** (old-style, NOT Redux Toolkit) — 8 slices with manual action types/creators/reducers
- **Konva.js 9** for canvas-based hex board rendering
- **MUI v7** with `sx` prop and `styled` API, dark theme
- **Vite 5** build system with `vite-plugin-pwa` for PWA support
- **React Router 6** with lazy-loaded routes (`React.lazy` + `Suspense`)
- **API client** auto-generated from OpenAPI spec (`web2/src/api-client/`) — new endpoints use raw `fetch()` instead
- **Runtime config** via `web2/public/env.json`

### Redux State Slices
`activeGame`, `apiClient`, `boards`, `config`, `images`, `navigation`, `notifications`, `session`

### Frontend Routes

| Route | Page Component | Auth Required | Description |
|-------|---------------|---------------|-------------|
| `/join` | QuickJoinPage | No | Default landing — username + optional email quick-join form |
| `/magic-link` | MagicLinkPage | No | Request magic link email for device transfer |
| `/auth/verify/:token` | MagicLinkVerifyPage | No | Auto-verifies magic link token, creates session |
| `/invite/:code` | JoinByInvitePage | No | Shows game info from invite code, join button (or quick-join form) |
| `/home` | HomePage | Yes | Dashboard — active games, search |
| `/new-game` | CreateGamePage | Yes | Create a new game |
| `/search-games` | SearchGamesPage | Yes | Search for public games |
| `/settings` | UserConfigPage | Yes | User settings |
| `/notifications` | NotificationsPage | Yes | Notifications list |
| `/rules` | RulesPage | No | Game rules |
| `/sign-out` | SignOutPage | Yes | Sign out |
| `/games/:gameId` | GamePage | Yes | Game overview/redirect |
| `/games/:gameId/play` | GamePlayPage | Yes | Game board (also serves spectator mode) |
| `/games/:gameId/lobby` | GameLobbyPage | Yes | Pre-game lobby with invite link for private games |
| `/games/:gameId/info` | GameInfoPage | Yes | Game info |
| `/games/:gameId/diplomacy` | GameDiplomacyPage | Yes | Diplomacy (stub — `JSON.stringify(game)`) |
| `/games/:gameId/outcome` | GameOutcomePage | Yes | Game results |
| `/games/:gameId/snapshots` | GameSnapshotsPage | Yes | Game state snapshots |

### PWA Infrastructure
- `vite-plugin-pwa` with Workbox `generateSW` strategy
- Cache-first for static assets, network-first for API calls
- Install prompt via `beforeinstallprompt` event in TopBar
- Offline fallback page at `public/offline.html`
- Full manifest with multiple icon sizes and maskable icon

### Spectator Mode
- Spectators are detected in `GamePlayPage.tsx` — user not found in `game.players` list
- Board renders in read-only mode (no cell selection), "Watching" indicator shown
- No backend changes yet — spectator access control and WebSocket streaming deferred to Phase 6

### Diplomacy Page (Stub)
- `web2/src/components/pages/GameDiplomacyPage.tsx` — placeholder, renders `JSON.stringify(game)`
- Route: `/games/:gameId/diplomacy` — wired in App.tsx and navigation drawer with icon
- Indicates social/diplomacy features were planned from the start

## Data Models

### Core Entities (EF Core, `api.db.model/Model/`)
- **User** — Account with optional password (PBKDF2) and optional email (max 254 chars), privilege level. Password is nullable to support quick-join (passwordless) auth.
- **Session** — Auth sessions with token, stored in DB (not JWT)
- **Game** — Game instance with status, parameters, board region count, optional invite code (8-char alphanumeric for private games)
- **Player** — Player in a game (user or neutral AI), with status and color
- **Event** — Game events with effects (moves, eliminations, etc.) — event-sourced
- **Snapshot** — Point-in-time game state captures
- **MagicLink** — One-time login tokens for device transfer. Fields: Token (GUID), UserId (FK), Email, CreatedOn, ExpiresOn (15 min TTL), UsedOn (nullable). Used by magic link email auth flow.
- **NeutralPlayerName** — Names for AI players

### Key Enums (`api/api.enums/Enums.fs`)
- **GameStatus**: Pending, InProgress, Canceled, Over
- **PlayerStatus**: Pending, Alive, Eliminated, Conceded, WillConcede, AcceptsDraw, Victorious
- **PlayerKind**: User, Neutral (AI)
- **Privilege**: Normal user, EditPendingGames, OpenParticipation, Snapshots, ViewUsers, EditUsers
- **EventKind**: GameParametersChanged, GameCanceled, GameStarted, TurnCommitted, TurnReset, PlayerStatusChanged, etc.
- **PieceKind**: Conduit, Thug, Scientist, Hunter, Diplomat, Reaper, Corpse

### Piece Types and Rules
| Piece | Role | Key Mechanic |
|-------|------|-------------|
| Conduit (Chief) | Power piece | Can hold center; killing it eliminates owning player |
| Thug (Militant) | Short-range killer | Basic attack piece |
| Scientist (Reporter) | Post-move targeting | Targets after moving |
| Hunter (Assassin) | Kill and return | Kills target, returns to target's position |
| Diplomat | Non-lethal | Displaces pieces without killing |
| Reaper (Necromobile) | Corpse collector | Moves corpses around the board |
| Corpse | Dead piece | Can be moved/collected by Reaper |

## Configuration

### Environment Variables (DJAMBI_ prefix, double-underscore nesting)
```bash
# Database
DJAMBI_Sql__ConnectionString="Data Source=...;Initial Catalog=Apex2;..."
DJAMBI_Sql__UseSqliteForTesting=true   # Integration tests only

# API
DJAMBI_Api__apiAddress=http://*:5100
DJAMBI_Api__cookieDomain=localhost
DJAMBI_Api__webAddress=http://localhost:8080
DJAMBI_Api__allowedOrigins=http://localhost:3000,http://localhost:8080
DJAMBI_Api__cookieName=DjambiSession

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
`web2/public/env.json` — API URL loaded at startup. Sed-replaced during deployment.

## Authentication & Security

### Auth Flows

**Quick-join (primary, passwordless)**:
1. User visits `/join` → enters username + optional email
2. Frontend calls `POST /api/users/quick` with `{ name, email? }`
3. Backend creates user (no password), creates session, sets `DjambiSession` HTTP-only cookie
4. User is immediately signed in and redirected to `/home`

**Magic link (device transfer)**:
1. User visits `/magic-link` → enters email address
2. Frontend calls `POST /api/sessions/magic-link` with `{ email }`
3. Backend finds user by email, generates a one-time token (GUID, 15-min expiry), sends email with link
4. Endpoint always returns 200 (prevents email enumeration)
5. User clicks link → opens `/auth/verify/:token`
6. Frontend calls `POST /api/sessions/magic-link/verify` with `{ token }`
7. Backend validates token (not expired, not used), marks it used, creates session, sets cookie
8. In dev mode, `ConsoleEmailService` logs the magic link URL to Serilog instead of sending email

**Legacy password auth**:
- `POST /api/sessions` (login) and `POST /api/users` (create account) still work
- Old `/sign-in` and `/create-account` frontend routes have been removed
- Existing password users can restore sessions via cookie (`GET /api/sessions`)

### Session-Based Auth
- Sessions stored in DB with token + expiry, returned as HTTP-only cookie (`DjambiSession`)
- Every request validated via `SessionContextProvider` middleware
- Cookie: HTTP-only, Secure, SameSite, configurable domain
- `createSessionForUser` method handles cleaning up old sessions before creating new ones

### Private Games & Invite Links
- When a game is created with `isPublic = false`, an 8-char alphanumeric invite code is generated
- Invite codes use unambiguous characters (`ABCDEFGHJKLMNPQRSTUVWXYZ23456789` — no 0/O/1/I/L)
- `GET /api/games/invite/{code}` (AllowAnonymous) returns game info for the invite page
- `POST /api/games/invite/{code}/join` (requires auth) adds the current user as a player
- Frontend route `/invite/:code` shows game info + join button (or quick-join form if not authenticated)

### Authorization
- Privilege-based system in `api.logic/Security.fs`
- Key functions: `ensureHas`, `ensureCreatorOrEditPendingGames`, `ensurePlayerOrHas`, `ensureCurrentPlayerOrOpenParticipation`, `ensureSelfOrHas`

### Known Security Issues
- **Token logging**: `SessionContextProvider.fs` logs full session tokens — security risk
- **No rate limiting** on login/API endpoints
- **No CSRF tokens** (relies on SameSite cookies)
- **Magic link tokens in URL**: Token is in the URL path, could leak via referrer headers. Consider using POST-based verification form instead.

### Middleware Pipeline
`LoggingMiddleware` (Serilog) → `ErrorHandlingMiddleware` (global exception handling) → routing → CORS → WebSockets

## Testing

### Backend
- **Framework**: xUnit with FakeItEasy for mocking
- **Unit tests**: `api/tests/api.unitTests/` — 4 test files (EncryptionService, CreateUser, BoardModelExtension, OpenSession)
- **Integration tests**: `api/tests/api.integrationTests/` — 32+ test files covering repos, services, managers
- **Integration DB**: SQLite in-memory (`DJAMBI_Sql__UseSqliteForTesting=true`)
- **Coverage estimate**: ~70% backend logic

### Frontend (web2)
- **Framework**: Jest + React Testing Library (v9.5, outdated)
- **Test files**: 7 (App smoke test, collections, logic, point, line, polygon, boardViewFactory)
- **Coverage estimate**: <10% — minimal component/Redux testing
- **5 TODO comments** in boardViewFactory.test.ts for unwritten tests

### CI/CD Quality Gates
- `.github/workflows/api-quality-gates.yml` — Build, unit tests, integration tests, Docker build
- `.github/workflows/web2-quality-gates.yml` — Build, lint, test
- `.github/workflows/check-for-api-contract-changes.yml` — API contract validation
- Triggered on push/PR to `master`, `develop`, `release/**`

## Deployment (AWS)

### Architecture
- **Web client**: S3 bucket → CloudFront CDN
- **API**: Docker image → ECR → Elastic Beanstalk (port 8081)
- **Database**: AWS RDS MySQL

### Deployment Triggers (GitHub Actions)
- `web2/*` changes → S3 + CloudFront (`web2-deploy.yml`)
- `api/*` changes → ECR + EB (`api-deploy.yml`)
- `api/api.db.model/**/*` changes → RDS migration (`sql-migration.yml`)

### CI/CD Issues
- `api-deploy.yml` uses deprecated `aws ecr get-login` syntax
- All web workflows use Node 10.x (EOL April 2021)
- S3/CloudFront actions reference `@master` branch (should pin versions)
- SQL migration runs directly on production without approval gate
- `check-for-api-contract-changes.yml` uses Java 9 (severely outdated)

## Branching Strategy (GitFlow)
- **`develop`** — Base branch for feature work
- **`master`** — Production; merges trigger deployments
- **`release/{date}`** — Release candidates from develop
- **`feature/{desc}`** — Feature branches into develop

## Common Gotchas

1. **MySQL required** — API uses `UseMySql()` in EF Core. SQLite only for integration tests.
2. **DJAMBI_ prefix** — Double-underscore for nesting: `DJAMBI_Sql__ConnectionString`
3. **Docker Compose uses MSSQL** — Local dev is MSSQL 2017; production is MySQL on RDS. SQL dialect differences.
4. **CORS must match frontend URL** — Set `DJAMBI_Api__allowedOrigins`
5. **Cookie domain matters** — `DJAMBI_Api__cookieDomain` must match frontend domain
6. **Two frontends exist** — `web2/` is active; `web/` is legacy
7. **API client is auto-generated** — Don't edit `web2/src/api-client/`
8. **F# compilation order** — `.fsproj` files list sources top-to-bottom; order matters
9. **Integration tests need env var** — `DJAMBI_Sql__UseSqliteForTesting=true`
10. **FAKE needs tool restore** — `dotnet tool restore` in `api/` first
11. **New endpoints use raw fetch** — Auto-generated API client (`web2/src/api-client/`) is not editable. New endpoints (quick-register, magic-link, invite) use raw `fetch()` in controllers
12. **Passwordless auth is default** — Old sign-in/create-account pages removed. Quick-join at `/join` is the only entry point. Password users restore via cookie only

## File Structure

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
│   ├── src/api-client/           # Auto-generated API client (DO NOT EDIT)
│   ├── src/board/                # Hex board geometry (point, line, polygon)
│   ├── src/components/           # React components
│   │   ├── App/                  # Root + routing
│   │   ├── Canvas/               # Konva.js board rendering
│   │   ├── forms/                # QuickJoin, RequestMagicLink, CreateGame, UserConfig
│   │   ├── pages/                # 17 page components (QuickJoin, MagicLink, MagicLinkVerify, JoinByInvite, etc.)
│   │   ├── tables/               # Game search, event logs
│   │   ├── TopBar/               # Header
│   │   ├── NavigationDrawer/     # Sidebar with game sections
│   │   ├── routing/              # Route guards
│   │   └── notifications/        # Error/success toasts
│   ├── src/controllers/          # API orchestration (9 files)
│   ├── src/hooks/                # selectors.ts only
│   ├── src/model/                # TypeScript types
│   ├── src/redux/                # 8 slices (old-style Redux)
│   ├── src/styles/               # Material-UI theme (dark)
│   ├── src/utilities/            # api, routes, logging, collections
│   ├── public/                   # Static assets, manifest.json, env.json
│   └── package.json              # CRA 3.4.3, React 16.13
├── web/                          # Legacy frontend (to be removed)
├── build.fsx                     # FAKE build script
├── docker-compose.yml            # Local dev stack (MSSQL + API + web)
├── .github/workflows/            # CI/CD pipelines
├── CLAUDE.md                     # Project vision and Claude guidelines
├── tasks.md                      # Task tracking
├── CONTRIBUTING.md               # GitFlow branching rules
├── Deployment.md                 # AWS architecture docs
└── readme.md                     # Project overview
```

## Codebase Quality Assessment

### Backend: 6.5/10
**Strengths**: Excellent architecture (layered DI, event sourcing, privilege-based security), clean F# idioms, good integration test coverage
**Weaknesses**: .NET 3.1 EOL, GameManager god object, frozen dependencies, token logging vulnerability

### Frontend: 7/10
**Strengths**: All functional components with hooks, strict TypeScript 5.7, React 18, MUI v7 with sx prop, Vite 5 build, solid Konva.js board rendering, good separation of concerns, PWA enabled with offline support, lazy-loaded routes, frictionless auth flow
**Weaknesses**: <10% test coverage, old-style Redux (not Redux Toolkit), some pre-existing ESLint warnings (stale disable comments)

### Infrastructure: 5/10
**Strengths**: Multi-stage Docker builds, proper CI/CD triggers, environment-based config
**Weaknesses**: Node 10.x in CI, deprecated AWS CLI, no IaC, no staging environment, MSSQL/MySQL mismatch

### What's Salvageable
- Backend architecture and game logic (modernize framework, keep patterns)
- Board rendering (Konva.js implementation is solid)
- Component structure (just needs React/MUI version bumps)
- TypeScript strict mode config
- Event sourcing pattern
- API design and endpoint structure

### What Needs Replacement
- Redux: hand-rolled → Redux Toolkit (or Zustand)
- Node in CI: 10 → 20 LTS
- Testing: needs comprehensive expansion
- Backend hosting layer: manual WebHostBuilder → minimal hosting (Phase 7b)
