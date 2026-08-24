#ifndef PublishDir
  #define PublishDir "..\Apps\UniRemoteExam.Client\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\windows"
#endif
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{B82D69F8-D3A7-4D9D-90A8-414FC5F56507}
AppName=UniRemoteExam
AppVersion={#AppVersion}
AppPublisher=Sanaa University
DefaultDirName={autopf}\UniRemoteExam
DefaultGroupName=UniRemoteExam
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=UniRemoteExam-Windows-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\UniRemoteExam.Client.exe

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\UniRemoteExam"; Filename: "{app}\UniRemoteExam.Client.exe"
Name: "{autodesktop}\UniRemoteExam"; Filename: "{app}\UniRemoteExam.Client.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\UniRemoteExam.Client.exe"; Description: "Launch UniRemoteExam"; Flags: nowait postinstall skipifsilent
