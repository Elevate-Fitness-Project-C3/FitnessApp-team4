$ErrorActionPreference = "Stop"

$rootPath = $PSScriptRoot
$identityPath = Join-Path $rootPath "IdentityService"
$envPath = Join-Path $rootPath ".env"
$envExamplePath = Join-Path $rootPath ".env.example"
$keyPath = Join-Path $env:USERPROFILE ".fitness-app\identity-service\keys"
$privateKeyPath = Join-Path $keyPath "private.pem"
$publicKeyPath = Join-Path $keyPath "public.pem"
$volumeName = "fitness-sql-data-v2"
$sqlServiceName = "sql-server"
$sqlServer = "tcp:127.0.0.1,14330"
$databaseName = "IdentityDb"
$sqlStartupTimeoutSeconds = 120

function Write-Step($message) {
    Write-Host "`n[SETUP] $message" -ForegroundColor Cyan
}

function Assert-Command($name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "Required command '$name' was not found."
    }
}

function Invoke-Checked($command, $arguments) {
    & $command @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "$command failed with exit code $LASTEXITCODE."
    }
}

function Read-Secret($message) {
    $secret = Read-Host $message -AsSecureString
    return [System.Net.NetworkCredential]::new("", $secret).Password
}

function Get-OpenSslPath {
    $command = Get-Command openssl -CommandType Application -ErrorAction SilentlyContinue

    if ($command) {
        return $command.Source
    }

    $paths = @(
        "$env:ProgramFiles\Git\usr\bin\openssl.exe",
        "${env:ProgramFiles(x86)}\Git\usr\bin\openssl.exe",
        "$env:LOCALAPPDATA\Programs\Git\usr\bin\openssl.exe"
    )

    foreach ($path in $paths) {
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            return $path
        }
    }

    throw "OpenSSL was not found. Install Git for Windows or OpenSSL."
}

function Set-EnvValue($name, $value) {
    $lines = @(Get-Content -LiteralPath $envPath)
    $pattern = "^\s*" + [regex]::Escape($name) + "\s*="
    $found = $false

    if ($value -match "[\r\n]") {
        throw "Multiline environment values are not supported."
    }

    $escapedValue = "'" + $value.Replace("'", "\'") + "'"
    $entry = "$name=$escapedValue"

    $lines = @(foreach ($line in $lines) {
        if ($line -match $pattern) {
            if (-not $found) {
                $entry
                $found = $true
            }
        } else {
            $line
        }
    })

    if (-not $found) {
        $lines += $entry
    }

    [System.IO.File]::WriteAllLines(
        $envPath, [string[]]$lines,
        [System.Text.UTF8Encoding]::new($false))
}

function Test-EnvValue($name) {
    $pattern = "^\s*" + [regex]::Escape($name) + "\s*=(.*)$"
    $value = $null

    foreach ($line in Get-Content -LiteralPath $envPath) {
        if ($line -match $pattern) {
            $value = $Matches[1].Trim().Trim('"').Trim("'")
        }
    }

    return (-not [string]::IsNullOrWhiteSpace($value))
}

function Test-NonEmptyFile($path) {
    return (Test-Path -LiteralPath $path -PathType Leaf) -and
        ((Get-Item -LiteralPath $path).Length -gt 0)
}

function Get-DatabaseConnectionString($database, $password) {
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()

    $builder.DataSource = $sqlServer
    $builder.InitialCatalog = $database
    $builder.UserID = "sa"
    $builder.Password = $password
    $builder.Encrypt = $true
    $builder.TrustServerCertificate = $true
    $builder.ConnectTimeout = 5

    return $builder.ConnectionString
}

function Find-RunningSqlContainer {
    $containers = @(Invoke-Checked "docker" @(
        "ps", "--filter", "volume=$volumeName",
        "--format", "{{.ID}}"
    ))

    if ($containers.Count -gt 1) {
        throw "Multiple running containers are using the SQL Server volume."
    }

    if ($containers.Count -eq 1) {
        $containerId = $containers[0]

        $ports = @(Invoke-Checked "docker" @(
            "port", $containerId, "1433/tcp"
        ))

        if (-not ($ports -match ":14330$")) {
            throw "Existing SQL Server container has an unexpected port mapping."
        }

        return $containerId
    }

    return $null
}

function Wait-SqlServer($connectionString) {
    $deadline = [DateTime]::UtcNow.AddSeconds(
        $sqlStartupTimeoutSeconds
    )

    do {
        $connection = [System.Data.SqlClient.SqlConnection]::new(
            $connectionString
        )

        try {
            $connection.Open()

            Write-Host "SQL Server is ready." -ForegroundColor Green
            return
        } catch {
            $lastError = $_.Exception.Message

            Write-Host "Waiting for SQL Server..."
        } finally {
            $connection.Dispose()
        }

        Start-Sleep -Seconds 3
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "SQL Server readiness check failed. $lastError"
}

function Assert-AuthenticationStorage($connectionString) {
    $connection = [System.Data.SqlClient.SqlConnection]::new(
        $connectionString
    )

    try {
        $connection.Open()
        $command = $connection.CreateCommand()

        try {
            $command.CommandText = @"
SELECT COUNT(*)
FROM sys.tables
WHERE name = N'DataProtectionKeys';
"@

            if ([int]$command.ExecuteScalar() -ne 1) {
                throw "DataProtectionKeys table was not found."
            }

            Write-Host "Authentication storage validated." -ForegroundColor Green
        } finally {
            $command.Dispose()
        }
    } finally {
        $connection.Dispose()
    }
}

# Check prerequisites.

Write-Step "Checking prerequisites"

Assert-Command "dotnet"
Assert-Command "docker"

if (-not (Test-Path -LiteralPath $identityPath -PathType Container)) {
    throw "IdentityService project directory was not found."
}

$opensslPath = Get-OpenSslPath

Invoke-Checked $opensslPath @("version")
Invoke-Checked "docker" @("info", "--format", "{{.ServerVersion}}")
Invoke-Checked "docker" @("compose", "version")
Invoke-Checked "dotnet" @("ef", "--version")

# Prepare local environment configuration.

Write-Step "Preparing environment configuration"

if (-not (Test-Path -LiteralPath $envPath)) {
    if (-not (Test-Path -LiteralPath $envExamplePath -PathType Leaf)) {
        throw ".env.example was not found."
    }

    Copy-Item -LiteralPath $envExamplePath -Destination $envPath

    Write-Host ".env created from .env.example."
} else {
    Write-Host "Existing .env file preserved."
}

if (-not (Test-EnvValue "SQL_SA_PASSWORD")) {
    $password = Read-Secret "Enter SQL Server SA password"

    if ([string]::IsNullOrWhiteSpace($password)) {
        throw "SQL Server password is required."
    }

    Set-EnvValue "SQL_SA_PASSWORD" $password
}

if (-not (Test-EnvValue "IDENTITY_EMAIL_USERNAME")) {
    Set-EnvValue "IDENTITY_EMAIL_USERNAME" (Read-Host "Enter email username")
}

if (-not (Test-EnvValue "IDENTITY_EMAIL_PASSWORD")) {
    Set-EnvValue "IDENTITY_EMAIL_PASSWORD" (Read-Secret "Enter email password")
}

if (-not (Test-EnvValue "IDENTITY_EMAIL_FROM")) {
    Set-EnvValue "IDENTITY_EMAIL_FROM" (Read-Host "Enter sender email")
}

# Generate and validate RSA keys.

Write-Step "Preparing RSA keys"

New-Item -ItemType Directory -Force -Path $keyPath | Out-Null

$hasPrivate = Test-NonEmptyFile $privateKeyPath
$hasPublic = Test-NonEmptyFile $publicKeyPath

if (-not $hasPrivate) {
    if ($hasPublic) {
        throw "Private RSA key is missing while the public key exists."
    }

    Invoke-Checked $opensslPath @(
        "genpkey", "-algorithm", "RSA",
        "-pkeyopt", "rsa_keygen_bits:2048",
        "-out", $privateKeyPath
    )

    Write-Host "New RSA private key generated."
} else {
    Write-Host "Existing RSA private key preserved."
}

Invoke-Checked $opensslPath @(
    "rsa", "-in", $privateKeyPath, "-check", "-noout"
)

$derivedPublicKey = Invoke-Checked $opensslPath @(
    "pkey", "-in", $privateKeyPath, "-pubout"
)

if (-not $hasPublic) {
    Invoke-Checked $opensslPath @(
        "pkey", "-in", $privateKeyPath,
        "-pubout", "-out", $publicKeyPath
    )

    Write-Host "RSA public key generated."
}

Invoke-Checked $opensslPath @(
    "pkey", "-pubin", "-in", $publicKeyPath,
    "-pubcheck", "-noout"
)

$keyDetails = Invoke-Checked $opensslPath @(
    "rsa", "-pubin", "-in", $publicKeyPath,
    "-text", "-noout"
)

if (($keyDetails -join "`n") -notmatch "Public-Key:\s*\((\d+)\s+bit") {
    throw "Unable to determine RSA key size."
}

if ([int]$Matches[1] -lt 2048) {
    throw "RSA key size must be at least 2048 bits."
}

$existingPublicKey = Invoke-Checked $opensslPath @(
    "pkey", "-pubin", "-in", $publicKeyPath, "-pubout"
)

if (($derivedPublicKey -join "`n") -cne ($existingPublicKey -join "`n")) {
    throw "RSA keys do not match."
}

Write-Host "RSA keys validated successfully." -ForegroundColor Green

Set-EnvValue "IDENTITY_RSA_KEYS_PATH" ($keyPath.Replace("\", "/"))

# Configure local IdentityService user secrets.

Write-Step "Configuring IdentityService user secrets"

Invoke-Checked "dotnet" @(
    "user-secrets", "set", "RsaKeys:PrivateKeyPath",
    $privateKeyPath, "--project", $identityPath
)

Invoke-Checked "dotnet" @(
    "user-secrets", "set", "RsaKeys:PublicKeyPath",
    $publicKeyPath, "--project", $identityPath
)

# Prepare external Docker volume.

Write-Step "Preparing Docker volume"

$volumes = @(Invoke-Checked "docker" @(
    "volume", "ls", "--format", "{{.Name}}"
))

if ($volumes -notcontains $volumeName) {
    Invoke-Checked "docker" @("volume", "create", $volumeName)
} else {
    Write-Host "Docker volume already exists."
}

# Validate Docker Compose configuration.

Write-Step "Validating Docker Compose configuration"

Push-Location $rootPath

$startedSqlServer = $false

try {
    Invoke-Checked "docker" @("compose", "config", "--quiet")

    $composeJson = Invoke-Checked "docker" @(
        "compose", "config", "--format", "json"
    )

    $composeConfig = ($composeJson -join "`n") | ConvertFrom-Json

    $sqlEnvironment = $composeConfig.services.$sqlServiceName.environment
    $sqlPassword = $sqlEnvironment.MSSQL_SA_PASSWORD

    if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
        $sqlPassword = $sqlEnvironment.SA_PASSWORD
    }

    if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
        throw "SQL Server password was not found in Docker Compose configuration."
    }

    # Configure the local database connection.

    Write-Step "Configuring IdentityService database connection"

    $masterConnectionString = Get-DatabaseConnectionString "master" $sqlPassword
    $connectionString = Get-DatabaseConnectionString $databaseName $sqlPassword

    Invoke-Checked "dotnet" @(
        "user-secrets", "set",
        "ConnectionStrings:DefaultConnection",
        $connectionString,
        "--project", $identityPath
    )

    # Start or reuse SQL Server.

    Write-Step "Preparing SQL Server"

    $sqlContainer = Find-RunningSqlContainer

    if (-not $sqlContainer) {
        Invoke-Checked "docker" @(
            "compose", "up", "-d", $sqlServiceName
        )

        $startedSqlServer = $true
    } else {
        Write-Host "Existing SQL Server container reused."
    }

    # Wait for SQL Server readiness.

    Write-Step "Waiting for SQL Server readiness"

    Wait-SqlServer $masterConnectionString

    # Apply pending database migrations.

    Write-Step "Applying IdentityService database migrations"

    Invoke-Checked "dotnet" @(
        "ef", "database", "update",
        "--project", $identityPath,
        "--startup-project", $identityPath,
        "--connection", $connectionString,
        "--", "--environment", "Development"
    )

    # Verify database migrations.

    Write-Step "Verifying IdentityService migrations"

    $migrations = Invoke-Checked "dotnet" @(
        "ef", "migrations", "list",
        "--project", $identityPath,
        "--startup-project", $identityPath,
        "--connection", $connectionString,
        "--", "--environment", "Development"
    )

    $migrations | Write-Host

    if (($migrations -join "`n") -match "\(Pending\)|Pending status not shown") {
        throw "Unable to verify that all database migrations are applied."
    }

    # Validate authentication storage.

    Write-Step "Validating authentication storage"

    Assert-AuthenticationStorage $connectionString

} finally {
    try {
        if ($startedSqlServer) {
            Write-Step "Stopping temporary SQL Server container"

            Invoke-Checked "docker" @(
                "compose", "stop", $sqlServiceName
            )
        }
    } finally {
        Pop-Location
    }
}

Write-Host "`nDevelopment environment is ready." -ForegroundColor Green