# PRIMOX Workshop — Sistema Enterprise de Gestão Automotiva

[![Versão](https://img.shields.io/badge/vers%C3%A3o-2.1.0%20Enterprise-orange.svg)](https://github.com/CamposCodingHub/PrimoAutoEletrica)
[![Plataforma](https://img.shields.io/badge/plataforma-Windows%20%7C%20Linux%20(Wine)-blue.svg)](https://github.com/CamposCodingHub/PrimoAutoEletrica)
[![Tecnologia](https://img.shields.io/badge/.NET-WPF%20%2F%20C%23-purple.svg)](https://github.com/CamposCodingHub/PrimoAutoEletrica)
[![Licença](https://img.shields.io/badge/licen%C3%A7a-Propriet%C3%A1ria%20%2F%20N%C3%A3o--Comercial-red.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-Produ%C3%A7%C3%A3o%20Est%C3%A1vel-green.svg)](https://github.com/CamposCodingHub/PrimoAutoEletrica)

> **AVISO DE DIREITOS AUTORAIS E PROPRIEDADE INTELECTUAL:**  
> Este software é um produto proprietário protegido pela legislação brasileira de direitos autorais e propriedade intelectual (Lei nº 9.609/1998 e Lei nº 9.610/1998) e por tratados internacionais.  
> **É EXPRESSAMENTE PROIBIDA A COMERCIALIZAÇÃO, REVENDA, SUBLICENCIAMENTO OU EXPLORAÇÃO ECONÔMICA DESTE SISTEMA POR TERCEIROS NÃO AUTORIZADOS.**  
> Consulte o documento formal em [DIREITOS_AUTORAIS.md](DIREITOS_AUTORAIS.md) e os termos de [LICENSE](LICENSE).

---

## 1. Visão Geral

O **PRIMOX Workshop** é um ERP e sistema operacional de chão de fábrica projetado especialmente para **Auto Elétricas, Oficinas Mecânicas de Alta Demanda, Centros Automotivos e Gestão de Frotas B2B**.

Diferente de sistemas web convencionais que sofrem lentidões ou travamentos com a oscilação da internet da oficina, o PRIMOX adota uma arquitetura **Offline-First Soberana**: executa de forma nativa e instantânea no computador local, mantendo seus dados em segurança sob custódia da própria empresa, com suporte nativo a sincronização em rede local (LAN) ou VPN corporativa para redes multi-filiais.

---

## 2. Principais Funcionalidades (Edição Enterprise v2.1)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                             PRIMOX WORKSHOP 2.1                             │
├──────────────────────┬──────────────────────┬───────────────────────────────┤
│   GESTÃO DE PÁTIO    │     AUTO ELÉTRICA    │     GESTÃO CORPORATIVA        │
├──────────────────────┼──────────────────────┼───────────────────────────────┤
│ • Orçamentos Rápidos │ • Copilot IA (RAG)   │ • Multi-Filiais & DRE Rede    │
│ • Ordens de Serviço  │ • Leitura DTCs       │ • Transferências Inter-Lojas  │
│ • Kanban de Oficina  │ • Teste de Ripple    │ • Gestão de Frotas B2B        │
│ • Agendamentos/Boxes │ • Queda de Tensão    │ • Compras & Ponto Pedido(ROP) │
│ • PDV Frente de Caixa│ • Ferramentaria 4.0  │ • Central Fiscal (NFS-e/NFC-e)│
│ • Dossiê do Veículo  │ • Trava Anti-Extravio│ • Impressão ZPL II (Térmica)  │
└──────────────────────┴──────────────────────┴───────────────────────────────┘
```

* **Copilot de IA Dual-Engine:** Inteligência Artificial especializada para auto elétrica, com motor determinístico que analisa DTCs (OBD-II), diagramas de chicote, pinouts e testes guiados de alternador e bateria com medição de ripple.
* **Ferramentaria 4.0 & Trava Anti-Extravio:** Controle rigoroso de instrumentos caros (scanners, osciloscópios, torquímetros), com check-out/check-in por técnico e trava de segurança que impede a liberação do carro caso alguma ferramenta tenha ficado no veículo.
* **Gestão de Compras Anti-Ruptura:** Previsão de demanda, Curva ABC e cálculo automático de Ponto de Pedido (ROP) com mapa comparativo de cotações multi-fornecedor.
* **Transferências Inter-Filiais ACID:** Movimentação transacional de mercadorias entre lojas com bloqueio de saldo em trânsito e rastreabilidade total.
* **Gestão de Frotas B2B:** Contratos corporativos para transportadoras e frotistas, com controle de telemetria de KM por placa e faturamento periódico consolidado com romaneio analítico.
* **Central Fiscal Integrada:** Emissão e transmissão em lote de NFS-e (ISS de serviços de oficina) e NFC-e (venda de peças no balcão) via Gateway Fiscal plugável.
* **Impressão Térmica ZPL II:** Geração direta de etiquetas adesivas industriais em impressoras Zebra, Argox e Elgin para chaves de carros, prateleiras e ferramentas.
* **Licenciamento HMAC SHA-256:** Proteção de integridade e amarração criptográfica ao Hardware ID da estação de trabalho.

---

## 3. Como Instalar o Programa

O PRIMOX Workshop conta com instaladores oficiais para **Windows** e **Linux**.  
Consulte o guia completo em [INSTALLATION.md](INSTALLATION.md).

### A. Instalação no Windows
1. Baixe o instalador oficial `PRIMOX-Workshop-Setup-2.1.0.exe` na pasta `artifacts/installer` ou na aba de Releases do GitHub.
2. Dê dois cliques e siga o assistente de instalação.
3. O atalho **"PRIMOX Workshop"** será criado na Área de Trabalho e no Menu Iniciar.

*(Alternativa Portable)*: Baixe `PRIMOX-Workshop-Portable-win-x64-2.1.0.zip`, descompacte em uma pasta (ex.: `C:\PRIMOX`) e execute `PrimoAutoEletrica.exe`.

### B. Instalação no Linux (Ubuntu, Debian, Fedora, Arch)
O sistema conta com instalador automatizado via Wine:
1. Instale o Wine:
   ```bash
   # Ubuntu / Debian / Mint:
   sudo apt install -y wine wine64 winetricks
   # Fedora:
   sudo dnf install -y wine winetricks
   # Arch Linux:
   sudo pacman -S wine wine-mono winetricks
   ```
2. Execute o instalador automático:
   ```bash
   ./Installer/linux/install.sh
   ```
3. O instalador configurará o ambiente, registrará o comando global `primox` no terminal e criará o ícone no menu de aplicativos do seu sistema.

---

## 4. Como Usar o Programa

Consulte o manual detalhado com passo a passo de todas as telas em [USER_MANUAL.md](USER_MANUAL.md).

### O Fluxo Essencial do Dia a Dia (Regra de Ouro)
```
[1. Cadastrar Cliente] ──► [2. Cadastrar Veículo] ──► [3. Gerar Orçamento]
                                                              │
                                                              ▼
[7. Acompanhar Dashboard] ◄── [6. Fechar Caixa] ◄── [5. Receber no PDV] ◄── [4. Converter em OS]
```

### Guia Rápido por Cargo
* **CEO / Diretor Geral:** DRE Consolidado da rede, ROI de estoques e contratos de frotas conveniadas.
* **Proprietário / Gerente:** Auditoria de caixa diária, aprovação de orçamentos especiais e Kanban do pátio.
* **Consultor / Recepção:** Acolhimento, checklist de entrada com fotos/avarias e envio de orçamentos via WhatsApp.
* **Eletricista / Técnico:** Diagnóstico de falhas com Copilot IA, medição de ripple e apontamento de peças na OS.
* **Almoxarife / Estoquista:** Recebimento com XML de NF-e, endereçamento de prateleiras e inventário rotativo.
* **Caixa:** Abertura com troco, leitor de código de barras, sangrias e fechamento cego de turno.

---

## 5. Compilação e Desenvolvimento

Para compilar o código-fonte a partir deste repositório:

### Pré-requisitos
* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (ou superior com roll-forward ativo)
* Suporte a Windows Targeting ativo: `-p:EnableWindowsTargeting=true`

### Compilação de Release
```bash
dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Release -p:EnableWindowsTargeting=true
```

### Publicação de Binários para Distribuição
```bash
dotnet publish PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Release -r win-x64 -p:EnableWindowsTargeting=true --self-contained false
```

---

## 6. Documentação Detalhada

* [Manual Completo do Usuário](USER_MANUAL.md) — Documentação operacional de cada tela e função
* [Guia de Instalação Windows e Linux](INSTALLATION.md) — Pré-requisitos, Wine e passos de instalação
* [Aviso de Direitos Autorais](DIREITOS_AUTORAIS.md) — Termos de propriedade e não-comercialização
* [Licença de Uso Restrito](LICENSE) — Licença proprietária
* [Docs/ManualUsuario/](Docs/ManualUsuario/) — Guias específicos em Markdown:
  * [01. Visão Geral da Expansão Enterprise](Docs/ManualUsuario/01_VISAO_GERAL_EXPANSAO_2.0.md)
  * [02. Controle de Ferramentas 4.0](Docs/ManualUsuario/02_CONTROLE_FERRAMENTAL_ANTI_EXTRAVIO.md)
  * [03. Compras Anti-Ruptura e ROP](Docs/ManualUsuario/03_COMPRAS_ANTI_RUPTURA.md)
  * [04. Copilot de IA e Diagnóstico](Docs/ManualUsuario/04_COPILOT_IA_DIAGNOSTICO.md)

---

## 7. Direitos Autorais e Proteção Legal

O código-fonte, arquitetura e documentação contidos neste repositório pertencem exclusivamente a **CamposCodingHub**. É expressamente vedada a venda, revenda, licenciamento oneroso ou comercialização sem autorização expressa por escrito.

Copyright (c) 2026 CamposCodingHub. Todos os direitos reservados.
