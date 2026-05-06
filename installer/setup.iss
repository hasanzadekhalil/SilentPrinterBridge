; SilentPrintBridge Installer Script
; Created by Khalil Hasanzade

#ifndef SourceDir
#define SourceDir "..\SilentPrintBridge-Professional"
#endif

#ifndef OutputBaseName
#define OutputBaseName "SilentPrintBridge-Setup"
#endif

#define MyAppName "SilentPrintBridge"
#define MyAppVersion "1.0.0"
#ifdef CustomerBuild
#define MyAppPublisher "SilentPrintBridge"
#define MyAppExeName "SilentPrintBridge.UI.exe"
#else
#define MyAppPublisher "Khalil Hasanzade"
#define MyAppURL "https://github.com/hasanzadekhalil/SilentPrintBridge"
#define MyAppExeName "SilentPrintBridge.UI.exe"
#endif

[Setup]
AppId={{8F9A2B3C-4D5E-6F7A-8B9C-0D1E2F3A4B5C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
#ifndef CustomerBuild
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
#endif
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=..\installer
OutputBaseFilename={#OutputBaseName}
SetupIconFile=..\logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "{#SourceDir}\SilentPrintBridge.UI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\SilentPrintBridge.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion confirmoverwrite
Source: "{#SourceDir}\appsettings.json"; DestDir: "{commonappdata}\SilentPrintBridge"; Flags: onlyifdoesntexist
Source: "{#SourceDir}\samples\*"; DestDir: "{app}\samples"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{commonappdata}\SilentPrintBridge"; Permissions: users-full
Name: "{commonappdata}\SilentPrintBridge\logs"; Permissions: users-full
Name: "{commonappdata}\SilentPrintBridge\temp"; Permissions: users-full

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "sc.exe"; Parameters: "create SilentPrintBridge binPath= ""{app}\SilentPrintBridge.exe"" start= auto DisplayName= ""SilentPrintBridge Service"""; Flags: runhidden; StatusMsg: "Installing Windows service..."
Filename: "sc.exe"; Parameters: "description SilentPrintBridge ""Silent browser-to-printer communication service for thermal printers"""; Flags: runhidden
Filename: "sc.exe"; Parameters: "config SilentPrintBridge start= delayed-auto depend= Spooler"; Flags: runhidden
Filename: "sc.exe"; Parameters: "failure SilentPrintBridge reset= 86400 actions= restart/60000/restart/60000/restart/60000"; Flags: runhidden
Filename: "sc.exe"; Parameters: "start SilentPrintBridge"; Flags: runhidden; StatusMsg: "Starting Windows service..."
#ifndef CustomerBuild
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
#endif

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop SilentPrintBridge"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete SilentPrintBridge"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\SilentPrintBridge"

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Check if .NET 8 Runtime is installed
  if not RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost') then
  begin
    if MsgBox('This application requires .NET 8 Runtime. Do you want to download it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, ResultCode);
    end;
    Result := False;
  end
  else
    Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Create logs directory
    CreateDir(ExpandConstant('{commonappdata}\SilentPrintBridge\logs'));
  end;
end;
