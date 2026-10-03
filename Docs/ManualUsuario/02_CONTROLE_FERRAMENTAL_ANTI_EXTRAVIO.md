# PRIMOX Workshop 2.0 — Manual do Usuário
## 02. Controle e Gestão de Ferramental Especializado (Anti-Extravio)

---

### 1. Objetivo do Módulo

O módulo de Ferramentaria foi desenvolvido para eliminar perdas de instrumentos caros (scanners, osciloscópios, alicates amperímetros, torquímetros, kits de ponto e chaves especiais), além de impedir acidentes graves onde ferramentas sejam esquecidas dentro do cofre do motor ou painel do veículo ao liberar a Ordem de Serviço.

---

### 2. Acesso ao Módulo

1. Na barra lateral esquerda do ERP, clique no menu **Ferramentas** (ou pressione a tecla de atalho associada).
2. O painel principal se divide em:
   - **Indicadores Rápidos (KPIs):** Total de Itens, Disponíveis, Emprestadas, Em Manutenção/Calibração e Itens em Atraso.
   - **Abas de Visualização:**
     - **Visual Armários / Gavetas:** Cards agrupados por armário físico e gaveta correspondente.
     - **Tabela Detalhada:** Lista corrida com filtros avançados por status, categoria e busca rápida.

---

### 3. Cadastro de Nova Ferramenta ou Instrumento

Para cadastrar um novo ativo:
1. Clique no botão **Nova Ferramenta** no topo da tela.
2. Preencha os campos da janela modal:
   - **Nome do Item:** Identificação clara (ex: *Osciloscópio Hantek 2D82Auto*, *Torquímetro de Estalo 1/2"*).
   - **Código de Barras / QR Code:** Bipe a etiqueta colada na ferramenta ou clique em *Gerar Código Aleatório*.
   - **Número de Série:** Número do fabricante (essencial para instrumentos de calibração).
   - **Categoria:** Instrumento de Medição, Scanner/Diagnóstico, Chave Especial, Pneumática, Elétrica ou Bancada.
   - **Localização Física:** Armário (ex: *Armário A*), Gaveta/Prateleira (ex: *Gaveta 02*) e Posição.
   - **Data da Próxima Calibração:** Data de vencimento do certificado (obrigatório para torquímetros e multímetros aferidos).
   - **Valor Estimado (R$):** Valor para ressarcimento em caso de dano ou extravio culposo.
3. Clique em **Salvar Ferramenta**.

---

### 4. Fluxo de Empréstimo e Devolução Rápida (< 4 Segundos)

Para manter a oficina ágil, o processo de checkout/check-in foi desenhado para leitura direta por leitor de código de barras:

#### A. Realizando um Empréstimo (Checkout)
1. Clique no botão **Empréstimo / Devolução Rápida** ou bipe o código diretamente na tela.
2. Na janela de bipagem:
   - **Bipe o Código da Ferramenta:** O sistema localiza o item imediatamente e valida sua disponibilidade.
   - **Selecione o Mecânico / Eletricista:** Escolha quem está retirando a ferramenta.
   - **Vincule à Ordem de Serviço (Recomendado):** Informe o número da OS onde a ferramenta será utilizada (ex: *OS-2026-0042*).
   - **Data Limite Prevista:** O sistema preenche automaticamente com o fim do expediente, permitindo ajuste se for serviço de múltiplos dias.
3. Clique em **Confirmar Empréstimo** (ou tecle *Enter*).
4. O status da ferramenta muda instantaneamente para **Emprestada** e fica destacada em âmbar.

#### B. Realizando uma Devolução (Check-in)
1. Na mesma janela de bipagem, aponte o leitor e bipe o código da ferramenta que está retornando.
2. O sistema detecta automaticamente que a ferramenta está emprestada e alterna a tela para o modo de devolução:
   - Exibe o técnico responsável e o tempo total decorrido do empréstimo.
   - Permite registrar o estado de conservação (*OK / Perfeito*, *Avariada*, *Necessita Limpeza/Calibração*).
   - Campo para observações técnicas.
3. Clique em **Confirmar Devolução**.
4. O item retorna imediatamente ao status **Disponível** no armário original.

---

### 5. Trava de Segurança na Conclusão de Ordem de Serviço (Safety Gate)

> [!IMPORTANT]
> **Checklist Anti-Extravio Ativo:**
> Ao tentar finalizar ou entregar uma Ordem de Serviço (`Concluída` ou `Entregue`), o sistema verifica se ainda existe alguma ferramenta vinculada àquela OS que não foi devolvida à ferramentaria.

- Caso existam pendências, o sistema **bloqueia a finalização** e exibe um alerta modal:
  > *"ATENÇÃO: Existem 2 ferramentas em posse do técnico João Silva vinculadas a esta OS (ex: Osciloscópio Hantek, Chave Torx T25). Devolva as ferramentas na ferramentaria antes de liberar o veículo para o cliente."*
- Isso garante que nenhuma ferramenta cara fique esquecida dentro do veículo do cliente.

---

### 6. Alertas Automáticos de Atraso e Auditoria

1. **Notificação no Shell do ERP:** Se uma ferramenta ultrapassar a data limite de devolução, o PRIMOX exibe uma tarja vermelha de alerta no topo do sistema.
2. **Histórico Completo de Movimentações:**
   - Selecione qualquer ferramenta e clique em **Histórico / Auditoria**.
   - O sistema exibe toda a linha do tempo do ativo: quem retirou, em qual data/hora, qual OS atendeu, em qual condição devolveu e eventuais manutenções registradas.
