# Manual de Operação — Multi-Filiais Corporativo & Transferências Inter-Lojas

**Módulo:** Multi-Filiais & Transferências ACID  
**Versão:** 2.1 Enterprise  
**Objetivo:** Gerenciar múltiplas unidades da oficina com consolidação financeira e transferências seguras de estoque.

---

## 1. Visão Geral

O PRIMOX Workshop Enterprise permite que uma rede de auto elétricas opere de forma integrada, com Matriz e Filiais compartilhando dados cadastrais e permitindo transferências de peças com total integridade transacional (garantia ACID).

---

## 2. Cadastro e Gestão de Filiais

1. Acesse `Configurações → Multi-Filiais`;
2. Cadastre a **Matriz** e cada uma das **Filiais**:
   * Razão Social e Nome Fantasia da Unidade;
   * CNPJ específico da filial;
   * Inscrição Estadual e Municipal;
   * Endereço completo e telefone;
3. Defina os usuários autorizados a operar em cada unidade.

---

## 3. Troca de Unidade de Trabalho

1. No cabeçalho superior do PRIMOX, localize o seletor de filial;
2. Selecione a filial desejada;
3. O sistema atualiza instantaneamente a visualização para o estoque, ordens de serviço e caixa da unidade selecionada;
4. Administradores e gerentes gerais podem visualizar o consolidado da rede inteira no Dashboard e nos Relatórios.

---

## 4. Transferência de Estoque Inter-Lojas (Passo a Passo)

### Etapa 1: Solicitação e Despacho (Filial Origem)
1. Acesse `Transferências → Nova Transferência`;
2. Selecione a **Filial de Origem** e a **Filial de Destino**;
3. Adicione os itens e quantidades a serem transferidos;
4. Clique em **"Despachar Transferência"**;
5. O sistema debita imediatamente as peças do saldo disponível da filial de origem e as coloca em status **"Em Trânsito"** (bloqueado para venda local).

### Etapa 2: Recebimento Físico (Filial Destino)
1. Ao receber a mercadoria na filial receptora, acesse `Transferências → Transferências Pendentes`;
2. Localize o romaneio correspondente;
3. Realize a conferência física das quantidades recebidas;
4. Clique em **"Confirmar Recebimento"**;
5. O sistema credita o estoque da filial de destino e finaliza a transação com consistência atômica.
