# ArtyVoiceBot - Azure Windows Server Deployment Script
# Run this script ON your Azure Windows Server as Administrator

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerDnsName,  # e.g., "arty-bot.eastus.cloudapp.azure.com" or "bot.yourdomain.com"
    
    [Parameter(Mandatory=$true)]
    [string]$BotAppId,
    
    [Parameter(Mandatory=$true)]
    [string]$BotAppSecret,
    
    [Parameter(Mandatory=$false)]
    [string]$TenantId = "98b652bf-e6ca-4869-aa75-1eeedc70e82f",
    
    [Parameter(Mandatory=$false)]
    [string]$DeployPath = "C:\ArtyVoiceBot"
)

Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "ArtyVoiceBot - Azure Deployment Script" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Server DNS: $ServerDnsName" -ForegroundColor White
Write-Host "  Bot App ID: $BotAppId" -ForegroundColor White
Write-Host "  Tenant ID: $TenantId" -ForegroundColor White
Write-Host "  Deploy Path: $DeployPath" -ForegroundColor White
Write-Host ""

# Step 1: Create SSL Certificate
Write-Host "[Step 1/7] Creating SSL Certificate..." -ForegroundColor Yellow

$cert = New-SelfSignedCertificate `
    -DnsName $ServerDnsName `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -KeyExportPolicy Exportable `
    -Provider "Microsoft RSA SChannel Cryptographic Provider" `
    -KeySpec KeyExchange `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

$certThumbprint = $cert.Thumbprint
Write-Host "  ✓ Certificate created: $certThumbprint" -ForegroundColor Green
Write-Host ""

# Step 2: Configure Firewall
Write-Host "[Step 2/7] Configuring Windows Firewall..." -ForegroundColor Yellow

New-NetFirewallRule -DisplayName "ArtyVoiceBot-Signaling" `
    -Direction Inbound `
    -LocalPort 9441 `
    -Protocol TCP `
    -Action Allow `
    -ErrorAction SilentlyContinue | Out-Null

New-NetFirewallRule -DisplayName "ArtyVoiceBot-Media" `
    -Direction Inbound `
    -LocalPort 8445 `
    -Protocol TCP `
    -Action Allow `
    -ErrorAction SilentlyContinue | Out-Null

Write-Host "  ✓ Firewall rules created" -ForegroundColor Green
Write-Host ""

# Step 3: Configure URL ACL
Write-Host "[Step 3/7] Configuring URL ACL..." -ForegroundColor Yellow

netsh http add urlacl url=https://+:9441/ user=Everyone | Out-Null
netsh http add urlacl url=http://+:9442/ user=Everyone | Out-Null

Write-Host "  ✓ URL ACL configured" -ForegroundColor Green
Write-Host ""

# Step 4: Bind SSL Certificate
Write-Host "[Step 4/7] Binding SSL Certificate..." -ForegroundColor Yellow

$appIdGuid = "{$BotAppId}"
netsh http add sslcert ipport=0.0.0.0:9441 certhash=$certThumbprint appid=$appIdGuid | Out-Null

Write-Host "  ✓ SSL certificate bound to port 9441" -ForegroundColor Green
Write-Host ""

# Step 5: Update appsettings.json
Write-Host "[Step 5/7] Updating appsettings.json..." -ForegroundColor Yellow

$appsettingsPath = Join-Path $DeployPath "appsettings.json"

if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    
    $appsettings.BotConfiguration.AadAppId = $BotAppId
    $appsettings.BotConfiguration.AadAppSecret = $BotAppSecret
    $appsettings.BotConfiguration.TenantId = $TenantId
    $appsettings.BotConfiguration.CallbackDomain = $ServerDnsName
    $appsettings.BotConfiguration.ServiceDnsName = $ServerDnsName
    $appsettings.BotConfiguration.ServiceCname = $ServerDnsName
    $appsettings.BotConfiguration.CertificateThumbprint = $certThumbprint
    
    $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
    
    Write-Host "  ✓ appsettings.json updated" -ForegroundColor Green
} else {
    Write-Host "  ! appsettings.json not found at $appsettingsPath" -ForegroundColor Yellow
    Write-Host "    Please update it manually" -ForegroundColor Yellow
}
Write-Host ""

# Step 6: Build the Bot
Write-Host "[Step 6/7] Building ArtyVoiceBot..." -ForegroundColor Yellow

Push-Location $DeployPath
try {
    dotnet build -c Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Build successful" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Build failed" -ForegroundColor Red
        Pop-Location
        exit 1
    }
} finally {
    Pop-Location
}
Write-Host ""

# Step 7: Summary
Write-Host "[Step 7/7] Deployment Summary" -ForegroundColor Yellow
Write-Host ""
Write-Host "✅ Deployment Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Cyan
Write-Host "  Server URL: https://$ServerDnsName:9441" -ForegroundColor White
Write-Host "  Certificate: $certThumbprint" -ForegroundColor White
Write-Host "  Deploy Path: $DeployPath" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Update Azure Bot callback URL to:" -ForegroundColor White
Write-Host "     https://$ServerDnsName`:9441/api/callback/calling" -ForegroundColor Yellow
Write-Host ""
Write-Host "  2. Start the bot:" -ForegroundColor White
Write-Host "     cd $DeployPath" -ForegroundColor Gray
Write-Host "     dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Test from external:" -ForegroundColor White
Write-Host "     curl https://$ServerDnsName`:9441/api/meeting/health" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. Join a meeting:" -ForegroundColor White
Write-Host "     POST https://$ServerDnsName`:9441/api/meeting/join" -ForegroundColor Gray
Write-Host ""
Write-Host "  5. Check audio files:" -ForegroundColor White
Write-Host "     C:\Users\<YourUser>\AppData\Local\AudioCapture" -ForegroundColor Gray
Write-Host ""
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

