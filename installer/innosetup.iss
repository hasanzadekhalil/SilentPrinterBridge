; SilentPrintBridge Inno Setup Script
; Version 1.0.0

[Setup]
AppName=SilentPrintBridge
AppVersion=1.0.0
AppPublisher=SilentPrintBridge
AppPublisherURL=https://github.com/yourusername/silentprintbridge
AppSupportURL=https://github.com/yourusername/silentprintbridge
AppUpdatesURL=https://github.com/yourusername/silentprintbridge
DefaultDirName={autopf}\SilentPrintBridge
DefaultGroupName=SilentPrintBridge
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=SilentPrintBridge-Setup-1.0.0
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\SilentPrintBridge.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\samples\sample.html"; DestDir: "{app}\samples"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Dirs]
Name: "{commonappdata}\SilentPrintBridge"; Permissions: users-full
Name: "{commonappdata}\SilentPrintBridge\logs"; Permissions: users-full
Name: "{commonappdata}\SilentPrintBridge\temp"; Permissions: users-full

[Icons]
Name: "{group}\Test Page"; Filename: "{app}\samples\sample.html"
Name: "{group}\Configuration"; Filename: "notepad.exe"; Parameters: """{app}\appsettings.json"""
Name: "{group}\Logs Folder"; Filename: "{commonappdata}\SilentPrintBridge\logs"
Name: "{group}\Uninstall SilentPrintBridge"; Filename: "{uninstallexe}"

[Run]
Filename: "sc.exe"; Parameters: "create SilentPrintBridge binPath= ""{app}\SilentPrintBridge.exe"" start= auto DisplayName= ""Silent Print Bridge"""; Flags: runhidden; StatusMsg: "Installing Windows service..."
Filename: "sc.exe"; Parameters: "description SilentPrintBridge ""Local HTTP bridge for silent browser-to-printer communication"""; Flags: runhidden
Filename: "sc.exe"; Parameters: "start SilentPrintBridge"; Flags: runhidden; StatusMsg: "Starting service..."
Filename: "{app}\samples\sample.html"; Description: "Open test page"; Flags: postinstall shellexec skipifsilent

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop SilentPrintBridge"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete SilentPrintBridge"; Flags: runhidden

[Code]
var
  ConfigPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  ConfigPage := CreateInputQueryPage(wpSelectDir,
    'Printer Configuration', 'Configure your thermal printer',
    'Please enter the exact name of your Windows printer. You can find this in Settings → Printers & scanners.');
  ConfigPage.Add('Printer Name:', False);
  ConfigPage.Values[0] := 'EPSON TM-T20III';
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
  MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  S: String;
begin
  S := '';
  S := S + MemoDirInfo + NewLine + NewLine;
  S := S + 'Printer Name:' + NewLine + Space + ConfigPage.Values[0] + NewLine;
  Result := S;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigFile: String;
  ConfigContent: TStringList;
  I: Integer;
  Line: String;
begin
  if CurStep = ssPostInstall then
  begin
    ConfigFile := ExpandConstant('{app}\appsettings.json');
    if FileExists(ConfigFile) then
    begin
      ConfigContent := TStringList.Create;
      try
        ConfigContent.LoadFromFile(ConfigFile);
        for I := 0 to ConfigContent.Count - 1 do
        begin
          Line := ConfigContent[I];
          if Pos('"PrinterName":', Line) > 0 then
          begin
            ConfigContent[I] := '    "PrinterName": "' + ConfigPage.Values[0] + '",';
            Break;
          end;
        end;
        ConfigContent.SaveToFile(ConfigFile);
      finally
        ConfigContent.Free;
      end;
    end;
  end;
end;

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nSilentPrintBridge enables silent receipt printing from web browsers to thermal printers without Windows dialogs.%n%nYou will need:%n- Administrator privileges%n- A thermal receipt printer (e.g., Epson TM-series)%n- The exact printer name from Windows
