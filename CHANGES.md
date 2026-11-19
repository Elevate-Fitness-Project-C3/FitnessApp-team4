# Applied fixes (second review of IdentityService_1 + FitnessApp-team4)

Layout of this package: repository root (compose files, `.dockerignore`, `.env.example`) + `IdentityService/`
(the service). The other services referenced by `docker-compose.yml` are not part of this archive.

The initial archive review used static checks. Subsequent workspace fixes have been built with .NET 10 and tested
against an isolated SQL Server. The repeatable regression runner is in `tests/IdentityService.SessionIntegration`.

| # | Finding | Status | What changed |
|---|---|---|---|
| 1 | `.env` with real secrets in the archive | **Partially** | `.env` removed from the package, `.env.example` extended. **You must still rotate** the SQL `sa` password and the Gmail app password (they were exposed) |
| 2 | Database setup | Manual | Docker does not provision databases or logins. Each service supplies its own connection string; its developer applies EF migrations explicitly. |
| 3 | Unreadable OTP blocks the user forever | Applied | `EmailVerificationCodeStore.IssueAsync` overwrites an unreadable challenge (and logs it) |
| 4 | Login holds the user lock while hashing | Applied | PBKDF2 + lockout pre-check outside the lock; lock only for the writes |
| 5 | `UserTokens` used as a queue, at-most-once | Applied | `EmailOutbox` table + entity + migration `AddEmailOutbox`; leasing, FIFO by Id, retry/back-off, at-least-once; raw SQL reduced to the single lock statement |
| 6 | Worker polls with a transaction every 2 s | Applied | cheap query without transaction, in-process signal after commit, 30 s safety poll, batches of 20 |
| 7 | Exceptions never logged | Applied | `GlobalExceptionHandler`, workers and cleanup log the exception; SMTP failures log only the status code (messages can contain the recipient) |
| 8 | Transaction+lock+reload block duplicated | Applied | `UserLockScope` used by Login, ConfirmEmail, ResetPassword, RefreshToken, e-mail worker |
| 9 | Exception for an expected race + duplicated check | Applied | `RefreshTokenIssuer.IssueAsync` returns `null` (→ 401); duplicate query removed; wrong call site still throws (programming error) |
| 10 | Binary `BuildingBlocks.*.dll` | Applied | results, API envelopes and validation behavior brought in as source from the local IdentityService project; binary references removed |
| 11 | Implicit Development override, all interfaces | Applied | `docker-compose.dev.yml` (explicit opt-in via `COMPOSE_FILE`), ports bound to `127.0.0.1`, base file has no Development settings |
| 12 | No root `.dockerignore`, `COPY . .` | Applied | root `.dockerignore`; Dockerfile copies only `IdentityService/` |
| 13 | Other services have no JWT wiring | **Partially** | `GET /.well-known/jwks.json` added to IdentityService. The other services must consume it (their code is not in the archive). Never mount the `identity-rsa-keys` volume into them |
| 14 | README/contract mismatch | Applied | deprecated aliases `forget-password` and `revoke-refresh-token` re-added; README rewritten to match the code |
| 15 | Model-state messages discarded | Applied | real messages returned; source validation behavior returns typed validation errors; exception handler also handles thrown validation exceptions |
| 16 | Register handler hygiene | Applied | parameter renamed, response built once, one `IdentityErrorMapper`. Response fields (Email/FirstName/LastName) kept: API contract |
| 17 | Login logs `UserId` null, redundant flags | Applied | |
| 18 | `ThrowIfCancellationRequested` boilerplate, `Delay` in catch | Applied | removed from all handlers; shutdown-safe delay |
| 19 | Controller duplication | Applied | `ToActionResult` extension, class-level 429/500 `ProducesResponseType` |
| 20 | Magic values / misleading names | Applied | `OtpOptions` (`Otp` section: expiration, max attempts, cooldown), `DbIndexes`, enum→template mapping, SQL not-ready error set |
| 21 | `JwtProvider` allocations | Applied | cached `JsonWebTokenHandler` and validation parameters; same token contract (single role = string) |
| 22 | Dead code / leftovers | Applied | `AddSignInManager`, `RequireConfirmedEmail`, `DeliveryFailed`, duplicate violation check removed. `DevelopmentRsaSetup` kept: the Docker dev stack builds Release and needs it (still refused outside Development) |
| 23 | `.gitignore` / csproj hygiene | Applied | complete `.gitignore`, `.csproj.user` removed, EF `Design` package dropped (`Tools` brings it), `Swashbuckle.AspNetCore.SwaggerUI` only |
| 24 | Compose hygiene | Applied | `compose.identity.yml` removed (use `docker compose up identity-service`), pinned SQL image `2022-CU14-ubuntu-22.04` (bump deliberately), quoted `ACCEPT_EULA`, `SQL_CONNECTION_OPTIONS` variable instead of 6 copies |
| 25 | `ForgetPassword*` names, inconsistent error codes | Applied | renamed to `ForgotPassword*`; reset-password errors now `Identity.<Code>` like register |

## Behavior / contract changes to know about

- Config key `EmailConfirmationToken` → `Otp` (plus `MaxAttempts`, `ResendCooldownSeconds`).
- reset-password validation errors use codes `Identity.<Code>` (before: raw Identity code).
- E-mail delivery is now at-least-once with retry; a retry issues a fresh OTP.
- The migration `AddEmailOutbox` drops old queue rows stored in `UserTokens`.
- `docker compose up` no longer implies Development; new variables in `.env.example` (per-service DB passwords, `COMPOSE_FILE`).
- Visual Studio Docker tooling expects `docker-compose.override.yml` (see README).

## Checklist before merging

1. `dotnet build` and fix anything the compiler finds (this package was not compiled).
2. `dotnet ef migrations has-pending-model-changes` (the new migration is hand-written).
3. Rotate the SQL `sa` password and the Gmail app password; fill a fresh `.env` from `.env.example`.
4. `docker compose up identity-service`, then: register → confirm with the e-mailed code → login → refresh (twice with the
   same token: the second call must revoke all sessions) → forgot-password → reset-password → logout.
5. Stop the SMTP server/credentials, register: the request must still return 201 and the message must be retried later.
6. Wrong password on an unconfirmed account must return `Auth.InvalidCredentials`, not `EmailNotConfirmed`.
7. Open `/.well-known/jwks.json` and validate a token with it from another service.
