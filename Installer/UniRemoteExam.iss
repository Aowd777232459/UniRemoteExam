#ifndef ClientPublishDir
  #define ClientPublishDir "..\Apps\UniRemoteExam.Client\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#endif
#ifndef ServerPublishDir
  #define ServerPublishDir "..\bin\Release\net10.0\win-x64\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\windows"
#endif
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{B82D69F8-D3A7-4D9D-90A8-414FC5F56507}
AppName=نظام الاختبارات الإلكترونية
AppVersion={#AppVersion}
AppPublisher=Sanaa University
DefaultDirName={autopf}\UniRemoteExam
DefaultGroupName=UniRemoteExam
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=UniRemoteExam-LAN-Windows-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\Client\UniRemoteExam.Client.exe
CloseApplications=yes

[Files]
Source: "{#ClientPublishDir}\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#ServerPublishDir}\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Launch-UniRemoteExam.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\نظام الاختبارات الإلكترونية"; Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Launch-UniRemoteExam.ps1"""; WorkingDir: "{app}"; IconFilename: "{app}\Client\UniRemoteExam.Client.exe"
Name: "{autodesktop}\نظام الاختبارات الإلكترونية"; Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Launch-UniRemoteExam.ps1"""; WorkingDir: "{app}"; IconFilename: "{app}\Client\UniRemoteExam.Client.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"; Flags: checkedonce

[Run]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""UniRemoteExam LAN Server"""; Flags: runhidden
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""UniRemoteExam LAN Server"" dir=in action=allow protocol=TCP localport=5113 remoteip=localsubnet profile=any"; Flags: runhidden
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Launch-UniRemoteExam.ps1"""; Description: "تشغيل نظام الاختبارات الإلكترونية"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM UniRemoteExam.exe"; Flags: runhidden; RunOnceId: "StopUniRemoteExamServer"
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""UniRemoteExam LAN Server"""; Flags: runhidden; RunOnceId: "RemoveUniRemoteExamFirewall"
