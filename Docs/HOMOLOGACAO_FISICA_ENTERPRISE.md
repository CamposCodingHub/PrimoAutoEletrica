# Homologacao fisica enterprise

Este roteiro cobre os itens que nao podem ser provados apenas no ambiente automatizado local: impressora fisica, Windows limpo real e rede com estacoes reais.

## 1. Impressora fisica

Executar na maquina que usara a impressora:

```powershell
.\Scripts\Test-PhysicalPrinter.ps1
```

Para enviar uma pagina real de teste:

```powershell
.\Scripts\Test-PhysicalPrinter.ps1 -PrinterName "NOME DA IMPRESSORA" -PrintProbe
```

Evidencias geradas em `TestResults\PhysicalHomologation\Printer\<data>`.

## 2. Windows limpo real

Em uma maquina limpa, gerar ou copiar o pacote instalavel e executar:

```powershell
.\Scripts\Test-CleanWindowsReadiness.ps1 -PackageRoot "C:\Caminho\Do\Pacote" -RequirePackage
```

Depois executar tambem a instalacao real pelo pacote:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Caminho\Do\Pacote\Install-PrimoAutoEletrica.ps1"
```

Evidencias geradas em `TestResults\PhysicalHomologation\CleanWindows\<data>`.

## 3. Rede com duas estacoes reais

Executar em pelo menos uma estacao apontando para a outra:

```powershell
.\Scripts\Test-RealNetworkStations.ps1 -PeerAddress "IP_OU_NOME_DA_OUTRA_ESTACAO" -SharedPath "\\SERVIDOR\CompartilhamentoPrimo" -Port 52000
```

O teste valida identidade da maquina, ping, porta TCP, leitura/escrita em compartilhamento e simulador local de sincronizacao.
Evidencias geradas em `TestResults\PhysicalHomologation\NetworkStations\<data>`.

## Criterio de aprovacao

- Impressora: impressora detectada e, quando `-PrintProbe` for usado, pagina impressa confirmada visualmente.
- Windows limpo: Windows 10/11 ou superior, runtime Desktop .NET 9 instalado, pacote com instalador, manifesto e executavel.
- Rede real: comunicacao com peer, pasta compartilhada com leitura/escrita e simulador sem erro.

Sem esses ambientes fisicos, o status correto continua sendo "preparado para homologacao", nao "homologado fisicamente".
