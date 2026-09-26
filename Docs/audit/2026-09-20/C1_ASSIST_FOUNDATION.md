# PRIMOX WORKSHOP — CICLO C1
# ARQUITETURA DA FUNDAÇÃO DO PRIMOX ASSIST (C1.4)
**Data:** 2026-09-25  
**Módulo:** PRIMOX Assist Foundation — Arquitetura de Assistência Técnica e RAG  
**Status Arquitetural:** IMPLEMENTATION SPEC  

---

## 1. VISÃO GERAL & PRINCÍPIO DO PRIMOX ASSIST

O **PRIMOX Assist** não é um chatbot genérico que responde amenidades ou inventa respostas.
O objetivo no Ciclo C1 é estabelecer a **fundação arquitetural** sólida para que, quando um modelo de inteligência artificial (local ou externo) for plugado, ele opere estritamente sobre a realidade física comprovada da oficina:

```
                  PERGUNTA DO USUÁRIO
                           ↓
                   CONTEXTO PRIMOX
                           ↓
             RECUPERAÇÃO ESTRUTURADA (RAG)
      ┌────────────────────┼────────────────────┐
      │                    │                    │
DADOS DO VEÍCULO      HISTÓRICO DA OS      BASE TÉCNICA
      │                    │                    │
      └────────────────────┼────────────────────┘
                           ↓
                MODELO / PROVIDER DE IA
                           ↓
                VALIDAÇÃO DE GROUNDING
                           ↓
                 RESPOSTA + EVIDÊNCIAS
                           ↓
                    DECISÃO HUMANA
```

---

## 2. REGRAS FUNDAMENTAIS DE GROUNDING & SEGURANÇA

### Regra 1: A IA Não Deve Inventar (Zero Alucinação)
Se a base de conhecimento estruturada e o histórico da oficina não contiverem evidências suficientes para respaldar uma hipótese, a resposta deve ser obrigatoriamente categórica:
> *"Não encontrei evidência suficiente na base PRIMOX para concluir isso. Recomendo realizar as seguintes medições técnicas..."*

### Regra 2: Papel de Assistente, Nunca de Autoridade Final
O assistente técnico sugere hipóteses ordenadas e procedimentos de verificação. Ele **nunca** afirma categoricamente: *"Troque a peça X"* sem evidência comprovada por medição física. O diagnóstico final e a responsabilidade da intervenção pertencem exclusivamente ao técnico humano.

### Regra 3: Falha Fechada (Fail-Closed)
Na ausência de conectividade, chave de API ou modelo configurado, o sistema opera de forma determinística utilizando o motor de regras local e busca estruturada de artigos técnicos, sem falhas de runtime nem travamentos de interface.

---

## 3. ABSTRAÇÃO DE PROVEDOR (`IAssistantProvider`)

Para evitar qualquer acoplamento a um fornecedor específico (OpenAI, Anthropic, Google, Ollama ou modelo local), o núcleo da aplicação interage exclusivamente por meio da interface `IAssistantProvider`:

```csharp
public interface IAssistantProvider
{
    string ProviderId { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default);
}
```

### Provedores Implementados no C1:
1. `GroundedLocalRuleAssistantProvider`: Provedor padrão, 100% autônomo, sem dependência externa de rede. Executa a correspondência do contexto operacional (Veículo, Tensão 12V/24V, Sintoma, D01-D06) contra a base técnica do PRIMOX e retorna respostas embasadas com roteiro de testes e evidências citadas.
2. `ExternalAssistantProviderAdapter`: Estrutura preparada e extensível para provedores externos via HTTP/REST, ativada somente quando credenciais forem fornecidas pelo administrador nas configurações.

---

## 4. MODELO DE CONTEXTO ESTRUTURADO (`AssistantQueryContext`)

O contexto enviado para a consulta técnica compõe todas as variáveis operacionais:
- `Query` (`string`): A pergunta formulada pelo técnico ou orçamentista.
- `VehicleContext` (`AssistantVehicleContext?`): Placa, Marca, Modelo, Ano, Categoria (Leve/Pesada) e Tensão Elétrica (12V ou 24V).
- `WorkOrderContext` (`AssistantWorkOrderContext?`): Número da OS, Sintoma relatado pelo cliente, status atual e itens já lançados.
- `TechnicalMeasurements` (`IReadOnlyList<AssistantMeasurementContext>`): Grandezas físicas reais já aferidas no veículo (tensão em repouso, queda de partida, corrente de repouso).
- `RelevantKnowledge` (`IReadOnlyList<TechnicalKnowledgeEntry>`): Documentos técnicos recuperados por similaridade textual e filtro semântico de subsistema.
- `RelevantCases` (`IReadOnlyList<DiagnosticCase>`): Casos reais anteriores do mesmo modelo de veículo ou mesmo sintoma.

---

## 5. RESPOSTA ESTRUTURADA COM EVIDÊNCIAS (`AssistantResponse`)

A saída do PRIMOX Assist entrega ao técnico não apenas um texto, mas blocos acionáveis:
- `AnswerMarkdown` (`string`): Explicação clara e serena da análise técnica.
- `Hypotheses` (`IReadOnlyList<AssistantHypothesis>`): Lista de causas prováveis ranqueadas com justificativa.
- `RecommendedActions` (`IReadOnlyList<string>`): Próximos passos e testes práticos (ex: medir queda no pino 30 do relé).
- `CitedSources` (`IReadOnlyList<AssistantSourceCitation>`): Códigos dos boletins (`KB-ELET-001`) ou casos reais (`CASO-20260925-001`) que embasam a recomendação.
- `ConfidenceLevel` (`AssistantConfidenceLevel`): `HIGH`, `MEDIUM`, `LOW`, `INSUFFICIENT_EVIDENCE`.
- `Disclaimers` (`string`): Aviso mandatório de segurança e validação física pelo técnico.
