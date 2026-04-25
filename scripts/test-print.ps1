# SilentPrintBridge - Test Print Script

param(
    [string]$Endpoint = "http://127.0.0.1:17878",
    [string]$PrinterName = "",
    [string]$ApiKey = ""
)

Write-Host "SilentPrintBridge Test Script" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

# Build headers
$headers = @{
    "Content-Type" = "application/json"
}

if ($ApiKey) {
    $headers["X-SilentPrintBridge-Key"] = $ApiKey
}

# Test 1: Health Check
Write-Host "1. Testing /health endpoint..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$Endpoint/health" -Method Get -Headers $headers
    Write-Host "   Status: OK" -ForegroundColor Green
    Write-Host "   Service: $($health.service)" -ForegroundColor Gray
    Write-Host "   Version: $($health.version)" -ForegroundColor Gray
    Write-Host "   Printer Configured: $($health.printerConfigured)" -ForegroundColor Gray
    Write-Host "   Configured Printer: $($health.printerName)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "   FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Is the service running?" -ForegroundColor Yellow
    exit 1
}

# Test 2: List Printers
Write-Host "2. Testing /printers endpoint..." -ForegroundColor Yellow
try {
    $printers = Invoke-RestMethod -Uri "$Endpoint/printers" -Method Get -Headers $headers
    Write-Host "   Found $($printers.printers.Count) printer(s):" -ForegroundColor Green
    foreach ($printer in $printers.printers) {
        $defaultTag = if ($printer.isDefault) { " [DEFAULT]" } else { "" }
        Write-Host "     - $($printer.name)$defaultTag" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "   FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 3: Test Print
Write-Host "3. Testing /test-print endpoint..." -ForegroundColor Yellow

$testPrintBody = @{}
if ($PrinterName) {
    $testPrintBody["printerName"] = $PrinterName
}

try {
    $result = Invoke-RestMethod -Uri "$Endpoint/test-print" -Method Post -Headers $headers -Body ($testPrintBody | ConvertTo-Json)

    if ($result.success) {
        Write-Host "   SUCCESS!" -ForegroundColor Green
        Write-Host "   Job ID: $($result.jobId)" -ForegroundColor Gray
        Write-Host "   Printer: $($result.printerName)" -ForegroundColor Gray
        Write-Host "   Message: $($result.message)" -ForegroundColor Gray
    } else {
        Write-Host "   FAILED: $($result.error)" -ForegroundColor Red
        Write-Host "   Error Code: $($result.errorCode)" -ForegroundColor Yellow
    }
    Write-Host ""
} catch {
    Write-Host "   FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 4: Plain Text Print
Write-Host "4. Testing plain text print..." -ForegroundColor Yellow

$textPrintBody = @{
    mode = "text"
    data = "SilentPrintBridge Test`nPlain Text Mode`n$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n`nThis is a test receipt.`nLine 1`nLine 2`nLine 3"
    encoding = "utf8"
    cut = $true
    jobName = "Text Test"
}

if ($PrinterName) {
    $textPrintBody["printerName"] = $PrinterName
}

try {
    $result = Invoke-RestMethod -Uri "$Endpoint/print" -Method Post -Headers $headers -Body ($textPrintBody | ConvertTo-Json)

    if ($result.success) {
        Write-Host "   SUCCESS!" -ForegroundColor Green
        Write-Host "   Job ID: $($result.jobId)" -ForegroundColor Gray
        Write-Host "   Printer: $($result.printerName)" -ForegroundColor Gray
        Write-Host "   Message: $($result.message)" -ForegroundColor Gray
    } else {
        Write-Host "   FAILED: $($result.error)" -ForegroundColor Red
        Write-Host "   Error Code: $($result.errorCode)" -ForegroundColor Yellow
    }
    Write-Host ""
} catch {
    Write-Host "   FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "All tests completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  - Open samples\sample.html in Chrome or Edge for browser testing"
Write-Host "  - Check logs at C:\ProgramData\SilentPrintBridge\logs"
