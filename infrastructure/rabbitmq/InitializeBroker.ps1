param([string]$ManagementUri = 'http://127.0.0.1:15672')
$ErrorActionPreference = 'Stop'
foreach ($key in @('AUDIT_BROKER_ADMIN_USER','AUDIT_BROKER_ADMIN_PASSWORD','AUDIT_BROKER_USER','AUDIT_BROKER_PASSWORD','IDENTITY_AUDIT_BROKER_USER','IDENTITY_AUDIT_BROKER_PASSWORD')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) { throw "Missing environment variable: $key" }
}
if ($env:AUDIT_BROKER_USER -eq $env:IDENTITY_AUDIT_BROKER_USER -or $env:AUDIT_BROKER_ADMIN_USER -in @($env:AUDIT_BROKER_USER,$env:IDENTITY_AUDIT_BROKER_USER)) { throw 'Broker identities must be distinct.' }
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
Put-Broker ('users/' + [Uri]::EscapeDataString($env:IDENTITY_AUDIT_BROKER_USER)) @{password=$env:IDENTITY_AUDIT_BROKER_PASSWORD;tags=''}
Put-Broker ('permissions/audit/' + [Uri]::EscapeDataString($env:AUDIT_BROKER_USER)) @{
    configure='^(audit-bus-[a-f0-9]+|audit-ingest-identity-service-v1(_error|_skipped)?|audit\.identity-service\.v1)$'
    write='^(audit-bus-[a-f0-9]+|audit-ingest-identity-service-v1(_error|_skipped)?)$'
    read='^(audit-bus-[a-f0-9]+|audit-ingest-identity-service-v1|audit\.identity-service\.v1)$'
}
Put-Broker ('permissions/audit/' + [Uri]::EscapeDataString($env:IDENTITY_AUDIT_BROKER_USER)) @{
    configure='^(identity-bus-[a-f0-9]+|audit\.identity-service\.v1)$'
    write='^(identity-bus-[a-f0-9]+|audit\.identity-service\.v1)$'
    read='^identity-bus-[a-f0-9]+$'
}
foreach ($exchange in @('audit.identity-service.v1','audit-ingest-identity-service-v1')) {
    Put-Broker ('exchanges/audit/' + $exchange) @{type='fanout';durable=$true;auto_delete=$false;internal=$false;arguments=@{}}
}
Put-Broker 'queues/audit/audit-ingest-identity-service-v1' @{durable=$true;auto_delete=$false;arguments=@{'x-queue-type'='quorum'}}
Bind-Broker 'bindings/audit/e/audit.identity-service.v1/e/audit-ingest-identity-service-v1'
Bind-Broker 'bindings/audit/e/audit-ingest-identity-service-v1/q/audit-ingest-identity-service-v1'
Write-Output 'Audit source topology and restricted service identities provisioned. Producers may now be enabled.'
