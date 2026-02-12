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
- [>] Review and finalize implementation plan

## Phase 1: Critical Bug Fixes
> Fix showstopper bugs in the current codebase before any modernization.

- [ ] Fix `useEffect` infinite loop in `web2/src/components/pages/HomePage.tsx` (missing dependency array)
- [ ] Fix `useEffect` infinite loop in `web2/src/components/pages/GamePlayPage.tsx` (missing dependency array)
- [ ] Fix `useEffect` infinite loop in `web2/src/components/pages/GameDiplomacyPage.tsx` (same pattern)
- [ ] Audit all other `useEffect` calls for missing dependency arrays
- [ ] Fix session token logging vulnerability in `api/api.web/SessionContextProvider.fs` (line 25 — logs full token)

## Phase 2: Frontend Modernization — Build System
> Migrate from Create React App 3.4 to Vite. This unblocks all other frontend upgrades.

- [ ] Create Vite config (`vite.config.ts`) to replace CRA
- [ ] Migrate `public/` assets to Vite public directory structure
- [ ] Update `index.html` to Vite entry point format
- [ ] Replace `react-scripts` scripts in `package.json` with Vite equivalents
- [ ] Update `tsconfig.json` target from ES5 to ES2020+
- [ ] Verify env.json runtime config loading works with Vite
- [ ] Remove `react-scripts` dependency
- [ ] Verify build output matches CRA output (assets, routing, etc.)

## Phase 3: Frontend Modernization — Dependencies
> Upgrade React, Redux, Material-UI, and other core dependencies.

- [ ] Upgrade React 16 → 18
  - [ ] Update `react` and `react-dom` packages
  - [ ] Replace `ReactDOM.render()` with `createRoot()` in index.tsx
  - [ ] Fix any breaking changes in components
  - [ ] Update `@testing-library/react` to v14+
- [ ] Upgrade TypeScript 3.9 → 5.x
  - [ ] Update `tsconfig.json` for new TS features
  - [ ] Fix any new strict mode errors
- [ ] Migrate Redux to Redux Toolkit (or evaluate Zustand as lighter alternative)
  - [ ] Install `@reduxjs/toolkit`
  - [ ] Convert slices one at a time: `session` → `activeGame` → `notifications` → rest
  - [ ] Replace manual action types/creators/reducers with `createSlice`
  - [ ] Add Redux DevTools integration
- [ ] Upgrade Material-UI v4 → MUI v5
  - [ ] Install `@mui/material`, `@mui/icons-material`
  - [ ] Replace `@material-ui/core` imports
  - [ ] Migrate `makeStyles` → `styled` or `sx` prop
  - [ ] Update theme from `createMuiTheme` → `createTheme`
- [ ] Upgrade React Router 5 → 6
  - [ ] Replace `<Switch>` with `<Routes>`
  - [ ] Replace `<Route component=...>` with `<Route element=...>`
  - [ ] Update route params access
  - [ ] Add route-based lazy loading (`React.lazy` + `Suspense`)
- [ ] Upgrade Konva.js 7 → latest
- [ ] Upgrade ESLint to v9+ with flat config
- [ ] Add Prettier for code formatting

## Phase 4: PWA Enablement
> Make the app installable on Android/iOS with offline support.

- [ ] Enable service worker in `index.tsx` (`serviceWorker.register()`)
- [ ] Implement proper service worker caching strategy
  - [ ] Cache-first for static assets (JS, CSS, images)
  - [ ] Network-first for API calls
  - [ ] Offline fallback page
- [ ] Update `manifest.json`
  - [ ] Add 512x512 icon
  - [ ] Add maskable icon for Android
  - [ ] Add screenshots for install prompt
  - [ ] Set proper `scope` and `start_url`
  - [ ] Add `display_override: ["standalone", "window-controls-overlay"]`
- [ ] Add `<meta name="apple-mobile-web-app-capable">` and iOS meta tags
- [ ] Implement "Add to Home Screen" prompt UI
- [ ] Test installability on Android (Chrome) and iOS (Safari)
- [ ] Add Web App Manifest validation to CI
- [ ] Implement app update notification (new version available)

## Phase 5: Mobile-First Responsive Design
> Ensure the game works well on phone and tablet screens.

- [ ] Audit all pages for mobile responsiveness
- [ ] Make navigation drawer responsive (full sidebar on desktop, bottom nav or hamburger on mobile)
- [ ] Make game board responsive (pinch-to-zoom, fit-to-screen on Konva canvas)
- [ ] Make forms mobile-friendly (proper input types, touch targets)
- [ ] Add viewport-aware board sizing
- [ ] Test on various screen sizes (320px to 1440px+)
- [ ] Handle safe areas for notched phones (env(safe-area-inset-*))

## Phase 6: Real-Time WebSocket Integration
> Connect the frontend to the existing backend WebSocket infrastructure.

- [ ] Establish WebSocket connection on app load (`/api/notifications/ws`)
- [ ] Handle WebSocket lifecycle (connect, reconnect, disconnect)
- [ ] Parse `StateAndEventResponse` messages and update Redux state
- [ ] Show real-time game updates without page refresh
- [ ] Add connection status indicator in UI
- [ ] Handle offline → online reconnection gracefully
- [ ] Test with multiple concurrent players

## Phase 7: Backend Modernization
> Upgrade from EOL .NET 3.1 to .NET 8 LTS.

- [ ] Update all `.fsproj` TargetFramework from `netcoreapp3.1` / `netstandard2.1` to `net8.0`
- [ ] Update NuGet packages to .NET 8 compatible versions
  - [ ] Pomelo.EntityFrameworkCore.MySql → latest
  - [ ] Serilog → 4.x
  - [ ] Swashbuckle → 6.x
  - [ ] Newtonsoft.Json → 13.x (or migrate to System.Text.Json)
- [ ] Update `Startup.fs` for .NET 8 minimal hosting (or keep traditional if simpler)
- [ ] Update Dockerfile base images from `dotnet/core/sdk:3.1` → `dotnet/sdk:8.0`
- [ ] Fix any F# language/library breaking changes
- [ ] Run all backend tests and fix failures
- [ ] Update `docker-compose.yml` for .NET 8
- [ ] Refactor `GameManager` into separate managers (break up god object)
- [ ] Fix token logging in SessionContextProvider
- [ ] Add rate limiting middleware

## Phase 8: CI/CD Modernization
> Update GitHub Actions to use current tool versions.

- [ ] Update Node version in all workflows: 10.x → 20.x
- [ ] Fix deprecated `aws ecr get-login` → `get-login-password` in `api-deploy.yml`
- [ ] Pin S3/CloudFront action versions (replace `@master` references)
- [ ] Update Java version in API contract check workflow
- [ ] Add web2 Dockerfile for Docker Compose parity
- [ ] Add manual approval gate to `sql-migration.yml`
- [ ] Add type checking (`tsc --noEmit`) to web2 quality gates
- [ ] Update `docker-compose.yml` to include web2 instead of legacy web

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

## Phase 10: AI Player Integration
> Server-side AI using Cicero-style architecture. See CLAUDE.md for full design.

- [ ] Define Djambi action space (all legal moves + social actions)
- [ ] Build game state encoder (board → tensor with social signals)
- [ ] Create AI service (Python, separate from F# API)
- [ ] Implement AI player API integration using `Neutral` PlayerKind
- [ ] Start with rule-based heuristic AI (playable but not smart)
- [ ] Collect training data from games
- [ ] Train base strategy model (supervised learning)
- [ ] Add social reasoning (rule-based → learned)
- [ ] Self-play RL training infrastructure

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

**Active work**: Phase 0 (documentation) → Phase 1 (critical fixes) → Phase 2 (Vite migration)

The strategy is: fix bugs first, modernize the build system, then incrementally upgrade dependencies while keeping the app functional at every step. PWA enablement (Phase 4) is the key milestone — once the app is installable, all other improvements layer on top.
