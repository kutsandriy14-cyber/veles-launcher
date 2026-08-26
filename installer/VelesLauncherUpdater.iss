[Setup]
AppId={{B3A8B12D-4D4A-43E8-9D75-000000000003}}
AppName=Veles Launcher Updater
AppVersion=0.1.1
AppPublisher=Veles PlayGame
DefaultDirName={localappdata}\Programs\Veles Launcher Updater
DefaultGroupName=Veles Launcher Updater
PrivilegesRequired=lowest
OutputDir=..\artifacts\setup
OutputBaseFilename=VelesLauncherUpdaterSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Veles.Updater.exe

[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    MsgBox('.NET Framework 4.8 обязателен. Установите его с https://dotnet.microsoft.com/download/dotnet-framework/net48 и повторите запуск.', mbError, MB_OK);
end;

[Files]
Source: "..\src-csharp\Veles.Updater\bin\Release\Veles.Updater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src-csharp\Veles.Updater\bin\Release\Veles.Core.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Veles Launcher Updater"; Filename: "{app}\Veles.Updater.exe"
Name: "{userdesktop}\Veles Launcher Updater"; Filename: "{app}\Veles.Updater.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"

[Run]
Filename: "{app}\Veles.Updater.exe"; Description: "Запустить Veles Launcher Updater"; Flags: nowait postinstall skipifsilent
