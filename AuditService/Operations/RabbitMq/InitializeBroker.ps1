param(
    [string]$ManagementUri = 'http://127.0.0.1:15672',
    [ValidatePattern('\A[a-z0-9-]{1,64}\z')]
    [string]$SourceService = 'identity-service'
)
$ErrorActionPreference = 'Stop'
$sourcePrefix = if ($SourceService -eq 'identity-service') { 'IDENTITY' } else { $SourceService.ToUpperInvariant().Replace('-', '_') }
$publisherUserKey = "${sourcePrefix}_AUDIT_BROKER_USER"
$publisherPasswordKey = "${sourcePrefix}_AUDIT_BROKER_PASSWORD"
foreach ($key in @('AUDIT_BROKER_ADMIN_USER','AUDIT_BROKER_ADMIN_PASSWORD','AUDIT_BROKER_USER','AUDIT_BROKER_PASSWORD',$publisherUserKey,$publisherPasswordKey)) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) { throw "Missing environment variable: $key" }
}
$publisherUser = [Environment]::GetEnvironmentVariable($publisherUserKey)
$publisherPassword = [Environment]::GetEnvironmentVariable($publisherPasswordKey)
if ($env:AUDIT_BROKER_USER -eq $publisherUser -or $env:AUDIT_BROKER_ADMIN_USER -in @($env:AUDIT_BROKER_USER,$publisherUser)) { throw 'Broker identities must be distinct.' }
$exchange = "audit.$SourceService.v1"
$queue = "audit-ingest-$SourceService-v1"
$publisherTopology = "$SourceService-bus-[a-f0-9]+"
$exchangePattern = [regex]::Escape($exchange)
$basic = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($env:AUDIT_BROKER_ADMIN_USER + ':' + $env:AUDIT_BROKER_ADMIN_PASSWORD))
$headers = @{ Authorization = 'Basic ' + $basic }
function Put-Broker($path, $value) {
    try { $null = Invoke-RestMethod -Method Put -Uri ($ManagementUri.TrimEnd('/') + '/api/' + $path) -Headers $headers -ContentType 'application/json' -Body ($value | ConvertTo-Json -Depth 8) }
    catch { throw "Broker provisioning failed for resource $path. Inspect the broker using an administrative identity." }
}
function Bind-Broker($path) {
    try { $null = Invoke-RestMethod -Method Post -Uri ($ManagementUri.TrimEnd('/') + '/api/' + $path) -Headers $headers -ContentType 'application/json' -Body '{"routing_key":"","arguments":{}}' }
    catch { throw 'Broker topology binding failed.' }
}
Put-Broker 'vhosts/audit' @{}
Put-Broker ('users/' + [Uri]::EscapeDataString($env:AUDIT_BROKER_USER)) @{password=$env:AUDIT_BROKER_PASSWORD;tags=''}
Put-Broker ('users/' + [Uri]::EscapeDataString($publisherUser)) @{password=$publisherPassword;tags=''}
Put-Broker ('permissions/audit/' + [Uri]::EscapeDataString($env:AUDIT_BROKER_USER)) @{
    configure='^(audit-bus-[a-f0-9]+|audit-ingest-[a-z0-9-]{1,64}-v1(_error|_skipped)?|audit\.[a-z0-9-]{1,64}\.v1)$'
    write='^(audit-bus-[a-f0-9]+|audit-ingest-[a-z0-9-]{1,64}-v1(_error|_skipped)?)$'
    read='^(audit-bus-[a-f0-9]+|audit-ingest-[a-z0-9-]{1,64}-v1|audit\.[a-z0-9-]{1,64}\.v1)$'
}
if ($SourceService -eq 'identity-service') { $publisherTopology = 'identity-bus-[a-f0-9]+' }
Put-Broker ('permissions/audit/' + [Uri]::EscapeDataString($publisherUser)) @{
    configure="^($publisherTopology|$exchangePattern)$"
    write="^($publisherTopology|$exchangePattern)$"
    read="^${publisherTopology}$"
}
foreach ($name in @($exchange,$queue)) {
    Put-Broker ('exchanges/audit/' + $name) @{type='fanout';durable=$true;auto_delete=$false;internal=$false;arguments=@{}}
}
Put-Broker "queues/audit/$queue" @{durable=$true;auto_delete=$false;arguments=@{'x-queue-type'='quorum'}}
Bind-Broker "bindings/audit/e/$exchange/e/$queue"
Bind-Broker "bindings/audit/e/$queue/q/$queue"
Write-Output 'Audit source topology and restricted service identities provisioned. Producers may now be enabled.'
