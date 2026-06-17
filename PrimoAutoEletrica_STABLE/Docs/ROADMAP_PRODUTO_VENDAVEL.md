# Roadmap para Produto Vendavel

Este documento planeja recursos futuros sem implementa-los antes da estabilizacao do sistema atual.

## 1. Multiempresa

Objetivo futuro: permitir que a arquitetura suporte mais de uma empresa sem misturar dados.

Premissas:

- Uma instalacao padrao continua atendendo uma oficina.
- Dados comerciais, logo, numeracao e configuracoes devem pertencer a uma empresa.
- Entidades operacionais futuras devem receber identificador de empresa quando houver necessidade real.

Riscos:

- Migracao de dados pode ser sensivel se for feita sem planejamento.
- PDFs, backup e permissoes precisam respeitar isolamento por empresa.

## 2. Multiestacao em rede local

Objetivo futuro: permitir uso em mais de uma maquina da oficina com seguranca e consistencia.

Direcao tecnica:

- SQLite local continua adequado para instalacao simples.
- Para concorrencia real, avaliar PostgreSQL ou SQL Server.
- Repositorios e servicos devem evitar dependencias fortes de SQLite.
- Rotinas de bloqueio, auditoria e backup precisam ser revisadas para ambiente multiusuario.

Riscos:

- SQLite em rede pode corromper ou degradar com muitos acessos simultaneos.
- Atualizacao de schema precisa ser coordenada entre estacoes.

## 3. Licenciamento

Objetivo futuro: permitir controle comercial sem prejudicar a oficina em caso de internet instavel.

Opcoes:

- Chave de licenca offline assinada.
- Ativacao online com periodo de tolerancia offline.
- Controle de versao e plano contratado.
- Backup do cliente protegido contra bloqueio indevido.

Principio:

- O licenciamento nunca deve impedir acesso emergencial aos dados da oficina sem uma regra clara de contingencia.

## 4. Planos comerciais futuros

Plano Basico:

- Clientes.
- Veiculos.
- Orcamentos.
- Ordens de servico.

Plano Profissional:

- Estoque.
- PDV.
- Financeiro.
- Relatorios.
- Importacao de NF-e.

Plano Premium:

- WhatsApp.
- Checklist com fotos.
- Assinatura.
- Diagnostico guiado.
- Recursos avancados de multiestacao quando estabilizados.

## 5. Pre-requisitos antes de vender amplamente

- Build sem erro.
- UI smoke completo aprovado.
- Workflow completo aprovado.
- Instalador e atualizador testados.
- Manual do usuario publicado.
- Manual tecnico publicado.
- Backup e restauracao validados em ambiente limpo.
- Checklist final de qualidade atualizado por versao.
