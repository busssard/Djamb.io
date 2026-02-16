# tasks.md — Project Roadmap & Task Tracking

**Goal**: Multi-platform PWA installable on Android/iOS, accessible via browser, AI hosted server-side, content rendered client-side.

**Legend**: `[ ]` todo | `[x]` done | `[>]` in progress | `[-]` blocked/skipped

---

## Phase 0: Documentation & Planning
- [x] Create CLAUDE.md with project overview and Claude guidelines
- [x] Add Cicero-style AI architecture section
- [x] Correct social reasoning treatment (NLP IS relevant to Djambi)
- [x] Restructure docs: CLAUDE.md (general) + repo_doc.md (technical) + tasks.md (tracking)
- [x] Comprehensive codebase quality assessment
- [x] Review and finalize implementation plan

## Phase 1: Critical Bug Fixes
> Fix showstopper bugs in the current codebase before any modernization.

- [x] Fix `useEffect` infinite loop in `web2/src/components/pages/HomePage.tsx` — added `[user?.name]` deps
- [x] Fix `useEffect` infinite loop in `web2/src/components/pages/GamePlayPage.tsx` — added `[game?.id, game?.status, gameId]`
- [x] Fix `useEffect` infinite loop in `web2/src/components/pages/GameDiplomacyPage.tsx` — added deps
- [x] Audit all `useEffect` calls — fixed 9 files total (GamePage, GameInfoPage, GameSnapshotsPage, GameLobbyPage, GameOutcomePage, CanvasCellsLayer)
- [x] Fix CanvasCellsLayer animation leak — added `[]` deps + cleanup `return () => { a.stop(); }`
- [x] Fix session token logging vulnerability — removed {Properties} from Serilog template (Phase 7b)

## Phase 2: Frontend Modernization — Build System
> Migrate from Create React App 3.4 to Vite. This unblocks all other frontend upgrades.

- [x] Create Vite config (`vite.config.ts`) — React plugin with classic JSX runtime for React 16
- [x] Move `index.html` from `public/` to root, add `<script type="module">` entry point
- [x] Add iOS PWA meta tags (`apple-mobile-web-app-capable`, `apple-mobile-web-app-status-bar-style`)
- [x] Replace `react-scripts` scripts with Vite equivalents (`dev`, `start`, `build`, `preview`)
- [x] Update `tsconfig.json` — target ES2020, moduleResolution bundler, Vite/Vitest types
- [x] Replace `process.env` with `import.meta.env` in serviceWorker.ts
- [x] Remove `react-scripts` dependency, CRA eslintConfig, browserslist
- [x] Remove `react-app-env.d.ts`, add `vite-env.d.ts`
- [x] Upgrade TypeScript 3.9 → 4.9, ESLint 6 → 8, @typescript-eslint 3 → 5
- [x] Add Vitest for testing (replaces CRA's hidden Jest config)
- [x] Verify build succeeds — 7267 modules, 474KB output (148KB gzip)

## Phase 3: Frontend Modernization — Dependencies
> Upgrade React, Redux, Material-UI, and other core dependencies.

- [x] Upgrade React 16 → 18
  - [x] Update `react` and `react-dom` packages to ^18.3.1
  - [x] Replace `ReactDOM.render()` with `createRoot()` in index.tsx
  - [x] Upgrade `react-konva` v16 → v18.2.14 for React 18 compatibility
  - [x] Update `@testing-library/react` to v16
- [x] Upgrade TypeScript 4.9 → 5.7
  - [x] Update `tsconfig.json` for new TS features (bundler moduleResolution, jsx: react-jsx)
  - [x] No new strict mode errors
- [ ] Migrate Redux to Redux Toolkit (or evaluate Zustand as lighter alternative)
  - [ ] Install `@reduxjs/toolkit`
  - [ ] Convert slices one at a time: `session` → `activeGame` → `notifications` → rest
  - [ ] Replace manual action types/creators/reducers with `createSlice`
  - [ ] Add Redux DevTools integration
- [x] Upgrade Material-UI v4 → MUI v7
  - [x] Install `@mui/material`, `@mui/icons-material`, `@mui/lab`, `@emotion/react`, `@emotion/styled`, `@mui/styles`
  - [x] Replace `@material-ui/core` → `@mui/material` imports across 43 files
  - [x] Replace `@material-ui/icons` → `@mui/icons-material` (5 files)
  - [x] Replace `@material-ui/lab` → `@mui/material` (Alert graduated)
  - [x] Migrate `makeStyles`/`withStyles` → MUI v7 `sx` prop and `styled` API (removed `@mui/styles` entirely)
  - [x] Update theme: `createMuiTheme` → `createTheme`, `palette.type` → `palette.mode`
  - [x] Fix Grid API for MUI v7: `item`/`xs` → `size="grow"`
  - [x] Fix `ListItem button` → `ListItemButton`
  - [x] Remove old `@material-ui/*` packages
- [x] Upgrade React Router 5 → 6
  - [x] Replace `<Switch>` with `<Routes>`
  - [x] Replace `<Route component=...>` / `<Route render=...>` with `<Route element=...>`
  - [x] Replace `Redirect` with `Navigate`
  - [x] Add `useParams()` hook wrappers for game page route params
  - [x] Add route-based lazy loading (`React.lazy` + `Suspense`) — 17 page components lazy-loaded, 28 output chunks
- [x] Upgrade Konva.js 7 → 9
  - [x] Update type import paths from `konva/types/*` to `konva/lib/*`
- [x] Upgrade ESLint to v9+ with flat config (`eslint.config.mjs`)
- [x] Add Prettier for code formatting (`.prettierrc`, `npm run format`)

## Phase 4: PWA Enablement
> Make the app installable on Android/iOS with offline support.
> **Current state**: CRA boilerplate service worker in `web2/src/serviceWorker.ts` (explicitly `unregister()`'d in `index.tsx`). Manifest at `web2/public/manifest.json` has only favicon.ico + logo192.png. iOS meta tags already in `index.html`. No Vite PWA plugin.

- [x] Replace CRA service worker with Vite PWA plugin
  - [x] Install `vite-plugin-pwa` and add to `web2/vite.config.ts`
  - [x] Configure Workbox with `generateSW` strategy
  - [x] Delete legacy `web2/src/serviceWorker.ts` (CRA boilerplate, 139 lines)
  - [x] Remove `serviceWorker.unregister()` call from `web2/src/index.tsx`
- [x] Configure caching strategies in `vite.config.ts` PWA plugin
  - [x] Cache-first for static assets (JS, CSS, images, fonts) — precache with globPatterns
  - [x] Network-first for API calls (`/api/*`) — `NetworkFirst` with 10s timeout fallback
  - [x] Precache app shell (index.html, main JS/CSS bundles) — 47 precache entries
  - [x] Set `navigateFallback: '/index.html'` for SPA routing
- [x] Create offline fallback page
  - [x] Design minimal offline page with "No connection" message and retry button (`public/offline.html`)
  - [x] Registered as fallback in service worker config
- [x] Complete `web2/public/manifest.json`
  - [x] Generate 512x512 app icon from existing logo192.png
  - [x] Generate maskable icon (safe zone padding) for Android adaptive icons
  - [x] Add icon entries: 48x48, 72x72, 96x96, 144x144, 192x192, 512x512
  - [x] Set `"start_url": "/"`
  - [x] Set `"scope": "/"`
  - [x] Add `"display_override": ["standalone", "minimal-ui"]`
  - [x] Set `"theme_color": "#161616"` (match app background)
  - [x] Add `"orientation": "any"`
  - [x] Add `"categories": ["games", "entertainment"]`
  - [ ] Add screenshots for richer install prompt (1 mobile, 1 desktop)
- [x] iOS-specific PWA support
  - [x] `apple-mobile-web-app-capable` meta tag — already in `index.html`
  - [x] `apple-mobile-web-app-status-bar-style` meta tag — already in `index.html`
  - [x] `apple-touch-icon` link tags with multiple sizes (72, 96, 144, 192)
  - [ ] Add `apple-touch-startup-image` for splash screen on iOS
- [x] Implement "Add to Home Screen" install prompt
  - [x] Listen for `beforeinstallprompt` event (`useInstallPrompt` hook)
  - [x] Show install button in TopBar (`InstallButton` component)
  - [ ] Track install outcome for analytics
- [x] Implement app update notification
  - [x] Detect new service worker via `registerSW` `onNeedRefresh` callback
  - [x] Show confirm prompt for update (simpler than snackbar for now)
  - [x] Handle reload on user confirmation
- [ ] Validation and testing
  - [ ] Run Lighthouse PWA audit — target all green checks
  - [ ] Test install flow on Android Chrome
  - [ ] Test install flow on iOS Safari (Add to Home Screen)
  - [ ] Verify offline mode shows fallback page (not browser error)
  - [ ] Verify app opens as standalone (no browser chrome)

## Phase 5: Mobile-First Responsive Design
> Ensure the game works well on phone and tablet screens.
> **Current state**: Zero `@media` queries in codebase. Board size (`CanvasBoard.tsx`) is passed as fixed `width`/`height`/`scale` props with no viewport awareness. No `resize` event listeners. Navigation is a desktop-style side drawer. Forms use hardcoded `width: '50%'`.

- [ ] Add responsive viewport foundation
  - [ ] Create shared breakpoint constants (align with MUI defaults: xs=0, sm=600, md=900, lg=1200)
  - [ ] Add `useWindowSize` hook (listen to `resize` event, return `{ width, height }`)
  - [ ] Add `useIsMobile` hook (returns true if `width < sm breakpoint`)
- [ ] Make game board responsive — `web2/src/components/Canvas/CanvasBoard.tsx`
  - [ ] Calculate board dimensions from viewport size instead of fixed values
  - [ ] Add `resize` listener to re-render `<Stage>` when window resizes
  - [ ] Cap board size to `min(viewportWidth, viewportHeight) - padding`
  - [ ] Implement pinch-to-zoom on touch devices (Konva supports `touchmove` events)
  - [ ] Implement drag-to-pan on mobile (Konva `Stage` draggable)
  - [ ] Add zoom controls (+ / - buttons) visible on mobile
  - [ ] Ensure tooltips (`CanvasTooltip.tsx`) work with touch (long-press → show, tap elsewhere → hide)
- [ ] Make navigation responsive
  - [ ] `NavigationDrawer.tsx`: Keep side drawer on desktop (>= 900px), use full-screen overlay on mobile
  - [ ] `TopBar.tsx`: Stack title below buttons on small screens, or hide title on xs
  - [ ] Ensure hamburger menu touch target is >= 48x48px (accessibility minimum)
  - [ ] Add swipe-to-open gesture for drawer on mobile
- [ ] Make forms mobile-friendly
  - [ ] `web2/src/styles/styles.ts`: Change `useFormStyles` button width from `'50%'` → responsive (100% on mobile, 50% on desktop)
  - [ ] All form pages (`SignInForm`, `CreateAccountForm`, `CreateGameForm`, `UserConfigForm`): Full-width on mobile, constrained max-width on desktop
  - [ ] Set appropriate `inputMode` on text fields (e.g., `inputMode="email"` for email fields)
  - [ ] Ensure touch targets are >= 48px height on all buttons and interactive elements
- [ ] Make tables mobile-friendly
  - [ ] `GameSearchResultsTable.tsx`: Horizontal scroll or card layout on narrow screens
  - [ ] `LobbyPlayersTable.tsx`: Stack columns or use card layout on mobile
  - [ ] `GameParametersTable.tsx`: Responsive column hiding or stacking
  - [ ] `InfoPlayersTable.tsx`: Responsive layout
- [ ] Make game pages responsive
  - [ ] `GamePlayPage.tsx`: Board fills available space, controls below on mobile / sidebar on desktop
  - [ ] `GameInfoPage.tsx`: Single-column layout on mobile
  - [ ] `GameLobbyPage.tsx`: Full-width player list on mobile
  - [ ] `HomePage.tsx`: Responsive game list / search results
- [ ] Handle device-specific concerns
  - [ ] Add `env(safe-area-inset-*)` padding for notched phones (iPhone X+)
  - [ ] Prevent double-tap zoom on game board (interferes with gameplay)
  - [ ] Prevent pull-to-refresh on game board (interferes with drag/pan)
  - [ ] Handle on-screen keyboard appearing (form pages shouldn't scroll weirdly)
  - [ ] Test landscape orientation (game board should rotate and fill space)
- [ ] Testing
  - [ ] Test at 320px width (iPhone SE / small Android)
  - [ ] Test at 375px width (iPhone 12/13/14)
  - [ ] Test at 768px width (iPad portrait)
  - [ ] Test at 1024px width (iPad landscape / small laptop)
  - [ ] Test at 1440px+ (desktop)
  - [ ] Test touch interactions on actual mobile device or emulator

## Phase 5.5: Lightweight Auth & Social Features
> Remove sign-up friction. Let players jump in with just a username, share game links with friends, and watch games in progress.
> **Replaces** the old password-based auth as the primary flow. Existing password users can still restore sessions via cookie.

### 5.5a. Anonymous auth — backend
- [x] Add `Email` column to `UserSqlModel` (nullable, max 254 chars)
- [x] Add `email : string option` to `User`, `UserDetails`, `CreateUserRequest` in `UserModel.fs`
- [x] Make `Password` column nullable in `UserSqlModel.cs`
- [x] Make `password` optional (`string option`) in `CreateUserRequest`
- [x] Update `UserRepository` to handle nullable password and email fields
- [x] Update `UserMappings.fs` to map email field
- [x] Update `UserManager.createUser` to skip password validation when password is None
- [x] Add `POST /api/users/quick` endpoint (AllowAnonymous): takes `{ name, email? }`, creates user + session, sets cookie
- [x] Add `quickRegister` method to `IUserManager` / `UserManager`
- [x] Add `QuickRegisterRequestDto` to web model DTOs

### 5.5b. Anonymous auth — frontend
- [x] Create `QuickJoinForm.tsx` — username + email (optional) fields
- [x] Create `QuickJoinPage.tsx` — wraps QuickJoinForm, becomes default unauthenticated landing
- [x] Add `quickJoin(name, email)` to `userController.ts`
- [x] Add `/join` route to `routes.ts` and `App.tsx`
- [x] Update `RedirectToSignInIfSignedOut` to redirect to `/join`
- [x] Remove old `/sign-in` and `/create-account` routes and forms
- [x] Update `NavigationDrawer` menu items

### 5.5c. Magic link email auth — backend
- [x] Create `MagicLinkSqlModel.cs` entity (Token, UserId, Email, CreatedOn, ExpiresOn, UsedOn)
- [x] Add `DbSet<MagicLinkSqlModel>` to `ApexDbContext`
- [x] Create `MagicLinkRepository.fs` (createToken, getByToken, markUsed)
- [x] Create `IEmailService.fs` interface (`sendMagicLink : email -> link -> Task<unit>`)
- [x] Create `ConsoleEmailService.fs` (logs magic link URL to Serilog for dev)
- [x] Register `IEmailService` in `Program.fs` DI
- [x] Add `POST /api/sessions/magic-link` endpoint: takes email, generates token, sends email (always returns 200)
- [x] Add `POST /api/sessions/magic-link/verify` endpoint: validates token, creates session, sets cookie

### 5.5d. Magic link email auth — frontend
- [x] Create `RequestMagicLinkForm.tsx` — email input + "Send login link" button
- [x] Create `MagicLinkPage.tsx` — form + success message
- [x] Create `MagicLinkVerifyPage.tsx` — route `/auth/verify/:token`, auto-verifies on load
- [x] Add `requestMagicLink(email)` and `verifyMagicLink(token)` to `userController.ts`
- [x] Add `/magic-link` and `/auth/verify/:token` routes
- [x] Add "Sign in on another device" link on `QuickJoinPage`

### 5.5e. Private games with invite links — backend
- [x] Add `InviteCode` column to `GameSqlModel` (nullable, max 12 chars, unique index)
- [x] Add `inviteCode : string option` to `Game` model
- [x] Generate random 8-char alphanumeric invite code in `GameCrudService` when `isPublic = false`
- [x] Add `GET /api/games/invite/{code}` (AllowAnonymous) — returns limited game info
- [x] Add `POST /api/games/invite/{code}/join` — joins game via invite code
- [x] Update `GameMappings.fs` and `GameDto` for invite code

### 5.5f. Private games with invite links — frontend
- [x] Create `JoinByInvitePage.tsx` — route `/invite/:code`, shows game info + join button
- [x] Add "Copy invite link" button to `GameLobbyPage` for private games
- [-] Update `CreateGameForm` — show invite link explanation when `isPublic` unchecked (deferred — minor UX polish)
- [x] Add `getGameByInvite(code)` and `joinByInvite(code)` to `gameController.ts`
- [x] Add `/invite/:code` route

### 5.5g. Spectator mode
- [-] Backend: Allow `GetGame` read access for non-players on public games (deferred — needs backend auth changes)
- [-] Backend: Allow spectator WebSocket/SSE connections for non-player viewers (deferred to Phase 6 WebSocket work)
- [x] Frontend: Detect spectator in `GamePlayPage` (user not in `game.players`)
- [x] Frontend: Hide turn controls, show read-only board with "Watching" indicator
- [-] Frontend: Add "Watch" button on in-progress games in `HomePage` (deferred — needs backend spectator access first)

### 5.5h. Enhanced lobby
- [x] Add invite link display + copy button for private games
- [x] Add player count indicator ("3/5 players")
- [-] Better layout with MUI Cards (deferred — cosmetic polish)
- [-] Player avatars (initials circles) (deferred — cosmetic polish)
- [-] Polling with `setInterval` every 3s for real-time updates (deferred to Phase 6 WebSocket work)

## Phase 5.6: Quick Wins — CI/CD, Tests & Cleanup
> Low-hanging fruit fixes: CI workflow bugs, outdated runtimes, test gaps, docs polish.

### CI/CD Fixes
- [ ] Fix broken workflow path filter typo: `reset-client-generator` → `rest-client-generator` in `.github/workflows/check-for-api-contract-changes.yml`
- [ ] Upgrade Node version in GitHub Actions workflows: 10.x → 20.x, `actions/setup-node@v1` → `v4`
- [ ] Replace deprecated AWS ECR login: `aws ecr get-login --no-include-email` → `get-login-password | docker login` in `api-deploy.yml`
- [ ] Upgrade Java setup in contract-check workflow: Java 9.0.4 + `actions/setup-java@v1` → LTS JDK + `v4`

### Test Gaps
- [ ] Add board geometry transform tests in `web2/src/board/point.test.ts` and `polygon.test.ts` (noted TODOs)
- [ ] Add GameManager auth/permission edge case tests (placeholder TODOs in integration test files)

### Docs & Cleanup
- [ ] Fix readme.md typo: "guidlines" → "guidelines"

## Phase 5.7: Frontend Redesign — Dark Gaming Theme & Lobby
> Modern dark gaming aesthetic (chess.com / boardgamearena.com style). Card-based layouts, proper lobby, spectator improvements.

### 5.7a. Design system — dark gaming theme
- [ ] Rewrite `web2/src/styles/materialTheme.ts` — gaming palette: background #0a0a0f, paper #12121a, primary cyan #00bcd4, secondary gold #ffd740
- [ ] Add custom `gaming` palette tokens: cardBg, cardBorder, cardHoverBorder, glow effects, status dot colors
- [ ] Add MUI component overrides: Paper, Card (hover glow), Button (gradient), AppBar, Chip
- [ ] Update `web2/src/index.css` — body gradient, scrollbar styling, selection color
- [ ] Create `web2/src/components/shared/GameCard.tsx` — game display card with status badge, player count, action button
- [ ] Create `web2/src/components/shared/StatusBadge.tsx` — colored dot + status text
- [ ] Create `web2/src/components/shared/SectionHeader.tsx` — section title with count badge + action
- [ ] Create `web2/src/components/shared/FilterBar.tsx` — chip-based filter row + search input

### 5.7b. Landing page (QuickJoinPage)
- [ ] Redesign hero section with themed logo (glow effect), heading, subtitle
- [ ] Style join form card with gradient background, themed inputs
- [ ] Theme feature highlights grid with new palette
- [ ] Style sign-in link for returning users

### 5.7c. Lobby (HomePage)
- [ ] Rewrite HomePage as card-based game browser
- [ ] Add "Your Active Games" section at top (games where user is a player)
- [ ] Add prominent "Create Game" button
- [ ] Add filter bar: All | Open | Live + text search
- [ ] Add "Public Games" card grid — Pending games show "Join", InProgress show "Watch"
- [ ] Parallel data fetch: my games + all public games via `searchGames()`

### 5.7d. Spectator improvements
- [ ] Add "Watch" button on InProgress game cards in lobby
- [ ] Improve spectator UI in GamePlayPage (styled badge, better indicator)

### 5.7e. Navigation & chrome
- [ ] Theme TopBar with gaming palette
- [ ] Theme NavigationDrawer with gaming palette

## Phase 5.8: Full-Stack Game Chat System
> In-game chat with separated player and spectator channels. Backend F# + frontend React.

### 5.8a. Chat backend — enum & model
- [ ] Add `ChatChannel` enum to `api/api.enums/Enums.fs` (Player=1, Spectator=2, All=3)
- [ ] Create `api/api.model/ChatModel.fs` — `ChatMessage`, `CreateChatMessageRequest`, `ChatMessagesQuery`
- [ ] Update `api/api.model/api.model.fsproj` — add ChatModel.fs

### 5.8b. Chat backend — database
- [ ] Create `api/api.db.model/Model/ChatMessageSqlModel.cs` — EF entity
- [ ] Add `DbSet<ChatMessageSqlModel>` to `ApexDbContext.cs`

### 5.8c. Chat backend — repository
- [ ] Add `IChatMessageRepository` to `api/api.db.interfaces/Interfaces.fs`
- [ ] Create `api/api.db/Mappings/ChatMappings.fs` — SQL↔domain mappings
- [ ] Create `api/api.db/Repositories/ChatMessageRepository.fs` — createMessage, getMessages
- [ ] Update `api/api.db/api.db.fsproj` — add new files

### 5.8d. Chat backend — manager
- [ ] Add `IChatMessageManager` to `api/api.logic.interfaces/Interfaces.fs`
- [ ] Create `api/api.logic/Managers/ChatMessageManager.fs` — sendMessage (validates text, checks player membership, enforces channel perms), getMessages (filters by role)
- [ ] Update `api/api.logic/api.logic.fsproj` — add ChatMessageManager.fs

### 5.8e. Chat backend — controller & DTOs
- [ ] Create `api/api.web/Model/ChatWebModel.fs` — SendChatMessageDto, ChatMessageDto, ChatMessagesQueryDto
- [ ] Create `api/api.web/Mappings/ChatWebMappings.fs` — DTO↔domain mappings
- [ ] Create `api/api.web/Controllers/ChatController.fs` — POST send + POST query endpoints
- [ ] Update `api/api.web/api.web.fsproj` — add new files

### 5.8f. Chat backend — DI & wiring
- [ ] Register `IChatMessageRepository` and `IChatMessageManager` in `api/api.host/Program.fs`

### 5.8g. Chat frontend — Redux module
- [ ] Create `web2/src/model/chat.ts` — ChatChannel enum, ChatMessage type
- [ ] Create `web2/src/redux/chat/` — state, actionTypes, actions, actionFactory, reducer
- [ ] Add chat slice to `web2/src/redux/root.ts`
- [ ] Add `selectChat` to `web2/src/hooks/selectors.ts`

### 5.8h. Chat frontend — controller & polling
- [ ] Create `web2/src/controllers/chatController.ts` — sendMessage, loadMessages, startPolling (2.5s), stopPolling

### 5.8i. Chat frontend — UI components
- [ ] Create `web2/src/components/chat/ChatPanel.tsx` — main container with tabs, messages, input
- [ ] Create `web2/src/components/chat/ChannelTabs.tsx` — Players | Spectators | All (filtered by role)
- [ ] Create `web2/src/components/chat/MessageList.tsx` — auto-scrolling list with player colors
- [ ] Create `web2/src/components/chat/MessageInput.tsx` — text field + send button

### 5.8j. Chat frontend — integration
- [ ] Add ChatPanel to GamePlayPage — side panel (desktop) / bottom drawer (mobile)
- [ ] Replace GameDiplomacyPage stub with full-page chat view

### 5.8k. Chat tests
- [ ] Backend: ChatMessageManager tests (channel permissions, text validation, role filtering)
- [ ] Frontend: chat reducer tests (MessagesLoaded, MessageSent, ClearChat, SetActiveChannel)

## Phase 6: Real-Time WebSocket Integration
> Connect the frontend to the existing backend WebSocket infrastructure.

- [ ] Establish WebSocket connection on app load (`/api/notifications/ws`)
- [ ] Handle WebSocket lifecycle (connect, reconnect, disconnect)
- [ ] Parse `StateAndEventResponse` messages and update Redux state
- [ ] Show real-time game updates without page refresh
- [ ] Add connection status indicator in UI
- [ ] Handle offline → online reconnection gracefully
- [ ] Test with multiple concurrent players

## Phase 7: Backend Modernization — Targeted Rewrite
> Strategy: **keep game logic** (`api.logic/`), **rewrite hosting/plumbing** layer with modern .NET 8 patterns.
> The game logic (board rules, piece movement, turn management) is solid and well-tested. The hosting layer
> (startup, middleware, DI, auth) uses outdated patterns from .NET Core 3.1 that are better rewritten than patched.

### 7a. Compatibility fixes (done)
- [x] Update all `.fsproj` TargetFramework from `netcoreapp3.1` / `netstandard2.1` to `net8.0`
- [x] Upgrade api.db.model from netcoreapp3.1 to net8.0 with EF Core 8.0 and Pomelo 8.0.0
- [x] Replace MySql.Data.MySqlClient → MySqlConnector namespace across all repositories
- [x] Add ServerVersion.AutoDetect() to all UseMySql() calls
- [x] Replace EF6 ObjectNotFoundException → custom NotFoundException (EF Core has no equivalent)
- [x] Remove Swashbuckle NewtonsoftJson support (dropped in 6.5+)
- [x] Replace builder.UseSerilog() → services.AddSerilog() (Serilog.AspNetCore 8.0 API)
- [x] Add EnsureCreated() for auto database schema creation
- [x] Switch Docker from MSSQL to MySQL 8.0 (matching codebase)
- [x] Fix run_server.sh for MySQL health checks and --no-launch-profile
- [x] Upgrade integration test packages to 8.0.0

### 7b. Full backend modernization (done)
- [x] Switch from EnsureCreated() to EF Core Migrations for schema management
- [x] Reset migration baseline to capture all Phase 5.5 additions (Email, MagicLink, InviteCode)
- [x] Add missing database indexes (Session.Token, MagicLink.Token, Game.InviteCode, User.Email)
- [x] Update design-time factory (optional config files, hardcoded MySQL version to avoid live DB requirement)
- [x] Update Dockerfile base images from `dotnet/core/sdk:3.1` → `dotnet/sdk:8.0`
- [x] Remove FSharp.Core 4.7.2 pin from all 9 fsproj files (SDK provides 8.x)
- [x] Remove TaskBuilder.fs 2.1.0 dependency + 46 `open FSharp.Control.Tasks` imports (native task CE)
- [x] Fix NotificationService DI lifetime: Scoped → Singleton (ConcurrentDictionary must persist)
- [x] Fix WebSocket handler: add read loop to keep connection alive (was single ReceiveAsync then exit)
- [x] Fix BotRunner: proper cancellation tokens, SemaphoreSlim overlap prevention, 5s polling, clean shutdown
- [x] Remove {Properties} from Serilog template (was leaking session tokens)
- [x] Remove hardcoded database password from api.db.model/appsettings.json
- [x] Add startup config validation (fail fast on missing ConnectionString)
- [x] Scope Swagger UI to Development environment only
- [x] Decompose GameManager god object into EventManager + PlayerManager + TurnManager + GameManager
- [x] Add shared EventProcessing module for event pipeline logic
- [x] Add correlation IDs (X-Correlation-Id header propagation + Serilog enrichment)
- [x] Update test packages (Test.Sdk 17.11, xunit 2.9, FakeItEasy 8.3)
- [x] Update integration test HostFactory for decomposed managers
- [x] Disable legacy web/ workflows (web-deploy.yml, web-quality-gates.yml)
- [x] Remove legacy web/ service from docker-compose.yml

### 7c. Architecture improvements (remaining)
- [ ] Modernize auth: replace custom session cookie system with ASP.NET Core Identity or JWT
- [ ] Add proper API versioning
- [ ] Consider migrating Newtonsoft.Json → System.Text.Json
- [ ] Add rate limiting middleware
- [ ] Define clear bot/AI player API interface (see Phase 10)

## Phase 7.5: Database Migration Governance
> Schema version policy, preflight checks, rollback plan. Important as features evolve.

- [ ] Document migration workflow: create → review → test against staging DB → apply
- [ ] Add preflight check script that validates pending migrations before apply
- [ ] Add rollback documentation (how to revert a migration)
- [ ] Add backup verification step before production migrations
- [ ] Consider adding `dotnet ef migrations script` to CI for reviewable SQL output

## Phase 7.6: Security Hardening Program
> Partially addressed in Phase 7b. Remaining items form a standalone epic.

- [ ] Add secret scanning to CI (prevent committing keys/passwords)
- [ ] Dependency vulnerability scanning (Dependabot or `dotnet list package --vulnerable`)
- [ ] CSRF hardening for cookie-based auth
- [ ] Session abuse protection (rate limiting on session creation, IP-based throttling)
- [ ] Threat model document for auth flows (quick-join, magic link, cookie sessions)

## Phase 7.7: Performance & Scale Testing
> Load/soak testing before WebSocket rollout. Slot before Phase 6.

- [ ] Set up load testing tool (k6, artillery, or similar)
- [ ] Define concurrent game target (e.g., 50 simultaneous games)
- [ ] Test WebSocket fanout performance (N subscribers per game)
- [ ] Profile database hotspots under load
- [ ] Soak test BotRunner with many concurrent bot games

## Phase 7.8: Product Analytics Foundation
> Event taxonomy and funnel metrics. Slot before Phase 11 (launch).

- [ ] Define event taxonomy (join, create-game, start-game, complete-game, install-pwa)
- [ ] Add funnel metrics (visit → join → create → play → complete)
- [ ] Evaluate analytics approach (self-hosted vs. service, privacy considerations)
- [ ] Feature flags infrastructure (for gradual rollout of AI players, chat, etc.)

## Phase 8: CI/CD Modernization
> Update GitHub Actions to use current tool versions.

- [ ] Update Node version in all workflows: 10.x → 20.x
- [ ] Fix deprecated `aws ecr get-login` → `get-login-password` in `api-deploy.yml`
- [ ] Pin S3/CloudFront action versions (replace `@master` references)
- [ ] Update Java version in API contract check workflow
- [ ] Add web2 Dockerfile for Docker Compose parity
- [ ] Add manual approval gate to `sql-migration.yml`
- [ ] Add type checking (`tsc --noEmit`) to web2 quality gates
- [x] Update `docker-compose.yml` — remove legacy web service, add depends_on for db

## Phase 9: Diplomacy & Chat System
> Build the social communication layer that Djambi needs.

- [ ] Design chat message data model (backend)
- [ ] Add chat endpoints to API
- [ ] Extend WebSocket/SSE to deliver chat messages
- [ ] Implement `GameDiplomacyPage.tsx` (replace stub)
  - [ ] In-game chat interface
  - [ ] Player status signals (AcceptsDraw, WillConcede) with UI controls
  - [ ] Alliance proposal UI (optional — may start with free-form chat only)
- [ ] Store chat history in database
- [ ] Add chat notification integration

## Phase 10: AI Player Integration & Bot Interface
> Server-side AI using Cicero-style architecture. See CLAUDE.md for full design.
> The bot interface is designed to support **multiple AI implementations** — from simple heuristics to Cicero-style agents — behind a common API contract.

### 10a. Bot Interface (API contract)
- [ ] Design `IBotPlayer` interface:
  - Input: game state (board, pieces, players, turn history, social signals)
  - Output: chosen move (piece, destination, optional social action)
  - Metadata: bot name, version, capabilities (supports_chat, supports_diplomacy)
- [ ] Define bot registration/discovery mechanism (config-based or plugin-based)
- [ ] Add bot API endpoints:
  - `POST /api/bots/register` — register a bot for a game
  - `GET /api/bots` — list available bots
  - `POST /api/bots/{id}/move` — request a move from a bot (or bot pushes via callback)
- [ ] Define game state serialization format for bot consumption (JSON schema)
- [ ] Add bot turn timeout handling (bot must respond within N seconds or forfeit turn)
- [ ] Support both synchronous (HTTP request/response) and asynchronous (WebSocket) bot communication

### 10b. Reference bot implementations
- [ ] **Random bot**: Picks a random legal move (testing/baseline)
- [ ] **Greedy bot**: Maximizes immediate material advantage (simple heuristic)
- [ ] **Positional bot**: Values center control + piece safety (intermediate heuristic)
- [ ] **Rule-based social bot**: Makes draw/concede decisions based on game state thresholds

### 10c. Advanced AI (Cicero-style)
- [ ] Define Djambi action space (all legal moves + social actions)
- [ ] Build game state encoder (board → tensor with social signals)
- [ ] Create AI service (Python, separate from F# API) implementing the bot interface
- [ ] Start with supervised learning on game logs
- [ ] Add social reasoning layer (interprets implicit signals, generates chat)
- [ ] Self-play RL training infrastructure
- [ ] Collect training data from human games

## Phase 11: Polish & Launch
> Final production readiness.

- [ ] Performance audit (Lighthouse, Web Vitals)
- [ ] Bundle size optimization (code splitting, tree shaking)
- [ ] Error boundaries on all route-level components
- [ ] Comprehensive E2E test suite
- [ ] Security audit (OWASP top 10)
- [ ] Accessibility audit (WCAG 2.1 AA)
- [ ] DNS/domain setup for production
- [ ] SSL/TLS configuration
- [ ] Monitoring and alerting
- [ ] User documentation / help pages

---

## Current Focus

**Active work**: Phase 5.6 (quick wins) → Phase 5.7 (frontend redesign) → Phase 5.8 (chat system).

**Execution order**:
1. Phase 5.6: CI/CD fixes + docs typo (quick batch commit)
2. Phase 5.7a: Design system — dark gaming theme (foundation for everything)
3. Phase 5.7b: Landing page redesign
4. Phase 5.7c: Lobby rewrite with game cards
5. Phase 5.7d-e: Spectator + navigation polish
6. Phase 5.6 tests: Board geometry + GameManager auth tests (alongside frontend work)
7. Phase 5.8a-f: Chat backend (F# endpoints, DB, permissions)
8. Phase 5.8g-j: Chat frontend (Redux, UI, integration)
9. Phase 5.8k: Chat tests

**Completed milestones**:
- Phase 0: Documentation restructured ✓
- Phase 1: Critical useEffect bugs fixed across 9 components ✓
- Phase 2: CRA → Vite migration ✓
- Phase 3 (core): React 18 + MUI v7 + React Router 6 + Konva 9 ✓
- Phase 3 (styles): Migrated all `@mui/styles` → MUI v7 `sx` prop/`styled` ✓
- Phase 4 (core): PWA with vite-plugin-pwa, install prompt, offline support ✓
- Phase 5.5 (core): Passwordless quick-join auth, magic link device transfer, private games with invite links, spectator mode, lobby enhancements ✓
- Phase 7a: Backend .NET 8 compatibility fixes ✓ (builds, runs, user creation + login works)
- Phase 7b: Backend full modernization ✓ (EF migrations, Dockerfile .NET 8, TaskBuilder removal, critical bug fixes, GameManager decomposition, correlation IDs, config hardening, test infrastructure)
- Phase 10a (partial): Bot interface + RandomBot + MinimaxBot + BotRunner ✓

**What works end-to-end**:
- `./run_server.sh --full-stack` starts MySQL (Docker) + API (.NET 8) + frontend (Vite)
- Quick-join (`POST /api/users/quick`) — username + optional email, no password needed
- Magic link email auth — request link (`POST /api/sessions/magic-link`), verify token (`POST /api/sessions/magic-link/verify`)
- Private game invite links — create private game, get invite code, share `/invite/:code` URL
- Spectator mode — non-players see read-only board with "Watching" indicator
- Legacy password auth deprecated — old `/sign-in` and `/create-account` routes removed
- Dev server on `http://localhost:3000`, API on `http://localhost:5100`
- AI bots (Random + Minimax) can play via BotRunner background service

**Deferred from Phase 5.5** (minor items, not blocking):
- Backend spectator access control (GetGame for non-players on public games)
- Spectator WebSocket/SSE connections (depends on Phase 6)
- CreateGameForm invite link explanation text
- These are tracked with `[-]` markers in Phase 5.5 above

**Strategy**: Design system + lobby + chat first (Phases 5.6-5.8), then responsive design (Phase 5), WebSocket real-time (Phase 6), backend rewrite (Phase 7b). Game logic (`api.logic/`) stays intact throughout.
