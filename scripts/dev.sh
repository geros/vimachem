#!/bin/bash
# scripts/dev.sh - Development workflow helper
# Usage: ./scripts/dev.sh [command]

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

PROJECT_NAME="library-management"

case "${1:-}" in
  up)
    echo -e "${GREEN}▶ Starting all services...${NC}"
    docker compose -p "$PROJECT_NAME" up --build -d
    echo ""
    echo -e "${GREEN}✓ All services started!${NC}"
    echo ""
    echo -e "  ${BLUE}Party.API:${NC}        http://localhost:5100/swagger"
    echo -e "  ${BLUE}Catalog.API:${NC}      http://localhost:5200/swagger"
    echo -e "  ${BLUE}Lending.API:${NC}      http://localhost:5300/swagger"
    echo -e "  ${BLUE}Audit.API:${NC}        http://localhost:5400/swagger"
    echo -e "  ${BLUE}Documentation:${NC}    http://localhost:8000"
    echo -e "  ${BLUE}RabbitMQ Mgmt:${NC}    http://localhost:15672 (guest/guest)"
    echo ""
    ;;

  down)
    echo -e "${YELLOW}▼ Stopping all services...${NC}"
    docker compose -p "$PROJECT_NAME" down
    echo -e "${GREEN}✓ All services stopped.${NC}"
    ;;

  clean)
    echo -e "${RED}✕ Stopping services and removing volumes (FULL RESET)...${NC}"
    docker compose -p "$PROJECT_NAME" down -v
    echo -e "${GREEN}✓ Clean slate. All data removed.${NC}"
    ;;

  restart)
    echo -e "${YELLOW}↻ Restarting ${2:-all services}...${NC}"
    if [ -z "${2:-}" ]; then
      docker compose -p "$PROJECT_NAME" restart
    else
      docker compose -p "$PROJECT_NAME" restart "$2"
    fi
    echo -e "${GREEN}✓ Restart complete.${NC}"
    ;;

  rebuild)
    echo -e "${YELLOW}↻ Rebuilding ${2:-all services}...${NC}"
    if [ -z "${2:-}" ]; then
      docker compose -p "$PROJECT_NAME" up --build -d
    else
      docker compose -p "$PROJECT_NAME" up --build -d "$2"
    fi
    echo -e "${GREEN}✓ Rebuild complete.${NC}"
    ;;

  logs)
    if [ -z "${2:-}" ]; then
      docker compose -p "$PROJECT_NAME" logs -f --tail=50
    else
      docker compose -p "$PROJECT_NAME" logs -f --tail=50 "$2"
    fi
    ;;

  status)
    echo -e "${BLUE}Service Status:${NC}"
    docker compose -p "$PROJECT_NAME" ps
    ;;

  infra)
    echo -e "${GREEN}▶ Starting infrastructure only (postgres, mongo, rabbitmq)...${NC}"
    docker compose -p "$PROJECT_NAME" up -d postgres mongo rabbitmq
    echo -e "${GREEN}✓ Infrastructure ready.${NC}"
    echo ""
    echo -e "  ${BLUE}PostgreSQL:${NC}    localhost:5432 (postgres/postgres)"
    echo -e "  ${BLUE}MongoDB:${NC}       localhost:27017"
    echo -e "  ${BLUE}RabbitMQ:${NC}      localhost:5672 | Mgmt: localhost:15672"
    ;;

  test)
    echo -e "${BLUE}Running all unit tests...${NC}"
    FAILED=0
    for project in Party.API.Tests Catalog.API.Tests Lending.API.Tests Audit.API.Tests; do
      echo ""
      echo -e "${BLUE}  → $project${NC}"
      dotnet test "tests/$project" --verbosity normal || FAILED=$((FAILED + 1))
    done
    echo ""
    if [ "$FAILED" -eq 0 ]; then
      echo -e "${GREEN}✓ All tests passed.${NC}"
    else
      echo -e "${RED}✕ $FAILED project(s) had failures.${NC}"
      exit 1
    fi
    ;;

  migrate)
    echo -e "${BLUE}Running EF Core migrations...${NC}"
    echo "  → Party.API"
    (cd backend/Party.API && dotnet ef database update)
    echo "  → Catalog.API"
    (cd backend/Catalog.API && dotnet ef database update)
    echo "  → Lending.API"
    (cd backend/Lending.API && dotnet ef database update)
    echo -e "${GREEN}✓ All migrations applied.${NC}"
    ;;

  seed-check)
    echo -e "${BLUE}Checking seed data...${NC}"
    echo "  → Parties:"
    docker compose -p "$PROJECT_NAME" exec postgres \
      psql -U postgres -d party_db -c "SELECT id, first_name, last_name FROM parties;" 2>/dev/null || echo "  (DB not ready)"
    echo "  → Categories:"
    docker compose -p "$PROJECT_NAME" exec postgres \
      psql -U postgres -d catalog_db -c "SELECT id, name FROM categories;" 2>/dev/null || echo "  (DB not ready)"
    echo "  → Books:"
    docker compose -p "$PROJECT_NAME" exec postgres \
      psql -U postgres -d catalog_db -c "SELECT id, title, available_copies, total_copies FROM books;" 2>/dev/null || echo "  (DB not ready)"
    ;;

  smoke)
    echo -e "${BLUE}Running smoke test...${NC}"
    echo ""

    echo -n "  Party.API health... "
    curl -sf http://localhost:5100/swagger/index.html > /dev/null 2>&1 && \
      echo -e "${GREEN}✓${NC}" || echo -e "${RED}✕${NC}"

    echo -n "  Catalog.API health... "
    curl -sf http://localhost:5200/swagger/index.html > /dev/null 2>&1 && \
      echo -e "${GREEN}✓${NC}" || echo -e "${RED}✕${NC}"

    echo -n "  Lending.API health... "
    curl -sf http://localhost:5300/swagger/index.html > /dev/null 2>&1 && \
      echo -e "${GREEN}✓${NC}" || echo -e "${RED}✕${NC}"

    echo -n "  Audit.API health... "
    curl -sf http://localhost:5400/swagger/index.html > /dev/null 2>&1 && \
      echo -e "${GREEN}✓${NC}" || echo -e "${RED}✕${NC}"

    echo -n "  Documentation... "
    curl -sf http://localhost:8000 > /dev/null 2>&1 && \
      echo -e "${GREEN}✓${NC}" || echo -e "${RED}✕${NC}"

    echo -n "  RabbitMQ management... "
    curl -sf http://localhost:15672 > /dev/null 2>&1 && \
      echo -e "${GREEN}✓${NC}" || echo -e "${RED}✕${NC}"

    echo ""
    echo -e "  ${BLUE}Checking seed data via API...${NC}"

    echo -n "  GET /api/parties (expect >0)... "
    COUNT=$(curl -sf http://localhost:5100/api/parties 2>/dev/null | jq 'if type == "array" then length elif .items then .items | length else 0 end' 2>/dev/null || echo "0")
    [ "${COUNT:-0}" -gt 0 ] && echo -e "${GREEN}✓ ($COUNT parties)${NC}" || echo -e "${RED}✕ (got ${COUNT:-0})${NC}"

    echo -n "  GET /api/catalog/books (expect >0)... "
    COUNT=$(curl -sf http://localhost:5200/api/catalog/books 2>/dev/null | jq 'if type == "array" then length elif .items then .items | length else 0 end' 2>/dev/null || echo "0")
    [ "${COUNT:-0}" -gt 0 ] && echo -e "${GREEN}✓ ($COUNT books)${NC}" || echo -e "${RED}✕ (got ${COUNT:-0})${NC}"

    echo ""
    echo -e "${GREEN}✓ Smoke test complete.${NC}"
    ;;

  *)
    echo "Library Management System - Development Helper"
    echo ""
    echo "Usage: ./scripts/dev.sh <command> [service]"
    echo ""
    echo "Commands:"
    echo "  up           Build and start all services"
    echo "  down         Stop all services"
    echo "  clean        Stop and remove all data (volumes)"
    echo "  restart      Restart all or a specific service"
    echo "  rebuild      Rebuild and restart all or a specific service"
    echo "  logs         Follow logs for all or a specific service"
    echo "  status       Show service status and health"
    echo "  infra        Start infrastructure only (for local dev)"
    echo "  test         Run all unit tests"
    echo "  migrate      Apply EF Core migrations (local)"
    echo "  seed-check   Verify seed data in databases"
    echo "  smoke        Quick health check all endpoints"
    echo ""
    echo "Examples:"
    echo "  ./scripts/dev.sh up"
    echo "  ./scripts/dev.sh logs lending-api"
    echo "  ./scripts/dev.sh rebuild party-api"
    echo "  ./scripts/dev.sh restart catalog-api"
    ;;
esac
