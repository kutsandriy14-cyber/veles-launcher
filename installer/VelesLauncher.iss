[Setup]
AppId={{B3A8B12D-4D4A-43E8-9D75-000000000001}}
AppName=Veles Launcher
AppVersion=0.1.5
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
Source: "..\src-csharp\Veles.Launcher\bin\Release\Veles.Launcher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src-csharp\Veles.Launcher\bin\Release\Veles.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src-csharp\Veles.Updater\bin\Release\Veles.Updater.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Veles Launcher"; Filename: "{app}\Veles.Launcher.exe"
Name: "{userprograms}\Veles Launcher Updater"; Filename: "{app}\Veles.Updater.exe"
Name: "{userdesktop}\Veles Launcher"; Filename: "{app}\Veles.Launcher.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"

[Run]
Filename: "{app}\Veles.Launcher.exe"; Description: "Запустить Veles Launcher"; Flags: nowait postinstall skipifsilent
