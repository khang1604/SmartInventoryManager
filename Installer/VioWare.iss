; Publish self-contained win-x64 to artifacts/publish/app-{version}-win-x64 first.
; .NET is bundled. SQL connection configuration remains a prerequisite.
#ifndef AppVersion
  #define AppVersion "1.0.5"
#endif

[Setup]
AppId={{B7D25B34-7BDB-4CCE-905E-497CFE363BB0}
AppName=VioWare
AppVersion={#AppVersion}
AppVerName=VioWare {#AppVersion}
DefaultDirName={localappdata}\Programs\VioWare
DefaultGroupName=VioWare
PrivilegesRequired=lowest
UsePreviousAppDir=yes
CloseApplications=yes
RestartApplications=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=VioWare-Setup-{#AppVersion}-win-x64-self-contained
SetupIconFile=..\SmartInventoryManager.UI\Resources\Images\vioware.ico
UninstallDisplayIcon={app}\SmartInventoryManager.UI.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "tailscale"; Description: "Install Tailscale VPN (official installer; Windows may request administrator approval)"; Flags: unchecked; Check: NeedsTailscale

[Files]
Source: "..\artifacts\publish\app-{#AppVersion}-win-x64\*"; DestDir: "{app}"; Excludes: "*.pdb,*.config,*.config.example,connection.dat,connection.dat.tmp,launchSettings.json"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\artifacts\tools\Tailscale\Tailscale_1.104.1_Machine_X64_wix_en-US.msi"; DestDir: "{tmp}"; DestName: "tailscale-setup.msi"; Flags: deleteafterinstall; Tasks: tailscale; Check: NeedsTailscale

[Icons]
Name: "{group}\VioWare"; Filename: "{app}\SmartInventoryManager.UI.exe"; WorkingDir: "{app}"
Name: "{group}\VioWare - Configure server"; Filename: "{app}\SmartInventoryManager.UI.exe"; Parameters: "--configure"; WorkingDir: "{app}"
Name: "{autodesktop}\VioWare"; Filename: "{app}\SmartInventoryManager.UI.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{sys}\msiexec.exe"; Parameters: "/i ""{tmp}\tailscale-setup.msi"""; Tasks: tailscale; Check: NeedsTailscale; Flags: shellexec waituntilterminated
Filename: "{app}\SmartInventoryManager.UI.exe"; Parameters: "--configure"; Description: "Configure VioWare now (you can close and configure after server access is approved)"; Flags: postinstall nowait skipifsilent unchecked

[Code]
function NeedsTailscale: Boolean;
begin
  Result := not (FileExists(ExpandConstant('{pf64}\Tailscale\tailscale-ipn.exe')) or
    FileExists(ExpandConstant('{pf32}\Tailscale\tailscale-ipn.exe')));
end;
