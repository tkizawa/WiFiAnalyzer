; Inno Setup Script for WiFiAnalyzer
#define MyAppName "WoodStream Wi-Fi Analyzer"
#define MyAppPublisher "WiFiAnalyzer Project"
#define MyAppURL "https://github.com/tkizawa/WiFiAnalyzer"
#define MyAppExeName "WiFiAnalyzer.exe"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0.0"
#endif

#ifndef MyAppArch
  #define MyAppArch "x64"
#endif

#ifndef MySourceDir
  #define MySourceDir "bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#endif

#ifndef MyOutputDir
  #define MyOutputDir "Installer"
#endif

[Setup]
; AppId is unique for WiFiAnalyzer
AppId={{B7C9B9C3-7E9D-4E66-8365-D6CCA6D6BC37}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#MyOutputDir}
OutputBaseFilename=WiFiAnalyzer_Setup_v{#MyAppVersion}_{#MyAppArch}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
#if MyAppArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
DisableProgramGroupPage=yes
SetupIconFile=Resources\app.ico

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
