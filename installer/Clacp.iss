#define MyAppName "Clacp"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Lylian Fredon (Daryu)"
#define MyAppExeName "Clacp.exe"

[Setup]
AppId={{9C6A3B7E-4C2C-4A9F-9C1B-9F9E7D5A4B01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=..\LICENSE.txt
SetupIconFile=..\src\Clacp\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=..\installer-output
OutputBaseFilename=Clacp-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Creer un raccourci sur le Bureau"; GroupDescription: "Raccourcis supplementaires :"
Name: "startupicon"; Description: "Demarrer Clacp automatiquement a l'ouverture de session"; GroupDescription: "Raccourcis supplementaires :"; Flags: unchecked

[Files]
Source: "..\publish\Clacp.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstaller {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName}"; Flags: nowait postinstall skipifsilent
