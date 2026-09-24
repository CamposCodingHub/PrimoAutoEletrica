# PRIMOX WORKSHOP — B5.0
## ESPECIFICAÇÃO DE INTERFACE: CHECKLIST TÉCNICO MULTIPONTO

**Entidades de Domínio:** `ChecklistTecnicoOS`, `ChecklistTecnicoItem`  
**Módulos Integrados:** Ordem de Serviço, Diagnóstico Técnico, Vehicle 360  
**Status:** **ESPECIFICADO PARA DESENVOLVIMENTO (BACKLOG B5-002)**

---

### 1. Visão Geral e Casos de Uso

O Checklist Técnico Multiponto permite que o técnico realize inspeções padronizadas no veículo durante a execução da OS. 

**Cenários Principais:**
1. **Inspeção de Entrada:** Verificação rápida de iluminação, palhetas, bateria e estado de pneus.
2. **Revisão Preventiva Programada:** Checagem sistemática de 30 a 50 itens de frotas e veículos pesados.
3. **Inspeção de Segurança:** Verificação de fusíveis de alta potência, relés principais e fixação de chicotes.

---

### 2. Estrutura de Dados da Interface

Cada item inspecionado exibe:
- **Grupo:** (Ex: Sistema de Partida, Sistema de Carga, Aterramentos, Iluminação, Sinalização)
- **Item / Descrição:** (Ex: Tensão de repouso da bateria, Queda de tensão no cabo massa)
- **Status da Inspeção (Botões de Ação Rápida):**
  - `OK` (Verde) — Conforme
  - `ATENCAO` (Amarelo) — Requer observação futura ou aprovação do cliente
  - `CRITICO` (Vermelho) — Apresenta risco iminente de pane ou acidente
  - `NAO_SE_APLICA` (Cinza) — Opcional inexistente no veículo
  - `NAO_DISPONIVEL` (Azul/Neutro) — Teste não pôde ser realizado por falta de acesso
- **Campo de Valor/Medição:** Ex: `12.45` + Unidade `V`
- **Observações Técnicas:** Campo de anotação rápida com sugestões automáticas
- **Anexo de Evidência:** Botão para vincular foto da peça defeituosa

---

### 3. Wireframe Conceitual da Tela (WPF UserControl)

```
+------------------------------------------------------------------------------------+
| CHECKLIST TÉCNICO MULTIPONTO — OS #1042                               [ X ] Fechar |
| Veículo: Volvo FH 540 (Placa: ABC-1234) | Técnico: Carlos Silva (Eletricista)     |
+------------------------------------------------------------------------------------+
| [ Filtro: Todos | Inconformes | Críticos ]             [ Barra de Progresso: 85% ] |
+------------------------------------------------------------------------------------+
| GRUPO: SISTEMA DE CARGA E PARTIDA                                                  |
| ---------------------------------------------------------------------------------- |
| 1. Tensão de Repouso Bateria A:  [ 12.60 ] V  ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] )   |
| 2. Tensão de Repouso Bateria B:  [ 11.85 ] V  ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] ) ! |
|    Obs: Desbalanceamento de carga detectado (>0.5V de diferença)                   |
|    Evidência: [ foto_bat_b.jpg ] [ Visualizar ]                                    |
| 3. Corrente de Carga Alternador: [ 75.00 ] A  ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] )   |
| 4. Queda de Tensão Aterramento:  [ 0.15 ] V   ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] )   |
+------------------------------------------------------------------------------------+
| GRUPO: ILUMINAÇÃO E SINALIZAÇÃO                                                    |
| ---------------------------------------------------------------------------------- |
| 5. Farol Principal Direito:                   ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] )   |
| 6. Farol Principal Esquerdo:                  ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] ) ! |
|    Obs: Lâmpada H7 queimada / Conector derretido                                   |
+------------------------------------------------------------------------------------+
| AÇÕES:                                                                             |
| [ + Adicionar Item ]   [ Gerar Orçamento Adicional ]   [ Salvar ]   [ Imprimir ]   |
+------------------------------------------------------------------------------------+
```

---

### 4. Integrações de Fluxo

1. **Geração Automática de Orçamento Complementar:** Itens marcados como `ATENCAO` ou `CRITICO` podem ser convertidos com 1 clique em itens de Orçamento Suplementar para aprovação do cliente.
2. **Gravação no Histórico do Vehicle360:** O resultado consolidado do checklist é anexado à linha do tempo técnica do veículo.
3. **Impressão de Laudo Técnico de Entrega:** Relatório visual anexado à nota fiscal ou via do cliente.
