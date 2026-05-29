#define MyAppName      "WallpaperPlatform"
#define MyAppVersion   "1.0.0"
#define MyAppPublisher "WallpaperPlatform"
#define MyAppExeName   "WallpaperPlatform.exe"
#define BuildDir       "..\bin\Release\net9.0-windows"

[Setup]
AppId={{D9E1C42C-E6BB-4DFF-AA9D-B727D257D9C0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=.
OutputBaseFilename=WallpaperPlatform-Setup
SetupIconFile=..\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application
Source: "{#BuildDir}\{#MyAppExeName}";                    DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\WallpaperPlatform.dll";              DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\WallpaperPlatform.deps.json";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\WallpaperPlatform.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

; WebView2 managed wrappers (runtime itself is pre-installed on Windows 11)
Source: "{#BuildDir}\Microsoft.Web.WebView2.Core.dll";    DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Microsoft.Web.WebView2.Wpf.dll";     DestDir: "{app}"; Flags: ignoreversion

; Wallpaper packages
Source: "{#BuildDir}\wallpapers\*"; DestDir: "{app}\wallpapers"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}";                       Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}";               Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Offer to launch after install (starts in Wait mode — no GPU load)
Filename: "{app}\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent

[UninstallRun]
; Remove the startup registry entry if the user had enabled it
Filename: "reg.exe"; \
  Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v WallpaperPlatform /f"; \
  Flags: runhidden; \
  RunOnceId: "RemoveStartupEntry"

[Code]
// Check for .NET 9 Desktop Runtime before installing
function IsDotNet9Installed(): Boolean;
var
  key:   String;
  names: TArrayOfString;
  i:     Integer;
begin
  Result := False;
  key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  if RegGetValueNames(HKLM, key, names) then
    for i := 0 to GetArrayLength(names) - 1 do
      if Pos('9.', names[i]) = 1 then
      begin
        Result := True;
        Exit;
      end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet9Installed() then
  begin
    MsgBox(
      'WallpaperPlatform requires the .NET 9 Windows Desktop Runtime.' + #13#10 +
      #13#10 +
      'Please download and install it from:' + #13#10 +
      'https://dotnet.microsoft.com/download/dotnet/9.0' + #13#10 +
      #13#10 +
      'Then run this installer again.',
      mbError, MB_OK);
    Result := False;
  end;
end;
