[Setup]
AppId={{B3A8B12D-4D4A-43E8-9D75-000000000002}}
AppName=Veles Build Publisher
AppVersion=0.1.0
AppPublisher=Veles PlayGame
DefaultDirName={localappdata}\Programs\Veles Build Publisher
DefaultGroupName=Veles Build Publisher
PrivilegesRequired=lowest
OutputDir=..\artifacts\setup
OutputBaseFilename=VelesBuildPublisherSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Veles.BuildPublisher.exe

[Files]
Source: "..\src-csharp\Veles.BuildPublisher\bin\Release\Veles.BuildPublisher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src-csharp\Veles.BuildPublisher\bin\Release\Veles.Core.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Veles Build Publisher"; Filename: "{app}\Veles.BuildPublisher.exe"
Name: "{userdesktop}\Veles Build Publisher"; Filename: "{app}\Veles.BuildPublisher.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"

[Run]
Filename: "{app}\Veles.BuildPublisher.exe"; Description: "Запустить Veles Build Publisher"; Flags: nowait postinstall skipifsilent
