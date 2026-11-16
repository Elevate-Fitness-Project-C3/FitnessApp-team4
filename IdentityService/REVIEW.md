# Review disposition (25 findings)
Static source changes only; not built or tested. Findings refer to the supplied Markdown review. The uploaded archive already included RS256, combined registration handler, OTP attempt window/cooldown, password-first login, generic confirmation failure and registration transaction.

| # | Disposition | Details |
|---|---|---|
| 1 | Changed / operational action | SMTP and database settings cleared; ignore files added. Rotate leaked credentials and clean Git history externally. |
| 2 | Already addressed in source | RS256 only; PEM paths configured externally. Revoke former symmetric key operationally. |
| 3 | Already addressed | Direct UserManager, no nested mediator calls. |
| 4 | Already addressed / refined | SQL-backed account/purpose attempt window and cooldown; bounded retries and cancellation. |
| 5 | Partly addressed | Password-first and dummy hash already present, generic confirmation; duplicate 409 kept deliberately; timing enumeration not eliminated. |
| 6 | Changed, partial | Persistent SQL queue and background SMTP; no guaranteed retry after claim commit. |
| 7 | Changed / operational action | Certificate required outside Development; old plaintext key rows require migration/rotation. |
| 8 | Changed | Explicit proxy allowlist and forwarded middleware; distributed IP limiter remains deployment responsibility. |
| 9 | Changed | Reuse detection, session cap, account locking and expiry cleanup; JWT access revocation not included. |
| 10 | Retained | Explicit transactions intentionally preserve failed OTP attempts, reuse revocation and registration queue atomicity; blanket success-only behavior is unsafe. |
| 11 | Already / retained contract | Entity commands already removed; registration identity fields retained for clients. EmailQueued replaces EmailSent. |
| 12 | Changed | Result-based validation and model binding envelope. |
| 13 | Changed | IssuedTokens decoupled from feature, SQL unique error helper centralized. SQL Server error mapping still depends on named indexes. |
| 14 | Changed / already | RS256 active in upload. Removed unused Mapster, data-protection path options and HTTP result helpers. |
| 15 | Partial | Structured auth events added; centralized production audit sink, alerting and exhaustive event coverage still needed. |
| 16 | Changed | Request DTOs separated; ToString redacted; destructuring still unsafe. |
| 17 | Changed / retained | OpenAPI response metadata improved. Approved controller pattern retained; no fictitious Location URL. |
| 18 | Changed / retained | Cancellation flows to EF and SMTP. One early cancellation check retained (Identity APIs lack tokens). |
| 19 | Changed | Three optimistic retries. |
| 20 | Changed | OtpPurpose enum; configured lifetime drives provider/store/template. Attempt/cooldown settings remain code policy. |
| 21 | Partial | Compatible route aliases and distinct access error; internal names not renamed. |
| 22 | Partial | Cleaned unused usings; applied migration IDs/names retained to avoid history divergence. Pending-model check must run. |
| 23 | Partial | Templates cached; SmtpClient retained for now to avoid unverified package migration; MailKit and delivery retries remain follow-up. |
| 24 | Partial | Cached RSA validation parameters; existing JwtSecurityTokenHandler retained, no benchmark. |
| 25 | Changed | Single public AddDependencies entry, private focused registration methods and policy constant. |

## Release blockers
SDK unavailable, so build and SQL integration tests are pending. Secrets and certificate provisioning, old key-ring remediation, distributed ingress limits, SMTP reliability and audit sink need environment-specific verification. Prior expired-JWT and invalid-algorithm tests remain pending; temporary endpoint removed from delivery. Do not treat this archive as production-certified.
