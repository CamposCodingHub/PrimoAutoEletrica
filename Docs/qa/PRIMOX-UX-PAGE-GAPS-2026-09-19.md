# Primox UX page gaps — 2026-09-19

## Referencias de mercado
ALLDATA / Mitchell1 / RO Writer / Orderry: workflow OS, estoque com transferencia de local, exportacoes, catalogo→pedido, cancelamento de agenda.

## Auditoria por pagina (resumo)
| Pagina | Ja tinha | Gap tratado agora | Adiado |
|--------|----------|-------------------|--------|
| Dashboard | Atalhos | — | Widgets KPI avancados |
| Agendamentos | Check-in, OS, Export PDF, Cancelar no detalhe | **Cancelar na toolbar** | Excluir fisico |
| Orcamentos | PDF, WhatsApp, excluir | — | Aprovacao digital polish |
| OS | Print, financeiro, excluir | — | Transferencia entre tecnicos UI |
| Kanban | Abrir OS | — | Drag+filtro avancado |
| PDV | Venda/cancel | — | Multi-caixa |
| Importar NF-e | Import/desfazer | — | — |
| Fiscal | Consultar/cancelar | — | — |
| Clientes | Export, 360, WhatsApp, excluir | — | Import CSV clientes |
| Veiculos | Export, excluir, agendar | — | — |
| Auto Eletrica | Laudo, WhatsApp | — | Export PDF dedicado botao |
| Estoque | Entrada/Saida/Inventario/360/Excluir | **Exportar CSV**, **Transferir localizacao** | Transferencia multi-loja |
| Catalogo | Import/Export/360/criar produto | **Ignorar**, **Excluir** toolbar | Conversao em lote |
| Fornecedores | Novo/Editar/Excluir | **Exportar CSV** | Pedido compra 360 |
| Funcionarios | 2FA, perfis | — | Export folha |
| Financeiro | Export, comissoes | — | Remessa bancaria |
| Relatorios | Export | — | — |

## Implementado nesta rodada
1. Estoque: Exportar CSV filtrado + Transferir (Localizacao/Prateleira/Gaveta) via `TransferirEstoqueWindow`
2. Fornecedores: Exportar CSV
3. Catalogo: Ignorar + Excluir (bloqueia se vinculado ao estoque)
4. Agendamentos: Cancelar visivel na toolbar superior
