# Foundation design

Accepted for local implementation by user request on 2026-10-14. Conflict decisions: ../research/CONFLICT_RESOLUTIONS.md.

API projects target .NET 10. API → Infrastructure → Application → Domain. PostgreSQL uses EF migrations. React calls the same-origin /api/v1 prefix, proxied in development. No public database connection or browser-stored auth token. Credentials generated into git-ignored .env, never hard-coded in source. Workspace SDK/binaries in .tools are excluded from Git.

Liveness is independent of PostgreSQL. Readiness checks the actual database and returns 503 on failure without connection-string details. OpenAPI is development-only. Serilog provides structured request logs. OpenTelemetry instruments inbound/outbound requests and metrics; remote exporters require separately approved deployment configuration.

Docker Desktop did not provide a working daemon during initial setup. Official EDB Windows PostgreSQL binaries, linked by postgresql.org, provide a local-only fallback; production remains Docker Compose on Ubuntu. This is a development-runtime alternative, not a change to the approved database stack.

Deployment preparation and the full modular feature list remain tracked separately. No module is considered complete based only on project folders.
