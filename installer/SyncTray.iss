; Inno Setup script for SyncTray
; Build with: iscc /DAppVersion=1.0.0 /DPublishDir=..\publish installer\SyncTray.iss
;
; Overridable defines:
;   AppVersion - version string shown in Add/Remove Programs (default 0.0.1)
;   PublishDir - folder containing the published SyncTray output (default ..\publish)

#ifndef AppVersion
  #define AppVersion "0.0.1"
#endif

#ifndef PublishDir
  #define PublishDir "..\publish"
#endif

#define AppName "SyncTray"
#define AppPublisher "SyncTray"
#define AppExeName "SyncTray.exe"
#define DefaultSyncthingExe "C:\Program Files\Syncthing\syncthing.exe"

[Setup]
AppId={{8B6F1B1E-6E2D-4D9E-9C0B-7B6B3D9A2F10}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=SyncTraySetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\src\SyncTray\Resources\logo.ico
UninstallDisplayIcon={app}\{#AppExeName}

[Tasks]
Name: "startupboot"; Description: "Start {#AppName} automatically when Windows starts"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

[Registry]
; Path to syncthing.exe consumed by the app at runtime
Root: HKLM; Subkey: "SOFTWARE\{#AppName}"; ValueType: string; ValueName: "SyncthingPath"; ValueData: "{code:GetSyncthingExePath}"; Flags: uninsdeletevalue uninsdeletekeyifempty
; Start at boot (only when the task is selected)
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startupboot

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  SyncthingChoicePage: TInputOptionWizardPage;
  SyncthingFilePage: TInputFileWizardPage;

procedure InitializeWizard;
begin
  SyncthingChoicePage := CreateInputOptionPage(wpSelectTasks,
    'Syncthing', 'SyncTray manages a local Syncthing daemon.',
    'Do you already have Syncthing installed on this computer?',
    True, False);
  SyncthingChoicePage.Add('Yes - I already have Syncthing installed (let me locate it)');
  SyncthingChoicePage.Add('No - download and install Syncthing for me (using winget)');

  if FileExists('{#DefaultSyncthingExe}') then
    SyncthingChoicePage.SelectedValueIndex := 0
  else
    SyncthingChoicePage.SelectedValueIndex := 1;

  SyncthingFilePage := CreateInputFilePage(SyncthingChoicePage.ID,
    'Locate Syncthing', 'Where is syncthing.exe?',
    'Select the syncthing.exe to use, then click Next.');
  SyncthingFilePage.Add('Path to syncthing.exe:',
    'Syncthing executable|syncthing.exe|All files|*.*', '.exe');

  if FileExists('{#DefaultSyncthingExe}') then
    SyncthingFilePage.Values[0] := '{#DefaultSyncthingExe}';
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  // Only show the file-browse page when the user said Syncthing is already installed
  if PageID = SyncthingFilePage.ID then
    Result := SyncthingChoicePage.SelectedValueIndex <> 0;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = SyncthingFilePage.ID) then
  begin
    if not FileExists(SyncthingFilePage.Values[0]) then
    begin
      MsgBox('The selected syncthing.exe could not be found. Please choose a valid file.',
        mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function GetSyncthingExePath(Param: String): String;
begin
  if SyncthingChoicePage.SelectedValueIndex = 0 then
    Result := SyncthingFilePage.Values[0]
  else
    Result := '{#DefaultSyncthingExe}';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Install Syncthing via winget when the user asked us to
    if SyncthingChoicePage.SelectedValueIndex = 1 then
    begin
      WizardForm.StatusLabel.Caption := 'Installing Syncthing via winget...';
      if not Exec('winget',
        'install -e --id Syncthing.Syncthing --source winget --accept-source-agreements --accept-package-agreements',
        '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      begin
        MsgBox('SyncTray could not launch winget to install Syncthing.' #13#10
          + 'Please install Syncthing manually, then restart SyncTray.',
          mbError, MB_OK);
      end
      else if ResultCode <> 0 then
      begin
        MsgBox('winget reported a problem installing Syncthing (exit code '
          + IntToStr(ResultCode) + ').' #13#10
          + 'You may need to install Syncthing manually.',
          mbInformation, MB_OK);
      end;
    end;
  end;
end;
