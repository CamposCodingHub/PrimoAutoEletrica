# PRIMOX-I18N-TRANSLATION-GLOSSARY

**Missão:** PRIMOX-I18N-04 — glossário oficial de termos UI  
**HEAD baseline:** `6036a40`  
**Referência de negócio:** **pt-BR** (cultura de negócio / formatação permanece pt-BR)  
**UI languages:** pt-BR · en-US · es-ES

---

## Regras

1. **pt-BR** is the source of truth for workshop meaning.
2. Prefer terms already used in localization catalogs (e.g. **Clients**, not Customers) when a key exists.
3. **Do not invent English replacements** for Brazilian legal/fiscal identifiers: CPF, CNPJ, NCM, CFOP, CST, CSOSN, NF-e (and related SEFAZ acronyms) stay as written in all languages.
4. Money, tax bases, and document numbers follow Brazilian business culture even when UI chrome is en-US or es-ES.
5. Product brand **PRIMOX** is never translated.

---

## Module / navigation terms

| Concept (EN note) | pt-BR | en-US | es-ES | Catalog / note |
|-------------------|-------|-------|-------|----------------|
| Dashboard / Panel | Painel / Dashboard | Dashboard | Panel / Dashboard | Prefer existing `OperationsCenter` / dashboard keys where bound |
| Clients / Customers | Clientes | Clients | Clientes | Use **Clients** to match catalog key `Clients` (not Customers) |
| Vehicles | Veículos | Vehicles | Vehículos | |
| Work Orders | Ordens de serviço (OS) | Work Orders | Órdenes de servicio | Internal nav id `OrdensServico` |
| Quotes | Orçamentos | Quotes | Presupuestos | |
| Inventory | Estoque | Inventory | Inventario | Catalog often `InventoryTitle` |
| Point of Sale (PDV) | PDV | Point of Sale (PDV) | Punto de venta (PDV) | Keep **PDV** acronym visible |
| Financial | Financeiro | Financial | Financiero | |
| Suppliers | Fornecedores | Suppliers | Proveedores | |
| Employees | Funcionários | Employees | Empleados | |
| Schedule / Appointments | Agenda / Agendamentos | Schedule / Appointments | Agenda / Citas | Catalog often `AppointmentsTitle` |
| Reports | Relatórios | Reports | Informes | |
| Parts Catalog | Catálogo de peças | Parts Catalog | Catálogo de piezas | |
| Settings | Configurações | Settings | Configuración | |
| Help | Ajuda | Help | Ayuda | Long help **body** may remain pt-BR |

---

## Common actions

| Concept | pt-BR | en-US | es-ES |
|---------|-------|-------|-------|
| Save | Salvar | Save | Guardar |
| Cancel | Cancelar | Cancel | Cancelar |
| Delete | Excluir | Delete | Eliminar |
| Edit | Editar | Edit | Editar |
| Search | Buscar / Pesquisar | Search | Buscar |
| Confirm | Confirmar | Confirm | Confirmar |
| Close | Fechar | Close | Cerrar |
| New | Novo / Nova | New | Nuevo / Nueva |
| Refresh | Atualizar | Refresh | Actualizar |
| Export | Exportar | Export | Exportar |
| Print | Imprimir | Print | Imprimir |
| Approve | Aprovar | Approve | Aprobar |
| Reject | Rejeitar | Reject | Rechazar |
| Save draft | Salvar rascunho | Save draft | Guardar borrador |

---

## States / empty

| Concept | pt-BR | en-US | es-ES |
|---------|-------|-------|-------|
| Loading | Carregando… | Loading… | Cargando… |
| No records | Nenhum registro | No records | Sin registros |

---

## Fiscal / legal identifiers (do not translate)

| Term | All UI languages | Rationale |
|------|------------------|-----------|
| CPF | CPF | Brazilian individual tax id |
| CNPJ | CNPJ | Brazilian company tax id |
| NCM | NCM | Mercosur customs nomenclature |
| CFOP | CFOP | Brazilian fiscal operation code |
| CST | CST | ICMS tax situation code |
| CSOSN | CSOSN | Simples Nacional situation code |
| NF-e | NF-e | Nota Fiscal eletrônica (legal document type) |

Labels around these fields may localize (e.g. "CNPJ do emitente" → "Issuer CNPJ"), but the **identifier tokens themselves** stay unchanged.

---

## Usage in I18N-04

- Auditors compare live UI against this glossary when scoring `PRIMOX-I18N-04-UX-MATRIX.md`.
- Deviations that still match an existing catalog key are **PARTIAL**, not FAIL, if meaning is clear.
- Invented synonyms that conflict with catalog keys (e.g. Customers vs Clients) should be flagged for localization cleanup — not product-code changes in this docs-only phase.
