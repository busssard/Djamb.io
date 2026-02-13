#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# run_server.sh — Djambi-N local development server
#
# Usage:
#   ./run_server.sh                  Start frontend dev server (default)
#   ./run_server.sh --full-stack     Start database + API + frontend
#   ./run_server.sh --skip-install   Skip npm install (faster restart)
#   ./run_server.sh --help           Show this help
# ─────────────────────────────────────────────────────────────────────────────

# ── Resolve project root (directory where this script lives) ─────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB_DIR="$SCRIPT_DIR/web2"
API_PROJECT="$SCRIPT_DIR/api/api.host/api.host.fsproj"

# ── Color and formatting ────────────────────────────────────────────────────
if [ -t 1 ]; then
    BOLD='\033[1m'
    DIM='\033[2m'
    RED='\033[0;31m'
    GREEN='\033[0;32m'
    YELLOW='\033[0;33m'
    BLUE='\033[0;34m'
    CYAN='\033[0;36m'
    RESET='\033[0m'
else
    BOLD='' DIM='' RED='' GREEN='' YELLOW='' BLUE='' CYAN='' RESET=''
fi

# ── Helper functions ─────────────────────────────────────────────────────────

print_header() {
    echo ""
    echo -e "${BOLD}${CYAN}═══════════════════════════════════════════${RESET}"
    echo -e "${BOLD}${CYAN}  $1${RESET}"
    echo -e "${BOLD}${CYAN}═══════════════════════════════════════════${RESET}"
    echo ""
}

print_section() {
    echo -e "${BOLD}${BLUE}[$1]${RESET} ${BOLD}$2${RESET}"
}

print_ok() {
    echo -e "  ${GREEN}✓${RESET} $1"
}

print_warn() {
    echo -e "  ${YELLOW}⚠${RESET} $1"
}

print_error() {
    echo -e "  ${RED}✗${RESET} $1"
}

print_info() {
    echo -e "  ${DIM}→${RESET} $1"
}

print_urls() {
    echo ""
    echo -e "${DIM}───────────────────────────────────────────${RESET}"
    if [ "$FULL_STACK" = true ]; then
        echo -e "  ${BOLD}Frontend${RESET}:  ${GREEN}http://localhost:3000${RESET}"
        echo -e "  ${BOLD}API${RESET}:       ${GREEN}http://localhost:5100${RESET}"
        echo -e "  ${BOLD}Swagger${RESET}:   ${GREEN}http://localhost:5100/swagger${RESET}"
        echo -e "  ${BOLD}Database${RESET}:  localhost:3306 ${DIM}(MySQL)${RESET}"
    else
        echo -e "  ${BOLD}Frontend${RESET}:  ${GREEN}http://localhost:3000${RESET}"
        echo ""
        echo -e "  ${DIM}API not running — use ${RESET}${BOLD}--full-stack${RESET}${DIM} to start backend${RESET}"
    fi
    echo -e "${DIM}───────────────────────────────────────────${RESET}"
    echo ""
    echo -e "  Press ${BOLD}Ctrl+C${RESET} to stop."
    echo ""
}

check_port() {
    local port=$1
    local name=$2
    if command -v ss &>/dev/null; then
        if ss -tlnp 2>/dev/null | grep -q ":${port} "; then
            print_warn "Port $port ($name) is already in use"
            return 1
        fi
    elif command -v lsof &>/dev/null; then
        if lsof -i ":${port}" -sTCP:LISTEN &>/dev/null; then
            print_warn "Port $port ($name) is already in use"
            return 1
        fi
    fi
    return 0
}

version_ge() {
    # Returns 0 (true) if $1 >= $2 using semantic versioning
    local v1="${1%%.*}" v2="${2%%.*}"
    [ "$v1" -ge "$v2" ] 2>/dev/null
}

show_help() {
    echo "Djambi-N Local Dev Server"
    echo ""
    echo "Usage: ./run_server.sh [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --full-stack      Start database (Docker) + API (.NET) + frontend"
    echo "  --skip-install    Skip npm install (use existing node_modules)"
    echo "  --help            Show this help message"
    echo ""
    echo "Default: starts frontend dev server only (Vite on port 3000)"
    echo ""
    echo "Prerequisites:"
    echo "  Required:  Node.js >= 18, npm"
    echo "  Full-stack: + dotnet SDK, Docker, docker compose"
}

# ── Process tracking for cleanup ─────────────────────────────────────────────
API_PID=""
DB_STARTED=false

cleanup() {
    echo ""
    echo ""
    print_section "STOP" "Shutting down..."

    if [ -n "$API_PID" ] && kill -0 "$API_PID" 2>/dev/null; then
        print_info "Stopping API server (PID $API_PID)"
        kill "$API_PID" 2>/dev/null
        wait "$API_PID" 2>/dev/null
        print_ok "API server stopped"
    fi

    if [ "$DB_STARTED" = true ]; then
        print_info "Stopping database container"
        docker compose -f "$SCRIPT_DIR/docker-compose.yml" stop db 2>/dev/null
        print_ok "Database stopped"
    fi

    print_ok "All services stopped. Goodbye!"
    echo ""
    exit 0
}

trap cleanup SIGINT SIGTERM

# ── Parse arguments ──────────────────────────────────────────────────────────
FULL_STACK=false
SKIP_INSTALL=false

for arg in "$@"; do
    case "$arg" in
        --full-stack)   FULL_STACK=true ;;
        --skip-install) SKIP_INSTALL=true ;;
        --help|-h)      show_help; exit 0 ;;
        *)
            echo "Unknown option: $arg"
            echo "Run ./run_server.sh --help for usage."
            exit 1
            ;;
    esac
done

# ── Main ─────────────────────────────────────────────────────────────────────

print_header "Djambi-N Dev Server"

# ── 1. Prerequisite checks ──────────────────────────────────────────────────

print_section "CHECK" "Prerequisites"

PREREQS_OK=true

# Node.js
if command -v node &>/dev/null; then
    NODE_VERSION=$(node --version | sed 's/^v//')
    NODE_MAJOR=$(echo "$NODE_VERSION" | cut -d. -f1)
    if [ "$NODE_MAJOR" -ge 18 ] 2>/dev/null; then
        print_ok "Node.js v${NODE_VERSION} ${DIM}(>= 18 required)${RESET}"
    else
        print_error "Node.js v${NODE_VERSION} is too old ${DIM}(>= 18 required)${RESET}"
        PREREQS_OK=false
    fi
else
    print_error "Node.js not found ${DIM}(>= 18 required)${RESET}"
    PREREQS_OK=false
fi

# npm
if command -v npm &>/dev/null; then
    NPM_VERSION=$(npm --version)
    print_ok "npm ${NPM_VERSION}"
else
    print_error "npm not found"
    PREREQS_OK=false
fi

# dotnet (optional unless --full-stack)
if command -v dotnet &>/dev/null; then
    DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "unknown")
    print_ok "dotnet ${DOTNET_VERSION}"
elif [ "$FULL_STACK" = true ]; then
    print_error "dotnet not found ${DIM}(required for --full-stack)${RESET}"
    PREREQS_OK=false
else
    print_warn "dotnet not found ${DIM}(needed only for --full-stack)${RESET}"
fi

# Docker (optional unless --full-stack)
if command -v docker &>/dev/null; then
    DOCKER_VERSION=$(docker --version 2>/dev/null | grep -oP '\d+\.\d+\.\d+' | head -1)
    print_ok "Docker ${DOCKER_VERSION:-installed}"
elif [ "$FULL_STACK" = true ]; then
    print_error "Docker not found ${DIM}(required for --full-stack)${RESET}"
    PREREQS_OK=false
else
    print_warn "Docker not found ${DIM}(needed only for --full-stack)${RESET}"
fi

# docker compose (optional unless --full-stack)
if command -v docker compose &>/dev/null; then
    print_ok "docker compose available"
elif docker compose version &>/dev/null 2>&1; then
    print_ok "docker compose (plugin) available"
elif [ "$FULL_STACK" = true ]; then
    print_error "docker compose not found ${DIM}(required for --full-stack)${RESET}"
    PREREQS_OK=false
else
    print_warn "docker compose not found ${DIM}(needed only for --full-stack)${RESET}"
fi

echo ""

if [ "$PREREQS_OK" = false ]; then
    print_error "Missing required prerequisites. Please install them and try again."
    exit 1
fi

# ── 2. Check ports ──────────────────────────────────────────────────────────

print_section "CHECK" "Port availability"

PORTS_OK=true

if ! check_port 3000 "frontend"; then
    PORTS_OK=false
fi

if [ "$FULL_STACK" = true ]; then
    if ! check_port 5100 "API"; then
        PORTS_OK=false
    fi
    if ! check_port 3306 "database"; then
        PORTS_OK=false
    fi
fi

if [ "$PORTS_OK" = true ]; then
    if [ "$FULL_STACK" = true ]; then
        print_ok "Ports 3000, 5100, 3306 are free"
    else
        print_ok "Port 3000 is free"
    fi
fi

echo ""

if [ "$PORTS_OK" = false ]; then
    print_warn "Some ports are in use. The server may fail to start."
    echo ""
fi

# ── 3. Install dependencies ─────────────────────────────────────────────────

if [ "$SKIP_INSTALL" = true ]; then
    print_section "SKIP" "Dependency installation (--skip-install)"
    if [ ! -d "$WEB_DIR/node_modules" ]; then
        print_warn "node_modules/ not found — you may need to run without --skip-install"
    else
        PKG_COUNT=$(ls -1d "$WEB_DIR/node_modules"/*/ 2>/dev/null | wc -l)
        print_ok "Using existing node_modules/ (${PKG_COUNT} packages)"
    fi
else
    print_section "STEP" "Installing dependencies"
    print_info "Running npm install in web2/"

    if ! npm install --prefix "$WEB_DIR" --legacy-peer-deps 2>&1 | tail -3; then
        echo ""
        print_error "npm install failed. Check the output above for details."
        exit 1
    fi

    PKG_COUNT=$(ls -1d "$WEB_DIR/node_modules"/*/ 2>/dev/null | wc -l)
    print_ok "${PKG_COUNT} packages installed"
fi

echo ""

# ── 4. Full-stack: Start database + API ─────────────────────────────────────

if [ "$FULL_STACK" = true ]; then

    # ── 4a. Database ─────────────────────────────────────────────────────────
    print_section "STEP" "Starting database (MySQL via Docker)"
    print_info "Running docker compose up -d db"

    if docker compose -f "$SCRIPT_DIR/docker-compose.yml" up -d db 2>&1 | tail -2; then
        DB_STARTED=true
        print_ok "Database container started on port 3306"
    else
        print_error "Failed to start database container"
        print_warn "Continuing without database — API may not function correctly"
    fi

    echo ""

    # Wait for database to accept connections
    if [ "$DB_STARTED" = true ]; then
        print_info "Waiting for database to be ready..."
        for i in $(seq 1 30); do
            if docker compose -f "$SCRIPT_DIR/docker-compose.yml" exec -T db mysqladmin ping -h localhost -u root -pdevpassword &>/dev/null 2>&1; then
                print_ok "Database is ready"
                break
            fi
            if [ "$i" -eq 30 ]; then
                print_warn "Database may not be ready yet (timed out after 30s)"
            fi
            sleep 1
        done
        echo ""
    fi

    # ── 4b. API Server ───────────────────────────────────────────────────────
    print_section "STEP" "Starting API server (.NET)"
    print_info "Building and running api.host"

    if [ ! -f "$API_PROJECT" ]; then
        print_error "API project not found at $API_PROJECT"
        print_warn "Continuing without API"
    else
        # Set required environment variables
        export DJAMBI_Api__apiAddress="http://*:5100"
        export DJAMBI_Api__cookieDomain="localhost"
        export DJAMBI_Api__webAddress="http://localhost:3000"
        export DJAMBI_Sql__connectionString="Server=localhost;Port=3306;Database=Apex2;Uid=root;Pwd=devpassword;"

        dotnet run --project "$API_PROJECT" --no-launch-profile &
        API_PID=$!

        # Wait for API to respond
        print_info "Waiting for API to be ready on port 5100..."
        for i in $(seq 1 60); do
            if curl -s -o /dev/null -w "%{http_code}" http://localhost:5100/swagger 2>/dev/null | grep -q "200\|301\|302"; then
                print_ok "API server is ready (PID $API_PID)"
                break
            fi
            if ! kill -0 "$API_PID" 2>/dev/null; then
                print_error "API server exited unexpectedly"
                API_PID=""
                break
            fi
            if [ "$i" -eq 60 ]; then
                print_warn "API may not be ready yet (timed out after 60s)"
            fi
            sleep 1
        done
        echo ""
    fi
fi

# ── 5. Start frontend dev server ────────────────────────────────────────────

print_section "STEP" "Starting frontend dev server"
print_info "Launching Vite on port 3000"

print_urls

# Run Vite in the foreground (this blocks until Ctrl+C)
cd "$WEB_DIR" && npx vite --host --no-open
