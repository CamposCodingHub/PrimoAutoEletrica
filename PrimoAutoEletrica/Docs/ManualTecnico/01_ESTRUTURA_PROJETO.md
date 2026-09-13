# Estrutura do Projeto

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

## Solucao

- `PrimoAutoEletrica.sln`: solucao principal.
- `PrimoAutoEletrica/PrimoAutoEletrica.csproj`: aplicacao WPF em .NET 9 para Windows.
- `Scripts/`: automacoes de validacao, smoke test, pacote, instalacao simulada e atualizacao.
- `Docs/`: documentos tecnicos gerais do repositorio.
- `PrimoAutoEletrica/Docs/`: documentos embarcados ou diretamente ligados ao produto.
- `TestResults/`: evidencias de execucoes automatizadas.

## Camadas principais

- `Models`: entidades de negocio usadas pela oficina.
- `Data/Repositories`: persistencia orientada por repositorios.
- `Services`: regras de negocio, seguranca, PDF, backup, logs, smoke tests e integracoes.
- `Views`: janelas WPF.
- `UserControls`: telas modulares usadas na navegacao principal.
- `ViewModels`: estado e comandos para telas com padrao MVVM.
- `Themes`: recursos visuais, tema e densidade.

## Convencoes de manutencao

- Servicos de negocio ficam em `Services`.
- Acesso direto a banco deve ficar concentrado em repositorios ou servicos de persistencia ja existentes.
- Novas telas devem entrar no smoke test quando forem relevantes para uso real.
- Qualquer recurso que afete seguranca deve passar por permissao e auditoria.
