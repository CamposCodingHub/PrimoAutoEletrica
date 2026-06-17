# NF-e, Backup, Restauracao e Permissoes

## Como importar NF-e

1. Acesse `Importar NF-e`.
2. Selecione o arquivo XML da nota.
3. Revise fornecedor, produtos, codigos, custos e margens.
4. Vincule produtos existentes quando necessario.
5. Confirme a importacao.
6. Confira se estoque e custos foram atualizados.

Cuidados:

- Revise unidade e quantidade antes de confirmar.
- Verifique duplicidade de produto.
- Use rollback ou cancele a operacao se houver inconsistencias.

## Como fazer backup

1. Acesse `Configuracoes` ou o menu de backup.
2. Escolha backup manual.
3. Aguarde a conclusao.
4. Confirme o arquivo gerado na pasta configurada.
5. Copie o backup para local externo quando possivel.

Boas praticas:

- Fazer backup diario em dia de uso real.
- Fazer backup antes de atualizar o sistema.
- Manter copia fora da maquina principal.

## Como restaurar backup

1. Feche telas de venda, OS e cadastro.
2. Acesse a rotina de restauracao com perfil autorizado.
3. Selecione o arquivo de backup.
4. Confirme que entende que os dados atuais serao substituidos.
5. Aguarde a restauracao.
6. Abra o sistema e valide login, clientes, veiculos, estoque e financeiro.

Cuidados:

- Nunca restaure backup sem saber de qual data ele veio.
- Salve um backup do estado atual antes da restauracao.
- Registre quem executou a restauracao.

## Como configurar permissoes

1. Acesse `Funcionarios` ou `Configuracoes`, conforme a tela de perfis.
2. Escolha o funcionario ou perfil.
3. Ative apenas as permissoes necessarias para a funcao.
4. Restrinja configuracoes, exclusoes, descontos, caixa e restauracao para perfis administrativos.
5. Salve e teste com o usuario alterado.

Perfis sugeridos:

- Administrador: acesso completo e responsabilidade por backup, restauracao e configuracoes.
- Caixa: PDV, recebimentos, fechamento e consultas necessarias.
- Estoque: produtos, fornecedores, entrada por NF-e e inventario.
- Mecanico: veiculos, OS, checklist, fotos e diagnostico.
- Atendimento: clientes, veiculos, orcamentos e agenda.
