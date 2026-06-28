# ApplyLocalTokens.ps1
param([string]$projectDir)

$jsonPath = Join-Path $projectDir "appsettings.local.json"

# Fallback check if the developer hasn't created the local secrets file yet
if (-not (Test-Path $jsonPath)) {
    Write-Warning "Local environment file 'appsettings.local.json' not found. Skipping token replacement."
    exit 0
}

# Load local development configurations
$secrets = Get-Content $jsonPath | ConvertFrom-Json

# Files to process
$webConfigPath = Join-Path $projectDir "Web.config"
$cryptoEnginePath = Join-Path $projectDir "Security\Services\CryptographyEngine.cs" # Adjust path to your file location

# 1. Update Web.config Placeholders
if (Test-Path $webConfigPath) {
    $content = Get-Content $webConfigPath -Raw
    $content = $content -replace "__SECURE_CONN_PLACEHOLDER__", $secrets.SecureConn
    Set-Content $webConfigPath $content
}

# 2. Update CryptoEngine.cs Placeholder
if (Test-Path $cryptoEnginePath) {
    $content = Get-Content $cryptoEnginePath -Raw
    $content = $content -replace "__MASTER_CRYPTO_KEY__", $secrets.MASTER_CRYPTO_KEY
    Set-Content $cryptoEnginePath $content
}

Write-Host "Successfully tokenized local development files."
