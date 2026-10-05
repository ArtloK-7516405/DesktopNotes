[Setup]
AppName=Desktop Notes
AppVersion=1.0.0
AppPublisher=Desktop Notes
AppMutex=DesktopNotes_Single
DefaultDirName={autopf}\DesktopNotes
DefaultGroupName=Desktop Notes
OutputDir=installer
OutputBaseFilename=DesktopNotes-Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\DesktopNotes.exe
SetupIconFile=src\app.ico

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
Source: "publish\DesktopNotes.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Desktop Notes"; Filename: "{app}\DesktopNotes.exe"; Parameters: "--settings"

[Run]
Filename: "{app}\DesktopNotes.exe"; Description: "Запустить Desktop Notes"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im DesktopNotes.exe"; Flags: runhidden; RunOnceId: "Kill"
Filename: "{app}\DesktopNotes.exe"; Parameters: "--uninstall"; Flags: runhidden; RunOnceId: "Cleanup"
