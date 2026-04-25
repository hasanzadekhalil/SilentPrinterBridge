# Client Handover Document

**Project:** SilentPrintBridge  
**Version:** 1.0.0  
**Date:** April 2026  
**Platform:** Windows 10/11

---

## Executive Summary

SilentPrintBridge is a Windows service that enables your web application to print receipts silently to thermal printers without showing Windows print dialogs. The service runs in the background and provides a local HTTP API that your web pages can call from Chrome or Edge browsers.

**Key Benefits:**
- ✅ No print dialogs or previews
- ✅ Instant printing (under 2 seconds)
- ✅ Works with Epson TM-series thermal printers
- ✅ Simple JavaScript integration
- ✅ Configurable printer selection
- ✅ Secure localhost-only access by default

---

## What Was Delivered

### 1. Windows Service Application
- **Location:** `C:\Program Files\SilentPrintBridge`
- **Service Name:** SilentPrintBridge
- **Runs automatically** on Windows startup
- **Binds to:** `http://127.0.0.1:17878` (localhost only)

### 2. Installation Scripts
- `publish.ps1` - Build and publish the application
- `install-service.ps1` - Install as Windows service
- `uninstall-service.ps1` - Remove the service
- `test-print.ps1` - Test printing functionality

### 3. Browser Test Page
- `samples/sample.html` - Interactive test page for Chrome/Edge
- No installation required, just open in browser
- Tests all printing modes

### 4. Documentation
- `README.md` - Complete setup and usage guide
- `docs/API.md` - API endpoint reference
- `docs/TROUBLESHOOTING.md` - Common issues and solutions
- `docs/ACCEPTANCE_TEST.md` - Step-by-step acceptance test
- `samples/sample-requests.http` - API request examples

### 5. Source Code
- Complete C# source code
- .NET 8 project
- Clean architecture with services, models, security layers

---

## Installation Summary

### Prerequisites
- Windows 10 or Windows 11
- Administrator access
- Epson TM-series printer installed in Windows
- .NET 8 Runtime (or use self-contained build)

### Quick Install Steps

1. **Build the project:**
   ```powershell
   cd scripts
   .\publish.ps1
   ```

2. **Configure your printer:**
   - Edit `publish\appsettings.json`
   - Set `"PrinterName": "YOUR EXACT PRINTER NAME"`
   - Find exact name: `.\SilentPrintBridge.exe --list-printers`

3. **Install service (as Administrator):**
   ```powershell
   .\install-service.ps1
   ```

4. **Test:**
   - Open `samples\sample.html` in Chrome or Edge
   - Click "Health Check"
   - Click "Built-In Test Receipt"
   - Receipt should print and cut within 2 seconds

---

## Configuring Your Epson TM-T20III

### Step 1: Install Printer Driver

1. Download **Epson Advanced Printer Driver (APD)** from Epson website
2. Install driver for TM-T20III model
3. Connect printer via USB
4. Power on printer

### Step 2: Verify in Windows

1. Open **Settings → Devices → Printers & scanners**
2. Confirm printer appears (e.g., "EPSON TM-T20III Receipt")
3. Click printer → **Manage → Print test page**
4. Test page should print successfully

### Step 3: Get Exact Printer Name

```powershell
cd "C:\Program Files\SilentPrintBridge"
.\SilentPrintBridge.exe --list-printers
```

Copy the exact name shown (case-sensitive, including spaces).

### Step 4: Configure SilentPrintBridge

1. Edit: `C:\Program Files\SilentPrintBridge\appsettings.json`
2. Find the `"Printer"` section
3. Set `"PrinterName": "EXACT NAME FROM STEP 3"`
4. Save file

### Step 5: Restart Service

```powershell
Restart-Service SilentPrintBridge
```

Or:
```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:17878/config/reload" -Method Post
```

---

## How to Use from Your Web Application

### JavaScript Example

```javascript
async function printReceipt(receiptText) {
  try {
    const response = await fetch('http://127.0.0.1:17878/print', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        mode: 'text',
        data: receiptText,
        encoding: 'utf8',
        cut: true,
        jobName: 'Receipt'
      })
    });

    const result = await response.json();

    if (result.success) {
      console.log('Receipt printed successfully');
      return true;
    } else {
      console.error('Print failed:', result.error);
      alert('Print failed: ' + result.error);
      return false;
    }
  } catch (error) {
    console.error('Cannot connect to print service:', error);
    alert('Print service not available. Please check if SilentPrintBridge is running.');
    return false;
  }
}

// Usage
const receipt = `Store Name
Receipt #12345
${new Date().toLocaleString()}

Item 1          $10.00
Item 2          $15.00
-----------------------
Total:          $25.00

Thank you!`;

printReceipt(receipt);
```

### Check Service Status

```javascript
async function checkPrintService() {
  try {
    const response = await fetch('http://127.0.0.1:17878/health');
    const data = await response.json();
    
    if (data.ok && data.printerConfigured) {
      console.log('Print service ready');
      console.log('Printer:', data.printerName);
      return true;
    } else {
      console.warn('Printer not configured');
      return false;
    }
  } catch (error) {
    console.error('Print service not running');
    return false;
  }
}
```

---

## Acceptance Test

### Test Scenario
Client opens `sample.html` in Chrome and Edge, clicks "Print Receipt", and the Epson TM-T20III prints and cuts the receipt within 2 seconds without showing any dialog.

### Test Steps

1. **Verify service is running:**
   ```powershell
   Get-Service SilentPrintBridge
   ```
   Status should be "Running".

2. **Open test page:**
   - Navigate to `samples\sample.html`
   - Open in Chrome
   - Click "Health Check" button
   - Verify green success response with printer name

3. **Test built-in receipt:**
   - Click "Built-In Test Receipt" button
   - Receipt should print immediately
   - Paper should cut automatically
   - No Windows dialog should appear

4. **Test plain text:**
   - Click "Print Plain Text" button
   - Receipt should print with sample items
   - Paper should cut

5. **Test custom text:**
   - Enter text in "Custom Text Print" area
   - Click "Print Custom Text"
   - Your text should print

6. **Repeat in Edge:**
   - Open `sample.html` in Microsoft Edge
   - Repeat steps 2-5
   - All tests should pass

### Pass Criteria
- ✅ No Windows print dialog appears
- ✅ No print preview window
- ✅ Receipt prints within 2 seconds
- ✅ Paper cuts automatically (if enabled)
- ✅ Works in both Chrome and Edge
- ✅ JSON response shows `"success": true`

---

## Supported Print Modes

### 1. Plain Text (Recommended for Simple Receipts)

**Best for:** Simple text receipts, easy to generate

```javascript
{
  "mode": "text",
  "data": "Your receipt text here\nLine 2\nLine 3",
  "encoding": "utf8",
  "cut": true
}
```

**Features:**
- Automatic line wrapping
- Normalized line endings
- Safe character encoding
- Auto-formatted to ESC/POS

### 2. ESC/POS (Recommended for Thermal Printers)

**Best for:** Full control, fastest printing, thermal printers

```javascript
{
  "mode": "escpos",
  "data": "base64-encoded-escpos-bytes",
  "encoding": "base64",
  "cut": false
}
```

**Features:**
- Raw ESC/POS commands
- Full formatting control (bold, align, size)
- Most reliable for Epson TM-series
- Fastest printing

### 3. PDF (Experimental, Not Recommended)

**Best for:** Not recommended for thermal receipt printers

```javascript
{
  "mode": "pdf_base64",
  "data": "base64-encoded-pdf",
  "encoding": "base64"
}
```

**Limitations:**
- Disabled by default
- Requires additional renderer configuration
- Not optimized for thermal printers
- Use ESC/POS or text instead

---

## Configuration Options

### Key Settings

**Printer Configuration:**
```json
{
  "Printer": {
    "PrinterName": "EPSON TM-T20III",
    "ReceiptWidthMm": 58,
    "AppendCutCommand": true,
    "AppendFeedBeforeCutLines": 4
  }
}
```

**Security:**
```json
{
  "Server": {
    "RequireApiKey": false,
    "ApiKey": "",
    "AllowRemoteConnections": false
  }
}
```

**Logging:**
```json
{
  "Logging": {
    "LogLevel": "Information",
    "LogDirectory": "C:\\ProgramData\\SilentPrintBridge\\logs"
  }
}
```

---

## Troubleshooting Quick Reference

### Service Not Running
```powershell
Get-Service SilentPrintBridge
Start-Service SilentPrintBridge
```

### Printer Not Found
```powershell
cd "C:\Program Files\SilentPrintBridge"
.\SilentPrintBridge.exe --list-printers
```
Update printer name in config and restart service.

### Receipt Doesn't Cut
- Check `"AppendCutCommand": true` in config
- Verify printer supports ESC/POS cut command
- Some printers require driver settings change

### Connection Refused
- Verify service is running
- Check endpoint: `http://127.0.0.1:17878/health`
- Check firewall settings

### Garbled Characters
- Try `"Encoding": "CP437"` in config
- Avoid special Unicode characters
- Use ASCII-safe text

---

## Support Information

### Log Files
```
C:\ProgramData\SilentPrintBridge\logs\silentprintbridge-YYYYMMDD.log
```

### When Reporting Issues, Provide:

1. **System Information:**
   - Windows version (10 or 11)
   - Printer model and driver version

2. **Service Status:**
   ```powershell
   Get-Service SilentPrintBridge | Format-List *
   ```

3. **Health Check Response:**
   ```powershell
   Invoke-RestMethod -Uri "http://127.0.0.1:17878/health"
   ```

4. **Printer List:**
   ```powershell
   Invoke-RestMethod -Uri "http://127.0.0.1:17878/printers"
   ```

5. **Error Response:**
   - Full JSON error response from `/print` endpoint
   - Screenshot of browser console error

6. **Log File:**
   - Last 50-100 lines from log file

---

## Important Notes

### Printer Compatibility

✅ **Guaranteed to work with:**
- Epson TM-T20III (with Epson APD driver)
- Other Epson TM-series thermal printers
- ESC/POS-compatible receipt printers

⚠️ **May work with:**
- Other thermal printers with ESC/POS support
- Standard Windows printers (text mode only)
- Network printers (requires configuration)

❌ **Not designed for:**
- PDF printing to thermal printers
- Complex graphics or images
- Non-ESC/POS printers (limited functionality)

### Security Considerations

✅ **Default security (safe):**
- Localhost only (127.0.0.1)
- No network access
- CORS restricted to localhost
- No API key required for local use

⚠️ **If enabling remote access:**
- Enable API key authentication
- Use firewall rules
- Only allow trusted networks
- Monitor logs for unauthorized access

### Performance

- **Typical print time:** Under 2 seconds
- **Maximum payload:** 1 MB (configurable)
- **Recommended receipt size:** Under 10 KB
- **Concurrent requests:** Processed sequentially

---

## Maintenance

### Regular Tasks

**Weekly:**
- Check log file size: `C:\ProgramData\SilentPrintBridge\logs`
- Logs rotate daily, kept for 30 days

**Monthly:**
- Verify service is running
- Test print functionality
- Check for Windows updates

**As Needed:**
- Update printer driver
- Adjust configuration
- Review logs for errors

### Updating Configuration

1. Edit: `C:\Program Files\SilentPrintBridge\appsettings.json`
2. Save changes
3. Reload without restart:
   ```powershell
   Invoke-RestMethod -Uri "http://127.0.0.1:17878/config/reload" -Method Post
   ```
   Or restart service:
   ```powershell
   Restart-Service SilentPrintBridge
   ```

---

## Next Steps

1. ✅ Complete acceptance test (see ACCEPTANCE_TEST.md)
2. ✅ Integrate into your web application
3. ✅ Test with real receipts
4. ✅ Configure security if needed
5. ✅ Deploy to production workstations

---

## Contact

For technical questions or issues:
- Review: `docs/TROUBLESHOOTING.md`
- Check logs: `C:\ProgramData\SilentPrintBridge\logs`
- Test in console mode: `.\SilentPrintBridge.exe --console`

---

**Thank you for using SilentPrintBridge!**

This service was built specifically for your silent receipt printing requirements and has been tested with Epson TM-series thermal printers. For best results, use ESC/POS or text mode with properly configured Epson drivers.
