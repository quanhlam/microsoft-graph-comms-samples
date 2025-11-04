# Setup Certificate for ArtyVoiceBot
# Run this as Administrator in PowerShell

param(
    [Parameter(Mandatory=$false)]
    [string]$DnsName = "arty-bot.ngrok.io"
)

Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "ArtyVoiceBot Certificate Setup" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "DNS Name: $DnsName" -ForegroundColor Green
Write-Host ""

# Create self-signed certificate
Write-Host "Creating self-signed certificate..." -ForegroundColor Yellow

try {
    $cert = New-SelfSignedCertificate `
        -DnsName $DnsName `
        -CertStoreLocation "Cert:\LocalMachine\My" `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -KeyLength 2048 `
        -KeyAlgorithm RSA `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(2)
    
    Write-Host "Certificate created successfully!" -ForegroundColor Green
    Write-Host ""
    
    # Display certificate details
    Write-Host "Certificate Details:" -ForegroundColor Cyan
    Write-Host "  Subject:    $($cert.Subject)" -ForegroundColor White
    Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor White
    Write-Host "  Valid From: $($cert.NotBefore)" -ForegroundColor White
    Write-Host "  Valid To:   $($cert.NotAfter)" -ForegroundColor White
    Write-Host ""
    
    # Save thumbprint to file
    $thumbprintFile = Join-Path $PSScriptRoot "certificate-thumbprint.txt"
    $cert.Thumbprint | Out-File -FilePath $thumbprintFile -Encoding ASCII
    
    Write-Host "Thumbprint saved to: $thumbprintFile" -ForegroundColor Green
    Write-Host ""
    
    # Update appsettings.json with thumbprint
    Write-Host "Updating appsettings.json..." -ForegroundColor Yellow
    $appsettingsPath = Join-Path $PSScriptRoot "appsettings.json"
    
    if (Test-Path $appsettingsPath) {
        $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $appsettings.BotConfiguration.CertificateThumbprint = $cert.Thumbprint
        $appsettings.BotConfiguration.ServiceDnsName = $DnsName
        $appsettings.BotConfiguration.ServiceCname = $DnsName
        
        $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
        Write-Host "appsettings.json updated!" -ForegroundColor Green
    } else {
        Write-Host "Warning: appsettings.json not found at $appsettingsPath" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host "IMPORTANT: Copy this thumbprint to your configuration!" -ForegroundColor Yellow
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  $($cert.Thumbprint)" -ForegroundColor White
    Write-Host ""
    Write-Host "Add this to appsettings.json:" -ForegroundColor Cyan
    Write-Host '  "CertificateThumbprint": "' -NoNewline -ForegroundColor Gray
    Write-Host $cert.Thumbprint -NoNewline -ForegroundColor White
    Write-Host '"' -ForegroundColor Gray
    Write-Host ""
    
    # Copy to clipboard if possible
    try {
        Set-Clipboard -Value $cert.Thumbprint
        Write-Host "Thumbprint copied to clipboard!" -ForegroundColor Green
    } catch {
        Write-Host "Note: Could not copy to clipboard" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Update appsettings.json with your Azure Bot credentials" -ForegroundColor White
    Write-Host "  2. Update ServiceDnsName with your ngrok domain" -ForegroundColor White
    Write-Host "  3. Run the bot: dotnet run" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host "ERROR: Failed to create certificate" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "Certificate setup complete!" -ForegroundColor Green
Write-Host ""

