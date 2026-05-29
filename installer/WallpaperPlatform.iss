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

; WebView2 managed wrappers + native loader (runtime itself is pre-installed on Windows 11)
Source: "{#BuildDir}\Microsoft.Web.WebView2.Core.dll";    DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Microsoft.Web.WebView2.Wpf.dll";     DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\runtimes\win-x64\native\WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion

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
// Check for .NET 9 Desktop Runtime by looking for a 9.x install folder
function IsDotNet9Installed(): Boolean;
var
  FindRec:  TFindRec;
  BasePath: String;
begin
  Result   := False;
  BasePath := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if FindFirst(BasePath + '\9.*', FindRec) then
  begin
    Result := (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0;
    FindClose(FindRec);
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet9Installed() then
  begin
    MsgBox(
      'WallpaperPlatform requires the .NET 9 Windows Desktop Runtime.' + Chr(13) + Chr(10) +
      Chr(13) + Chr(10) +
      'Please download and install it from:' + Chr(13) + Chr(10) +
      'https://dotnet.microsoft.com/download/dotnet/9.0' + Chr(13) + Chr(10) +
      Chr(13) + Chr(10) +
      'Then run this installer again.',
      mbError, MB_OK);
    Result := False;
  end;
end;
