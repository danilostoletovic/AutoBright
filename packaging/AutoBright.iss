#ifndef AppVersion
  #error AppVersion is required
#endif
#ifndef SourceExe
  #error SourceExe is required
#endif
#ifndef OutputDirectory
  #error OutputDirectory is required
#endif

[Setup]
AppId=AutoBright.Desktop
AppName=AutoBright
AppVersion={#AppVersion}
AppPublisher=Danilo Stoletovic
AppPublisherURL=https://danilostoletovic.com
AppSupportURL=https://danilostoletovic.com
DefaultDirName={localappdata}\Programs\AutoBright
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir={#OutputDirectory}
OutputBaseFilename=AutoBright-Setup-win-x64
SetupIconFile=..\Assets\AutoBright.ico
UninstallDisplayIcon={app}\AutoBright.exe
UninstallDisplayName=AutoBright
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\AutoBright"; Filename: "{app}\AutoBright.exe"

[Run]
Filename: "{app}\AutoBright.exe"; Description: "Open AutoBright"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if not UninstallSilent then
    if MsgBox('Close AutoBright before continuing. Uninstall will turn automatic brightness OFF, remove its scheduled task, and restore your previous brightness. Continue?', mbConfirmation, MB_YESNO) <> IDYES then
      Exit;
  if not Exec(ExpandConstant('{app}\AutoBright.exe'), '--disable', ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('AutoBright cleanup could not start. Repair the installation and try again. ' + SysErrorMessage(ResultCode), mbError, MB_OK);
    Exit;
  end;
  if ResultCode <> 0 then
  begin
    MsgBox('AutoBright could not disable automatic brightness. Open AutoBright, turn it OFF, resolve the displayed error, and retry uninstalling. Your installation has been kept.', mbError, MB_OK);
    Exit;
  end;
  Result := True;
end;
