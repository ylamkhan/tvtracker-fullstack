# Project name
PROJECT_NAME=tvtracker

# Docker Compose command (supports both v1 and v2)
DC=docker compose

# Default target
.PHONY: help
help:
	@echo "Available commands:"
	@echo "  make build        Build all services"
	@echo "  make up           Start services in background"
	@echo "  make down         Stop and remove containers"
	@echo "  make restart      Restart all services"
	@echo "  make logs         Show logs"
	@echo "  make logs-backend Show backend logs"
	@echo "  make logs-db      Show postgres logs"
	@echo "  make ps           Show running containers"
	@echo "  make clean        Remove containers, volumes, images"

# Build services
.PHONY: build
build:
	$(DC) build

# Start services
.PHONY: up
up:
	$(DC) up -d

# Stop services
.PHONY: down
down:
	$(DC) down

# Restart services
.PHONY: restart
restart:
	$(DC) down
	$(DC) up -d

# View logs
.PHONY: logs
logs:
	$(DC) logs -f

# Backend logs only
.PHONY: logs-backend
logs-backend:
	$(DC) logs -f backend

# Database logs only
.PHONY: logs-db
logs-db:
	$(DC) logs -f postgres

# Show running containers
.PHONY: ps
ps:
	$(DC) ps

# Clean everything (⚠️ removes volumes too)
.PHONY: clean
clean:
	$(DC) down -v --rmi all --remove-orphans
