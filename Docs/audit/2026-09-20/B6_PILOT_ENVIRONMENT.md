# PRIMOX WORKSHOP — B6 PILOT ENVIRONMENT SPECIFICATION
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Hardware da Oficina Piloto
- **Estação de Operação:** Terminal Integrado Balcão / Gestão
- **CPU:** Intel(R) Xeon(R) CPU E5-2697A v4 @ 2.60GHz (16 Cores físicos, 32 Threads lógicas)
- **Memória RAM:** 32 GB DDR4 ECC
- **Armazenamento:** Disco C: SSD NVMe 512 GB (138 GB livres dedicados à aplicação e backups)
- **Monitores & Resolução:** Full HD (1920x1080), 60Hz, Escala DPI padrão 100%
- **Impressora de Balcão:** Impressora A4 / Spooler Windows para orçamentos e relatórios
- **Leitor de Código de Barras:** Scanner USB HID para entrada de peças e consulta de estoque
- **Rede Local:** Ethernet Gigabit LAN 1 Gbps cabeada, IP estático interno
- **Conectividade Internet:** Banda larga estável para atualizações e verificação de licença local

---

## 2. Software & Runtime
- **Sistema Operacional:** Microsoft Windows 11 Pro 64-bit (Build 26200)
- **Runtime .NET:** .NET 10.0 (TFM `net10.0-windows10.0.19041.0` / SDK 10.0.302)
- **Engine de Banco de Dados:** SQLite 3.46+ (via Microsoft.Data.Sqlite e SQLitePCLRaw.bundle_e_sqlite3)
- **Antivírus & Segurança:** Microsoft Windows Defender ativo com exceção de performance no diretório de dados `%LOCALAPPDATA%\PrimoAutoEletrica`
- **Permissões de Execução:** Usuário padrão com elevação sob demanda (UAC compatível)

---

## 3. Modelo Operacional da Oficina
- **Perfil do Estabelecimento:** Auto Elétrica e Mecânica Geral Especializada (Linha Leve 12V e Linha Pesada 24V)
- **Volume Diário:** 5 a 20 veículos atendidos/dia
- **Duração do Piloto:** 30 dias contínuos de operação
- **Turno:** Segunda a Sexta, das 08:00 às 18:00; Sábados das 08:00 às 12:00
- **Equipe Operacional Mapeada (5 Papéis Reais):**
  1. **Administrador / Gestor Geral:** Douglas Ciro de Campos (`admin@primoauto.com`) — Acesso pleno, DRE, precificação e auditoria.
  2. **Recepção / Atendimento:** Atendente de Balcão — Abertura de orçamentos, cadastro de clientes/veículos e agendamento.
  3. **Técnico Linha Leve (12V):** Mecânico / Eletricista de Automóveis — Execução de OS, checklist e diagnóstico D01 a D06.
  4. **Técnico Linha Pesada (24V):** Eletricista de Caminhões e Ônibus — Diagnósticos especializados D01 a D06 24V.
  5. **Financeiro / Caixa:** Operador Financeiro — Fechamento de caixa, recebimentos, contas a pagar e controle de cheques/cartões.
