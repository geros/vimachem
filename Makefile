.PHONY: up down clean logs status infra test smoke rebuild

PROJECT = library-management

up:
	docker compose -p $(PROJECT) up --build -d
	@echo ""
	@echo "Party.API:     http://localhost:5100/swagger"
	@echo "Catalog.API:   http://localhost:5200/swagger"
	@echo "Lending.API:   http://localhost:5300/swagger"
	@echo "Audit.API:     http://localhost:5400/swagger"
	@echo "RabbitMQ:      http://localhost:15672"

down:
	docker compose -p $(PROJECT) down

clean:
	docker compose -p $(PROJECT) down -v

logs:
	docker compose -p $(PROJECT) logs -f --tail=50 $(SVC)

status:
	docker compose -p $(PROJECT) ps

infra:
	docker compose -p $(PROJECT) up -d postgres mongo rabbitmq

test:
	dotnet test tests/Party.API.Tests/
	dotnet test tests/Catalog.API.Tests/
	dotnet test tests/Lending.API.Tests/
	dotnet test tests/Audit.API.Tests/

smoke:
	@./scripts/dev.sh smoke

rebuild:
	docker compose -p $(PROJECT) up --build -d $(SVC)
