# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project: Djambi-N (Djamb.io)

Djambi-N is an online multiplayer strategy board game in curved space, based on [Djambi](https://en.wikipedia.org/wiki/Djambi) — historically known as "Machiavelli's Chessboard." It supports 3-8 players on dynamically sized hexagonal boards. The game is fundamentally about multi-agent strategic reasoning where political dynamics (alliance formation, betrayal, implicit threats) are inseparable from board tactics.

**Live site**: [djambi-n.com](https://djambi-n.com/)

## Product Goal

Build a **multi-platform progressive web app (PWA)** that:
- Can be installed as a standalone app on **Android** and **iOS** (via Add to Home Screen / TWA)
- Works fully in any modern **browser**
- Renders all content **client-side** (the server provides API + AI inference only)
- Supports **real-time multiplayer** via WebSockets
- Eventually includes **AI players** with social reasoning capabilities (Cicero-style architecture)

## Repository Status

This is a **fork of an abandoned project** (last active development ~2020). The architecture is solid but the tech stack is severely outdated. Major modernization is required before production use. See `tasks.md` for the full roadmap.

## Documentation Structure

| File | Contents |
|------|----------|
| `CLAUDE.md` | This file — project vision, goals, and guidelines for Claude |
| `repo_doc.md` | All technical documentation: architecture, commands, config, API, data models, codebase assessment |
| `tasks.md` | Task tracking: phases, todos, sub-todos, progress markers |

## Guidelines for Claude

1. **Check `tasks.md` first** — understand what's in progress before making changes
2. **Read `repo_doc.md`** for technical context on any area you're modifying
3. **Frontend work goes in `web2/`** — `web/` is legacy and will be removed
4. **Don't edit `web2/src/api-client/`** — auto-generated from OpenAPI spec
5. **Respect F# file ordering** — `.fsproj` files list sources in compilation order; dependencies must come first
6. **Run quality gates before pushing** — `dotnet test` for backend, `npm run lint && npm test` for frontend
7. **Prioritize PWA compatibility** — all new frontend work should consider offline-first, installability, and mobile responsiveness
8. **Keep it simple** — don't over-engineer; this codebase needs modernization, not more abstraction layers

## Development Environment

### Node.js Requirement
The frontend requires **Node.js 22+**. The system Node on this machine is v12 (too old for all modern tooling). Node 22 is installed at `~/.local/node/`. A `.nvmrc` file in `web2/` specifies the version.

To run any Node tool, use the absolute path since the system PATH resolves to v12:
```bash
/home/ole/.local/node/bin/node <script>
```

For example:
```bash
# TypeScript check
/home/ole/.local/node/bin/node node_modules/typescript/lib/tsc.js --noEmit

# Vite build
/home/ole/.local/node/bin/node node_modules/vite/bin/vite.js build

# Run tests
/home/ole/.local/node/bin/node node_modules/vitest/vitest.mjs run

# ESLint
/home/ole/.local/node/bin/node node_modules/eslint/bin/eslint.js .

# Prettier
/home/ole/.local/node/bin/node node_modules/prettier/bin/prettier.cjs --write 'src/**/*.{ts,tsx,css}'

# npm install (must use npm from Node 22)
/home/ole/.local/node/bin/node /home/ole/.local/node/lib/node_modules/npm/bin/npm-cli.js install
```

**Do NOT use `npx`** — it uses `#!/usr/bin/env node` which resolves to system Node v12.

### Browser Automation (Chrome DevTools MCP)

Claude Code can inspect the running frontend via Chrome DevTools Protocol. Setup:

1. **Chromium** is installed as a Flatpak (`org.chromium.Chromium`). It must be launched with remote debugging enabled:
   ```bash
   flatpak run org.chromium.Chromium --remote-debugging-port=9222
   ```

2. **MCP config** at `.claude/mcp.json` connects the `chrome-devtools-mcp` server to the running instance:
   ```json
   {
     "mcpServers": {
       "chrome-devtools": {
         "command": "/home/ole/.local/node/bin/npx",
         "args": ["-y", "chrome-devtools-mcp@latest", "--browserUrl", "http://127.0.0.1:9222"]
       }
     }
   }
   ```
   Uses `/home/ole/.local/node/bin/npx` (Node 22) instead of system `npx` (Node 12).

3. **Startup order**: Chromium must be running with `--remote-debugging-port=9222` **before** Claude Code starts, so the MCP server can connect on launch.

4. **Verify** the debug port is live: `curl -s http://localhost:9222/json/version` should return browser info.

### Frontend Tech Stack (web2/)
| Tool | Version | Config File |
|------|---------|-------------|
| React | 18 | — |
| TypeScript | 5.7 | `tsconfig.json` (`jsx: "react-jsx"`, `moduleResolution: "bundler"`) |
| Vite | 5 | `vite.config.ts` |
| vite-plugin-pwa | 1.2 | `vite.config.ts` (Workbox generateSW) |
| MUI | 7 | `@mui/material` + `@emotion/react` + `@emotion/styled` (uses `sx` prop, no `@mui/styles`) |
| React Router | 6 | Route lazy loading with `React.lazy` + `Suspense` |
| Redux | 4 | Not yet migrated to Redux Toolkit |
| Vitest | 1.6 | `vite.config.ts` (`globals: true`, `environment: 'jsdom'`) |
| ESLint | 9 | `eslint.config.mjs` (flat config) |
| Prettier | 3 | `.prettierrc` (single quotes, trailing commas, 100 width) |
| Konva | 9 | Canvas rendering for game board |

### Frontend Commands (from web2/)
```bash
npm run build    # tsc --noEmit && vite build
npm run dev      # vite dev server on port 3000
npm test         # vitest run
npm run lint     # eslint .
npm run format   # prettier --write src/**
```

### Backend (.NET 8 / F#)
The API is an F# ASP.NET Core project targeting `net8.0`. It uses Pomelo.EntityFrameworkCore.MySql 8.0 with MySQL 8.0 (via Docker). The backend compiles and runs but needs a targeted rewrite of the hosting/plumbing layer — the game logic (`api.logic/`) is solid and should be preserved.

```bash
# Start full stack (DB + API + frontend)
./run_server.sh --full-stack

# Start frontend only
./run_server.sh

# Build backend
dotnet build api/api.host/api.host.fsproj

# Run backend tests
dotnet test api/tests/api.unitTests/api.unitTests.fsproj
dotnet test api/tests/api.integrationTests/api.integrationTests.fsproj
```

### Known Issues
- **App.test.tsx fails**: Test needs `ThemeProvider` wrapper — pre-existing, not blocking
- **25 ESLint warnings**: Stale `eslint-disable` comments for rules removed in flat config — safe to clean up incrementally
- **Backend needs rewrite**: Hosting/plumbing layer uses outdated patterns (manual `WebHostBuilder`, no minimal hosting). Game logic is sound. See `tasks.md` Phase 7.

## User Preferences & Workflow

### Git Commit Strategy
- **Always create multiple commits** — separate features/changes logically rather than one large commit
- Group related changes together (e.g., one commit for a dependency upgrade, another for the code changes it requires)
- Write clear, descriptive commit messages
- **Never commit real server IPs, passwords, or secrets** — use placeholders and reference environment variables or GitHub secrets

### Writing Documentation & Instructions
- **Self-contained copy-paste blocks** — when writing setup guides, deployment docs, or how-to instructions, inline everything the reader needs right where they need it. Don't say "see file X for the values" and make them cross-reference — put the actual values, commands, and config directly in the step.
- **Append-friendly over edit-in-place** — when modifying config files, prefer appending a block to the end rather than asking the reader to find and edit specific lines scattered through a large file. Most config formats use "last value wins".
- **Assume the reader follows top-to-bottom** — each step should be runnable in sequence without jumping ahead or back. If step 5 depends on something from step 2, repeat the relevant info rather than saying "as configured in step 2".
- **No jargon without context** — if a step says "apply tuning parameters", show exactly what to run. If it says "edit the config", show the exact commands and content.

### Bash Scratchpad for Multi-Command Scripts
- **Use a scratchpad file** (e.g., `/tmp/djambi_test.sh`) when combining multiple bash commands into a test or verification script. Write the script to the file with the Write tool, then execute it with `bash /tmp/djambi_test.sh`. This avoids needing individual approval for each command and makes multi-step workflows (start server, wait for health check, run curl tests, kill server) a single reviewable unit.

### Bug-Driven Testing Policy
- **Every bug reported by the user must get a thorough regression test** before or alongside the fix
- Tests should be resilient: not break on simple refactors, but also not just test one narrow case
- Think through edge cases, boundary conditions, and related scenarios
- Test the behavior, not the implementation — if a function name changes, the test should still validate the concept
- Include both the exact failing case and reasonable variations

## Multi-Agent Team Workflow

### Team Structure
- **Optimal structure**: 3-5 specialist agents + 1 coordinating team lead
- Each specialist should own a clear domain with minimal overlap
- Give each agent a specific role, deliverable format, and file path to prevent overlap and confusion
- The team lead should NOT do specialist work — focus on orchestration, conflict resolution, and synthesis

### Model Selection for Agents
Choose the model per agent based on what the task demands. This is about cost efficiency, not hard rules — use judgment.

**Opus** is worth the cost when the task involves:
- Debugging, complex problem solving, multi-step reasoning
- Architecture decisions or resolving conflicting requirements
- Code that touches tricky logic, concurrency, security, or subtle edge cases
- Synthesizing information across many sources into coherent decisions

**Sonnet** is the right pick when the task is primarily:
- Writing documentation, analysis, or other prose
- Generating straightforward, well-defined code (CRUD endpoints, boilerplate, tests from a clear spec)
- Cross-review and refinement of existing drafts
- Translating requirements into structured output (config files, migration scripts, data models)

**Haiku** makes sense for:
- Quick lookups, file searches, formatting, or validation checks
- Simple boilerplate or template generation
- Tasks where speed matters more than nuance

The goal is to avoid running 5 Opus agents when 3 of them are writing docs. Match the model to the cognitive demand of the task.

### The 2-3 Round Review Pattern
1. **Round 1**: Independent specialist work with clear deliverable and file path
2. **Round 2**: Each specialist reads ALL other outputs and refines their own, with specific cross-references called out by the coordinator. This is the biggest value-add — surfaces contradictions, sharpens estimates, creates interdisciplinary insights
3. **Round 3** (optional): Final coherence pass with explicit resolution of all open tensions. Diminishing returns beyond Round 2.

### Communication
- **Specific cross-team prompts outperform generic ones.** Messages that point to specific disagreements produce much better refinements than generic "review and update" instructions
- **Direct messages to specific agents are more effective and cheaper than broadcasts.** Use broadcasts only for universal policy changes
- **Direct agent-to-agent messaging resolves alignment issues faster** than routing everything through the team lead

### File Ownership
- **Assign each deliverable to exactly one agent.** Other agents provide input via messages, not direct file edits
- When spawning agent teams, assign non-overlapping file sets to avoid merge conflicts
- Documentation-only agents should never touch code/test files and vice versa
- CLAUDE.md updates should be done by the team lead after all agents finish

### Task Dependencies & Parallel Execution
- Use `blockedBy` for tasks that genuinely depend on prior work
- Don't block tasks that can start in parallel — let agents read partial outputs from peers
- All specialists working simultaneously saves significant time
- Pre-reading during idle time accelerates cross-review rounds

### Common Pitfalls
- **Agent context limits cause stalling** — keep documents focused; consider summary sections for cross-team consumption
- **Don't spawn agents before test infrastructure works** — multiple agents all running broken tests simultaneously is wasteful
- **Fix foundation first** — test output, CI, then feature work
- **File conflict risk is real** — exclusive file ownership is critical
- **Cold-start for agents requires full context** — include all critical context (file paths, team decisions, specific cross-references) in every round's message
- **Linters/hooks can revert changes** — work WITH the linter, not against it

## Future Direction: AI Players via Cicero-Style Architecture

### Inspiration
Meta's [Cicero](https://github.com/facebookresearch/diplomacy_cicero) plays the board game Diplomacy at human level by combining a language model (for negotiation) with a game-theoretic planning engine (for strategy). The repo is archived but the architecture and pretrained models are available under CC-BY-NC 4.0.

### Why This Matters for Djambi-N
Djambi has been described academically as "Machiavelli's Chessboard" — a game fundamentally about "subversion, duplicity, lying and denial" where political dynamics are inseparable from board tactics. Djambi and Diplomacy share the same core challenge: **multi-agent strategic reasoning** where you must model what opponents will do, form temporary alliances, and betray at the right moment. Cicero's planning engine solves exactly this class of problem. Critically, both games involve a social/negotiation dimension: in Diplomacy, structured negotiation rounds precede simultaneous orders; in Djambi, negotiation is asynchronous and informal — happening between turns through chat, implied threats via piece positioning, and status signaling (draw offers, concession warnings).

### What Transfers Directly
- **Strategic planning engine** (`fairdiplomacy/agents/`) - Bilateral and correlated search for multi-player games
- **Base strategy model** (`fairdiplomacy/models/base_strategy_model/`) - Supervised learning + RL via self-play
- **Self-play infrastructure** (`fairdiplomacy/selfplay/`) - RL training loop
- **Agent architecture** - Modular agent specification via protobuf configs

### What Needs Adaptation
| Cicero Component | Djambi Adaptation Needed |
|-----------------|-------------------------|
| Diplomacy map (fixed 75 territories) | Hexagonal board with 3-8 player configurations, curved topology |
| 7 identical unit types | 7 distinct piece types with unique movement and capture rules |
| Simultaneous moves per turn | Sequential turns (one player moves at a time) |
| NLP negotiation model (`parlai_diplomacy/`) | Needs adaptation for asynchronous informal negotiation (chat, implied threats, status signaling via `AcceptsDraw`/`WillConcede`) |
| Fixed 7-player game | Variable 3-8 players with dynamically sized boards |

### What Does NOT Transfer
- **webDiplomacy.net integration** - Game-specific UI/protocol code
- **Structured negotiation message format** - Djambi negotiation is unstructured natural language and implicit board signals

### Social Reasoning Is Core to Djambi
The NLP/negotiation component was originally considered irrelevant. This was incorrect. Djambi — described in academic literature as a "Foucauldian chessboard" — is fundamentally a game of political dynamics where social reasoning is as important as tactical play.

**Existing codebase evidence:**
- `web2/src/components/pages/GameDiplomacyPage.tsx` — stub diplomacy page, indicating this was planned from the start
- `PlayerStatus` enum includes `AcceptsDraw` and `WillConcede` — implicit social signals
- Draw coordination requires all living players to independently accept — this IS negotiation mediated through game mechanics
- WebSocket + SSE infrastructure exists, ready to be extended for chat/messaging

**How negotiation differs:**
| Aspect | Diplomacy | Djambi |
|--------|-----------|--------|
| Timing | Structured rounds before simultaneous moves | Asynchronous, between sequential turns |
| Format | Formal messages with game-specific grammar | Unstructured chat + implicit board signals |
| Social signals | Explicit proposals (alliance, support) | Piece positioning as threats, draw/concede timing |
| Commitment | Non-binding agreements | Visible, revocable status changes |

### Architecture Sketch

```
┌───────────────────────────────────────────────────┐
│              Djambi AI Agent                       │
├───────────────────────────────────────────────────┤
│  Social Reasoning Layer                            │
│  (Adapted from Cicero's parlai_diplomacy)          │
│  - Interprets implicit social signals              │
│  - Models opponent social intent                   │
│  - Generates chat / signals AcceptsDraw            │
├───────────────────────────────────────────────────┤
│  Intent Prediction Model                           │
│  - Trained on game logs + self-play RL             │
│  - Incorporates social signals as input            │
├───────────────────────────────────────────────────┤
│  Strategic Search Engine                           │
│  (Adapted from Cicero's bilateral search)          │
│  - Evaluates positions, models opponents           │
│  - Integrates social reasoning into search         │
├───────────────────────────────────────────────────┤
│  Djambi Action Space + Game State Encoder          │
│  - 7 piece types, hex board, social actions        │
│  - Board → tensor with social signal features      │
└───────────────────────────────────────────────────┘
```

### Key Technical Decisions (Open)
- **Board representation**: Flat hex grid vs. graph neural networks (curved topology)
- **Social reasoning complexity**: Rule-based heuristics vs. learned models
- **Chat system scope**: Full NL chat (LLM) vs. structured signals only (UI buttons)
- **Serving**: Separate Python service vs. embedded in F# API

### References
- Paper: "Human-level play in the game of Diplomacy by combining language models with strategic reasoning" (Meta AI, Science 2022)
- Repo: https://github.com/facebookresearch/diplomacy_cicero (archived April 2025)
- Djambi as political game: Academic literature frames it as "Machiavelli's Chessboard" / "the Foucauldian chessboard"
- The Diplodocus variant (no-press, strategy-only) represents a **lower bound** for Djambi AI, not the target
