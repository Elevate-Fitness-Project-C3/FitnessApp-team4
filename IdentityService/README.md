> Development setup has changed in this candidate. See the root README for the current Docker workflow. The original review notes below are retained for API/security context.

# IdentityService — authentication review revision
.NET 10, SQL Server, ASP.NET Core Identity, MediatR, vertical slices. No custom repository or unit of work.

## Configuration and deployment
No working credentials or signing keys are bundled. Supply these through the deployment secret store, CI/CD or container secret mounts. Developers can use their own environment or .NET user secrets (never shared production credentials). All replicas of one environment require the same RSA key pair and data-protection certificate and database. Use distinct keys per environment. No keys are generated automatically at boot.

| Configuration environment variable | Purpose |
|---|---|
| ConnectionStrings__DefaultConnection | SQL Server connection string |
| RsaKeys__PrivateKeyPath | read-only mounted RSA private PEM, IdentityService only |
| RsaKeys__PublicKeyPath | RSA public PEM, distribute to resource services |
| DataProtection__CertificatePath | read-only mounted PFX with private key (required outside Development) |
| DataProtection__CertificatePassword | PFX password from secret store |
| Email__UserName / Email__Password / Email__From | SMTP credentials and sender |
| Email__Host / Email__Port / Email__EnableSsl | SMTP endpoint, default Gmail STARTTLS 587 |
| Proxy__KnownProxies__0 | actual trusted reverse proxy address, if applicable |

Keep all secret mounts outside the repository and image build context. The supplied Dockerfile expects the parent solution directory as build context; put equivalent `.dockerignore` rules in that directory too. The runtime UID must have read access to mounted secrets. Configure TLS at the ingress; never trust arbitrary forwarded headers. Use a distributed ingress limiter in multi-replica production (the built-in IP limiter is per process). OTP cooldown and failed-attempt windows are persisted in SQL Server per user and purpose.

Rotate the old exposed SMTP password immediately and any previously committed symmetric signing key/database credential; clean repository history with your team. Existing plaintext DataProtectionKeys rows are NOT retroactively encrypted by configuring a certificate. For a fresh deployment use a clean key ring; in an existing environment plan key migration/rotation, token invalidation and backup handling before enforcing the certificate. Never delete keys blindly. Keep previous certificates for decryption during rotation; current code loads one certificate, so coordinate rollover separately. RSA key rollover likewise requires a coordinated token transition across consumers.

## API changes
Registration response replaces `emailSent` with `emailQueued`: it means persisted work, not delivered mail. Existing identity fields and duplicate-email 409 contract are retained. `forgot-password` and `logout` are aliases; existing routes continue working. Invalid access token now has its own error. Validation errors use `Validation.<property>` keys. Commands and request DTOs redact ToString, but never destructure or log request bodies or JWTs. Temporary `test-auth` is removed; use an authorized endpoint in a local test host to finish JWT verification.

## Email delivery semantics
Registration, role assignment and queued confirmation request commit together. Jobs are stored as dedicated provider rows in existing `UserTokens`, not local files or memory. A background worker claims a row and issues the OTP in one transaction. SMTP runs after commit. **Delivery is best effort**: a crash after claim commit or SMTP rejection may lose that send; the user can request resend after cooldown. This is not a fully guaranteed transactional outbox with retry and leases. OTP lifetime can be shorter on resend because the original attempt window is preserved. Do not log OTPs or enable EF sensitive-data logging.

Refresh token reuse (unexpired revoked token) revokes active refresh sessions for the account, including concurrent duplicate refreshes and replay of a logged-out token. Clients must serialize refresh calls. Already issued access JWTs remain valid until expiry; immediate access-token revocation is not implemented. Active refresh sessions are capped at 10 by default; cleanup deletes rows expired more than seven days ago.

## Verification commands (after supplying configuration)
From the IdentityService directory, with .NET 10 SDK and matching EF tool installed:
```sh
dotnet restore
dotnet build --no-restore
dotnet ef migrations has-pending-model-changes
dotnet ef database update
```
No schema change intended. The design-time factory uses environment variables or explicit connection arguments; it does not load user secrets. For migration deployment supply the connection securely and run once before starting replicas, not per replica. Do not run database update until the target is verified.

Test registration and rollback, duplicate email concurrently, correct/incorrect password and unconfirmed account, lockout, missing/tampered/expired/HS256 JWT against a protected test endpoint, OTP resend and five failures across two instances, concurrent code use, password reset revocation, concurrent refresh/reuse, session cap, cleanup, proxy spoofing, certificate restart across replicas, SMTP failures and worker restarts. Build/runtime/integration tests were not run in the editing environment (SDK unavailable).

See [FINAL-CHECKLIST.md](FINAL-CHECKLIST.md) for the final cleanup changes and release gates.
