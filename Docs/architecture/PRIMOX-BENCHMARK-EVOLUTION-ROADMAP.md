# PRIMOX Workshop — Blueprint Master de Evolução & Benchmark de Mercado

**Documento de Arquitetura, Engenharia e Roadmap Técnico**  
**Data:** 2026-10-04  
**Versão:** 1.0.0  
**Status:** Planejamento e Especificação Estrutural  
**Base de Referência:** Doutor-IE, Simplo, Mitchell 1 (ProDemand/SureTrack), ALLDATA (Diagnostics/Heavy Duty), Identifix (Direct-Hit), Tekmetric, Shop-Ware, Bosch ESI[tronic].

---

## 1. Visão Geral e Proposta de Valor Única

### 1.1 O Diagnóstico do Mercado Atual
As oficinas mecânicas e de auto elétrica de alto desempenho atualmente enfrentam uma fragmentação severa de softwares:
1. **Sistemas de Gestão Tradicionais (ERPs):** Fazem OS, estoque e financeiro, mas são cegos para dados técnicos automotivos, não sabem o que é um relé, não compreendem códigos DTC e não oferecem esquemas elétricos.
2. **Plataformas de Informação Técnica (Doutor-IE, Simplo, ALLDATA):** Excelentes manuais e esquemas, porém operam como "ilhas isoladas". O técnico precisa consultar o esquema em uma tela, anotar o código da peça em um papel e redigitar manualmente no sistema de OS.
3. **Sistemas de DVI Americanos (Tekmetric, Shop-Ware):** Revolucionaram a aprovação de orçamentos com inspeção digital com fotos no celular do cliente, mas não possuem integração com a base técnica de auto elétrica brasileira.

### 1.2 O Posicionamento Estratégico do PRIMOX
O **PRIMOX Workshop** se posiciona como a **primeira plataforma unificada do mercado** que funde:
- **ERP Completo de Oficina 4.0** (OS, Orçamentos, PDV, Financeiro, Gestão de Frotas B2B, Multi-Filial).
- **Bancada Técnica Especializada em Auto Elétrica & Linha Pesada** (Esquemas interativos, pinagens de módulos, testes guiados de multímetro/osciloscópio).
- **Inteligência Artificial Automotiva RAG + Web** (Diagnóstico inteligente, busca de diagramas, cálculo de quedas de tensão).
- **DVI 2.0 (Digital Vehicle Inspection)** (Checklist fotográfico na recepção com aprovação 1-clique via WhatsApp pelo cliente).
- **Ponte Integrada de Diagnóstico para OS** (Inserção automática das peças e serviços identificados na IA direto para a Ordem de Serviço).

---

## 2. Inventário Detalhado dos Recursos de Mercado e Adaptação PRIMOX

Abaixo estão detalhadas todas as funcionalidades identificadas nos líderes de mercado, o que cada uma faz, o comportamento esperado no PRIMOX e as regras de negócio.

```
+---------------------------------------------------------------------------------------------------+
|                                 ECOSSISTEMA UNIFICADO PRIMOX                                      |
+---------------------------------------------------------------------------------------------------+
|  [ PILAR 1: DVI 2.0 ]       |  [ PILAR 2: ESQUEMAS INTERATIVOS ] |  [ PILAR 3: PONTE IA -> OS ]  |
|  - Checklist fotográfico    |  - Trace Wire Destaque Neon        |  - 1-Click RO Item Insertion   |
|  - Anotações visuais        |  - Zoom Vetorial sem serrilhado    |  - Consulta de estoque auto    |
|  - Aprovação via WhatsApp   |  - Probes com tensão/onda esperada |  - Vinculação com mão de obra  |
+-----------------------------+------------------------------------+--------------------------------+
|  [ PILAR 4: SURETRACK BASE ]|  [ PILAR 5: BIBLIOTECA TÉCNICA ]   |  [ PILAR 6: COPILOT NEURAL ]   |
|  - Casos reais da oficina   |  - Pinagens ECU/BCM/Painel         |  - RAG técnico offline/online  |
|  - Probabilidade de defeito |  - Linha Pesada 24V Frotas         |  - Guia de teste passo a passo |
|  - DTC + Peça + Sintoma     |  - Torques e fluidos R134a/PAG     |  - Busca & visualização web    |
+---------------------------------------------------------------------------------------------------+
```

---

### PILAR 1: DVI 2.0 (Digital Vehicle Inspection) com Fotos, Anotações e Aprovação WhatsApp
*Referência de Mercado: Tekmetric, Shop-Ware, CheckMotors.*

#### O que faz:
Substitui a prancheta de papel por uma inspeção digital realizada na entrada do veículo no pátio ou elevador. O técnico/recepcionista registra avarias estéticas, estado da bateria, nível de fluidos, lâmpadas queimadas e chicotes danificados com fotos e pequenas filmagens. Cada item recebe uma classificação por cores (Semáforo).

#### Como deve funcionar no PRIMOX:
1. **Interface de Inspeção Rápida:**
   - Acessível diretamente pelo botão `[📸 Inspeção DVI]` na tela de Ordens de Serviço (`OrdensServicoControl`) ou na tela de Veículos (`VeiculosControl`).
   - Grid de itens padronizados por categoria:
     - *Sistema Elétrico:* Bateria (tensão repouso/partida), Alternador, Iluminação dianteira/traseira, Chicotes aparentes, Aterramentos, Caixa de fusíveis.
     - *Segurança e Geral:* Pneus, Freios, Palhetas, Nível de fluidos.
     - *Funilaria/Avarias de Entrada:* Painel interativo do veículo (frente, laterais, traseira, teto) para marcar arranhões e amassados pré-existentes.
2. **Classificação Semafórica:**
   - 🟢 **Verde (OK):** Item inspecionado em perfeitas condições.
   - 🟡 **Amarelo (Atenção):** Desgaste moderado, sugerir revisão futura (ex: bateria com 12.3V e 40% CCA).
   - 🔴 **Vermelho (Crítico/Urgente):** Risco de parada ou perigo iminente (ex: chicote em curto roçando na lata, alternador gerando 16.5V).
3. **Editor de Anotações em Fotos:**
   - Ao capturar ou anexar foto (via webcam, leitor de arquivo ou celular via rede local), o usuário pode desenhar **círculos vermelhos**, **setas** e **textos curtos** sobre a foto destacando o defeito.
4. **Resumo Interativo para WhatsApp:**
   - Botão `[📲 Enviar DVI para o Cliente]` gera uma mensagem formatada e compacta com link ou imagens:
     - *"Olá [Cliente], seu [Veículo] deu entrada na Primo Auto Elétrica. Identificamos 2 itens críticos e 1 preventivo. Veja as fotos e aprove o orçamento aqui: [Link/Resumo]"*.
   - Se o cliente responder aprovando, os itens vermelhos são convertidos automaticamente em linhas de serviço e peças na Ordem de Serviço.

---

### PILAR 2: Bancada de Esquemas Elétricos Interativos com Trace Wire & Zoom Vetorial
*Referência de Mercado: Mitchell 1 ProDemand (Interactive Color Wiring Diagrams), Simplo.*

#### O que faz:
Diferente de um simples PDF estático que fica ilegível com zoom, o esquema interativo transforma fios e nós em elementos vetoriais clicáveis. Ao clicar em um terminal (ex: Linha 30 de um relé), todo o trajeto do fio até o fusível e a bateria se acende em cor fluorescente (Neon Yellow/Green), enquanto o restante do diagrama tem sua opacidade reduzida para 20%, eliminando a poluição visual.

#### Como deve funcionar no PRIMOX:
1. **Renderização Vetorial Baseada em Grafo:**
   - O circuito é estruturado como um Grafo de Conexões (`CircuitNode` e `CircuitWire`).
   - Tipos de componentes padronizados: Bateria, Chave de Ignição, Fusíveis (F01..F40), Relés (Linhas 30, 85, 86, 87, 87a), Módulos (ECU/BCM), Cargas (Motor de Partida, Eletroventilador, Lâmpadas) e Pontos de Massa (GND/Linha 31).
2. **Destaque Dinâmico (Trace Wire Highlight):**
   - Ao passar o mouse ou clicar em qualquer ponto de uma rede elétrica:
     - Linhas conectadas recebem espessura aumentada (StrokeThickness = 3.5) e cor fluorescente dinâmica (`#00F5FF` para comando, `#FF1493` para Linha 30, `#39FF14` para Linha 15, `#76FF03` para Linha 87).
     - Componentes e fios não pertencentes ao circuito ativo recebem `Opacity = 0.25`.
     - Um painel lateral instantâneo exibe: Nome do circuito, fusível de proteção correspondente, bitola do fio (ex: `1.5 mm² Marrom/Branco`) e pinos de entrada/saída.
3. **Pontos de Teste (Probes de Bancada):**
   - Pontos de medição interativos (A, B, C...) marcados no diagrama.
   - Clicar no ponto exibe:
     - **Tensão esperada:** Chave ligada (12.4V a 12.8V), motor funcionando (13.8V a 14.4V).
     - **Comportamento com multímetro:** Escala de continuidade, resistência esperada de bobina de relé (ex: 75 a 90 Ohms).
     - **Forma de onda esperada no osciloscópio:** Imagem ou representação gráfica de PWM, sinal de rotação indutivo/hall ou comunicação CAN.

---

### PILAR 3: Ponte Direta Diagnóstico IA ➔ Ordem de Serviço / Orçamento (1-Click RO Conversion)
*Referência de Mercado: Mitchell 1 (1Search Plus integrado ao Manager SE).*

#### O que faz:
Elimina o atrito entre a descoberta técnica e o faturamento. Quando o técnico utiliza o Copilot IA ou o Centro de Diagnóstico e a solução é encontrada (ex: defeito na ventoinha provocado pelo relé queimado e sensor de temperatura), o sistema oferece um botão direto de ação que cadastra os itens necessários na OS em aberto, sem precisar fechar telas ou redigitar códigos.

#### Como deve funcionar no PRIMOX:
1. **Cards de Sugestão Acionáveis na IA:**
   - Na resposta da IA (`AiDiagnosticCenterControl` e `CopilotFlyoutPanel`), quando forem citadas peças ou serviços, o componente renderiza um bloco interativo de ação:
     ```
     [🛒 Inserir na OS #1048]
     - Peça: Relé Auxiliar 40A 4 Pinos (Cód: DNI-0101) - R$ 28,00 [Disponível: 8 un]
     - Peça: Sensor de Temperatura da Água (Cód: MTE-4050) - R$ 64,00 [Disponível: 2 un]
     - Serviço: Troca de Relé e Sangria de Arrefecimento - R$ 90,00 [Tempo estimado: 40 min]
     ```
2. **Consulta em Tempo Real de Estoque:**
   - O serviço cruza o termo com o banco de produtos (`Produtos` e `CatalogoPecas`). Se o produto existir em estoque, puxa o preço de venda configurado e saldo disponível.
   - Se o produto não tiver saldo em estoque, exibe badge amarelo: `[⚠️ Sem estoque - Gerar Solicitação de Compras]` permitindo adicionar à lista de compras do `GestaoComprasService` com 1 clique.
3. **Seleção de Ordem de Serviço Alvo:**
   - Se o operador já estiver trabalhando com uma OS ativa no contexto, o PRIMOX pré-seleciona a OS atual.
   - Caso contrário, abre um seletor modal rápido listando as OSs em andamento no pátio com filtro por placa do veículo.
4. **Atualização Automática de Totais:**
   - Ao confirmar a inserção, a OS recalcula instantaneamente: Total de Peças, Total de Serviços, Descontos de tabela B2B/Frota (se aplicável) e valor líquido final.

---

### PILAR 4: Base "SureTrack" de Casos Reais & Estatísticas de Falhas da Oficina
*Referência de Mercado: Identifix (Direct-Hit), Mitchell 1 (SureTrack).*

#### O que faz:
A maior riqueza de uma oficina é seu histórico acumulado de soluções. O SureTrack transforma cada OS concluída em inteligência coletiva. Quando um veículo similar entra na oficina com o mesmo sintoma ou código de falha (DTC), o sistema informa a taxa de probabilidade de cada causa com base no histórico real comprovado.

#### Como deve funcionar no PRIMOX:
1. **Indexação Automática no Encerramento da OS:**
   - Ao finalizar e entregar uma OS com status "Concluída" (`StatusOrdemServico.Concluida`), o PRIMOX indexa em uma tabela dedicada (`CasosResolvidosSureTrack`):
     - Montadora, Modelo, Motor, Ano do veículo.
     - Queixa/Sintoma relatado pelo cliente (ex: "Motor falha em retomada").
     - Códigos DTC gravados (ex: "P0300, P0303").
     - Peças efetivamente substituídas que resolveram o problema (ex: "Jogo de velas de ignição", "Cabo de vela cilindro 3").
     - Serviços executados.
2. **Painel de Probabilidade de Defeito (SureTrack Statistics):**
   - Na tela do Centro IA ou no Prontuário Técnico do Veículo (`AutoEletricaTecnicaControl`), ao selecionar um veículo e um código DTC (ex: `P0300`), o sistema exibe:
     ```
     📊 Histórico de Casos Resolvidos (Base da Oficina - 14 ocorrências):
     - 64% das vezes: Bobina de ignição com fuga de corrente (9 casos)
     - 21% das vezes: Bico injetor travado aberto ou sujo (3 casos)
     - 14% das vezes: Chicote do sensor de fase em curto com chicote do alternador (2 casos)
     ```
3. **Dicas e Atalhos de Teste Rápido (15-Minute Short-Cuts):**
   - Exibe a rotina de verificação preliminar anotada pelos próprios eletricistas seniores da oficina para descartar falsos diagnósticos.

---

### PILAR 5: Biblioteca Técnica Automotiva Nacional & Linha Pesada 24V
*Referência de Mercado: Doutor-IE, Simplo, ALLDATA Heavy Duty.*

#### O que faz:
Fornece dados normativos e manuais estruturados para os veículos mais frequentes no Brasil (linha leve flex, utilitários diesel e linha pesada 24V). Evita que a oficina perca horas procurando na internet qual é o fusível do ar-condicionado ou a pinagem da ECU de um caminhão Scania.

#### Como deve funcionar no PRIMOX:
1. **Catálogo de Pinagens de Módulos (ECU, BCM, Painel, ABS):**
   - Tabela organizada por pino, cor do fio, função do sinal (Terra de potência, Linha 30, Linha 15, Sinal de sensor hall, Linha K, CAN-H, CAN-L).
   - Filtro rápido de busca por texto (ex: "Injetor 1", "Sensor de fase", "Relé principal").
2. **Mapas de Caixas de Fusíveis e Relés (Fuse Box Interactive Viewer):**
   - Ilustração gráfica do layout físico da central elétrica do vão do motor e do painel interno.
   - Ao passar o mouse sobre o fusível `F12`, exibe: Amperagem (ex: `15A Azul`), circuito protegido (`Bomba de combustível e bicos injetores`) e relé correspondente (`R02`).
3. **Módulo Especializado Linha Pesada & Frotas 24V:**
   - Parâmetros elétricos dedicados para 24V: alternadores de 28V, baterias em série (balanceamento de 12V+12V), Coordenadores Scania (COO7), módulos de chassi Mercedes-Benz (PLD/MR, ADM) e Volvo (FH/FM LCM).
   - Tabela de torques de cabeçote, folga de válvulas e especificações de ar-condicionado (carga de gás R134a em gramas e tipo/viscosidade de óleo PAG 46/100).

---

### PILAR 6: Assistente Neural de Diagnóstico Guiado por Passo a Passo
*Referência de Mercado: Bosch ESI[tronic] AI Diagnostics, PRIMOX Copilot.*

#### O que faz:
Conduz o mecânico por uma árvore de diagnóstico estruturada (Troubleshooting Flowchart) em vez de apenas sugerir a troca de peças. Faz perguntas de eliminação lógica ("Você mediu a tensão no pino 86 do relé com a chave ligada? Deu 12V ou 0V?") e guia o técnico para a causa raiz.

#### Como deve funcionar no PRIMOX:
1. **Modo Árvore de Decisão Interativa:**
   - Respostas da IA com botões de clique rápido: `[Sim, tem 12V]`, `[Não, está zerado]`, `[Queda de tensão superior a 0.5V]`.
   - Cada clique avança para o próximo ramo do fluxo sem exigir que o técnico digite textos longos com mãos sujas na bancada.
2. **Calculadora Integrada de Queda de Tensão (Voltage Drop):**
   - Ferramenta nativa na tela: O técnico informa a tensão na bateria (ex: 12.6V) e a tensão no terminal de carga (ex: 11.4V sob corrente de 15A).
   - O sistema calcula automaticamente: Queda de 1.2V (8x acima do limite tolerado pela norma SAE de 0.2V para circuito de potência), alertando sobre resistência parasita de 0.08 Ohms em terminais ou conexões de aterramento.

---

## 3. Arquitetura de Dados, Modelos e Contratos (C# / SQLite)

Para suportar os 6 pilares sem poluir o modelo existente e garantindo máxima integridade referencial, serão criadas entidades dedicadas organizadas por domínio.

```
                            DIAGRAMA ENTIDADE-RELACIONAMENTO
                            
   +-------------------+        1:N        +-----------------------+
   |   OrdensServico   |-------------------|     InspecaoDvi       |
   |   (Tabela atual)  |                   | - Id                  |
   +-------------------+                   | - OrdemServicoId (FK) |
             |                             | - ResponsavelTecnico  |
             | 1:N                         | - DataInspecao        |
             v                             | - StatusAprovacao     |
   +-----------------------+               +-----------------------+
   | CasosResolvidosSureTrack|                        | 1:N
   | - Id                  |                          v
   | - VeiculoModelo/Ano   |               +-----------------------+
   | - SintomaRelatado     |               |   InspecaoDviItem     |
   | - DTCs (P0300...)     |               | - Categoria (Eletrica)|
   | - CausaConfirmada     |               | - NomeItem (Bateria)  |
   | - PecasUtilizadas     |               | - Status (Verde/Am/Vm)|
   +-----------------------+               | - ObservacaoTecnica   |
                                           +-----------------------+
                                                      | 1:N
                                                      v
                                           +-----------------------+
                                           |   InspecaoDviFoto     |
                                           | - CaminhoArquivoLocal |
                                           | - ImagemAnotadaJson   |
                                           +-----------------------+
```

### 3.1 Entidades do Pilar DVI 2.0 (`PrimoAutoEletrica.Models.Dvi`)
```csharp
public enum DviStatusSeveridade
{
    Ok = 1,          // Verde
    Atencao = 2,     // Amarelo
    Critico = 3      // Vermelho
}

public enum DviStatusAprovacao
{
    Pendente = 0,
    EnviadoWhatsApp = 1,
    AprovadoCliente = 2,
    RecusadoCliente = 3
}

public class InspecaoDvi
{
    public int Id { get; set; }
    public int OrdemServicoId { get; set; }
    public int VeiculoId { get; set; }
    public int ClienteId { get; set; }
    public DateTime DataInspecao { get; set; } = DateTime.Now;
    public string ResponsavelTecnico { get; set; } = string.Empty;
    public DviStatusAprovacao StatusAprovacao { get; set; } = DviStatusAprovacao.Pendente;
    public string? TokenAprovacaoRemota { get; set; }
    public DateTime? DataAprovacao { get; set; }
    public List<InspecaoDviItem> Itens { get; set; } = new();
}

public class InspecaoDviItem
{
    public int Id { get; set; }
    public int InspecaoDviId { get; set; }
    public string Categoria { get; set; } = "Elétrica"; // Elétrica, Motor, Freios, Fluidos, Carroceria
    public string NomeItem { get; set; } = string.Empty;
    public DviStatusSeveridade Severidade { get; set; } = DviStatusSeveridade.Ok;
    public string? Observacao { get; set; }
    public decimal? ValorEstimadoReparo { get; set; }
    public bool AprovadoPeloCliente { get; set; }
    public List<InspecaoDviFoto> Fotos { get; set; } = new();
}

public class InspecaoDviFoto
{
    public int Id { get; set; }
    public int InspecaoDviItemId { get; set; }
    public string CaminhoRelativoArquivo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? AnotacoesJson { get; set; } // Vetores de marcação (círculos, setas)
}
```

### 3.2 Entidades da Base SureTrack da Oficina (`PrimoAutoEletrica.Models.SureTrack`)
```csharp
public class CasoResolvidoSureTrack
{
    public int Id { get; set; }
    public int OrdemServicoOrigemId { get; set; }
    public string Montadora { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Motorizacao { get; set; } = string.Empty;
    public int Ano { get; set; }
    public string SintomaPrincipal { get; set; } = string.Empty;
    public string CodigosDTC { get; set; } = string.Empty; // "P0300,P0304"
    public string CausaRaizDetectada { get; set; } = string.Empty;
    public string ProcedimentoSolucao { get; set; } = string.Empty;
    public string PecasSubstituidasJson { get; set; } = string.Empty;
    public DateTime DataResolucao { get; set; } = DateTime.Now;
    public int OcorrenciasConfirmadas { get; set; } = 1;
}
```

### 3.3 Entidades de Esquemas Elétricos Vetoriais Interativos (`PrimoAutoEletrica.Models.Circuitos`)
```csharp
public class CircuitoEletricoDefinicao
{
    public string CodigoCircuito { get; set; } = string.Empty; // ex: "VENT-FIRE-01"
    public string Titulo { get; set; } = string.Empty;
    public string Sistema { get; set; } = "Arrefecimento";
    public List<CircuitoComponenteNode> Componentes { get; set; } = new();
    public List<CircuitoLinhaFio> Fios { get; set; } = new();
    public List<CircuitoPontoTeste> PontosTeste { get; set; } = new();
}

public class CircuitoLinhaFio
{
    public string Identificador { get; set; } = string.Empty;
    public string PontoOrigemId { get; set; } = string.Empty;
    public string PontoDestinoId { get; set; } = string.Empty;
    public string CorFio { get; set; } = "Vermelho";
    public string Bitola { get; set; } = "2.5 mm²";
    public string LinhaPadrao { get; set; } = "30"; // 30, 15, 31, 87, 85, 86, 50, CAN-H, CAN-L
    public List<Point> RoteamentoCanvas { get; set; } = new();
}

public class CircuitoPontoTeste
{
    public string Identificador { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string TensaoEsperadaRepouso { get; set; } = "12.4V a 12.8V";
    public string TensaoEsperadaFuncionando { get; set; } = "13.8V a 14.4V";
    public string ProcedimentoTeste { get; set; } = string.Empty;
    public string? ImagemOndaOsciloscopio { get; set; }
}
```

---

## 4. Mapa de Navegação e Ligações entre Páginas (UI/UX Linkage)

O maior erro dos sistemas legados é a falta de pontes de comunicação entre as telas. No PRIMOX, as informações fluem naturalmente entre todos os módulos.

```
+-----------------------------------------------------------------------------------------------------+
|                                   MAPA DE LIGAÇÕES ENTRE PÁGINAS                                    |
+-----------------------------------------------------------------------------------------------------+
|                                                                                                     |
|  [ OrdensServicoControl ]  <==== 1-Click OS Item ====>  [ AiDiagnosticCenterControl ]              |
|             |                                                         |                             |
|             | (Abre DVI)                                              | (Consulta Esquemas)         |
|             v                                                         v                             |
|    [ DviInspectionWindow ]                               [ Bancada Esquemas Interativos ]           |
|             |                                                         |                             |
|             | (WhatsApp wa.me)                                        | (Probes & Pinagens)         |
|             v                                                         v                             |
|   [ Smartphone Cliente ]                                [ AutoEletricaTecnicaControl ]              |
|             |                                                         |                             |
|             | (Aprovação)                                             | (Encerramento de OS)        |
|             +====================> [ Ordem de Serviço ] <=============+                             |
|                                           |                                                         |
|                                           v                                                         |
|                             [ CasosResolvidosSureTrack ]                                            |
|                              (Alimenta a IA no futuro)                                              |
|                                                                                                     |
+-----------------------------------------------------------------------------------------------------+
```

### 4.1 Principais Pontes de Interação:
1. **Centro IA (`AiDiagnosticCenterControl`) ➔ Ordem de Serviço (`OrdensServicoControl`):**
   - Evento / Comando: `InserirPecaEmOrdemServicoCommand`.
   - Ao clicar no botão de inserção no chat da IA, o sistema navega até a OS aberta ou abre o modal de seleção rápida, injetando o item no grid de itens com código, descrição, valor e tempo padrão de aplicação.
2. **Ordens de Serviço (`OrdensServicoControl`) ➔ DVI Fotográfico (`DviInspectionWindow`):**
   - Na lista de ordens ou no formulário de edição de OS, botão `[📸 Inspeção Digital]`.
   - Permite tirar fotos com webcam/câmera conectada ou carregar fotos tiradas no smartphone da oficina.
   - Gera o link de aprovação e altera a badge no Kanban da Oficina (`OficinaKanbanControl`) para `DVI Enviado` ou `DVI Aprovado`.
3. **Auto Elétrica Técnica (`AutoEletricaTecnicaControl`) ➔ Bancada IA Fullscreen:**
   - Botão `[⚡ Esquemas & Centro IA]` já existente agora passa o contexto do veículo selecionado (Montadora, Modelo, Motor, Ano) para abrir diretamente o esquema elétrico correspondente no `AiDiagnosticCenterControl`.
4. **Fechamento de Ordem de Serviço ➔ SureTrack Knowledge Base:**
   - Ao alterar o status da OS para `Concluída`, o sistema executa em background a indexação no `SureTrackService`, salvando o par Sintoma-DTC-Peças sem impactar o tempo de resposta da tela.

---

## 5. Ações de Engenharia para Prevenir Bugs, Falhas e Degradação de Performance

Para garantir a confiabilidade profissional que o usuário exigiu ("100% transparente, sem mascarar nada e evitar bugs e erros"), foram definidas medidas defensivas estritas:

| Risco / Ponto Crítico | Impacto Potencial | Ação de Prevenção e Engenharia Defensiva no PRIMOX |
|---|---|---|
| **Ambiente Wine / Linux** | Falha de renderização, fontes distorcidas ou crash em chamadas nativas GDI/DirectX | Usar estritamente vetores XAML (`Path`, `Line`, `Canvas`) com brushes dinâmicos indexados no `App.Resources`. Não utilizar chamadas nativas de P/Invoke de GDI32. Garantir testes no harness Wine com verificação de contraste WCAG. |
| **Vazamento de Memória com Fotos DVI** | App fica lento ou consome gigabytes de RAM ao anexar muitas fotos de alta resolução | **Compressão Assíncrona Obrigatória:** Antes de salvar ou carregar na memória, redimensionar fotos para resolução máxima de 1280x960px com qualidade JPEG 80%. Carregar bitmaps no WPF usando `BitmapCacheOption.OnLoad` com `Freeze()` imediato do `BitmapSource` para desacoplar a imagem do arquivo físico em disco e liberar threads. |
| **Queda de Conexão com a Internet** | Travamento ou tela em branco se a IA ou busca externa cair | **Arquitetura Offline-First Blindada:** O PRIMOX nunca depende exclusivamente de nuvem. Se a internet cair, o `DeterministicFallbackAIService` assume 100% com base local RAG em SQLite, e o envio de WhatsApp é feito via protocolo local `wa.me` no navegador da máquina, sem precisar de API paga em nuvem. |
| **Concorrência e Bloqueios em SQLite** | Erros de `database is locked` ao salvar fotos DVI ou indexar SureTrack concorrentemente | Utilizar o `RegistroBloqueioService` já existente no PRIMOX. Configurar SQLite em modo `WAL` (Write-Ahead Logging) com `PRAGMA busy_timeout = 5000`. Executar escritas em fila única assíncrona desacoplada da Thread de UI. |
| **Inconsistência de Negócio na Inserção IA ➔ OS** | Tentar adicionar peças em uma OS já faturada, cancelada ou fechada | Validação estrita de máquina de estados: o serviço de inserção verifica se `OS.Status == StatusOrdemServico.Aberta || StatusOrdemServico.EmAndamento`. Se a OS estiver faturada, a ação é bloqueada com mensagem clara: *"Esta OS já foi faturada e não permite novos itens"*. |
| **Canvas Loop / Congelamento de UI no Trace Wire** | Travamento ao calcular caminhos de circuitos complexos | O grafo do circuito é resolvido em memória via algoritmo de Busca em Largura (BFS) em milissegundos sobre uma lista de adjacências pura em C#, sem disparar reflow de layout XAML até que o conjunto final de IDs de fios a destacar esteja calculado. |

---

## 6. Plano de Implementação Progressivo em Etapas ("Trabalhando Devagar")

Conforme a estratégia alinhada com o usuário, a implementação será feita de forma pausada, progressiva e testada camada por camada, sem quebrar nenhuma funcionalidade estável.

```
+-----------------------------------------------------------------------------------------------+
|                             CRONOGRAMA DE IMPLEMENTAÇÃO EM FASES                              |
+-----------------------------------------------------------------------------------------------+
|                                                                                               |
|  FASE 1: DVI 2.0 (Checklist Digital Fotográfico & Aprovação WhatsApp)                         |
|  [x] Models e Tabelas SQLite (InspecaoDvi, InspecaoDviItem, InspecaoDviFoto)                  |
|  [x] Serviço DviInspectionService com compressão de imagem segura                             |
|  [x] Janela DviInspectionWindow com anotação e preview de fotos                               |
|  [x] Integração WhatsApp 1-clique para envio de laudo wa.me ao cliente                        |
|  [x] Conversão direta 1-clique de avarias para itens de Ordem de Serviço                       |
|  [x] Testes unitários e de integração homologados no Wine (80/80 - 100%)                       |
|                                                                                               |
|  FASE 2: Ponte 1-Click IA Copilot ➔ Ordem de Serviço & Orçamento                              |
|  [x] Contrato de Ação de Inserção de Itens no AIToolRegistry                                  |
|  [x] Cards acionáveis na UI do AiDiagnosticCenterControl e CopilotFlyoutPanel                 |
|  [x] Modal SelecionarOrdemServicoDialog com validação de status e busca rápida                 |
|  [x] Cruzamento com estoque real e requisição de compra anti-ruptura para itens em falta      |
|  [x] Testes de integridade de recálculo financeiro, auditoria e trava de OS (87/87 - 100%)     |
|                                                                                               |
|  FASE 3: Bancada de Esquemas Interativos 2.0 (Trace Wire com Destaque Neon)                   |
|  [x] Modelo de Grafo de Circuito (CircuitNode, CircuitWire, AdjacencyList)                    |
|  [x] Algoritmo BFS para cálculo de nós interligados                                           |
|  [x] Controle interativo no AiDiagnosticCenterControl com zoom e efeito fluorescente          |
|  [x] Probes interativos com exibição de tensão e osciloscópio esperado                        |
|  [x] Homologado e testado em Wine (92/92 - 100%)                                              |
|                                                                                               |
|  FASE 4: Base "SureTrack" Local da Oficina (Casos Confirmados & Estatísticas)                 |
|  [x] Hook no encerramento de OS para indexar Veículo + Sintoma + DTC + Peça                   |
|  [x] Tabela CasosResolvidosSureTrack com busca rápida indexada e seed de bancada curado       |
|  [x] Widget visual de probabilidade estatística no Centro IA e na Auto Elétrica Técnica       |
|  [x] Atalhos de 15 minutos do eletricista sênior para evitar diagnósticos falsos              |
|  [x] Ferramenta ConsultarBaseSureTrack no AIToolRegistry e citação automática pelo Copilot    |
|                                                                                               |
|  FASE 5: Biblioteca Técnica Automotiva Nacional & Linha Pesada 24V (Pinagens & Centrais)      |
|  [x] Models e Tabelas SQLite (ModulosEletronicos, PinosConectores, CentraisEletricas, Pesados)|
|  [x] Carga Curada de Injeções Nacionais (Bosch ME 7.5.20, Delphi MT27E, Marelli 4AF)          |
|  [x] Módulos e Pinagens Linha Pesada 24V (Scania COO7 Coordenador, MB PLD/MR, Volvo LCM)      |
|  [x] Centrais Elétricas e Mapas de Fusíveis/Relés (VW G5, GM Onix, Scania 24V, MB Atego 24V)  |
|  [x] Especificações de Alta Engenharia Linha Pesada 24V (Scania R440, MB Atego 2426, Volvo FH) |
|  [x] Ferramenta ConsultarBibliotecaTecnica no AIToolRegistry e citação no Copilot IA           |
|  [x] Workbench Técnico no AiDiagnosticCenterControl e atalho rápido em AutoEletricaTecnica     |
|  [x] 100% Homologado e Testado no Wine (104/104 testes aprovados - 100.0%)                    |
|                                                                                               |
|  FASE 6: Assistente Neural de Diagnóstico Guiado por Passo a Passo (Troubleshooting & Queda)  |
|  [x] Models e Tabelas SQLite (FluxogramasDiagnostico, FluxogramasPassos, HistoricoQuedaTensao)|
|  [x] TroubleshootingFlowService com 6 fluxogramas curados (Partida, Alternador, Fuga, CAN 24V)|
|  [x] CalculadoraQuedaTensaoService com análise SAE/DIN, resistência parasita e bitola sugerida|
|  [x] Ferramentas ConsultarFluxogramaDiagnostico e CalcularQuedaTensao no AIToolRegistry & IA  |
|  [x] UI Interativa no AiDiagnosticCenterControl (Tab 7) e atalhos rápidos em AutoEletricaTec   |
|  [x] Integração 1-Clique para geração de proposta em Ordem de Serviço (AbrirDialogoPonteOS)   |
|  [x] 100% Homologado e Testado no Wine (111/111 testes aprovados - 100.0%)                    |
|                                                                                               |
+-----------------------------------------------------------------------------------------------+
```

---

## 7. Critérios de Aceite e Garantia de Qualidade

Cada fase só será considerada concluída após passar pelos seguintes crivos:
1. **Compilação 100% limpa:** Zero warnings críticos ou quebras de build (`dotnet build`).
2. **Compatibilidade Multiplataforma:** Execução validada em ambiente Linux/Wine e Windows, sem falhas de renderização de fontes ou cores.
3. **Bateria de Testes Automatizada:** Criação de testes dedicados em `Tests/PrimoAutoEletrica.Tests` e no testador de bancada `AiTester`.
4. **Validação Visual e de UX:** Verificação com o usuário de cada tela e fluxo antes de avançar para a próxima etapa.
