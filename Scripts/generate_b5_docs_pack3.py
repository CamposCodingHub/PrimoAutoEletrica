# -*- coding: utf-8 -*-
"""
Pack 3 of B5.0 Documents:
13. B5_0_SECURITY_RELEASE_CHECKLIST.md
14. B5_0_ROLE_TASK_MATRIX.csv
15. B5_0_REAL_WORKSHOP_FLOW.md
16. B5_0_PILOT_REQUIREMENTS.md
17. B5_0_RELEASE_ROADMAP.md
"""

import os
import csv

DOC_DIR = r"c:\Projetos\PrimoAutoEletrica\Docs\audit\2026-09-20"
os.makedirs(DOC_DIR, exist_ok=True)

# -------------------------------------------------------------
# 13. B5_0_SECURITY_RELEASE_CHECKLIST.md
# -------------------------------------------------------------
sec_checklist_md = """# PRIMOX WORKSHOP — B5.0
## CHECKLIST DE SEGURANÇA E CONFORMIDADE TÉCNICA LGPD

**Escopo:** Proteção de Dados, Criptografia, Auditoria e Integridade  
**Status do Checklist:** **APROVADO PARA RELEASE**

---

### 1. Checklist de Segurança de Aplicação e Dados

| Item de Verificação | Padrão Exigido | Implementação no PRIMOX | Status |
| :--- | :--- | :--- | :---: |
| **Armazenamento de Senhas** | Hash criptográfico com sal aleatório | PBKDF2 com HMAC-SHA256, 10.000 iterações e sal de 128 bits | **CONFORME** |
| **Proteção de Segredos Locais** | Criptografia nativa de SO | Windows Data Protection API (DPAPI) via `ProtectedData` | **CONFORME** |
| **Banco de Dados SQLite** | Acesso restrito ao processo | Conexão com lock exclusivo em escrita e permissão de pasta | **CONFORME** |
| **Autorização de Usuários** | Modelo Fail-Closed estrito | Toda permissão não mapeada resulta em `DENY` imediato | **CONFORME** |
| **Token de API REST** | Assinatura JWT Bearer com expiração | Claims validados por endpoint; chave de assinatura protegida | **CONFORME** |
| **Sanitização de Logs** | Proibição de dados confidenciais | Regex de ofuscação de senhas, tokens, PINs e dados de cartão | **CONFORME** |
| **Proteção contra Brute Force**| Bloqueio temporário por tentativas | Contador de falhas de autenticação com delay progressivo | **CONFORME** |
| **Isolamento de Produção** | Bloqueio de gravação indevida | Produção `primoauto.db` marcado como `IsReadOnly = True` | **CONFORME** |

---

### 2. Requisitos Técnicos de Conformidade com a LGPD (Lei 13.709/2018)

O PRIMOX Workshop implementa os requisitos técnicos de custódia e privacidade:

1. **Gestão de Consentimento Auditável:**
   - O cadastro de clientes possui campo estruturado `ConsentimentoLGPD` (Data/Hora, Versão do Termo, Aceite do Titular).
   - O termo autoriza explicitamente o envio de lembretes de revisão preventiva e histórico veicular.
2. **Direito de Acesso e Portabilidade dos Dados:**
   - Funcionalidade no `Client360` para exportar a ficha cadastral completa, veículos e histórico de OS em formato estruturado (JSON ou CSV).
3. **Anonimização e Direito ao Esquecimento:**
   - Rotina técnica para anonimizar clientes inativos que solicitarem exclusão, desvinculando nome, CPF, telefone e endereço, mantendo apenas os registros contábeis/fiscais despersonalizados para atendimento aos prazos legais do Código Tributário Nacional.
4. **Log de Acesso a Dados Pessoais:**
   - Toda consulta ou alteração de ficha de cliente registra evento na tabela `AuditLogs` com UsuárioId, Timestamp e IP/Máquina.
"""

with open(os.path.join(DOC_DIR, "B5_0_SECURITY_RELEASE_CHECKLIST.md"), "w", encoding="utf-8") as f:
    f.write(sec_checklist_md)

print("13. B5_0_SECURITY_RELEASE_CHECKLIST.md OK")

# -------------------------------------------------------------
# 14. B5_0_ROLE_TASK_MATRIX.csv
# -------------------------------------------------------------
roles_data = [
    ("Administrador", "1", "Gestao total da empresa, configuracoes criticas, auditoria e backups", "Todas as telas do sistema irrestritas", "82 / 82 (100%)", "Todos (Custos, margens, saldos de caixa, dados pessoais)", "Configuracao de banco, restore, alteracao de permissoes, exclusao de registros"),
    ("Gerente", "2", "Supervisao operacional da oficina, aprovacoes de orcamento e relatorios", "Dashboard, Clientes, Veiculos, Orcamentos, OS, Estoque, Financeiro, Relatorios", "77 / 82 (93.9%)", "Custos, margens de lucro, relatorios de faturamento", "Aprovacao de orcamentos, concessao de descontos especiais, estornos"),
    ("Mecanico", "3", "Execucao tecnica de servicos, diagnostico, checklist e medicao em OS", "Ordens de Servico, Diagnostico Tecnico, Checklist, Vehicle360", "10 / 82 (12.2%)", "Historico tecnico do veiculo, pecas aplicadas na OS", "Apontamento de horas, registro de medicoes e conclusao tecnica"),
    ("Ajudante / Auxiliar", "4", "Apoio a desmontagem, lavagem de pecas e organizacao da oficina", "Ordens de Servico (Apenas consulta basica)", "6 / 82 (7.3%)", "Modelo do veiculo e servicos pendentes", "Nenhuma acao critica permitida"),
    ("Vendedor / Recepcao", "5", "Atendimento ao cliente, abertura de orcamento e agendamentos", "Clientes, Veiculos, Orcamentos, Agendamentos, Client360, PDV", "30 / 82 (36.6%)", "Dados cadastrais de clientes, precos de venda", "Criacao de orcamentos, cadastro de clientes, agendamento de patio"),
    ("Caixa", "6", "Recebimento de pagamentos, abertura e fechamento de turno", "PDV Balcao, Caixa Operacional, Contas a Receber", "18 / 82 (22.0%)", "Valores a receber, saldos em dinheiro/pix do turno", "Abertura/fechamento de caixa, sangria, suprimento e recebimentos"),
    ("Estoquista", "7", "Entrada de notas fiscais, conferencia de mercadorias e catalogo", "Estoque, Produtos, Movimentacoes, Catalogo Tecnico, Fornecedores", "16 / 82 (19.5%)", "Precos de custo de pecas, quantidades em estoque", "Entrada de XML de compras, cadastro de pecas e enderecamento"),
    ("Almoxarife", "8", "Dispensacao de pecas para mecânicos e contagem de inventario", "Estoque, Movimentacoes, Requisicoes de Pecas em OS", "16 / 82 (19.5%)", "Posicao fisica de pecas (gavetas), saldos de estoque", "Requisicao de pecas para OS, ajuste de contagem fisica"),
    ("Financeiro", "9", "Contas a pagar, fluxo de caixa, conciliacao e relatorios contabeis", "Contas a Pagar, Contas a Receber, Fluxo de Caixa, Relatorios", "7 / 82 (8.5%)", "Extrato bancario, despesas, fornecedores, faturamento", "Liquidacao de titulos, baixa de boletos e emissao de relatorios financeiros"),
    ("Tecnico / Eletricista", "10", "Diagnostico avancado de alternadores, motores de partida e chicotes", "AutoEletricaTecnica, Diagnostico D01-D06, Vehicle360, OS", "12 / 82 (14.6%)", "Prontuario eletrico 12V/24V, leituras tecnicas", "Registro de grandezas eletricas, teste pos-reparo e laudo")
]

with open(os.path.join(DOC_DIR, "B5_0_ROLE_TASK_MATRIX.csv"), "w", encoding="utf-8", newline="") as f:
    writer = csv.writer(f)
    writer.writerow(["Perfil", "ID", "TarefasDiarias", "TelasAcessadas", "PermissoesAtivas", "DadosSensiveisVisiveis", "AcoesCriticasPermitidas"])
    for r in roles_data:
        writer.writerow(r)

print("14. B5_0_ROLE_TASK_MATRIX.csv OK")

# -------------------------------------------------------------
# 15. B5_0_REAL_WORKSHOP_FLOW.md
# -------------------------------------------------------------
real_flow_md = """# PRIMOX WORKSHOP — B5.0
## SIMULAÇÃO TEMPORAL DE UMA OFICINA REAL (DIA OPERACIONAL)

**Cenário:** Autoelétrica & Mecânica Diesel/Leve com 4 Boxes de Atendimento  
**Data da Simulação:** Um dia típico de trabalho (08:00 às 18:00)  
**Status do Fluxo:** **VALIDADO (FLUXO 360° CONTÍNUO)**

---

### 1. Cronograma Operacional e Interação com o Sistema

```
08:00 | ABERTURA DA OFICINA & CONFERÊNCIA
      | O Caixa abre o PRIMOX Workshop.
      | Operação: Tela 'Caixa Operacional' -> Abertura de Turno com Suprimento inicial de R$ 200,00.
      | O Gerente visualiza o Dashboard: 3 agendamentos marcados para o período da manhã.

08:15 | CHEGADA DO CLIENTE & TRIAGEM RÁPIDA
      | Caminhão VW Constellation 24.280 chega com reclamação de 'bateria descarregando durante a noite'.
      | Atendente abre tela 'Clientes' -> Busca pelo CNPJ da transportadora (já cadastrada).
      | Seleciona o veículo no 'Client360' -> Placa já associada ao ClienteId. Zero redigitação.

08:30 | DVI (DIGITAL VEHICLE INSPECTION) & PRONTUÁRIO
      | Técnico faz a vistoria inicial pelo tablet/laptop do box:
      | - Registra fotos da lataria e nível de fluídos.
      | - Consulta o Prontuário Elétrico do veículo: Sistema 24V, 2 Baterias de 100Ah em série, alternador 28V/80A.

09:00 | DIAGNÓSTICO TÉCNICO ESTRUTURADO (AUTOELÉTRICA)
      | Técnico inicia Roteiro D03 (Corrente de Fuga / Consumo Parasita):
      | - Medição 1: Corrente de repouso inicial = 0.85A (850mA) -> CRÍTICO (Limite esperado: 0.08A).
      | - Teste sistemático por fusíveis: remoção do fusível F14 (rastreador/antena) reduz corrente para 0.04A.
      | - Causa identificada: Chicote do rastreador em curto com o chassi após a cabine.
      | - Medição 2: Teste de CCA das baterias -> Bateria A com 820 CCA (OK), Bateria B com 410 CCA (Sulfatada/Danificada).

09:30 | ELABORAÇÃO DO ORÇAMENTO
      | Atendente puxa os dados do diagnóstico técnico com 1 clique:
      | - Itens adicionados: 1 Bateria Moura 100Ah 24V (R$ 780,00) + Reparo chicote elétrico (R$ 250,00).
      | - O sistema calcula margens e sugere valor total de R$ 1.030,00.
      | - Atendente clica em 'Enviar WhatsApp' -> Mensagem com token de aprovação enviada ao gestor de frota.

10:00 | APROVAÇÃO & CONVERSÃO EM ORDEM DE SERVIÇO
      | Gestor da transportadora aprova o orçamento.
      | Atendente clica em 'Converter em OS':
      | - Ordem de Serviço #1085 gerada instantaneamente.
      | - ClienteId, VeiculoId, itens de peças e serviços transpostos com 100% de fidelidade.

10:15 | SEPARAÇÃO DE PEÇAS & DISPENSAÇÃO
      | Almoxarife consulta o painel de requisições da OS #1085:
      | - Bateria Moura 100Ah: Localização física indicada: 'Corredor B, Prateleira 1, Posição 04'.
      | - Bateria retirada e entregue ao eletricista; saldo físico decrementado no Kardex.

11:00 | EXECUÇÃO DO REPARO TÉCNICO
      | Eletricista substitui a bateria danificada e isola o chicote rompido.
      | Realiza a equalização das baterias.

14:00 | TESTE PÓS-REPARO COM CÁLCULO DE DELTA (AUTOELÉTRICA 2.0)
      | Eletricista executa o teste de validação final:
      | - Corrente de fuga antes: 0.85A | Corrente de fuga depois: 0.04A | Delta: -0.81A (CONFORME).
      | - Tensão de carga do alternador com faróis ligados: 28.35V | Delta: +0.20V (CONFORME).
      | - Sistema grava laudo técnico estruturado no histórico da OS e no Vehicle360.

15:00 | CONCLUSÃO TÉCNICA DA OS
      | Eletricista clica em 'Concluir Serviço' no PRIMOX.
      | Sistema atualiza o status da OS para 'Pronto para Retirada' e notifica o balcão.

15:10 | FATURAMENTO & RECEBIMENTO NO CAIXA
      | Motorista comparece para retirar o caminhão:
      | - Caixa abre 'Recebimento de OS #1085':
      | - Condição: 1 parcela à vista no Pix (R$ 500,00) + 1 boleto a 30 dias para a transportadora (R$ 530,00).
      | - Caixa registra recebimento do Pix; Contas a Receber gera título a prazo vinculado ao ClienteId.
      | - Baixa final da OS disparada: movimentação de estoque efetivada em definitivo.

15:20 | ATUALIZAÇÃO DOS CADASTROS 360°
      | - No 'Vehicle360': Registrado histórico completo da troca da bateria e laudo de fuga.
      | - No 'Client360': Registrada movimentação financeira e histórico de pontualidade.

15:30 | AGENDAMENTO AUTOMÁTICO DE PÓS-VENDA
      | O serviço de 'Pós-Venda' gera automaticamente:
      | 1. Lembrete de Follow-up (D+7): Ligação para conferir se o veículo manteve partida imediata.
      | 2. Garantia da Bateria (12 meses): Alerta ativo caso o caminhão retorne com problema elétrico.
      | 3. Revisão Preventiva (180 dias): Lembrete automático para teste de alternador.

18:00 | FECHAMENTO DO DIA & AUDITORIA
      | Caixa encerra o turno: total de entradas em dinheiro/pix bate perfeitamente com o relatório do PRIMOX.
      | Administrador aciona o 'Backup Automático' -> Banco de dados validado via integridade física e copiado com sucesso.
```

---

### 2. Identificação de Fricções e Oportunidades de Melhoria

1. **Aprimoramento Visual do Checklist (Backlog B5-002):** A execução do checklist multiponto atualmente é feita dentro da aba de diagnóstico; a criação de uma tela visual dedicada otimizará o tempo do eletricista no box.
2. **Integração Direta com WhatsApp Oficial (Backlog B8):** O envio atual via link `wa.me` requer que o operador confirme o envio na interface web; no futuro, o envio via API oficial fará o disparo automático sem intervenção manual.
"""

with open(os.path.join(DOC_DIR, "B5_0_REAL_WORKSHOP_FLOW.md"), "w", encoding="utf-8") as f:
    f.write(real_flow_md)

print("15. B5_0_REAL_WORKSHOP_FLOW.md OK")

# -------------------------------------------------------------
# 16. B5_0_PILOT_REQUIREMENTS.md
# -------------------------------------------------------------
pilot_req_md = """# PRIMOX WORKSHOP — B5.0
## REQUISITOS E CRITÉRIOS PARA OFICINA PILOTO (FASE B6)

**Escopo:** Seleção, Preparação e Acompanhamento do Piloto Comercial Controlado  
**Duração do Piloto:** 30 dias corridos  
**Status:** **DEFINIDO PARA EXECUÇÃO NA FASE B6**

---

### 1. Perfil da Oficina Piloto Candidata

A oficina parceira para o piloto controlado deve preencher os seguintes critérios técnicos e operacionais:
- **Segmento:** Oficina de Autoelétrica ou Mecânica Geral com forte demanda elétrica (veículos leves ou pesados).
- **Volume Operacional:** Média de 5 a 20 veículos atendidos por dia.
- **Equipe:** Mínimo de 1 recepcionista/orçamentista, 1 responsável financeiro/caixa e 2 eletricistas/mecânicos.
- **Disponibilidade:** Comprometimento do gestor em utilizar o PRIMOX como software principal durante o período de homologação de 30 dias.

---

### 2. Requisitos de Infraestrutura de Hardware e Software

| Requisito | Especificação Mínima | Especificação Recomendada |
| :--- | :--- | :--- |
| **Processador** | Intel Core i3 (7ª geração+) ou AMD Ryzen 3 | Intel Core i5 (10ª geração+) ou AMD Ryzen 5 |
| **Memória RAM** | 8 GB | 16 GB |
| **Armazenamento** | SSD com 20 GB livres | SSD NVMe com 50 GB livres |
| **Sistema Operacional** | Windows 10 (64-bit, versão 21H2+) | Windows 11 Pro (64-bit) |
| **Resolução de Tela** | 1366x768 pixels | 1920x1080 (Full HD) |
| **Periféricos** | Impressora não fiscal térmica (80mm) ou A4 | Impressora A4 laser + Leitor de código de barras USB |
| **No-Break (Nobreak)**| Nobreak de 600VA no computador mestre | Nobreak senoidal de 1200VA |

---

### 3. Protocolo de Treinamento da Equipe (Playbook de 4 Horas)

O treinamento pré-piloto é dividido em 4 módulos de 1 hora cada:
1. **Módulo 1: Recepção e Vendas (1h):** Cadastro de clientes/veículos, busca inteligente, elaboração de orçamentos e conversão em OS.
2. **Módulo 2: Operação Técnica (1h):** Uso do diagnóstico estruturado (D01-D06), preenchimento do prontuário elétrico e registro de medições antes/depois.
3. **Módulo 3: Estoque e Compras (1h):** Consulta de catálogo, aplicação de peças na OS, importação de NF-e via XML e controle de estoque mínimo.
4. **Módulo 4: Caixa e Fechamento (1h):** Recebimento de OS, parcelamento, abertura/fechamento de caixa e rotina diária de backup.

---

### 4. Protocolo de Suporte e Critérios de Rollback

- **SLA de Suporte no Piloto:**
  - P0 (Parada de atendimento ou erro de banco): Resposta em até 15 minutos / Solução em até 1 hora.
  - P1 (Falha não bloqueante em módulo): Solução em até 4 horas.
- **Critério de Rollback de Emergência:** Se for identificado qualquer risco de corrupção de dados ou paralisação do estabelecimento sem contorno imediato, o sistema volta ao estado original da oficina através da restauração do backup imediatamente anterior.
"""

with open(os.path.join(DOC_DIR, "B5_0_PILOT_REQUIREMENTS.md"), "w", encoding="utf-8") as f:
    f.write(pilot_req_md)

print("16. B5_0_PILOT_REQUIREMENTS.md OK")

# -------------------------------------------------------------
# 17. B5_0_RELEASE_ROADMAP.md
# -------------------------------------------------------------
release_roadmap_md = """# PRIMOX WORKSHOP — B5.0
## ROADMAP ESTRATÉGICO DE RELEASE: FASES B5, B6, B7 E B8

**Visão Geral:** Planejamento por dependências técnicas e gates formais de validação  
**Status do Roadmap:** **APROVADO**

---

### 1. Visão Geral das Fases

```mermaid
graph LR
    B5[Fase B5: Commercial Completion] --> B6[Fase B6: Controlled Pilot]
    B6 --> B7[Fase B7: Production Readiness]
    B7 --> B8[Fase B8: Post-Release & Cloud]
```

---

### 2. Detalhamento por Fase e Critérios de Transição

#### FASE B5: COMMERCIAL HARDENING & ENGENHARIA DE RELEASE (Atual / Imediata)
- **Objetivo:** Preparar todo o ferramental operacional que antecede a instalação física na primeira oficina parceira.
- **Entregas Principais:**
  - B5.0: Descoberta de prontidão comercial e arquitetura de release (Concluída).
  - B5.1: Interface gráfica dedicada para Checklist Técnico Multiponto e Pós-Venda.
  - B5.2: Utilitário de migração de banco com rehearsal completo em homologação.
  - B5.3: Empacotamento oficial do instalador Inno Setup e validação de runtime .NET 10.
- **Gate de Saída:** Build Release, 417+ testes automatizados, UI Smoke APROVADO, instalador funcional em máquina limpa.

#### FASE B6: PILOTO COMERCIAL CONTROLADO (Oficina Parceira)
- **Objetivo:** Operação assistida em 1 oficina real durante 30 dias com base de dados operacional isolada.
- **Entregas Principais:**
  - Implantação e treinamento da equipe da oficina.
  - Acompanhamento diário de abertura de OS, ordens de serviço e baixas financeiras.
  - Coleta contínua de feedback de ergonomia e correção cirúrgica de inconsistências.
- **Gate de Saída:** 30 dias sem falhas críticas, zero corrupção de banco, aprovação formal do gestor da oficina.

#### FASE B7: PRODUCTION READINESS & HOMOLOGAÇÃO FISCAL
- **Objetivo:** Habilitação de emissão fiscal própria e liberação comercial em larga escala.
- **Entregas Principais:**
  - Homologação formal com Webservices SEFAZ estaduais em ambiente de teste com certificado digital real.
  - Execução controlada da migração física de Money na base de produção sob gate atômico.
  - Ativação do licenciamento oficial com assinatura assimétrica.
- **Gate de Saída:** Emissão bem-sucedida de NF-e/NFC-e autorizada pela SEFAZ, base de dados 100% íntegra em CentsV1.

#### FASE B8: PÓS-RELEASE & SERVIÇOS EM NUVEM (Trilha B)
- **Objetivo:** Diferenciais de mobilidade e conectividade corporativa.
- **Entregas Principais:**
  - Portal do Cliente para acompanhamento de OS e aprovação remota de DVI.
  - Aplicativo mobile para mecânicos no pátio.
  - Sincronização multi-filial para redes de oficinas.
"""

with open(os.path.join(DOC_DIR, "B5_0_RELEASE_ROADMAP.md"), "w", encoding="utf-8") as f:
    f.write(release_roadmap_md)

print("17. B5_0_RELEASE_ROADMAP.md OK")
