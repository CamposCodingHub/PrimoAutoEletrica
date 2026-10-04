# PRIMOX 3.0 — DIAGNOSTIC EVIDENCE ENGINE & SAFETY LAYER SPECIFICATION
**Data:** 04 de Outubro de 2026  
**Versão:** 3.0.0-DIAGNOSTICS  
**Status:** ESPECIFICAÇÃO DO MOTOR DE EVIDÊNCIAS E CAMADA DE SEGURANÇA TÉCNICA  
**Princípio Central:** O PRIMOX não é um chatbot automotivo opinativo; é um motor determinístico de investigação baseado em medições reais e provas físicas.

---

## 1. O CICLO DE EVIDÊNCIA DIAGNÓSTICA

O diagnóstico técnico automotivo profissional no PRIMOX 3.0 segue o rigor do método científico e da engenharia de teste:

```
[Entrada do Caso]
  Veículo (Aplicação: Ano / Motor / Injeção / Tensão 12V ou 24V)
      │
      ▼
  Sintoma Observado + DTCs Gravados (Scan Tool)
      │
      ▼
[Engenharia de Hipóteses]
  Hipóteses Formuladas (ex: Linha 50 interrompida vs Falha de massa no motor)
      │
      ▼
[Barreira de Segurança - Diagnostic Safety Layer]
  Validação de Risco (Risco de curto? Risco térmico? Perigo a módulos?)
  ──► SE PERIGOSO: Exige condição de parada, EPI e ferramenta específica
      │
      ▼
[Execução de Teste e Medição Física]
  Instrumento (Multímetro / Osciloscópio / Ponta Lógica / Caneta de Polaridade)
  Condição de Teste (Sob partida, Chave ligada, Linha 30 em repouso)
      │
      ▼
  Medição Coletada: [ Valor ] [ Unidade: V, Ω, A, bar, ms, °C ]
  Comparação com Faixa Esperada OEM: [ Min .. Max ]
  Resultado do Teste: PASS / FAIL / INCONCLUSIVO
      │
      ▼
[Avaliação da Hipótese]
  ├── Se FAIL: Hipótese Confirmada ou Nova Investigação no Circuito
  └── Se PASS: Hipótese DESCARTADA com Prova Técnica Registrada
      │
      ▼
[Conclusão e Registro de Reparo]
  Causa Raiz Identificada ──► Peça / Procedimento Aplicado ──► Teste de Validação
      │
      ▼
[Alimentação do PRIMOX Repair Intelligence]
  Caso Registrado com Proveniência Auditável (Sem estatísticas falsas)
```

---

## 2. NÍVEIS HIERÁRQUICOS DE CONFIANÇA DA EVIDÊNCIA

Para que o técnico e o proprietário da oficina sempre saibam a procedência de cada informação técnica exibida na interface, o sistema adota 7 níveis padronizados de evidência:

| Nível | Identificador | Denominação | Descrição e Critério de Classificação |
| :---: | :--- | :--- | :--- |
| **0** | `INSUFFICIENT` | **Evidência Insuficiente** | Dados ausentes ou contraditórios. O sistema recusa emitir conclusão e solicita testes específicos. |
| **1** | `HYPOTHESIS` | **Hipótese de Trabalho** | Sugestão lógica baseada em sintomas relatados, pendente de confirmação física por medição. |
| **2** | `INFERENCE` | **Inferência Estatística / IA** | Associação gerada por modelo de linguagem ou correlação semântica de sintomas. |
| **3** | `SIMILAR_CASE` | **Caso Semelhante Catalogado** | Ocorrência resolvida em veículo da mesma família mecânica ou com a mesma central de injeção. |
| **4** | `WORKSHOP_CONFIRMED` | **Caso PRIMOX Confirmado** | Defeito solucionado com O.S. finalizada, medições anexadas e sem retorno em garantia por mais de 30 dias. |
| **5** | `VALIDATED_TECHNICAL` | **Literatura Técnica Validada** | Procedimento extraído de manual de bancada especializado, diagramas revisados ou norma técnica (ex: DIN 72551). |
| **6** | `OEM_LICENSED` | **Evidência OEM / Licenciada** | Boletim técnico oficial de montadora (TSB) ou diagrama elétrico licenciado de fábrica. |

> [!IMPORTANT]
> **Regra Absoluta da IA:** Hipóteses e Inferências (Níveis 1 e 2) **NUNCA** podem ser apresentadas na interface com o peso visual de casos confirmados ou especificações OEM (Níveis 4 a 6).

---

## 3. PROVENIÊNCIA E CLASSIFICAÇÃO DE DADOS (CORREÇÃO DE DADOS SINTÉTICOS)

Em substituição à denominação errônea de "RedeHomologada" anteriormente utilizada no protótipo, a taxonomia de origem de dados é reestruturada em:

```csharp
public enum KnowledgeSourceKind
{
    Synthetic,           // Dado de laboratório gerado para testes e bootstrap
    CuratedTechnical,    // Caso técnico redigido e curado por especialista do projeto
    ValidatedTechnical,  // Procedimento de bancada validado com diagrama comprovado
    PRIMOXVerifiedCase,  // Reparo real solucionado na oficina local com O.S. vinculada
    OEMLicensed,         // Manual oficial da montadora ou literatura homologada
    UserReported,        // Relato de técnico sem medição comprobatória anexada
    AIHypothesis         // Proposição gerada dinamicamente por modelo generativo
}
```

---

## 4. `DIAGNOSTIC SAFETY LAYER` (A CAMADA DE SEGURANÇA TÉCNICA)

O PRIMOX recusa atuar como um manual imprudente. Qualquer procedimento sugerido pela IA ou pelos fluxogramas de diagnóstico deve ser filtrado pela **Camada de Segurança**, avaliando os riscos inerentes à intervenção:

### 4.1 Categorias de Risco Monitoradas

1. **Risco de Curto-Circuito / Dano a Módulos (ECU/BCM):**
   * *Operações Críticas:* Teste de continuidade com conector acoplado; aplicação direta de 12V em linhas de sinal (5V / TPS / MAF / MAP); energização reversa de sensores Hall.
   * *Barreira PRIMOX:* **TRAVA AUTOMÁTICA**. Exige desconexão do chicote da ECU antes de aplicar qualquer tensão externa de teste.
2. **Risco em Redes de Comunicação (CAN / LIN):**
   * *Operações Críticas:* Medição de resistência de terminação (60 Ω) com bateria conectada ou chave ligada.
   * *Barreira PRIMOX:* Exige desligamento da chave de ignição e repouso da rede CAN (bateria desconectada) para evitar destruição de transceivers TJA1050/PCA82C250.
3. **Risco Térmico e Alta Corrente (Partida e Alternador):**
   * *Operações Críticas:* Ponte direta (jumper) no automático do motor de partida; teste de rotor de alternador travado.
   * *Barreira PRIMOX:* Exige confirmação de veículo em ponto morto / freio de mão acionado; proíbe uso de cabos finos sob risco de queima do chicote.
4. **Sistemas 24V de Linha Pesada:**
   * *Operações Críticas:* Instalação de acessórios 12V na tomada de 24V; medição em bancos de baterias em série.
   * *Barreira PRIMOX:* Verificação automática da tensão nominal do veículo. Se 24V, exibe alerta visual de alta energia e exige uso de atenuadores ou ferramentas classe CAT-III.

### 4.2 Estrutura de Contrato de Segurança

```csharp
namespace PRIMOX.Diagnostics.Safety
{
    public record DiagnosticSafetyCheck(
        string OperationName,
        SafetyRiskLevel RiskLevel,
        string HazardDescription,
        string RequiredPrerequisite,
        string RequiredTool,
        string StopCondition
    );

    public enum SafetyRiskLevel
    {
        Low,       // Medições passivas de tensão contínua em repouso
        Moderate,  // Testes de continuidade com chicote isolado
        High,      // Energização forçada de atuadores e relés de potência
        Critical   // Intervenção direta em linha de combustível, alta corrente ou 24V
    }
}
```

---

## 5. ESTRUTURA UNIVERSAL DA MEDIÇÃO FÍSICA

O técnico deve conseguir registrar a prova material do defeito em poucos toques:

```csharp
namespace PRIMOX.Diagnostics.Measurements
{
    public class DiagnosticMeasurement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DiagnosticSessionId { get; set; }
        public string ComponentOrCircuit { get; set; } = string.Empty; // ex: "Linha 50 - Automático do Arranque"
        public decimal MeasuredValue { get; set; }                    // ex: 3.10
        public string Unit { get; set; } = "V";                        // "V", "mV", "A", "Ω", "bar", "°C"
        public string InstrumentUsed { get; set; } = "Multímetro Digital Minipa";
        public string TestCondition { get; set; } = "Sob acionamento da partida";
        
        public decimal ExpectedMin { get; set; }                       // ex: 10.50
        public decimal ExpectedMax { get; set; }                       // ex: 12.80
        
        public MeasurementResult Result => (MeasuredValue >= ExpectedMin && MeasuredValue <= ExpectedMax)
            ? MeasurementResult.Pass
            : MeasurementResult.Fail;

        public string? TechnicalNote { get; set; }
        public string? PhotoOrWaveformPath { get; set; }
    }

    public enum MeasurementResult
    {
        Pass,
        Fail,
        Inconclusive
    }
}
```

---

## 6. EVOLUÇÃO PARA O `PRIMOX REPAIR INTELLIGENCE`

A base de conhecimento proprietária do PRIMOX substitui a dependência de nomenclaturas de terceiros (`SureTrack`). Cada oficina participante do ecossistema contribui com reparos confirmados de forma anonimizada:

1. **Filtro de Qualidade de Entrada:** Uma O.S. só gera um registro de inteligência se possuir:
   - Diagnóstico final preenchido.
   - Pelo menos uma medição técnica registrada (prova do defeito).
   - Peça ou serviço efetivamente faturado.
   - Ausência de retorno por garantia no mesmo sistema.
2. **Estatísticas Confiáveis:** O cálculo de probabilidade de causas exibe claramente o volume de amostras:
   - *"Baseado em 14 casos confirmados da literatura técnica curada (CuratedTechnical) para Onix 1.0/1.4 SPE/4."*
   - Nunca inventando percentuais fictícios quando a amostra for pequena.
