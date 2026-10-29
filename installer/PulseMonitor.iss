; PulseMonitor kurulum betiği (Inno Setup 6). Derleme: tools\build_release.ps1
; Yönetici yetkisi istemez: kullanıcı klasörüne kurar. .NET 10 Masaüstü Çalışma Zamanı yoksa Microsoft'tan indirip kurar.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\app"
#endif
#define RuntimeUrl "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"

[Setup]
AppId={{6F1C2B8E-4E5D-4C1A-9C1F-7A2E5B3D9A10}
AppName=PulseMonitor
AppVersion={#AppVersion}
AppVerName=PulseMonitor {#AppVersion}
AppPublisher=omeryol
AppPublisherURL=https://github.com/omeryol/PulseMonitor
AppSupportURL=https://github.com/omeryol/PulseMonitor/issues
DefaultDirName={localappdata}\Programs\PulseMonitor
DisableDirPage=auto
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts
OutputBaseFilename=PulseMonitor-Setup-{#AppVersion}
SetupIconFile=..\Assets\PulseMonitor.ico
UninstallDisplayIcon={app}\PulseMonitor.exe
UninstallDisplayName=PulseMonitor
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
CloseApplications=no
LicenseFile=..\LICENSE

[Languages]
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
tr.AutoStart=Windows açılışında başlat
en.AutoStart=Start with Windows
tr.DesktopIcon=Masaüstüne kısayol koy
en.DesktopIcon=Create a desktop shortcut
tr.Launch=PulseMonitor'u şimdi başlat
en.Launch=Launch PulseMonitor now
tr.RuntimeTitle=.NET 10 Masaüstü Çalışma Zamanı
en.RuntimeTitle=.NET 10 Desktop Runtime
tr.RuntimeText=PulseMonitor için gerekli; Microsoft'tan indiriliyor (yaklaşık 55 MB).
en.RuntimeText=Required by PulseMonitor; downloading from Microsoft (about 55 MB).
tr.RuntimeFailed=.NET 10 Masaüstü Çalışma Zamanı kurulamadı. Kurulum sürecek; PulseMonitor'u çalıştırmadan önce https://dotnet.microsoft.com/download/dotnet/10.0 adresinden "Desktop Runtime"ı kurun.
en.RuntimeFailed=The .NET 10 Desktop Runtime could not be installed. Setup will continue; before running PulseMonitor, install the "Desktop Runtime" from https://dotnet.microsoft.com/download/dotnet/10.0
tr.RemoveData=Ayarlarınız ve trafik geçmişiniz de silinsin mi?%n%n%1
en.RemoveData=Also delete your settings and traffic history?%n%n%1

[Tasks]
Name: "autostart"; Description: "{cm:AutoStart}"
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\PulseMonitor"; Filename: "{app}\PulseMonitor.exe"
Name: "{userdesktop}\PulseMonitor"; Filename: "{app}\PulseMonitor.exe"; Tasks: desktopicon

[Registry]
; Program içindeki "Windows ile başlat" ile aynı kayıt (Platform/StartupRegistration.cs)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PulseMonitor"; \
  ValueData: """{app}\PulseMonitor.exe"""; Tasks: autostart

[Run]
Filename: "{app}\PulseMonitor.exe"; Description: "{cm:Launch}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Çalışan kopya trafik geçmişini kaydedip kapansın
Filename: "{app}\PulseMonitor.exe"; Parameters: "--exit"; Flags: runhidden waituntilterminated; RunOnceId: "ExitApp"

[Code]
var
  DownloadPage: TDownloadWizardPage;

function RuntimeInstalled: Boolean;
var
  Rec: TFindRec;
begin
  Result := False;
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\10.*'), Rec) then
  begin
    Result := True;
    FindClose(Rec);
  end;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(CustomMessage('RuntimeTitle'), CustomMessage('RuntimeText'), nil);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if (CurPageID <> wpReady) or RuntimeInstalled then Exit;
  DownloadPage.Clear;
  DownloadPage.Add('{#RuntimeUrl}', 'windowsdesktop-runtime-win-x64.exe', '');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
      // Microsoft'un kurulumu yönetici onayı ister; ShellExec onay penceresini açar.
      ShellExec('', ExpandConstant('{tmp}\windowsdesktop-runtime-win-x64.exe'), '/install /quiet /norestart', '',
        SW_SHOW, ewWaitUntilTerminated, ResultCode);
    except
      Log(GetExceptionMessage);
    end;
  finally
    DownloadPage.Hide;
  end;
  if not RuntimeInstalled then
    MsgBox(CustomMessage('RuntimeFailed'), mbInformation, MB_OK);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  Exe: String;
begin
  Result := '';
  // Güncelleme: eski sürüm çalışıyorsa verisini kaydederek kapansın.
  Exe := ExpandConstant('{app}\PulseMonitor.exe');
  if FileExists(Exe) then
    Exec(Exe, '--exit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep <> usPostUninstall then Exit;
  // "Windows ile başlat" programın içinden de açılabildiği için kayıt her durumda silinir.
  RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PulseMonitor');
  DataDir := ExpandConstant('{localappdata}\PulseMonitor');
  if DirExists(DataDir) and not UninstallSilent then
    if MsgBox(FmtMessage(CustomMessage('RemoveData'), [DataDir]), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      DelTree(DataDir, True, True, True);
end;
