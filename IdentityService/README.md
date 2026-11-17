# IdentityService

Authentication service of the Fitness App: registration, e-mail OTP confirmation, login, refresh-token rotation,
logout, forgot/reset password. Access tokens are **RS256** JWTs; the public key is published at
`/.well-known/jwks.json`.

## Run locally (Docker)

Development uses `dotnet watch` inside the SDK container. Save a C# file and the service rebuilds and restarts
automatically; refresh Swagger after the restart. Source is mounted read-only and Linux build outputs use Docker
volumes, separate from host `bin`/`obj`. Polling supports Docker Desktop on Windows/macOS and Linux.
Start once with `docker compose up -d --build identity-service`. Changes to Dockerfiles or Compose settings still
require recreating the container. Production uses the published image and does not watch source files.

```bash
cp .env.example .env          # fill in the secrets (never commit/zip the real .env)
docker compose up identity-service
# Swagger (Development only): http://localhost:5002/swagger
```

`docker-compose.dev.yml` is selected through `COMPOSE_FILE` in `.env`. It is intentionally **not** applied implicitly.
Visual Studio's Docker tooling expects `docker-compose.override.yml`; use the `COMPOSE_FILE` setting (or `-f` flags) instead.

## Configuration (secrets come from environment / user-secrets, never from committed files)

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | the service's own SQL login (not `sa`) |
| `Jwt:Audience`, `Jwt:Issuer`, `Jwt:AccessTokenExpirationMinutes`, `Jwt:RefreshTokenExpirationDays`, `Jwt:MaxActiveRefreshTokens` | token settings |
| `RsaKeys:PrivateKeyPath`, `RsaKeys:PublicKeyPath` | RS256 key pair (PEM files mounted read-only) |
| `Otp:ExpirationMinutes`, `Otp:MaxAttempts`, `Otp:ResendCooldownSeconds` | OTP policy (**renamed** from `EmailConfirmationToken`) |
| `Email:*` | SMTP (`Email__Password` must come from a secret store) |
| `DataProtection:CertificatePath`, `DataProtection:CertificatePassword` | certificate that encrypts the Data Protection key ring (required outside Development) |
| `Proxy:KnownProxies` | IPs of trusted reverse proxies (needed for correct per-IP rate limiting behind a proxy) |
| `DevelopmentSetup:GenerateRsaKeys` | Creates development RSA keys; refused outside Development |

## Endpoints (`/api/auth`)

`register`, `login`, `confirm-email`, `resend-confirmation-email`, `forgot-password` (alias: `forget-password`),
`reset-password`, `refresh`, `logout` (alias: `revoke-refresh-token`). Aliases are deprecated and kept for existing clients.

`GET /.well-known/jwks.json` returns the public signing key (RSA, `kid`, `n`, `e`) for the other services.

## How it works

- **Per-user critical section** (`UserLockScope`): login, confirm-email, reset-password, refresh and the e-mail worker run
  their state changes inside one transaction holding a per-user row lock, on freshly reloaded data. Password hashing
  (PBKDF2) happens *before* taking the lock.
- **OTP** (`EmailVerificationCodeStore`): 6 digits, protected with Data Protection, stored in a dedicated `UserTokens` row.
  Resend cooldown, failed attempts are carried across re-issues until the attempt window ends, constant-time comparison.
  An unreadable challenge (e.g. after key rotation) is overwritten instead of blocking the user.
- **E-mail outbox** (`EmailOutbox` table, `AuthEmailQueue`, `AuthEmailWorker`): messages are written in the same transaction as
  the business change; the worker leases due rows, issues the OTP under the user lock, sends the e-mail and only then marks
  the row processed. SMTP failures are retried with back-off (up to 4 attempts, each retry issues a fresh OTP). Delivery is
  at-least-once. Processed rows are purged after 7 days.
- **Refresh tokens**: stored hashed, rotated on every use, replay of a revoked token revokes all sessions of the user,
  at most `Jwt:MaxActiveRefreshTokens` active sessions per user, stale rows are purged by `RefreshTokenCleanupWorker`.
  Every login creates a `SessionId` that is preserved during rotation. Logout takes the same per-user lock as refresh
  and revokes every token of that session, including a replacement issued by a concurrent refresh. Repeating logout
  with a recognized, unexpired token succeeds. Other sessions are unaffected by logout.
- **Account enumeration**: login verifies the password before reporting "e-mail not confirmed"; forgot-password and
  resend-confirmation always return the same response. Register still answers `409` for an existing e-mail (product decision).

## Operations notes

- Database creation and schema updates are manual in every environment. Docker does not create application databases
  or logins. Set `IDENTITY_CONNECTION_STRING` in `.env` for the container, choosing your database and credentials.
  For host EF tools configure `ConnectionStrings:DefaultConnection` via user-secrets (host: `localhost,14330`).
  Run from this service directory:

  ```powershell
  dotnet ef migrations add YourChange
  dotnet ef database update
  ```

  Existing migrations only need `database update`; add a migration when you change the model. The migration login
  needs permission to create/update its database. Startup checks for pending migrations and seeds the User role,
  but never calls `Migrate` or `EnsureCreated`.

- Production: use `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d identity-service`.
  Set `IDENTITY_DP_CERTIFICATE_FILE` to an existing PFX containing the private key and set
  `IDENTITY_DP_CERTIFICATE_PASSWORD`. The overlay mounts it read-only; ensure the container user can read it.
  Provision the RSA key volume and apply migrations before startup. Development continues using the dev overlay
  and does not need a certificate. Keep the certificate and its password outside source control and build context.
  Retain certificates needed to decrypt existing Data Protection keys when rotating certificates.

- Apply `AddRefreshTokenSession` before running the updated service. Existing tokens receive distinct session IDs;
  historical rotation chains cannot be reconstructed, so the concurrent logout guarantee applies to rotations
  performed by the updated service. Stop old instances before applying this migration and deploying the update.

- Apply migrations (production): `dotnet ef database update`. The `AddEmailOutbox` migration was written by hand in the
  project's compact style: run `dotnet ef migrations has-pending-model-changes` and, if it reports differences, regenerate it.
  It deletes the old best-effort queue rows (`UserTokens` with provider `FitnessApp.AuthEmailQueue`).
- Rotating the Data Protection certificate or key ring invalidates pending OTPs (users simply request a new code).
- Rotate any secret that was ever shared in an archive, chat or repository.

## Known limitations

- Access tokens stay valid until they expire (15 min by default) after a password reset or logout.
- The outbox is polled every 30 s as a safety net and woken immediately after commit by an in-process signal; with several
  instances the signal only wakes the instance that handled the request (others pick messages up on the next poll).

## Session concurrency integration tests

Results, response mapping and validation behavior are compiled from source under `Common/Results`, `Common/Responses`
and `Common/Behaviors`; the service no longer references the legacy `BuildingBlocks` DLLs.

Set `IDENTITY_SESSION_TEST_CONNECTION` to a dedicated SQL Server test database, then run:

```powershell
dotnet run --project tests/IdentityService.SessionIntegration/IdentityService.SessionIntegration.csproj
```

The runner applies migrations and creates test accounts. It checks rotation followed by logout with the old token,
logout followed by refresh, repeated logout, unrelated session isolation, unknown tokens and ten concurrent races.
JWT validation is stubbed; transactions, row locks, handlers and persistence use the real SQL Server provider.
It also verifies that the first login upgrades an Identity V2 password hash successfully. The checks for SMTP timeout
versus shutdown cancellation, compatibility routes, response mapping and validation can run without a database:

```powershell
dotnet run --project tests/IdentityService.SessionIntegration/IdentityService.SessionIntegration.csproj -- --unit-only
```

JWT checks cover asynchronous validation, malformed tokens, audience rejection, expired access tokens for refresh/logout,
and cancellation. The SQL tests also inject a lock failure and verify transaction cleanup on the same DbContext.
