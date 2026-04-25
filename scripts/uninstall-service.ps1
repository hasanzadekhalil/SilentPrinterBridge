# SilentPrintBridge - Windows Service Uninstallation Script
# Run as Administrator

param(
    [switch]$RemoveData = $false,
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

Write-Host "Uninstalling SilentPrintBridge Windows Service..." -ForegroundColor Cyan

$serviceName = "SilentPrintBridge"

# Check if service exists
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($service) {
    # Stop service
    if ($service.Status -eq "Running") {
        Write-Host "Stopping service..." -ForegroundColor Yellow
        Stop-Service -Name $serviceName -Force
        Start-Sleep -Seconds 2
    }

    # Delete service
    Write-Host "Removing service..." -ForegroundColor Gray
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2

    Write-Host "Service removed successfully" -ForegroundColor Green
} else {
    Write-Host "Service not found (already uninstalled)" -ForegroundColor Yellow
}

# Remove installation directory
if (Test-Path $InstallPath) {
    Write-Host "Removing installation directory: $InstallPath" -ForegroundColor Gray
    Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Installation directory removed" -ForegroundColor Green
} else {
    Write-Host "Installation directory not found" -ForegroundColor Yellow
}

# Remove data directory if requested
if ($RemoveData) {
    if (Test-Path $DataPath) {
        Write-Host "Removing data directory: $DataPath" -ForegroundColor Gray
        Remove-Item -Path $DataPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Data directory removed" -ForegroundColor Green
    }
} else {
    Write-Host "`nData directory preserved: $DataPath" -ForegroundColor Cyan
    Write-Host "Use -RemoveData switch to delete logs and configuration" -ForegroundColor Gray
}

Write-Host "`nUninstallation completed!" -ForegroundColor Green
