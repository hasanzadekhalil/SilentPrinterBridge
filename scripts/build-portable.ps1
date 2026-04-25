# SilentPrintBridge Portable Package Builder

param(
    [string]$OutputDir = "..\..\SilentPrintBridge-Portable"
)

Write-Host "Building SilentPrintBridge Portable Package..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Publish self-contained
Write-Host "Step 1: Publishing self-contained executable..." -ForegroundColor Yellow
$publishPath = "..\..\publish-portable"

cd ..\src\SilentPrintBridge

dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Publish failed!" -ForegroundColor Red
    exit 1
}

cd ..\..\scripts

# Step 2: Create portable directory structure
Write-Host ""
Write-Host "Step 2: Creating portable package structure..." -ForegroundColor Yellow

if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path "$OutputDir\data\logs" | Out-Null
New-Item -ItemType Directory -Force -Path "$OutputDir\data\temp" | Out-Null
New-Item -ItemType Directory -Force -Path "$OutputDir\samples" | Out-Null

# Step 3: Copy files
Write-Host "Step 3: Copying files..." -ForegroundColor Yellow

Copy-Item "$publishPath\SilentPrintBridge.exe" -Destination $OutputDir
Copy-Item "$publishPath\appsettings.json" -Destination $OutputDir

# Update config for portable mode
$configPath = Join-Path $OutputDir "appsettings.json"
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$config.Logging.LogDirectory = ".\data\logs"
$config.Pdf.TempDirectory = ".\data\temp"
$config | ConvertTo-Json -Depth 10 | Set-Content $configPath

# Copy samples
Copy-Item "..\samples\sample.html" -Destination "$OutputDir\samples\"
Copy-Item "..\samples\sample-requests.http" -Destination "$OutputDir\samples\"

# Copy launcher
Copy-Item "START_PORTABLE.bat" -Destination "$OutputDir\START.bat"

# Step 4: Create README
Write-Host "Step 4: Creating portable README..." -ForegroundColor Yellow

$readmeContent = @"
# SilentPrintBridge - Portable Edition

## Quick Start

1. **Configure Printer:**
   - Right-click 'appsettings.json' → Edit with Notepad
   - Change "PrinterName" to your printer's exact name
   - Save and close

2. **Run:**
   - Double-click 'START.bat'
   - Service will start in console mode
   - Open 'samples\sample.html' in Chrome or Edge

3. **Test:**
   - Click "Health Check" in sample.html
   - Click "Built-In Test Receipt"
   - Your printer should print immediately

## Features

- No installation required
- No .NET Runtime required (self-contained)
- Runs from any folder (USB drive, Desktop, etc.)
- All data stored in 'data' subfolder
- Easy to backup and move

## Configuration

Edit 'appsettings.json':

```json
{
  "Printer": {
    "PrinterName": "EPSON TM-T20III"
  }
}
```

## Finding Your Printer Name

Run in Command Prompt:
```
SilentPrintBridge.exe --list-printers
```

## API Endpoint

When running: http://127.0.0.1:17878

## Stopping the Service

Press Ctrl+C in the console window

## Logs

Check: data\logs\silentprintbridge-YYYYMMDD.log

## Support

See full documentation in the main project folder.
"@

Set-Content -Path "$OutputDir\README.txt" -Value $readmeContent

# Step 5: Create printer setup helper
Write-Host "Step 5: Creating setup helper..." -ForegroundColor Yellow

$setupBat = @"
@echo off
echo ========================================
echo   SilentPrintBridge - Printer Setup
echo ========================================
echo.
echo Listing installed printers...
echo.
SilentPrintBridge.exe --list-printers
echo.
echo ========================================
echo.
echo Copy the exact printer name above and paste it into appsettings.json
echo.
pause
notepad appsettings.json
"@

Set-Content -Path "$OutputDir\SETUP_PRINTER.bat" -Value $setupBat

# Step 6: Create test helper
$testBat = @"
@echo off
echo ========================================
echo   SilentPrintBridge - Test Print
echo ========================================
echo.
echo Make sure the service is running first!
echo (Run START.bat in another window)
echo.
pause
echo.
echo Sending test print...
SilentPrintBridge.exe --test-print
echo.
pause
"@

Set-Content -Path "$OutputDir\TEST_PRINT.bat" -Value $testBat

# Step 7: Get file size
$exeSize = (Get-Item "$OutputDir\SilentPrintBridge.exe").Length / 1MB

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Portable Package Created Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Location: $OutputDir" -ForegroundColor Cyan
Write-Host "EXE Size: $([math]::Round($exeSize, 2)) MB" -ForegroundColor Cyan
Write-Host ""
Write-Host "Package Contents:" -ForegroundColor Yellow
Write-Host "  - SilentPrintBridge.exe (self-contained)" -ForegroundColor Gray
Write-Host "  - START.bat (double-click to run)" -ForegroundColor Gray
Write-Host "  - SETUP_PRINTER.bat (configure printer)" -ForegroundColor Gray
Write-Host "  - TEST_PRINT.bat (test printing)" -ForegroundColor Gray
Write-Host "  - appsettings.json (configuration)" -ForegroundColor Gray
Write-Host "  - README.txt (instructions)" -ForegroundColor Gray
Write-Host "  - samples\sample.html (browser test)" -ForegroundColor Gray
Write-Host "  - data\ (logs and temp files)" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Copy the entire folder to any location" -ForegroundColor White
Write-Host "  2. Double-click SETUP_PRINTER.bat" -ForegroundColor White
Write-Host "  3. Double-click START.bat" -ForegroundColor White
Write-Host "  4. Open samples\sample.html in browser" -ForegroundColor White
Write-Host ""
Write-Host "The package is ready to use!" -ForegroundColor Green
