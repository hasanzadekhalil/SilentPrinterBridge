# SilentPrintBridge Installer

This directory contains optional installer configurations for creating a professional Windows installer package.

## Quick Install (Recommended)

For most users, the PowerShell scripts in the `scripts/` directory are sufficient:

```powershell
cd scripts
.\publish.ps1
.\install-service.ps1
```

## Advanced Installer Options

### Option 1: Inno Setup (Recommended)

Inno Setup is a free installer for Windows programs.

**Download:** https://jrsoftware.org/isinfo.php

**Features:**
- Professional installer UI
- Automatic service installation
- Start menu shortcuts
- Uninstaller
- Configuration preservation on upgrade

**To build:**

1. Install Inno Setup
2. Edit `innosetup.iss` and update paths if needed
3. Open `innosetup.iss` in Inno Setup Compiler
4. Click Build → Compile
5. Output: `Output\SilentPrintBridge-Setup.exe`

### Option 2: WiX Toolset

WiX creates MSI installers for enterprise deployment.

**Download:** https://wixtoolset.org/

**Features:**
- MSI package for enterprise deployment
- Group Policy deployment support
- Windows Installer features
- Repair and rollback support

**Note:** WiX configuration not included. Use Inno Setup for simpler deployment.

### Option 3: PowerShell Scripts (Included)

The included PowerShell scripts provide:
- Simple installation
- Service registration
- Configuration management
- Easy uninstallation

**Location:** `scripts/install-service.ps1`

## Installer Comparison

| Feature | PowerShell | Inno Setup | WiX |
|---------|------------|------------|-----|
| Ease of Use | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| Professional UI | ❌ | ✅ | ✅ |
| Uninstaller | ✅ | ✅ | ✅ |
| Start Menu | ❌ | ✅ | ✅ |
| Enterprise Deploy | ❌ | ⚠️ | ✅ |
| Free | ✅ | ✅ | ✅ |
| Setup Time | 5 min | 30 min | 2 hours |

## Recommendation

- **Development/Testing:** Use PowerShell scripts
- **Client Deployment:** Use Inno Setup
- **Enterprise/MSI Required:** Use WiX Toolset

## Creating Inno Setup Installer

A basic `innosetup.iss` configuration is provided. Customize as needed:

```iss
[Setup]
AppName=SilentPrintBridge
AppVersion=1.0.0
DefaultDirName={pf}\SilentPrintBridge
DefaultGroupName=SilentPrintBridge
OutputDir=Output
OutputBaseFilename=SilentPrintBridge-Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Run]
Filename: "sc.exe"; Parameters: "create SilentPrintBridge binPath= ""{app}\SilentPrintBridge.exe"" start= auto"; Flags: runhidden
Filename: "sc.exe"; Parameters: "start SilentPrintBridge"; Flags: runhidden

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop SilentPrintBridge"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete SilentPrintBridge"; Flags: runhidden
```

## Notes

- All installers require Administrator privileges
- Service must be stopped before uninstall
- Configuration files should be preserved on upgrade
- Logs directory should not be deleted on uninstall
