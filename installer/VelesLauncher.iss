[Setup]
AppId={{B3A8B12D-4D4A-43E8-9D75-000000000001}}
AppName=Veles Launcher
AppVersion=0.1.0
AppPublisher=Veles PlayGame
DefaultDirName={localappdata}\Programs\Veles Launcher
DefaultGroupName=Veles Launcher
PrivilegesRequired=lowest
OutputDir=..\artifacts\setup
OutputBaseFilename=VelesLauncherSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Veles.Launcher.exe

[Files]
Source: "..\src-csharp\Veles.Launcher\bin\Release\Veles.Launcher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src-csharp\Veles.Launcher\bin\Release\Veles.Core.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Veles Launcher"; Filename: "{app}\Veles.Launcher.exe"
Name: "{userdesktop}\Veles Launcher"; Filename: "{app}\Veles.Launcher.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"

[Run]
Filename: "{app}\Veles.Launcher.exe"; Description: "Запустить Veles Launcher"; Flags: nowait postinstall skipifsilent
