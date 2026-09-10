# PRIMOX-I18N-05-GLOSSARY — Terminologia oficial (conteúdo)

**Fonte de verdade:** catálogo existente (`LocalizationService` + Modules + Interaction + Content)  
**Regra:** se o catálogo já consolida um termo, **não** substituir por preferência pessoal.

Português = idioma de referência do produto.  
`CurrentCulture` de negócio permanece **pt-BR**.

---

## Módulos (UI)

| PT | EN | ES | Chave típica |
|----|----|----|--------------|
| Clientes | Clients | Clientes | `Clients` |
| Veículos | Vehicles | Vehículos | `Vehicles` / `VehiclesLabel` |
| Ordem de Serviço | Work Order | Orden de servicio | `WorkOrder` |
| Orçamentos | Quotes | Presupuestos | `QuotesLabel` |
| Ponto de Venda / PDV | Point of Sale / PDV | Punto de venta / PDV | `Pdv*` / PDV brand |
| Estoque | Inventory | Inventario | `Inventory` |
| Financeiro | Finance | Finanzas | `FinanceTitle` |
| Fornecedores | Suppliers | Proveedores | `Suppliers` / `Supplier` |
| Funcionários | Employees | Empleados | `Employees` |
| Agenda | Schedule | Agenda | `Agenda*` |
| Relatórios | Reports | Informes | `Reports*` |
| Catálogo | Catalog | Catálogo | `OperationalCatalog` |
| Configurações | Settings | Configuración | `Settings` / `SettingsTitle` |
| Ajuda | Help | Ayuda | Help* |

## Ações comuns

| PT | EN | ES | Chave |
|----|----|----|-------|
| Salvar | Save | Guardar | `Save` |
| Cancelar | Cancel | Cancelar | `Cancel` |
| Excluir | Delete | Eliminar | `Delete` |
| Editar | Edit | Editar | `Edit` |
| Pesquisar / Buscar | Search | Buscar | `Search` |
| Confirmar | Confirm | Confirmar | `Confirm` |
| Fechar | Close | Cerrar | `Close` |
| Finalizar | Finish | Finalizar | `Finish` |
| Aprovar | Approve | Aprobar | `Approve` |
| Rejeitar | Reject | Rechazar | `Reject` |

## Exceções / não traduzir

- Brand: **PRIMOX**
- Fiscal: CPF, CNPJ, NCM, CFOP, CST, CSOSN, NF-e, IBS, CBS
- Dados cadastrados, SKU, códigos internos de navegação/audit
- PIX permanece **PIX**

## Consistência

- EN: preferir **Client(s)** (já no catálogo), não misturar com Customer sem justificativa.
- EN: **Work Order** / **Inventory** alinhados ao catálogo.
- ES: **Eliminar** para Delete; **Guardar** para Save.
