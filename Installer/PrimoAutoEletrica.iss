; PRIMOX Workshop — instalador comercial oficial (Inno Setup 6)
; Fonte de verdade do TFM: PrimoAutoEletrica.csproj → net6.0-windows
; NÃO embute banco de dados de produção.
;
; Variáveis injetáveis pelo pipeline:
;   AppVersion, PublishDir, OutputDir

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef AppVersionInfo
  #define AppVersionInfo "1.0.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

#define AppName "PRIMOX Workshop"
#define AppPublisher "CamposCodingHub"
#define AppURL "https://github.com/CamposCodingHub/PrimoAutoEletrica"
#define AppExeName "PrimoAutoEletrica.exe"
#ifndef AppId
  #define AppId "PRIMOX.Workshop.1"
#endif
#ifndef AppGroupName
  #define AppGroupName "PRIMOX Workshop"
#endif
; Pasta técnica de dados permanece %LOCALAPPDATA%\PrimoAutoEletrica (compatibilidade 1.0.0)

#ifndef OutputBaseFilename
  #define OutputBaseFilename "PRIMOX-Workshop-Setup-1.0.0"
#endif

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={commonpf}\PRIMOX\Workshop
DefaultGroupName={#AppGroupName}
AllowNoIcons=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
LicenseFile=..\PrimoAutoEletrica\Docs\TERMOS_USO_RASCUNHO.md
VersionInfoVersion={#AppVersionInfo}
VersionInfoProductVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoCopyright=Copyright (C) 2026 {#AppPublisher}
VersionInfoDescription=PRIMOX Workshop — gestão para oficinas / autoelétrica
VersionInfoProductName={#AppName}
VersionInfoOriginalFileName={#OutputBaseFilename}.exe
SetupIconFile=..\PrimoAutoEletrica\icon.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=no
; Fecha o EXE do produto (e locks de arquivo) sem diálogo — crítico para /VERYSILENT.
; Sem isto, uninstall silencioso pode aguardar CloseApplications e aparentar hang (unins000).
CloseApplications=force
CloseApplicationsFilter=PrimoAutoEletrica.exe
RestartApplications=no
; Dados do usuário NÃO ficam em {app}. App cria %LOCALAPPDATA%\PrimoAutoEletrica.
; Uninstall NÃO remove AppData (política: preservar banco/backups/config/mídia).
; Code signing: aplicado pelo pipeline APÓS ISCC somente com certificado comercial real
; (env PRIMOX_CODESIGN_THUMBPRINT). Nunca usar certificado de teste como produção.
[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: checkedonce

[Files]
; Publicação self-contained (gerada pelo pipeline oficial)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NÃO incluir primoauto.db / dados reais

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Iniciar {#AppName}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppName}"; ValueType: string; ValueName: "DataPathHint"; ValueData: "%LOCALAPPDATA%\PrimoAutoEletrica"

[Code]
function TryClosePrimoxProcesses(): Boolean;
var
  ResultCode: Integer;
begin
  { Encerramento explícito e documentado do processo do produto — não genérico. }
  { 1) pedido normal; 2) força árvore se ainda houver instância. }
  Exec('taskkill.exe', '/IM PrimoAutoEletrica.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(750);
  Exec('taskkill.exe', '/F /T /IM PrimoAutoEletrica.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(500);
  Result := True;
end;

function InitializeSetup(): Boolean;
begin
  TryClosePrimoxProcesses();
  Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  TryClosePrimoxProcesses();
  Result := True;
end;

function NeedRestart(): Boolean;
begin
  Result := False;
end;
