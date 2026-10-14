# Lokal Supabase stack'inin anahtarlarını E2E testlerinin beklediği değişkenlere yazar.
#
# KULLANIM: nokta ile kaynak göster —  . ./scripts/e2e-env.ps1
# (`./scripts/e2e-env.ps1` ayrı bir süreçte koşar ve değişkenler kaybolur.)

$ErrorActionPreference = 'Stop'

$status = npx --yes supabase status -o env 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "Lokal Supabase stack'i ayakta değil. Önce: npx supabase start"
}

function Get-StatusValue([string]$Key) {
    ($status | Select-String "^$Key=" | Select-Object -First 1) -replace "^$Key=", '' -replace '"', ''
}

$env:E2E_SUPABASE_URL = Get-StatusValue 'API_URL'
$env:E2E_SUPABASE_ANON_KEY = Get-StatusValue 'ANON_KEY'
$env:E2E_SUPABASE_SERVICE_ROLE_KEY = Get-StatusValue 'SERVICE_ROLE_KEY'
if (-not $env:E2E_POSTGRES) {
    $env:E2E_POSTGRES = 'Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres'
}

Write-Host "E2E ortamı hazır:"
Write-Host "  E2E_SUPABASE_URL = $($env:E2E_SUPABASE_URL)"
Write-Host "  E2E_SUPABASE_ANON_KEY = $($env:E2E_SUPABASE_ANON_KEY.Substring(0,12))…"
Write-Host "  E2E_SUPABASE_SERVICE_ROLE_KEY = $($env:E2E_SUPABASE_SERVICE_ROLE_KEY.Substring(0,12))…"
