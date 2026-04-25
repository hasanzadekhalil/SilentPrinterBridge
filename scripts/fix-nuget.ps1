# Fix NuGet Package Restore Issues

Write-Host "Fixing NuGet package restore issues..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Clear NuGet cache
Write-Host "Step 1: Clearing NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear

# Step 2: Add NuGet source
Write-Host ""
Write-Host "Step 2: Configuring NuGet sources..." -ForegroundColor Yellow
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org --configfile $env:APPDATA\NuGet\NuGet.Config 2>$null

# Step 3: Restore packages
Write-Host ""
Write-Host "Step 3: Restoring packages..." -ForegroundColor Yellow
cd ..\src\SilentPrintBridge
dotnet restore --force --no-cache

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "SUCCESS! Packages restored." -ForegroundColor Green
    Write-Host ""
    Write-Host "Now run: CREATE_PORTABLE_PACKAGE.bat" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "ERROR: Package restore failed." -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible causes:" -ForegroundColor Yellow
    Write-Host "  1. No internet connection" -ForegroundColor Gray
    Write-Host "  2. Firewall blocking NuGet" -ForegroundColor Gray
    Write-Host "  3. Proxy settings needed" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Try:" -ForegroundColor Yellow
    Write-Host "  - Check internet connection" -ForegroundColor Gray
    Write-Host "  - Disable firewall temporarily" -ForegroundColor Gray
    Write-Host "  - Run as Administrator" -ForegroundColor Gray
}

cd ..\..\scripts
