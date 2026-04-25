# SilentPrintBridge

**Silent thermal printer bridge for web applications**

**Developed by Khalil Hasanzade for Algernon Wede**

> **Note**: This project was developed at the request of my valued client, **Algernon Wede**, to provide a robust solution for silent thermal printing in web-based POS systems.

SilentPrintBridge is a Windows service that enables web applications to print directly to thermal printers (ESC/POS) without showing print dialogs. Perfect for POS systems, receipt printing, and kiosk applications.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Silent Printing**: No Windows print dialogs
- **ESC/POS Support**: Full support for thermal printers (Epson TM-series, Star, etc.)
- **Multiple Modes**: ESC/POS, Plain Text, and PDF printing
- **REST API**: Simple HTTP API for web applications
- **Windows Service**: Runs as a background service
- **Professional UI**: Easy configuration and testing
- **Security**: API key authentication
- **Auto-cut**: Configurable paper cutting
- **Multi-printer**: Support for multiple printers

## Quick Start

### Installation

1. Download the latest release from [Releases](../../releases)
2. Run the installer (SilentPrintBridge-Setup.exe)
3. Launch SilentPrintBridge from Start Menu
4. Configure your printer and save settings
5. Click "Install Service" to install as Windows Service

### Using the UI

The professional UI application provides:
- Service start/stop/restart controls
- Printer configuration
- Test printing
- Windows Service installation
- Real-time logs and diagnostics
- Health monitoring

### Configuration

Edit `appsettings.json` or use the UI:

```json
{
  "Server": {
    "Host": "127.0.0.1",
    "Port": 17878,
    "AllowRemoteConnections": false,
    "RequireApiKey": true,
    "ApiKey": "your-secret-key-here",
    "AllowedOrigins": ["http://localhost:3000"]
  },
  "Printer": {
    "PrinterName": "TM-T20III",
    "ReceiptWidthMm": 80,
    "AppendCutCommand": true,
    "AppendFeedBeforeCutLines": 3
  }
}
```

## API Usage

### Health Check

```bash
curl http://127.0.0.1:17878/health
```

### Print Receipt

```bash
curl -X POST http://127.0.0.1:17878/print \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secret-key-here" \
  -d '{
    "mode": "escpos",
    "printerName": "TM-T20III",
    "content": {
      "header": "MY STORE",
      "items": [
        {"name": "Product 1", "quantity": 2, "price": 10.00},
        {"name": "Product 2", "quantity": 1, "price": 15.00}
      ],
      "total": 35.00
    }
  }'
```

### JavaScript Example

```javascript
async function printReceipt(receipt) {
  const response = await fetch('http://127.0.0.1:17878/print', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-API-Key': 'your-secret-key-here'
    },
    body: JSON.stringify({
      mode: 'escpos',
      content: receipt
    })
  });
  
  const result = await response.json();
  console.log('Print result:', result);
}
```

## Supported Printers

- Epson TM-T20, TM-T20II, TM-T20III
- Epson TM-T82, TM-T88
- Star TSP100, TSP143, TSP650
- Any ESC/POS compatible thermal printer
- Microsoft Print to PDF (for testing)

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Service health check |
| `/version` | GET | Get service version |
| `/printers` | GET | List installed printers |
| `/print` | POST | Print receipt/document |
| `/test-print` | POST | Send test print |
| `/config/reload` | POST | Reload configuration |

## Print Modes

### ESC/POS Mode
Direct ESC/POS commands for thermal printers. Supports text formatting, barcodes, QR codes, and paper cutting.

### Text Mode
Plain text printing for any printer. Automatic text wrapping and formatting.

### PDF Mode
Print PDF documents to any printer including virtual PDF printers.

## Development

### Requirements

- .NET 8.0 SDK
- Windows 10/11
- Visual Studio 2022 or VS Code

### Build

```bash
cd src/SilentPrintBridge
dotnet build -c Release
```

### Run

```bash
dotnet run --console
```

### Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Architecture

```
┌─────────────────┐
│  Web Browser    │
│  (JavaScript)   │
└────────┬────────┘
         │ HTTP
         ▼
┌─────────────────┐
│ SilentPrint     │
│ Bridge Service  │
│ (ASP.NET Core)  │
└────────┬────────┘
         │ Win32 API
         ▼
┌─────────────────┐
│ Windows Spooler │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Thermal Printer │
│ (ESC/POS)       │
└─────────────────┘
```

## Security

- API key authentication
- CORS configuration
- Localhost-only by default
- No external dependencies
- Runs as Windows Service with limited privileges

## Troubleshooting

### Service won't start
- Check if port 17878 is available
- Run UI as Administrator
- Check logs in `C:\ProgramData\SilentPrintBridge\logs\`

### Printer not found
- Verify printer is installed in Windows
- Check printer name matches configuration
- Use UI to list available printers

### PDF files corrupted
- Service automatically detects PDF printers
- Uses text-based printing for PDF/XPS printers
- Check printer driver is up to date

## Command Line Options

```bash
SilentPrintBridge.exe --console          # Run in console mode
SilentPrintBridge.exe --list-printers    # List installed printers
SilentPrintBridge.exe --test-print       # Send test print
SilentPrintBridge.exe --printer "Name"   # Specify printer for test
```

## License

MIT License - see [LICENSE](LICENSE) file for details

## Author

**Khalil Hasanzade**
- GitHub: [@hasanzadekhalil](https://github.com/hasanzadekhalil)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues and questions, please use the [GitHub Issues](../../issues) page.

---

**Developed by Khalil Hasanzade for Algernon Wede**

This project was created to meet the specific requirements of a professional POS system implementation, ensuring reliable and silent thermal printing for web-based applications.
