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
