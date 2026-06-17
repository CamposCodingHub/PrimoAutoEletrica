# Instalacao e Primeiro Acesso

## Como instalar

1. Abra a pasta do pacote gerado pelo instalador.
2. Execute `Install-PrimoAutoEletrica.ps1` em um PowerShell com permissao de escrita na pasta escolhida.
3. Se a maquina ja possuir .NET Desktop Runtime 9, use a instalacao normal.
4. Se o pacote for self-contained, a verificacao do runtime pode ser dispensada pelo proprio pacote.
5. Confirme que foram criadas a pasta do programa, a pasta de dados e os atalhos do sistema.

Comando padrao:

```powershell
.\Install-PrimoAutoEletrica.ps1
```

Comando com destino customizado:

```powershell
.\Install-PrimoAutoEletrica.ps1 -InstallDirectory "C:\PrimoAutoEletrica" -AppDataDirectory "C:\PrimoAutoEletricaDados"
```

## Verificacao depois da instalacao

- O arquivo `PrimoAutoEletrica.exe` deve existir na pasta instalada.
- A pasta de dados deve existir e aceitar escrita.
- O atalho da area de trabalho deve abrir o programa.
- O log de instalacao deve estar disponivel para auditoria.

## Primeiro acesso

1. Abra o sistema pelo atalho ou pelo executavel.
2. Entre com o usuario administrador inicial configurado para a oficina.
3. Acesse `Configuracoes` e revise dados da empresa, telefone, WhatsApp, logo, numeracao e backup automatico.
4. Cadastre pelo menos um funcionario administrador adicional, se necessario.
5. Execute um backup manual antes de iniciar a operacao real.

## Rotina inicial recomendada

- Configure dados da empresa antes de gerar PDFs.
- Configure impressora padrao antes de imprimir documentos.
- Configure permissoes antes de liberar uso para caixa, estoque ou mecanico.
- Cadastre produtos basicos e fornecedores antes de operar o PDV.
