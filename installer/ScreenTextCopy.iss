; ============================================================================
;  ScreenTextCopy — Windows installer script (Inno Setup 6)
; ----------------------------------------------------------------------------
;  This installer is FULLY SELF-CONTAINED. The payload it ships already
;  includes:
;    * the .NET 8 runtime (the app is published self-contained), and
;    * the Tesseract 5 OCR engine + eng/fas/ara language data.
;  The end user therefore needs NO prerequisites at all — no .NET SDK/runtime,
;  no Tesseract install, no Visual C++ redistributable.
;
;  WHY A PER-USER INSTALL (LocalAppData) INSTEAD OF PROGRAM FILES:
;  the app stores OCR language packs it downloads on demand next to its own
;  executable (<app>\Tesseract\tessdata). Inside "Program Files" that folder is
;  read-only for a normal process, so installing new languages from Settings
;  would fail with access-denied unless the app ran elevated. Installing into
;  %LocalAppData%\Programs keeps that folder writable, needs no UAC prompt at
;  all, and is the same strategy VS Code and similar tools use.
;
;  Build it with:  scripts\build-release.ps1
;  or manually  :  ISCC.exe /DAppVersion=2.0.0 /DPayloadDir="..\release\app" ScreenTextCopy.iss
; ============================================================================

#ifndef AppVersion
  #define AppVersion "2.0.0"
#endif

; Folder containing the published, self-contained application.
#ifndef PayloadDir
  #define PayloadDir "..\release\app"
#endif

#define AppName        "ScreenTextCopy"
#define AppPublisher   "Reza Kazemi Fathi"
#define AppUrl         "https://github.com/rezakazemifathi/ScreenTextCopy"
#define AppExeName     "ScreenTextCopy.exe"

[Setup]
; A stable AppId is what lets a new version upgrade an old one in place
; instead of installing side by side. Never change it between releases.
AppId={{7C4B1F42-9E3A-4D58-9F21-5B6A0C7E31D4}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup

DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayName={#AppName} {#AppVersion}
UninstallDisplayIcon={app}\{#AppExeName}
LicenseFile=..\LICENSE

; No elevation: a per-user install keeps the OCR language-pack folder writable
; and lets anyone install without an administrator password.
PrivilegesRequired=lowest

; The app is x64-only (WPF + the native Tesseract DLLs are 64-bit).
ArchitecturesAllowed=x64compatible
MinVersion=10.0

; Shut the app down cleanly before overwriting it, otherwise the running EXE is
; locked and an upgrade fails halfway through the file copy.
CloseApplications=yes
CloseApplicationsFilter=*.exe,*.dll
RestartApplications=no

OutputDir=..\release
OutputBaseFilename={#AppName}-Setup-{#AppVersion}-win-x64
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
SetupIconFile=..\src\ScreenTextCopy\Assets\logo.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
english.CreateDesktopIcon=Create a &desktop shortcut
english.LaunchAtStartup=Start {#AppName} automatically when Windows starts
english.LaunchApp=Launch {#AppName}
english.RemoveSettings=Also delete your settings, saved API key and downloaded OCR language packs?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupicon"; Description: "{cm:LaunchAtStartup}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The whole published payload: the app, the bundled .NET runtime, the
; Tesseract engine, tessdata and the localization JSON files.
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";                        Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}";  Filename: "{uninstallexe}"
Name: "{userdesktop}\{#AppName}";                  Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}";                  Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; OCR language packs downloaded after installation are unknown to the
; uninstaller's file list, so remove the whole tessdata folder explicitly.
Type: filesandordirs; Name: "{app}\Tesseract\tessdata"
Type: dirifempty;     Name: "{app}\Tesseract"
Type: dirifempty;     Name: "{app}"

[Code]
// Settings, the API key and downloaded language data live outside the
// application folder. Leaving them behind makes reinstalling painless, so only
// delete them when the user explicitly asks.
//
// NOTE: use // comments here, not Pascal { } comments - a brace comment would
// swallow the first closing brace of an Inno constant such as {app}.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{userappdata}\{#AppName}');
    if DirExists(DataDir) then
      if MsgBox(ExpandConstant('{cm:RemoveSettings}'),
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(DataDir, True, True, True);
  end;
end;
