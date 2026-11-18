param(
    [string]$ApiUrl = "http://127.0.0.1:8081"
)

$envFile = Join-Path $PSScriptRoot "..\..\.env.production.local"
$apiKey = ""

if (Test-Path $envFile) {
    foreach ($line in Get-Content $envFile) {
        if ($line -match '^API_KEY=(.*)$') {
            $apiKey = $Matches[1]
            break
        }
    }
}

if (-not $apiKey) {
    Write-Error "No se encontró API_KEY en .env.production.local"
    exit 1
}

try {
    Invoke-RestMethod `
        -Uri "$ApiUrl/api/auth/seed-admin" `
        -Method Post `
        -Headers @{ "X-API-KEY" = $apiKey } `
        -TimeoutSec 30
}
catch {
    Write-Error "No se pudo crear el usuario admin: $($_.Exception.Message)"
    exit 1
}
