# Get-Sha256Hash.ps1
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, HelpMessage = "Enter the plain text string you want to hash.")]
    [string]$SecretText
)

try {
    # 1. Convert the plain text string into a byte array
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($SecretText)

    # 2. Compute the cryptographic SHA-256 byte array stream
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $hashBytes = $sha256.ComputeHash($bytes)
    $sha256.Dispose()

    # 3. Format the bytes into a clean, 64-character lowercase hex string
    $hexHash = ($hashBytes | ForEach-Object { $_.ToString("x2") }) -join ""

    # 4. Output the result and push it straight to the Windows Clipboard
    Write-Host ""
    Write-Host "=================== NII CRYPTO TOOL ===================" -ForegroundColor Cyan
    
    Write-Host "Raw Token:   " -NoNewline
    Write-Host $SecretText -ForegroundColor Yellow
    
    Write-Host "SHA256 Hash: " -NoNewline
    Write-Host $hexHash -ForegroundColor Green
    
    Write-Host "=======================================================" -ForegroundColor Cyan
    
    $hexHash | clip
    Write-Host "[Success] Hash copied directly to your Windows clipboard!" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Error "An unexpected error occurred during the cryptographic hashing phase: $_"
}
