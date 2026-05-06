# SilentPrintBridge - Windows Service Installation Script
# Run as Administrator

param(
    [string]$SourcePath = "..\..\publish",
    [string]$InstallPath = "C:\Program Files\SilentPrintBridge",
    [string]$DataPath = "C:\ProgramData\SilentPrintBridge"
)

# Check for Administrator privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Installing SilentPrintBridge Windows Service..." -ForegroundColor Cyan

# Resolve source path
$fullSourcePath = Join-Path $PSScriptRoot $SourcePath
if (-not (Test-Path $fullSourcePath)) {
    Write-Host "ERROR: Source path not found: $fullSourcePath" -ForegroundColor Red
    Write-Host "Please run publish.ps1 first" -ForegroundColor Yellow
    exit 1
}

$exePath = Join-Path $fullSourcePath "SilentPrintBridge.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: SilentPrintBridge.exe not found in $fullSourcePath" -ForegroundColor Red
    exit 1
}

# Stop existing service if running
$serviceName = "SilentPrintBridge"
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# Create installation directory
Write-Host "Creating installation directory: $InstallPath" -ForegroundColor Gray
New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null

# Create data directories
Write-Host "Creating data directories: $DataPath" -ForegroundColor Gray
New-Item -ItemType Directory -Force -Path "$DataPath\logs" | Out-Null
New-Item -ItemType Directory -Force -Path "$DataPath\temp" | Out-Null

# Copy files
Write-Host "Copying files to $InstallPath..." -ForegroundColor Gray
Copy-Item -Path "$fullSourcePath\*" -Destination $InstallPath -Recurse -Force

# Backup existing config if present
$configPath = Join-Path $InstallPath "appsettings.json"
$backupConfigPath = Join-Path $InstallPath "appsettings.json.backup"

if (Test-Path $configPath) {
    Write-Host "Backing up existing configuration..." -ForegroundColor Yellow
    Copy-Item -Path $configPath -Destination $backupConfigPath -Force
}

# Install or update service
$serviceExePath = Join-Path $InstallPath "SilentPrintBridge.exe"

if ($existingService) {
    Write-Host "Updating existing service..." -ForegroundColor Yellow

    # Delete and recreate service
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Creating Windows service..." -ForegroundColor Gray
New-Service -Name $serviceName `
    -BinaryPathName $serviceExePath `
    -DisplayName "Silent Print Bridge" `
    -Description "Local HTTP bridge for silent browser-to-printer communication" `
    -StartupType Automatic

sc.exe config $serviceName start= delayed-auto depend= Spooler | Out-Null
sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

# Start service
Write-Host "Starting service..." -ForegroundColor Gray
Start-Service -Name $serviceName

# Wait and check status
Start-Sleep -Seconds 3
$service = Get-Service -Name $serviceName

if ($service.Status -eq "Running") {
    Write-Host "`nInstallation completed successfully!" -ForegroundColor Green
    Write-Host "`nService Status: Running" -ForegroundColor Green
    Write-Host "Service Name: $serviceName" -ForegroundColor Cyan
    Write-Host "Install Path: $InstallPath" -ForegroundColor Cyan
    Write-Host "Data Path: $DataPath" -ForegroundColor Cyan
    Write-Host "Endpoint: http://127.0.0.1:17878" -ForegroundColor Cyan

    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "  1. Edit $configPath"
    Write-Host "  2. Set your printer name in the 'PrinterName' field"
    Write-Host "  3. Restart the service: Restart-Service $serviceName"
    Write-Host "  4. Test with: Invoke-RestMethod -Uri http://127.0.0.1:17878/health"
    Write-Host "  5. Open samples\sample.html in Chrome or Edge"
} else {
    Write-Host "`nWARNING: Service installed but not running" -ForegroundColor Yellow
    Write-Host "Status: $($service.Status)" -ForegroundColor Yellow
    Write-Host "Check logs at: $DataPath\logs" -ForegroundColor Cyan
}
