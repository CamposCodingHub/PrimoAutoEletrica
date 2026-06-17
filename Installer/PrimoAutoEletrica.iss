; Script de Instalação - Primo Auto Elétrica
; Tecnologia: Inno Setup 6.x
; Versão: 1.0.0

#define AppName "Primo Auto Elétrica"
#define AppVersion "1.0.0"
#define AppPublisher "CamposCodingHub"
#define AppURL "https://github.com/camposcodinghub/PrimoAutoEletrica"
#define AppExeName "PrimoAutoEletrica.exe"

[Setup]
; Informações básicas
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={commonpf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=..\Releases
OutputBaseFilename=PrimoAutoEletrica-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Requer administrador
PrivilegesRequired=admin
; Exibe licença
LicenseFile=..\PrimoAutoEletrica\Docs\TERMOS_USO_RASCUNHO.md
; Informações de versão
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoCopyright=Copyright (C) 2026 {#AppPublisher}
VersionInfoDescription=Sistema ERP para oficinas elétricas automotivas
VersionInfoOriginalFileName={#AppExeName}
; Ícone
SetupIconFile=..\PrimoAutoEletrica\icon.ico
; Desinstalador
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
; Arquitetura
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "quicklaunchicon"; Description: "Criar atalho na barra de tarefas"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
; Executável principal
Source: "..\PrimoAutoEletrica\bin\Release\net9.0-windows\win-x64\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; DLLs e dependências
Source: "..\PrimoAutoEletrica\bin\Release\net9.0-windows\win-x64\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
; Arquivos de configuração
Source: "..\PrimoAutoEletrica\bin\Release\net9.0-windows\win-x64\publish\*.json"; DestDir: "{app}"; Flags: ignoreversion
; Documentação
Source: "..\PrimoAutoEletrica\Docs\*"; DestDir: "{app}\Docs"; Flags: ignoreversion recursesubdirs

[Dirs]
; Diretório de dados do aplicativo
Name: "{commonappdata}\{#AppName}\Data"
Name: "{commonappdata}\{#AppName}\Logs"
Name: "{commonappdata}\{#AppName}\Config"
Name: "{commonappdata}\{#AppName}\Backups"
Name: "{commonappdata}\{#AppName}\Media"
Name: "{commonappdata}\{#AppName}\Media\Produtos"
Name: "{commonappdata}\{#AppName}\Media\Clientes"

[Icons]
; Menu Iniciar
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Documentação"; Filename: "{app}\Docs\README.md"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
; Área de trabalho
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
; Barra de tarefas
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: quicklaunchicon

[Run]
; Executar após instalação
Filename: "{app}\{#AppExeName}"; Description: "Iniciar {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remover diretórios de dados se solicitado
Type: filesandordirs; Name: "{commonappdata}\{#AppName}\Temp"

[Registry]
; Registrar versão instalada
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
; Associação de arquivos (opcional)
Root: HKCR; Subkey: ".primo"; ValueType: string; ValueName: ""; ValueData: "PrimoAutoFile"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "PrimoAutoFile"; ValueType: string; ValueName: ""; ValueData: "Arquivo do Primo Auto Elétrica"; Flags: uninsdeletekey
Root: HKCR; Subkey: "PrimoAutoFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"; Flags: uninsdeletekey
Root: HKCR; Subkey: "PrimoAutoFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey

[Code]
function InitializeSetup: Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  // Backup automático pode ser implementado aqui se necessário
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  // Limpeza adicional se necessário
end;

function NeedRestart: Boolean;
begin
  Result := False;
end;
