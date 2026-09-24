# -*- coding: utf-8 -*-
import os

def generate_b2_persistence_plan():
    content = """# PRIMOX WORKSHOP — PLANO DE PERSISTÊNCIA DIAGNÓSTICA (FASE B2)
## ESTRATÉGIA DE ARMAZENAMENTO RELACIONAL, MIGRAÇÃO FUTURA E CONVIVÊNCIA COM LEGADO

**Data:** 2026-09-23  
**Branch:** `audit/product-discovery-2026-09`  
**Regra Suprema:** O banco real de produção permanece 100% INTACTO. Nenhuma migration será aplicada em produção nesta fase.

---

## 1. Arquitetura de Persistência em Duas Camadas

Para garantir total conformidade com a salvaguarda de produção e entregar persistência pericial estruturada, adotou-se o padrão de persistência em duas camadas:

```text
[Camada 1: Runtime Desktop Modular]
- Persistência estruturada por documento JSON em:
  %LOCALAPPDATA%\\PrimoAutoEletrica\\AutoEletrica\\diagnosticos\\{DiagnosticoId}.json
- Índice de busca rápida em memória e cache relacional
- Vínculo obrigatório com OrdemServicoId e VeiculoId
- Sem interferência em schema de banco de produção

[Camada 2: Schema Relacional SQLite / SQL Server para Migração Futura]
- Definição formal das tabelas DiagnosticosTecnicos e DiagnosticoMedicoes
- Homologado em ambiente de testes automatizados com SQLite isolado
- Migration formal pronta para aplicação em fase futura de banco
```

---

## 2. Especificação do Schema Relacional Futuro

### Tabela 1: `DiagnosticosTecnicos`
Armazena a entidade mestre de cada intervenção diagnóstica realizada em uma OS ou veículo.

| Coluna | Tipo SQLite | Restrições | Descrição |
| :--- | :--- | :--- | :--- |
| `Id` | `TEXT (GUID)` | `PRIMARY KEY NOT NULL` | Identificador único universal do diagnóstico |
| `OrdemServicoId` | `TEXT (GUID)` | `NOT NULL` | Vínculo obrigatório com a Ordem de Serviço |
| `VeiculoId` | `TEXT (GUID)` | `NOT NULL` | Vínculo obrigatório com o Veículo testado |
| `ClienteId` | `TEXT (GUID)` | `NULLABLE` | Vínculo com o cliente proprietário |
| `TecnicoId` | `TEXT` | `NULLABLE` | Usuário/Eletricista responsável pelo laudo |
| `DataHora` | `TEXT (ISO8601)` | `NOT NULL` | Data e hora de abertura do diagnóstico |
| `DataConclusao` | `TEXT (ISO8601)` | `NULLABLE` | Data e hora de conclusão técnica |
| `Status` | `TEXT` | `NOT NULL DEFAULT 'EmAndamento'` | 'EmAndamento', 'Concluido', 'Cancelado' |
| `RoteiroCodigo` | `TEXT` | `NOT NULL` | Código do roteiro (ex: 'D01', 'D02', 'D03') |
| `SintomaRelatado` | `TEXT` | `NOT NULL` | Descrição do problema informado na abertura |
| `SintomaCategoria` | `TEXT` | `NULLABLE` | Categoria (Partida, Carga, Iluminação, etc.) |
| `DiagnosticoLaudo`| `TEXT` | `NULLABLE` | Parecer pericial do eletricista |
| `CausaStatus` | `TEXT` | `NOT NULL DEFAULT 'Provavel'` | 'Confirmada', 'Provavel', 'NaoDeterminada' |
| `CausaDescricao` | `TEXT` | `NULLABLE` | Causa física identificada da anomalia |
| `CorrecaoExecutada`| `TEXT` | `NULLABLE` | Ação corretiva realizada na oficina |
| `PecaUtilizadaId` | `TEXT (GUID)` | `NULLABLE` | FK para Produtos (peça substituída) |
| `ServicoUtilizadoId`| `TEXT (GUID)`| `NULLABLE` | FK para Serviços (mão de obra executada) |
| `Observacoes` | `TEXT` | `NULLABLE` | Observações técnicas gerais |
| `CreatedAt` | `TEXT` | `NOT NULL DEFAULT (CURRENT_TIMESTAMP)` | Registro de auditoria |
| `UpdatedAt` | `TEXT` | `NOT NULL DEFAULT (CURRENT_TIMESTAMP)` | Registro de auditoria |

**Índices Requeridos:**
* `IX_DiagnosticosTecnicos_OrdemServicoId` em `(OrdemServicoId)`
* `IX_DiagnosticosTecnicos_VeiculoId` em `(VeiculoId)`
* `IX_DiagnosticosTecnicos_DataHora` em `(DataHora DESC)`

---

### Tabela 2: `DiagnosticoMedicoes`
Armazena cada medição objetiva executada dentro de um diagnóstico técnico, suportando ciclo Antes vs. Depois.

| Coluna | Tipo SQLite | Restrições | Descrição |
| :--- | :--- | :--- | :--- |
| `Id` | `TEXT (GUID)` | `PRIMARY KEY NOT NULL` | Identificador único da medição |
| `DiagnosticoId` | `TEXT (GUID)` | `NOT NULL REFERENCES DiagnosticosTecnicos(Id) ON DELETE CASCADE` | FK para a intervenção diagnóstica |
| `NomeTeste` | `TEXT` | `NOT NULL` | Nome do teste (ex: 'Tensão de partida') |
| `TipoGrandeza` | `TEXT` | `NOT NULL` | 'Tensao', 'Corrente', 'Resistencia', 'QuedaTensao', 'FugaCorrente', 'CCA' |
| `Instrumento` | `TEXT` | `NULLABLE` | 'Multimetro', 'AlicateAmperimetroDC', 'TestadorBateria' |
| `Unidade` | `TEXT` | `NOT NULL` | 'V', 'mV', 'A', 'mA', 'Ω', 'kΩ', 'CCA', '°C' |
| `ValorReferenciaMin`| `DECIMAL(18,4)`| `NULLABLE` | Limite inferior tolerável |
| `ValorReferenciaMax`| `DECIMAL(18,4)`| `NULLABLE` | Limite superior tolerável |
| `TextoReferencia` | `TEXT` | `NULLABLE` | Especificação textual de fábrica |
| `ValorInicial` | `DECIMAL(18,4)`| `NOT NULL` | Valor obtido antes da correção |
| `ValorPosReparo` | `DECIMAL(18,4)`| `NULLABLE` | Valor obtido no teste de validação final |
| `Resultado` | `TEXT` | `NOT NULL` | 'NORMAL', 'FORA_DO_ESPERADO', 'INCONCLUSIVO', 'NAO_REALIZADO' |
| `Observacao` | `TEXT` | `NULLABLE` | Anotações periciais do técnico |

**Índices Requeridos:**
* `IX_DiagnosticoMedicoes_DiagnosticoId` em `(DiagnosticoId)`

---

## 3. Estratégia de Migração e Convivência com Legado

### 3.1 Tratamento do Arquivo `roteiros-resultados.json`
O arquivo legado não será excluído nem corrompido:
1. **Fallback de Leitura:** O serviço `DiagnosticoTecnicoService` manterá leitura transparente do arquivo legado para histórico anterior à B2.
2. **Importação Não-Destrutiva:** Uma rotina de importação `ImportarResultadosLegados()` lê as entradas de `roteiros-resultados.json`, converte-as em registros do novo modelo (atribuindo `VeiculoId = Guid.Empty` e marcando `OrigemLegado = true`) e salva na nova base.
3. **Escrita Nova:** Todos os novos diagnósticos executados a partir da Fase B2 são gravados no novo formato com `OrdemServicoId` e `VeiculoId` obrigatórios.

---

## 4. Relação com a Camada de Dinheiro (Money Safeguard)

> [!IMPORTANT]
> A persistência diagnóstica manipula grandezas físicas (`Volts`, `Amperes`, `Ohms`, `CCA`). Nenhum valor monetário é armazenado na tabela de medições.
> O vínculo financeiro ocorre exclusivamente por relacionamento: `PecaUtilizadaId` aponta para `Produtos.Id` e `ServicoUtilizadoId` para serviços, ambos já homologados e operando sob a blindagem **MoneyIO/CentsV1**.
"""
    with open("Docs/audit/2026-09-20/B2_DIAGNOSTIC_PERSISTENCE_PLAN.md", "w", encoding="utf-8") as f:
        f.write(content.strip() + "\n")
    print("B2_DIAGNOSTIC_PERSISTENCE_PLAN.md written successfully.")

if __name__ == "__main__":
    generate_b2_persistence_plan()
