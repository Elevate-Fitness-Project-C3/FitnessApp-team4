# Final cleanup and release gate

This revision is based on IdentityService-reviewed.zip. No TestController, test-auth route, old JWT generator, or Mapster package was present in that input. The empty IdentityService.http scratch file was removed; duplicate global/local usings were removed. Existing migration identifiers, API aliases, tooling packages, result interfaces and Docker build context were deliberately retained. No database schema changes were made. Confirm/reset reload the user after acquiring the SQL lock; refresh checks expiry using the time after lock acquisition. Registration now documents HTTP 429. Ignore rules cover more secret and test artifacts.

## Replacement on a clean feature branch
Back up local edits and keys separately. Extract the archive outside the repository, then mirror only the IdentityService directory (never the solution root). Preview deletions first using robocopy /MIR /L, excluding bin, obj, .vs, .git, local config and keys. Do not mirror if local secrets are stored inside the directory until they are safely relocated. Inspect git status and git diff --stat. Preserve solution and compose files. Never commit secrets.

## Release steps
1. Revoke previously exposed SMTP app password and any exposed database/signing credentials; coordinate Git history cleanup. Provision per-environment RSA keys, SQL connection, SMTP credentials, and data protection PFX using deployment secret store. See README.md for variable names. Share only the RSA public key with other services. Update compose/CI to inject secrets; these parent files were not supplied.
2. From the service directory run dotnet --info, dotnet restore, dotnet build --no-restore -c Release. Check dotnet list package --vulnerable --include-transitive and dotnet list package --outdated. Do not remove tooling packages without checking the team workflow.
3. Run dotnet ef migrations has-pending-model-changes. Verify the intended database and back it up; generate and review an idempotent migration script before applying migrations once. Do not delete existing key ring or migrations.
4. From solution root run docker compose config --quiet, docker compose build identity-service, docker compose up -d identity-service, docker compose logs --tail=100 identity-service (adapt to actual compose setup).
5. Run the integration test matrix in README.md, especially expired, wrong-algorithm, missing and tampered JWT on a protected integration-test host; no production test endpoint. Test OTP throttling and concurrency, refresh reuse/reset, SMTP failures and restart, multiple replicas and proxy handling.
6. Resolve remaining release blockers: durable email retries/leases and recovery (current implementation is best-effort); shared ingress limits; certificate rollover and existing plaintext key ring migration; audit/alerting; registration enumeration trade-off; consumer compatibility for emailQueued and RSA. Add automated regression tests and CI gates. Access JWTs remain active until expiry after session revocation.
7. Review git diff and secret scan, then commit and open a PR. Do not claim production approval before build, integration and deployment gates pass.

## Verification limitations
Static source inspection only; .NET SDK, SQL Server and Docker runtime testing were unavailable. No parent solution or compose files were changed. Package restore and migration model consistency are not verified. No keys or credentials were generated or included. No name changes or new packages were introduced.
