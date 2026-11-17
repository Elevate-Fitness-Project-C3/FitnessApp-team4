# FitnessApp — Development Setup

FitnessApp is a .NET 10 microservices solution. IdentityService provides reusable authentication functionality and can also run independently of the other FitnessApp services.

## Prerequisites

- Git and Docker with Compose v2 (Linux containers).
- Internet access for container images and package restore.
- .NET 10 SDK only for host builds or debugging outside Docker.

No host OpenSSL, EF CLI, or PowerShell setup script is required for Docker development.

## Environment configuration

Copy `.env.example` to `.env` in the repository root and supply the required values:

```dotenv
SQL_SA_PASSWORD=
IDENTITY_JWT_AUDIENCE=FitnessApp
IDENTITY_EMAIL_USERNAME=
IDENTITY_EMAIL_PASSWORD=
IDENTITY_EMAIL_FROM=
IDENTITY_EMAIL_HOST=smtp.gmail.com
IDENTITY_EMAIL_PORT=587
IDENTITY_EMAIL_ENABLE_SSL=true
```

Use a strong SQL password without semicolons or quotes, quote dotenv values as necessary, and use an SMTP app password where required. Never commit `.env` or private keys. Keep the JWT audience consistent with consumer validation settings. For existing SQL volumes, retain the existing password unless coordinating an actual SQL password change.

## Run FitnessApp

From the repository root:

```sh
docker compose config --quiet
docker compose up --build -d
docker compose ps
docker compose logs -f identity-service
```

Alternatively, open `FitnessApp-team4.slnx` in Visual Studio and select Docker Compose as the startup project.

| Service | Host port |
| --- | --- |
| Fitness Profile | 5001 |
| Identity | 5002 |
| Nutrition | 5003 |
| Payment | 5004 |
| Progress | 5005 |
| Training | 5006 |
| SQL Server (localhost only) | 14330 |

Identity API: http://localhost:5002/swagger/index.html

OpenAPI: http://localhost:5002/openapi/v1.json

Stop the environment with `docker compose down`.

## Run IdentityService independently

Use the same environment configuration, with an audience appropriate to the intended consumers.

```sh
docker compose -f compose.identity.yml config --quiet
docker compose -f compose.identity.yml up --build -d
docker compose -f compose.identity.yml logs -f identity-service
```

Stop with `docker compose -f compose.identity.yml down`.

The standalone environment has separate database and RSA volumes. Both deployments use host ports 5002 and 14330, so stop one before starting the other unless ports are explicitly changed.

For a host build of IdentityService and its shared libraries:

```sh
dotnet build IdentityPlatform.slnx
```

## Development initialization and persistence

Development startup is configured to generate RSA keys when needed, apply IdentityService EF Core migrations, and seed the User role. Docker Compose creates named volumes and waits for SQL Server health before starting dependent services. Automatic migrations for other services are not guaranteed by these settings.

FitnessApp uses `fitness-sql-data-v2` for SQL data and `fitness-identity-rsa-keys` for identity signing keys. Standalone volumes are project-scoped. Never use `down -v` unless intentionally deleting development state. Back up existing data and keys; do not delete an existing volume to resolve a Compose ownership warning.

Existing signing keys are not migrated automatically. A newly generated key pair invalidates signatures made with the old pair. Preserving existing tokens requires securely preserving the original keys and compatible JWT settings. Legacy OTP and email queue provider identifiers remain unchanged for compatibility.

## JWT consumer integration

Export only the public key from the running environment:

```sh
docker compose cp identity-service:/var/lib/identity/keys/public.pem ./public.pem
```

For standalone, use `docker compose -f compose.identity.yml cp identity-service:/var/lib/identity/keys/public.pem ./public.pem` instead.

Never distribute the private key. Consumer JWT validation and automated public key distribution are not configured by these deployment files; consumers must validate signature, issuer, audience, and lifetime with the appropriate public key.

## Production

Development Compose settings are not production settings. Provision RSA signing keys, a Data Protection certificate, JWT configuration, database and SMTP credentials explicitly. Automatic RSA generation and schema migration are disabled outside Development; apply reviewed migrations separately before startup. Configure TLS, trusted proxies, secret storage, least-privileged database access, backups, key rotation and monitoring. Role seeding remains application behavior. Review service-specific documentation before deployment.

## Validation status

The integrated development deployment has previously started successfully according to local testing. The latest root configuration changes still require verification. Before merging, verify full and standalone builds/startup, fresh and existing database migrations, role seeding, restart persistence, JWT validation including expiration and invalid signatures/algorithms, real SMTP delivery, and authentication endpoints. Email delivery is best-effort; no outbox guarantee is provided. Shared libraries are source project references, not published NuGet packages. This is not a production-validated release.
