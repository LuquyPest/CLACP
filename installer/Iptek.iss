#define MyAppName "IPTEK"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Lylian Fredon (Daryu)"
#define MyAppExeName "Iptek.exe"

[Setup]
AppId={{2E7B6C4F-1A9D-4E3B-8C2A-6F5D9E2B7C34}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=..\LICENSE-IPTEK.txt
SetupIconFile=..\src\Iptek\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=..\installer-output
OutputBaseFilename=Iptek-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Creer un raccourci sur le Bureau"; GroupDescription: "Raccourcis supplementaires :"
Name: "startupicon"; Description: "Demarrer IPTEK automatiquement a l'ouverture de session"; GroupDescription: "Raccourcis supplementaires :"; Flags: unchecked

[Files]
Source: "..\publish-iptek\Iptek.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstaller {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName}"; Flags: nowait postinstall skipifsilent
