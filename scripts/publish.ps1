# SilentPrintBridge - PowerShell Publish Script

param(
    [switch]$SelfContained = $false,
    [string]$OutputPath = "..\..\publish"
)

Write-Host "Publishing SilentPrintBridge..." -ForegroundColor Cyan

$projectPath = ".\SilentPrintBridge.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Host "ERROR: Project file not found at $projectPath" -ForegroundColor Red
    Write-Host "Please run this script from the src/SilentPrintBridge directory" -ForegroundColor Yellow
    exit 1
}

# Create output directory
$fullOutputPath = Join-Path $PSScriptRoot $OutputPath
New-Item -ItemType Directory -Force -Path $fullOutputPath | Out-Null

# Build publish command
$publishArgs = @(
    "publish",
    $projectPath,
    "-c", "Release",
    "-r", "win-x64",
    "-o", $fullOutputPath,
    "--no-self-contained"
)

if ($SelfContained) {
    $publishArgs[-1] = "--self-contained"
    Write-Host "Publishing as self-contained..." -ForegroundColor Yellow
} else {
    Write-Host "Publishing framework-dependent (requires .NET 8 Runtime)..." -ForegroundColor Yellow
}

# Execute publish
Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
& dotnet $publishArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPublish completed successfully!" -ForegroundColor Green
    Write-Host "Output directory: $fullOutputPath" -ForegroundColor Cyan
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "  1. Review appsettings.json and configure your printer name"
    Write-Host "  2. Run install-service.ps1 as Administrator to install the Windows service"
} else {
    Write-Host "`nPublish failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
