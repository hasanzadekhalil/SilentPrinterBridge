# SilentPrintBridge Troubleshooting Guide

## Table of Contents

1. [Service Issues](#service-issues)
2. [Connection Issues](#connection-issues)
3. [Printer Issues](#printer-issues)
4. [Print Quality Issues](#print-quality-issues)
5. [Configuration Issues](#configuration-issues)
6. [Security Issues](#security-issues)
7. [Performance Issues](#performance-issues)
8. [Diagnostic Commands](#diagnostic-commands)

---

## Service Issues

### Service Won't Start

**Symptoms:**
- Service shows "Stopped" in Windows Services
- Error in Windows Event Viewer
- No response from API endpoints

**Solutions:**

1. **Check Windows Event Viewer**
   ```
   Event Viewer → Windows Logs → Application
   Look for "SilentPrintBridge" source
   ```

2. **Run in Console Mode**
   ```powershell
   cd "C:\Program Files\SilentPrintBridge"
   .\SilentPrintBridge.exe --console
   ```
   This will show startup errors directly.

3. **Check Port Conflict**
   ```powershell
   netstat -ano | findstr :17878
   ```
   If port is in use, change port in `appsettings.json` or stop conflicting service.

4. **Check Configuration Syntax**
   - Validate `appsettings.json` is valid JSON
   - Use online JSON validator
   - Check for missing commas, quotes, brackets

5. **Check Permissions**
   - Service must have read access to install directory
   - Service must have write access to log directory
   - Run: `icacls "C:\ProgramData\SilentPrintBridge" /grant "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)F"`

6. **Check .NET Runtime**
   ```powershell
   dotnet --list-runtimes
   ```
   Ensure .NET 8.0 runtime is installed (if not using self-contained build).

### Service Starts But Stops Immediately

**Symptoms:**
- Service starts then stops within seconds
- No log files created

**Solutions:**

1. **Check Log Directory Permissions**
   ```powershell
   New-Item -ItemType Directory -Force -Path "C:\ProgramData\SilentPrintBridge\logs"
   icacls "C:\ProgramData\SilentPrintBridge\logs" /grant "Everyone:(OI)(CI)F"
   ```

2. **Check Configuration Path**
   - Ensure `appsettings.json` exists in install directory
   - Check file is not corrupted

3. **Review Startup Logs**
   - Check first few lines in log file
   - Look for configuration loading errors

---

## Connection Issues

### Browser Cannot Connect to Service

**Symptoms:**
- `ERR_CONNECTION_REFUSED` in browser
- `fetch` fails with network error
- Health check fails

**Solutions:**

1. **Verify Service is Running**
   ```powershell
   Get-Service SilentPrintBridge
   ```
   Status should be "Running".

2. **Test Endpoint Directly**
   ```powershell
   Invoke-RestMethod -Uri "http://127.0.0.1:17878/health"
   ```

3. **Check Firewall**
   - Windows Firewall may block localhost connections
   - Add exception for SilentPrintBridge.exe
   - Or temporarily disable firewall to test

4. **Check Endpoint URL**
   - Must be exactly `http://127.0.0.1:17878`
   - Not `localhost` (unless configured)
   - Not `https` (service uses HTTP only)

5. **Check Browser Console**
   - Open Developer Tools (F12)
   - Check Console tab for errors
   - Check Network tab for failed requests

### CORS Errors

**Symptoms:**
- Browser console shows CORS error
- Request blocked by CORS policy
- Works in PowerShell but not browser

**Solutions:**

1. **Check Allowed Origins**
   Edit `appsettings.json`:
   ```json
   {
     "Server": {
       "AllowedOrigins": [
         "http://localhost",
         "http://127.0.0.1",
         "file://"
       ]
     }
   }
   ```

2. **For Local HTML Files**
   - Use `file://` in AllowedOrigins
   - Or serve HTML via local web server

3. **Reload Configuration**
   ```powershell
   Invoke-RestMethod -Uri "http://127.0.0.1:17878/config/reload" -Method Post
   ```

### API Key Errors

**Symptoms:**
- `401 Unauthorized` response
- "API key required" error
- "Invalid API key" error

**Solutions:**

1. **Check API Key Configuration**
   ```json
   {
     "Server": {
       "RequireApiKey": true,
       "ApiKey": "your-key-here"
     }
   }
   ```

2. **Include Header in Request**
   ```javascript
   headers: {
     'X-SilentPrintBridge-Key': 'your-key-here'
   }
   ```

3. **Disable API Key for Testing**
   ```json
   {
     "Server": {
       "RequireApiKey": false
     }
   }
   ```

---

## Printer Issues

### Printer Not Found

**Symptoms:**
- Error: "Printer 'X' not found"
- Error code: `PRINTER_NOT_FOUND`

**Solutions:**

1. **List Installed Printers**
   ```powershell
   cd "C:\Program Files\SilentPrintBridge"
   .\SilentPrintBridge.exe --list-printers
   ```

2. **Check Exact Printer Name**
   - Name is case-sensitive
   - Must match exactly including spaces
   - Example: `EPSON TM-T20III Receipt` not `Epson TM-T20III`

3. **Verify Printer in Windows**
   - Settings → Devices → Printers & scanners
   - Printer should show as "Ready" or "Idle"

4. **Update Configuration**
   Edit `C:\Program Files\SilentPrintBridge\appsettings.json`:
   ```json
   {
     "Printer": {
       "PrinterName": "EXACT PRINTER NAME HERE"
     }
   }
   ```

5. **Restart Service**
   ```powershell
   Restart-Service SilentPrintBridge
   ```

### Printer Not Configured

**Symptoms:**
- Error: "No printer configured"
- Error code: `PRINTER_NOT_CONFIGURED`

**Solutions:**

1. **Set Printer Name in Config**
   ```json
   {
     "Printer": {
       "PrinterName": "EPSON TM-T20III"
     }
   }
   ```

2. **Or Enable Default Printer Fallback**
   ```json
   {
     "Printer": {
       "AllowDefaultPrinterFallback": true
     }
   }
   ```

3. **Or Provide Printer in Request**
   ```json
   {
     "printerName": "EPSON TM-T20III",
     "mode": "text",
     "data": "test"
   }
   ```

### Print Job Appears But Nothing Prints

**Symptoms:**
- API returns success
- Job appears in Windows print queue
- Nothing comes out of printer

**Solutions:**

1. **Check Printer Status**
   - Open Printers & scanners
   - Click printer → Open queue
   - Check for errors or paused jobs

2. **Print Windows Test Page**
   - Right-click printer → Printer properties
   - Click "Print Test Page"
   - If this fails, issue is with printer/driver, not SilentPrintBridge

3. **Check Printer Connection**
   - USB cable connected
   - Printer powered on
   - Network printer accessible

4. **Check Printer Driver**
   - Update to latest driver from manufacturer
   - For Epson TM-series, use Advanced Printer Driver (APD)

5. **Check Paper**
   - Paper loaded correctly
   - Paper roll not empty
   - Paper sensor not blocked

6. **Clear Print Queue**
   ```powershell
   Stop-Service Spooler
   Remove-Item -Path "C:\Windows\System32\spool\PRINTERS\*" -Force
   Start-Service Spooler
   ```

### Access Denied / Permission Errors

**Symptoms:**
- Error: "Failed to open printer"
- Windows error code 5 (Access Denied)

**Solutions:**

1. **Check Printer Permissions**
   - Right-click printer → Printer properties → Security
   - Ensure "Everyone" or "LOCAL SERVICE" has Print permission

2. **Run Service as Different Account**
   - Services → SilentPrintBridge → Properties → Log On
   - Try "Local System account"

3. **Check Printer Sharing**
   - If network printer, ensure sharing is enabled
   - Check network credentials

---

## Print Quality Issues

### Receipt Prints But Doesn't Cut

**Symptoms:**
- Receipt prints successfully
- Paper does not cut
- Must tear manually

**Solutions:**

1. **Enable Cut Command**
   ```json
   {
     "Printer": {
       "AppendCutCommand": true,
       "AppendFeedBeforeCutLines": 4
     }
   }
   ```

2. **Check Printer Supports Cut**
   - Not all printers support ESC/POS cut command
   - Check printer manual
   - Epson TM-series typically support GS V commands

3. **Try Alternative Cut Command**
   - Full cut: `0x1D 0x56 0x00`
   - Partial cut: `0x1D 0x56 0x01`
   - Alternative: `0x1D 0x56 0x41 0x00`

4. **Check Printer Settings**
   - Some printers have cut disabled in driver settings
   - Check printer properties → Device Settings

5. **Manual ESC/POS Cut**
   Include cut in your ESC/POS data:
   ```javascript
   const escPos = [
     // ... your receipt data ...
     0x0A, 0x0A, 0x0A, 0x0A,  // Feed lines
     0x1D, 0x56, 0x00          // Full cut
   ];
   ```

### Garbled or Incorrect Characters

**Symptoms:**
- Special characters print as boxes or question marks
- Accented characters incorrect
- Line drawing characters wrong

**Solutions:**

1. **Try Different Encoding**
   ```json
   {
     "Printer": {
       "Encoding": "CP437"
     }
   }
   ```
   
   Common encodings:
   - `CP437` - US/International (default)
   - `CP850` - Western European
   - `CP852` - Central European
   - `CP858` - Western European with Euro
   - `UTF8` - Unicode (if printer supports)

2. **Avoid Unsupported Characters**
   - Stick to ASCII printable characters (32-126)
   - Avoid emoji and complex Unicode
   - Test with simple text first

3. **Check Printer Code Page**
   - Some printers require code page selection command
   - ESC/POS: `ESC t n` where n is code page number

4. **Use ASCII-Safe Text**
   ```javascript
   function toAsciiSafe(text) {
     return text.normalize('NFD')
                .replace(/[̀-ͯ]/g, '')
                .replace(/[^\x20-\x7E]/g, '?');
   }
   ```

### Text Alignment Issues

**Symptoms:**
- Text not centered/aligned correctly
- Lines wrap incorrectly
- Receipt too wide or narrow

**Solutions:**

1. **Configure Receipt Width**
   ```json
   {
     "Printer": {
       "ReceiptWidthMm": 58,
       "CharsPerLine58mm": 32
     }
   }
   ```

2. **Adjust Characters Per Line**
   - 58mm paper: typically 32-42 characters
   - 80mm paper: typically 48-56 characters
   - Depends on font size

3. **Use ESC/POS Alignment**
   ```javascript
   const escPos = [
     0x1B, 0x61, 0x01,  // Center
     ...textToBytes('CENTERED TEXT'),
     0x0A,
     0x1B, 0x61, 0x00,  // Left
     ...textToBytes('Left aligned'),
     0x0A
   ];
   ```

### Faint or Dark Printing

**Symptoms:**
- Text too light to read
- Text too dark/bold

**Solutions:**

1. **Check Printer Density Settings**
   - Printer properties → Device Settings → Print Density
   - Adjust to medium or high

2. **Check Paper Quality**
   - Use thermal paper designed for your printer
   - Old paper may not print well

3. **Clean Print Head**
   - Follow manufacturer cleaning instructions
   - Use thermal printer cleaning cards

---

## Configuration Issues

### Configuration Not Loading

**Symptoms:**
- Changes to appsettings.json not applied
- Service uses old settings

**Solutions:**

1. **Restart Service**
   ```powershell
   Restart-Service SilentPrintBridge
   ```

2. **Or Reload Configuration**
   ```powershell
   Invoke-RestMethod -Uri "http://127.0.0.1:17878/config/reload" -Method Post
   ```

3. **Check File Location**
   - Config must be in: `C:\Program Files\SilentPrintBridge\appsettings.json`
   - Not in publish folder or source folder

4. **Validate JSON Syntax**
   - Use online JSON validator
   - Check for trailing commas
   - Check for missing quotes

### Invalid Configuration

**Symptoms:**
- Service won't start
- Error: "Configuration error"

**Solutions:**

1. **Restore Default Configuration**
   - Copy from `publish\appsettings.json`
   - Or reinstall service

2. **Check Required Fields**
   - All sections must be present
   - Printer.PrinterName can be empty but field must exist

3. **Check Data Types**
   - Port must be integer
   - Boolean values must be `true` or `false` (lowercase)
   - Arrays must use `[]`

---

## Security Issues

### Unauthorized Access

**Symptoms:**
- Prints happening without authorization
- Unknown print jobs

**Solutions:**

1. **Enable API Key**
   ```json
   {
     "Server": {
       "RequireApiKey": true,
       "ApiKey": "generate-strong-random-key-here"
     }
   }
   ```

2. **Restrict Origins**
   ```json
   {
     "Server": {
       "AllowedOrigins": [
         "http://127.0.0.1",
         "http://your-app-domain.com"
       ]
     }
   }
   ```

3. **Disable Remote Connections**
   ```json
   {
     "Server": {
       "AllowRemoteConnections": false,
       "Host": "127.0.0.1"
     }
   }
   ```

4. **Monitor Logs**
   - Check `C:\ProgramData\SilentPrintBridge\logs`
   - Look for suspicious activity

---

## Performance Issues

### Slow Printing

**Symptoms:**
- Long delay before printing starts
- Timeout errors

**Solutions:**

1. **Check Printer Connection**
   - USB 2.0 or higher
   - Network latency if network printer

2. **Reduce Payload Size**
   - Keep receipts under 10 KB
   - Avoid large images in ESC/POS

3. **Check Spooler Service**
   ```powershell
   Get-Service Spooler
   Restart-Service Spooler
   ```

4. **Disable Dry Run Mode**
   ```json
   {
     "Printer": {
       "DryRun": false
     }
   }
   ```

### High Memory Usage

**Symptoms:**
- Service uses excessive RAM
- System slowdown

**Solutions:**

1. **Check Payload Dump**
   ```json
   {
     "Printer": {
       "EnablePayloadDump": false
     }
   }
   ```

2. **Reduce Log Level**
   ```json
   {
     "Logging": {
       "LogLevel": "Warning"
     }
   }
   ```

3. **Restart Service Periodically**
   - Schedule daily restart if needed

---

## Diagnostic Commands

### Check Service Status

```powershell
Get-Service SilentPrintBridge | Format-List *
```

### View Recent Logs

```powershell
Get-Content "C:\ProgramData\SilentPrintBridge\logs\silentprintbridge-*.log" -Tail 50
```

### Test Health Endpoint

```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:17878/health" | ConvertTo-Json
```

### List Printers

```powershell
cd "C:\Program Files\SilentPrintBridge"
.\SilentPrintBridge.exe --list-printers
```

### Test Print

```powershell
cd "C:\Program Files\SilentPrintBridge"
.\SilentPrintBridge.exe --test-print --printer "EPSON TM-T20III"
```

### Check Port Usage

```powershell
netstat -ano | findstr :17878
```

### Check Firewall Rules

```powershell
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*SilentPrintBridge*"}
```

### View Windows Event Log

```powershell
Get-EventLog -LogName Application -Source "SilentPrintBridge" -Newest 20
```

---

## Getting Help

If you cannot resolve the issue:

1. **Collect Diagnostic Information:**
   - Windows version
   - Exact printer name and model
   - Screenshot of Printers & scanners
   - Service log file (last 100 lines)
   - Response from `/health` endpoint
   - Response from `/printers` endpoint
   - Error response from `/print` endpoint
   - Screenshot of error in browser console

2. **Check Logs:**
   ```
   C:\ProgramData\SilentPrintBridge\logs\silentprintbridge-YYYYMMDD.log
   ```

3. **Test in Console Mode:**
   ```powershell
   cd "C:\Program Files\SilentPrintBridge"
   .\SilentPrintBridge.exe --console
   ```
   Then try print request and observe console output.

4. **Test with PowerShell:**
   ```powershell
   cd scripts
   .\test-print.ps1
   ```

5. **Verify Printer Works:**
   - Print Windows test page
   - Print from Notepad
   - If these fail, issue is with printer/driver, not SilentPrintBridge
