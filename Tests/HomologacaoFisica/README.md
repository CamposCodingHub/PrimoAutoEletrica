# Homologacao fisica e comercial

Esta pasta concentra a suite de homologacao pedida para validar o PrimoAutoEletrica em ambiente real.

Arquivos principais:

- `Run-HomologacaoFisica.ps1`: executa a suite completa e gera o relatorio master.
- `HomologacaoFisica.Config.sample.psd1`: modelo de configuracao para rede, SQL Server, impressora e paths.
- `Invoke-Homologacao*.ps1`: scripts por area.

Saidas esperadas:

- `TestResults/HomologacaoFisica/<timestamp>/...`
- `TestResults/HomologacaoFisica/RELATORIO_FINAL_HOMOLOGACAO.md`

Exemplo:

```powershell
pwsh .\Tests\HomologacaoFisica\Run-HomologacaoFisica.ps1 `
  -ConfigPath .\Tests\HomologacaoFisica\HomologacaoFisica.Config.sample.psd1
```

Observacoes:

- A suite nao mascara ausencia de hardware ou de segunda maquina. Nesses casos ela marca a area como ignorada com o motivo correto.
- Parte da validacao reutiliza `dotnet test`, `--smoke-test` e `--workflow-test` do proprio projeto.
- A validacao visual salva screenshots apenas quando a resolucao alvo esta fisicamente ativa na estacao em teste.
