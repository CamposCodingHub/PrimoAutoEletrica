# PRIMOX Workshop — Trilha B: Plano de Avanço Paralelo de Produto (B1 a B12)

**Data:** 2026-09-23  
**Status da Trilha:** Em Planejamento Ativo e Estruturação Técnica  
**Premissa Operacional:** A migração de dados no banco de produção continua **BLOQUEADA** (`MONEY MIGRATION REAL = BLOCKED`). O desenvolvimento de produto avança sem depender da virada de schema físico em produção.

---

## 1. Visão Geral da Trilha B

A Trilha B consolida a transição do PRIMOX de uma suíte técnica em homologação para um **produto comercial de classe empresarial para oficinas mecânicas e autoelétricas pesadas**.

O foco é eliminar mocks, fakes e telas puramente estéticas, garantindo que todo fluxo possua:
1. Modelo de dados persistente e normalizado;
2. Serviços de domínio com regras de negócio e validação matemática;
3. Interface WPF responsiva e harmônica com o Design System (Light/Dark);
4. Rastreabilidade auditável por ID.

---

## 2. Detalhamento dos Módulos da Trilha B

### B1 — Product Discovery & Ciclo 360 Completo
Mapeamento do ciclo de vida ponta-a-ponta:
$$\text{Lead/Cliente} \rightarrow \text{Veículo} \rightarrow \text{DVI/Inspeção} \rightarrow \text{Orçamento} \rightarrow \text{Aprovação} \rightarrow \text{OS} \rightarrow \text{Diagnóstico} \rightarrow \text{Peças/Serviços} \rightarrow \text{Execução} \rightarrow \text{Pagamento/Caixa} \rightarrow \text{Histórico 360} \rightarrow \text{Pós-Venda}$$

* **Auditoria de Componentes:**
  - *Existente com Persistência:* Clientes, Veículos, Produtos, Orçamentos, Ordens de Serviço, Contas a Pagar/Receber, Movimentações Financeiras, Caixa.
  - *Parcialmente Integrado:* DVI (Digital Vehicle Inspection), Checklists fotográficos, Histórico Técnico de Baterias.
  - *Em Consolidação:* Encadeamento automático de status (Inspeção com Não-Conformidade $\rightarrow$ Item no Orçamento $\rightarrow$ Orçamento Aprovado $\rightarrow$ Liberação na OS).

---

### B2 & B3 — Autoelétrica Tech/Heavy & Diagnóstico Técnico Avançado
Diferencial competitivo central do PRIMOX no segmento elétrico automotivo e frota pesada (24V, alternadores de alta potência, módulos ECU, sistemas de partida multiplexados).

#### Estrutura Técnica de Dados
```text
Veículo (Placa / Chassi)
 └── Sistema Elétrico (Carga / Partida / Injeção / Acessórios / Iluminação)
      └── Sintoma Declarado (Ex: "Partida pesada a frio", "Queda de tensão intermitente")
           └── Teste Físico Executado
                ├── Medição Registrada (Ex: Tensão de Repouso: 12.4V; Partida: 9.6V; Carga: 14.2V; Fuga: 80mA)
                ├── Diagnóstico Emitido (Ex: "Resistência de contato elevada no borne negativo")
                ├── Causa Provável (Ex: "Oxidação por sulfatação / aterramento deficiente")
                ├── Correção / Ação (Ex: "Substituição de terminal e limpeza de malha de terra")
                ├── Peça Utilizada (Código do Estoque) + Mão de Obra
                ├── Teste Pós-Reparo (Ex: Tensão na partida subiu para 11.2V)
                └── Certificado de Garantia Elétrica
```

---

### B4 — Integração DVI $\rightarrow$ Orçamento $\rightarrow$ OS (Zero Redigitação)
* **Objetivo:** O técnico executa a inspeção visual e elétrica no tablet/computador da bancada.
* **Mecanismo:** Cada item não-conforme gera automaticamente uma sugestão de peça/serviço no orçamento.
* **Aprovação do Cliente:** Itens aprovados pelo cliente no orçamento são convertidos em tarefas da OS com tempo padrão e valor travado.

---

### B5 & B6 — Hubs Cliente 360 e Veículo 360
* **Cliente 360:**
  - Painel com resumo patrimonial (todos os veículos vinculados);
  - Histórico de orçamentos, aprovações e recusas;
  - LTV (*Life Time Value* / Total Gasto) com precisão monetária `MoneyIO`;
  - Score de pontualidade financeira e histórico de inadimplência;
  - Vínculo obrigatório por `ClienteId` (`Guid`), nunca por correspondência de string.
* **Veículo 360:**
  - Linha do tempo técnica completa de todas as OSs já executadas no carro/caminhão;
  - Rastreabilidade de peças substituídas (marcas, modelos e números de série);
  - Próximas revisões preventivas (quilometragem projetada por média de rodagem diária);
  - Histórico de diagnósticos elétricos e testes de bateria.

---

### B7 — Design System & Harmonização UX (Light & Dark)
* **Resoluções Suportadas:**
  - `1280x720` (notebooks básicos de oficina e terminais de bancada);
  - `1366x768` (resolução predominante no setor);
  - `1920x1080` (estações de gerência e monitores principais).
* **Diretrizes de Estilo:**
  - Modo Escuro (Dark Mode): Fundo `#121214`, superfícies `#1E1E24`, cartões `#282830`, textos `#E1E1E6`, acentos `#FF7A00` / `#00875F`.
  - Zero fundos brancos acidentais ou bordas `#FFFFFF` não-estilizadas no Dark Mode.
  - Tipografia moderna (*Segoe UI* / *Inter*), tabelas com scroll suave e densidade equilibrada.

---

### B8 — Perfis Operacionais de Usuário (RBAC)
Simulação dos 4 perfis operacionais do ecossistema da oficina:
1. **Caixa / Atendente:**
   - Abertura de caixa, registro de recebimentos (dinheiro, PIX, cartão), sangrias, suprimentos e fechamento de caixa cego com conciliação.
2. **Eletricista / Mecânico:**
   - Visualização da fila de OSs atribuídas, preenchimento de checklists de medição técnica, solicitação de peças no almoxarifado, apontamento de horas e encerramento técnico.
3. **Ajudante Financeiro / Faturamento:**
   - Gestão de contas a pagar (fornecedores), emissão de cobranças de contas a receber, conciliação bancária diária e emissão de notas fiscais/recibos.
4. **Gestor / CEO:**
   - Dashboard em tempo real com faturamento diário/mensal, ticket médio, margem líquida, ranking de produtividade técnica e previsão de fluxo de caixa.

---

### B9 — Pipeline Contínuo de Confiabilidade (QA)
Fluxo mandatário para qualquer evolução de produto:
$$\text{Requisito} \rightarrow \text{Design} \rightarrow \text{Implementação} \rightarrow \text{Compilação Release (0 erros)} \rightarrow \text{Testes Unitários / Integração} \rightarrow \text{Validação Light/Dark} \rightarrow \text{Commit}$$

---

### B10, B11 & B12 — Auditoria de Maturidade Comercial e Readiness

| Módulo do Produto | Classificação de Maturidade | Dependências / Status |
| :--- | :---: | :--- |
| **Cadastros (Clientes/Veículos)** | `CORE` | 100% Persistente e Integrado |
| **Produtos e Almoxarifado** | `CORE` | 100% Persistente e Integrado |
| **Orçamentos e Cálculos** | `CORE` | 100% Blindado com `MoneyIO` |
| **Ordens de Serviço (OS)** | `CORE` | 100% Persistente e Integrado |
| **Financeiro e Caixa** | `CORE` | 100% Blindado com `MoneyIO` |
| **Cliente 360 / Veículo 360** | `ADVANCED` | Integrado por ID; expansão visual contínua |
| **Diagnóstico Autoelétrica Tech** | `ADVANCED` | Estrutura de dados definida; modelos ativos |
| **DVI / Checklist Fotográfico** | `PARTIAL` | Funcional; integração com fluxo de OS em expansão |
| **Emissão Fiscal (NFe/NFCe)** | `PARTIAL` | Schemas de importação prontos; contingência local |
| **TEF / Pagamento Integrado** | `DEMO` | Requer homologação de gateway externo |
| **WhatsApp Cloud / Portal Web** | `ROADMAP` | Não implementar sem validação de conectividade |

---

## 3. Conclusão

A Trilha B fornece o plano de rota técnico e comercial para transformar o PRIMOX em uma referência definitiva de tecnologia para centros automotivos, enquanto a infraestrutura de dados monetários permanece estritamente resguardada até a homologação final.
