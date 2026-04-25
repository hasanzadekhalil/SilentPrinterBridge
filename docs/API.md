# SilentPrintBridge API Documentation

## Base URL

```
http://127.0.0.1:17878
```

## Authentication

Optional API key authentication via header:

```
X-SilentPrintBridge-Key: your-api-key-here
```

Enable in `appsettings.json`:

```json
{
  "Server": {
    "RequireApiKey": true,
    "ApiKey": "your-secret-key-here"
  }
}
```

---

## Endpoints

### GET /health

Check service health and configuration status.

**Request:**
```http
GET /health HTTP/1.1
Host: 127.0.0.1:17878
```

**Response:**
```json
{
  "ok": true,
  "service": "SilentPrintBridge",
  "version": "1.0.0",
  "printerConfigured": true,
  "printerName": "EPSON TM-T20III",
  "availablePrintersCount": 3
}
```

**Status Codes:**
- `200 OK` - Service is healthy

---

### GET /printers

List all installed Windows printers.

**Request:**
```http
GET /printers HTTP/1.1
Host: 127.0.0.1:17878
```

**Response:**
```json
{
  "printers": [
    {
      "name": "EPSON TM-T20III",
      "isDefault": true
    },
    {
      "name": "Microsoft Print to PDF",
      "isDefault": false
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Printers listed successfully

---

### GET /version

Get service version information.

**Request:**
```http
GET /version HTTP/1.1
Host: 127.0.0.1:17878
```

**Response:**
```json
{
  "service": "SilentPrintBridge",
  "version": "1.0.0",
  "platform": "Windows",
  "runtime": "8.0.0"
}
```

**Status Codes:**
- `200 OK` - Version information returned

---

### POST /print

Print a receipt or document.

**Request:**
```http
POST /print HTTP/1.1
Host: 127.0.0.1:17878
Content-Type: application/json

{
  "mode": "text",
  "printerName": "EPSON TM-T20III",
  "profile": "default",
  "data": "Receipt text here",
  "encoding": "utf8",
  "cut": true,
  "openDrawer": false,
  "copies": 1,
  "jobName": "Receipt",
  "receiptWidth": 58,
  "codePage": "CP437",
  "requestId": "optional-request-id"
}
```

**Request Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `mode` | string | Yes | Print mode: `escpos`, `text`, or `pdf_base64` |
| `data` | string | Yes | Print data (text, base64, or PDF base64) |
| `printerName` | string | No | Override configured printer (if allowed) |
| `profile` | string | No | Use named printer profile from config |
| `encoding` | string | No | Data encoding: `utf8` or `base64` (default: `base64`) |
| `cut` | boolean | No | Append paper cut command (default: `true`) |
| `openDrawer` | boolean | No | Open cash drawer (default: `false`, requires config) |
| `copies` | integer | No | Number of copies (1-5, default: `1`) |
| `jobName` | string | No | Print job name (default: `"Receipt"`) |
| `receiptWidth` | integer | No | Receipt width in mm: `58` or `80` (default: from config) |
| `codePage` | string | No | Character encoding: `CP437`, `UTF8`, etc. (default: from config) |
| `requestId` | string | No | Optional request tracking ID |

**Print Modes:**

1. **`escpos`** - Raw ESC/POS bytes
   - `encoding: "base64"` - Base64-encoded ESC/POS bytes
   - `encoding: "utf8"` - UTF-8 text converted to bytes
   - Most reliable for thermal printers
   - Full control over formatting

2. **`text`** - Plain text
   - Automatically formatted to ESC/POS
   - Line wrapping based on `receiptWidth`
   - Normalized line endings
   - Safe character encoding

3. **`pdf_base64`** - PDF file (experimental)
   - Base64-encoded PDF
   - Disabled by default
   - Requires renderer configuration
   - Not recommended for thermal printers

**Response (Success):**
```json
{
  "success": true,
  "jobId": "a1b2c3d4e5f6",
  "requestId": "optional-request-id",
  "printerName": "EPSON TM-T20III",
  "mode": "text",
  "message": "Successfully printed 1 copy"
}
```

**Response (Error):**
```json
{
  "success": false,
  "jobId": null,
  "requestId": "optional-request-id",
  "printerName": "EPSON TM-T20III",
  "mode": "text",
  "error": "Printer 'EPSON TM-T20III' not found",
  "errorCode": "PRINTER_NOT_FOUND"
}
```

**Status Codes:**
- `200 OK` - Print successful
- `400 Bad Request` - Print failed (see error response)

**Error Codes:**

| Code | Description |
|------|-------------|
| `INVALID_MODE` | Invalid or unsupported print mode |
| `INVALID_DATA` | Data field is empty or invalid |
| `PAYLOAD_TOO_LARGE` | Data exceeds maximum payload size |
| `INVALID_COPIES` | Copies must be between 1 and 5 |
| `PRINTER_NOT_CONFIGURED` | No printer configured in settings |
| `PRINTER_NOT_FOUND` | Specified printer not found in Windows |
| `PRINTER_NOT_ALLOWED` | Printer not in allowed printers list |
| `PRINT_FAILED` | Print job failed (see message for details) |
| `PDF_DISABLED` | PDF printing is disabled in configuration |
| `PDF_RENDERER_NOT_CONFIGURED` | PDF renderer not configured |
| `INVALID_BASE64` | Base64 decoding failed |
| `INVALID_PDF` | PDF validation failed |
| `INTERNAL_ERROR` | Internal server error |

---

### POST /test-print

Print a built-in test receipt.

**Request:**
```http
POST /test-print HTTP/1.1
Host: 127.0.0.1:17878
Content-Type: application/json

{
  "printerName": "EPSON TM-T20III",
  "profile": "default",
  "cut": true
}
```

**Request Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `printerName` | string | No | Override configured printer |
| `profile` | string | No | Use named printer profile |
| `cut` | boolean | No | Append paper cut command |

**Response:**
```json
{
  "success": true,
  "jobId": "a1b2c3d4e5f6",
  "printerName": "EPSON TM-T20III",
  "mode": "escpos",
  "message": "Test receipt printed successfully (256 bytes)"
}
```

**Status Codes:**
- `200 OK` - Test print successful
- `400 Bad Request` - Test print failed

---

### POST /config/reload

Reload configuration from disk without restarting the service.

**Request:**
```http
POST /config/reload HTTP/1.1
Host: 127.0.0.1:17878
```

**Response:**
```json
{
  "success": true,
  "message": "Configuration reloaded successfully"
}
```

**Status Codes:**
- `200 OK` - Configuration reloaded
- `400 Bad Request` - Reload failed

---

## JavaScript Examples

### Basic Print Request

```javascript
async function printReceipt(text) {
  const response = await fetch('http://127.0.0.1:17878/print', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      mode: 'text',
      data: text,
      encoding: 'utf8',
      cut: true,
      jobName: 'Receipt'
    })
  });

  const result = await response.json();
  
  if (result.success) {
    console.log('Print successful:', result.jobId);
  } else {
    console.error('Print failed:', result.error);
  }
  
  return result;
}
```

### With API Key

```javascript
async function printWithApiKey(text, apiKey) {
  const response = await fetch('http://127.0.0.1:17878/print', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-SilentPrintBridge-Key': apiKey
    },
    body: JSON.stringify({
      mode: 'text',
      data: text,
      encoding: 'utf8',
      cut: true
    })
  });

  return await response.json();
}
```

### ESC/POS Print

```javascript
async function printEscPos() {
  // Build ESC/POS command bytes
  const escPos = new Uint8Array([
    0x1B, 0x40,           // ESC @ - Initialize
    0x1B, 0x61, 0x01,     // ESC a 1 - Center align
    ...textToBytes('RECEIPT'),
    0x0A, 0x0A,           // Line feeds
    0x1B, 0x61, 0x00,     // ESC a 0 - Left align
    ...textToBytes('Item 1    $10.00'),
    0x0A,
    0x0A, 0x0A, 0x0A, 0x0A, // Feed lines
    0x1D, 0x56, 0x00      // GS V 0 - Full cut
  ]);

  // Convert to base64
  const base64 = btoa(String.fromCharCode(...escPos));

  const response = await fetch('http://127.0.0.1:17878/print', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      mode: 'escpos',
      data: base64,
      encoding: 'base64',
      cut: false,  // Already included in ESC/POS
      jobName: 'ESC/POS Receipt'
    })
  });

  return await response.json();
}

function textToBytes(text) {
  const bytes = [];
  for (let i = 0; i < text.length; i++) {
    bytes.push(text.charCodeAt(i));
  }
  return bytes;
}
```

### Error Handling

```javascript
async function printWithErrorHandling(text) {
  try {
    const response = await fetch('http://127.0.0.1:17878/print', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        mode: 'text',
        data: text,
        encoding: 'utf8',
        cut: true
      })
    });

    const result = await response.json();

    if (!result.success) {
      switch (result.errorCode) {
        case 'PRINTER_NOT_FOUND':
          alert('Printer not found. Please check printer configuration.');
          break;
        case 'PRINTER_NOT_CONFIGURED':
          alert('No printer configured. Please configure a printer in settings.');
          break;
        case 'PRINT_FAILED':
          alert('Print failed: ' + result.error);
          break;
        default:
          alert('Print error: ' + result.error);
      }
    }

    return result;
  } catch (error) {
    console.error('Network error:', error);
    alert('Cannot connect to print service. Is it running?');
    return null;
  }
}
```

---

## PowerShell Examples

### Health Check

```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:17878/health" -Method Get
```

### List Printers

```powershell
$printers = Invoke-RestMethod -Uri "http://127.0.0.1:17878/printers" -Method Get
$printers.printers | Format-Table
```

### Print Text

```powershell
$body = @{
    mode = "text"
    data = "Test Receipt`nLine 1`nLine 2`nThank you!"
    encoding = "utf8"
    cut = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://127.0.0.1:17878/print" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

### With API Key

```powershell
$headers = @{
    "X-SilentPrintBridge-Key" = "your-api-key-here"
}

$body = @{
    mode = "text"
    data = "Secure receipt"
    encoding = "utf8"
    cut = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://127.0.0.1:17878/print" `
    -Method Post `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $body
```

---

## CORS Configuration

By default, the service allows requests from:
- `http://localhost`
- `http://127.0.0.1`
- `file://` (local HTML files)

To add additional origins, edit `appsettings.json`:

```json
{
  "Server": {
    "AllowedOrigins": [
      "http://localhost",
      "http://127.0.0.1",
      "file://",
      "http://myapp.local"
    ]
  }
}
```

---

## Rate Limiting

No built-in rate limiting. The service processes requests synchronously. For high-volume scenarios, consider implementing client-side queuing.

---

## Payload Limits

Default maximum payload size: **1 MB** (1,048,576 bytes)

Configure in `appsettings.json`:

```json
{
  "Server": {
    "MaxPayloadBytes": 1048576
  }
}
```

---

## Best Practices

1. **Use ESC/POS mode for thermal printers** - Most reliable and fastest
2. **Use text mode for simple receipts** - Easier to generate, automatic formatting
3. **Avoid PDF mode for receipts** - Not optimized for thermal printers
4. **Keep payloads small** - Receipts should be under 10 KB
5. **Handle errors gracefully** - Check `success` field and display user-friendly messages
6. **Test with actual hardware** - Emulators may not reflect real printer behavior
7. **Use request IDs** - For tracking and debugging in logs
8. **Enable API key in production** - Prevent unauthorized access
9. **Monitor logs** - Check `C:\ProgramData\SilentPrintBridge\logs` for issues
10. **Test cut commands** - Not all printers support ESC/POS cut commands
