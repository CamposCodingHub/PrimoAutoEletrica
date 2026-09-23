# PRIMOX WORKSHOP — FASE B2
## ESTADO ATUAL DA AUTOELÉTRICA TÉCNICA (AUDITORIA PRÉ-IMPLEMENTAÇÃO)

**Data de Auditoria:** 2026-09-23  
**Branch:** `audit/product-discovery-2026-09`  
**Objetivo:** Mapear rigorosamente todo o ecossistema existente de autoeletricidade técnica antes de iniciar qualquer modificação no código.

---

## 1. Visão Geral do Componente Atual

A funcionalidade de **Autoelétrica Técnica** foi introduzida no PRIMOX Workshop para fornecer apoio operacional ao eletricista automotivo. A auditoria B1 classificou o módulo como **`PARTIAL`** pelas seguintes razões estruturais:

1. **Prontuário Elétrico Existente no Banco de Dados (`CORE`):** A tabela `Veiculos` possui 17 campos específicos de telemetria elétrica (bateria, alternador, motor de partida, testes de tensão, corrente de fuga, aterramentos, lâmpadas, relés, fusíveis e chicotes).
2. **Roteiros Guiados de Diagnóstico Existentes (`PARTIAL`):** Existem 17 procedimentos técnicos catalogados (D01 a D17) cobrindo os defeitos mais comuns de oficinas de autopeças e serviços elétricos.
3. **Persistência Global Sem Vínculo de Identidade (`PARTIAL` / `RISK`):** A gravação do resultado de um roteiro é executada pelo serviço `AutoEletricaRoteiroPersistService.cs`, que armazena os dados em um arquivo JSON global (`AppData/AutoEletrica/roteiros-resultados.json`), indexado apenas pela chave textual `Codigo` (ex: "D01").
4. **Ausência de Chaves Estrangeiras:** O resultado não registra `OrdemServicoId` nem `VeiculoId`. Se um veículo passar pelo teste D01 hoje, o teste D01 de outro veículo amanhã sobrescreverá o resultado anterior.
5. **Histórico por Sintoma Desconectado de IDs:** O serviço `SintomaCausaHistoricoService.cs` armazena dados em `sintoma-causa-historico.json` contendo apenas strings textuais de `Veiculo` e `Placa`, sem chaves primárias.

---

## 2. Inventário de Arquivos e Componentes Existentes

| Tipo | Arquivo | Função Atual |
| :--- | :--- | :--- |
| **Model** | `PrimoAutoEletrica/Models/AutoEletricaTecnica.cs` | Contém `ProntuarioEletricoCampo`, `ProntuarioEletricoVeiculo`, `DiagnosticoGuiadoRoteiro`, `BibliotecaTecnicaItem`, `DefeitoRecorrenteResumo`, `SugestaoPecasServico`, `ServicoTecnicoAutoEletrica`, `AutoEletricaTecnicaSnapshot`. |
| **View (Control)** | `PrimoAutoEletrica/UserControls/AutoEletricaTecnicaControl.xaml` | Interface em 3 colunas: Coluna 1 (Prontuário e Defeitos), Coluna 2 (Diagnóstico Guiado e Biblioteca), Coluna 3 (Serviços e Sugestão de Peças). |
| **Codebehind** | `PrimoAutoEletrica/UserControls/AutoEletricaTecnicaControl.xaml.cs` | Orquestra carregamento, seleção de roteiro e geração de rascunho de orçamento via botão. |
| **ViewModel** | `PrimoAutoEletrica/ViewModels/AutoEletricaTecnicaViewModel.cs` | `AutoEletricaTecnicaViewModel` com coleções observáveis para roteiros, biblioteca e defeitos. |
| **Service Principal**| `PrimoAutoEletrica/Services/AutoEletricaTecnicaService.cs` | Constrói o snapshot, mapeia prontuário da tabela `Veiculos` e define catálogo em memória dos roteiros D01 a D17. |
| **Service Persistência**| `PrimoAutoEletrica/Services/AutoEletricaRoteiroPersistService.cs` | Salva e carrega resultados em `roteiros-resultados.json`. |
| **Service Histórico**| `PrimoAutoEletrica/Services/SintomaCausaHistoricoService.cs` | Registra e contabiliza frequência de sintomas em `sintoma-causa-historico.json`. |
| **View Integradora**| `PrimoAutoEletrica/Views/VisualizarVeiculoWindow.xaml.cs` | Exibe a telemetria do veículo (Vehicle 360) lendo os campos de `Veiculos`. |
| **View OS** | `PrimoAutoEletrica/Views/OrdemServicoWindow.xaml.cs` | Manipula campos textuais livres `DiagnosticoInicial` e `DiagnosticoFinal` da OS. |
| **Banco Real** | Tabela `Veiculos` (SQLite) | Armazena dados atuais de telemetria do veículo em 17 colunas TEXT/INTEGER. |

---

## 3. Roteiros Existentes no Catálogo em Memória (D01–D17)

* **D01:** Veículo não dá partida
* **D02:** Bateria descarregando
* **D03:** Alternador não carrega
* **D04:** Motor de partida pesado
* **D05:** Fusível queimando
* **D06:** Farol fraco
* **D07:** Luz de ré não acende
* **D08:** Lanterna não acende
* **D09:** Limpador não funciona
* **D10:** Limpador só funciona uma velocidade
* **D11:** Vidro elétrico não funciona
* **D12:** Trava elétrica não funciona
* **D13:** Seta não funciona
* **D14:** Painel marcando errado
* **D15:** Curto intermitente
* **D16:** Relé não aciona
* **D17:** Mau aterramento

---

## 4. Análise do Arquivo Legado `roteiros-resultados.json`

O arquivo é gerado no diretório `%LOCALAPPDATA%\PrimoAutoEletrica\AutoEletrica\roteiros-resultados.json`:
```json
[
  {
    "Codigo": "D01",
    "Resultado": "Tensao de partida 8.9V",
    "Conclusao": "Bateria sem capacidade de partida",
    "AtualizadoEm": "2026-09-18T10:30:00"
  }
]
```
**Limitações Críticas:**
1. A lista é sobrescrita a cada gravação do mesmo código (`lista.RemoveAll(x => x.Codigo == roteiro.Codigo)`).
2. Não há identificação do veículo testado.
3. Não há número da OS associada.
4. Não há distinção entre a medição inicial e a medição pós-reparo.

---

## 5. Diretrizes Obrigatórias para a Fase B2

1. **Preservação:** Não apagar o arquivo legado nem os métodos existentes de `AutoEletricaRoteiroPersistService`.
2. **Nova Entidade `DiagnosticoTecnico`:** Criar entidade rica com `Id = Guid`, `OrdemServicoId = Guid`, `VeiculoId = Guid`, lista de medições estruturadas e status.
3. **Medição Objetiva:** Separar Teste de Medição (unidades: `V`, `A`, `mA`, `Ω`, `CCA`), com referências 12V e 24V.
4. **Ciclo Pós-Reparo:** Armazenar `ValorInicial` e `ValorPosReparo` para comprovação pericial de melhoria.
5. **Integração sem Redigitação:** Permitir selecionar a OS e o Veículo ativo para registrar o laudo.
