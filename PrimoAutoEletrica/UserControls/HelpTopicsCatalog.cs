using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.UserControls
{
    /// <summary>
    /// Conteúdo da Escola PRIMOX — linguagem simples para quem acabou de comprar
    /// e precisa ensinar a equipe sem curso pago.
    /// </summary>
    internal static class HelpTopicsCatalog
    {
        public static Dictionary<string, HelpTopic> Create()
        {
            var t = new Dictionary<string, HelpTopic>(StringComparer.OrdinalIgnoreCase);

            t["comece-aqui"] = new HelpTopic
            {
                Title = "Acabei de comprar — COMECE AQUI",
                Purpose = "Se você acabou de instalar o programa e não quer pagar treinamento: siga esta página na ordem. Não pule.",
                QuickLinks = new[] { "tour-30min", "cargo-gerente", "glossario", "rotina-diaria", "treinar-equipe" },
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Regra de ouro (grave isso)",
                        Content = "O sistema só funciona bem se a oficina seguir este caminho:",
                        Steps = new[]
                        {
                            "Cadastrar o CLIENTE",
                            "Cadastrar o VEÍCULO desse cliente",
                            "Fazer o ORÇAMENTO (lista do que vai fazer/cobrar)",
                            "Se o cliente aceitar → abrir a ORDEM DE SERVIÇO (OS)",
                            "Fazer o serviço e atualizar a OS",
                            "Receber o dinheiro (Financeiro ou PDV)",
                            "No fim do dia: fechar o caixa e conferir o Dashboard"
                        },
                        Callouts = new[]
                        {
                            new HelpCallout
                            {
                                Kind = "danger",
                                Title = "Não faça assim",
                                Message = "Não atenda “de cabeça”, não anote só no caderno e não venda peça sem passar no PDV. Isso quebra estoque e dinheiro."
                            }
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Seu caminho nos primeiros 60 minutos",
                        Steps = new[]
                        {
                            "Leia o glossário (5 min) — para entender as palavras do sistema.",
                            "Faça o tour de 30 minutos — um atendimento de mentira do começo ao fim.",
                            "Abra o guia do SEU cargo (Dono, Recepção, Caixa ou Eletricista).",
                            "Imprima mentalmente a Rotina da manhã e ensine a equipe.",
                            "Se travar: abra “Erros comuns e como corrigir”."
                        },
                        ScreenshotKey = "dashboard",
                        ScreenshotCaption = "Depois do login você cai no Dashboard. É a “capa” do dia."
                    },
                    new HelpSection
                    {
                        Heading = "Quem lê o quê",
                        BulletPoints = new[]
                        {
                            "DONO/GERENTE → Comece aqui + Tour 30 min + Guia Dono + Treinar equipe + Configurações",
                            "RECEPÇÃO → Glossário + Cadastrar cliente/veículo/orçamento + Guia Recepção",
                            "CAIXA → Glossário + Vender no PDV + Guia Caixa + Fechar o dia",
                            "ELETRICISTA → Glossário + Abrir OS + Kanban + Guia Eletricista"
                        }
                    }
                },
                Tip = "Não tente aprender tudo de uma vez. Faça o tour de 30 minutos HOJE. O resto vem na primeira semana.",
                RelatedTopics = new[] { "tour-30min", "primeira-semana", "treinar-equipe" }
            };

            t["tour-30min"] = new HelpTopic
            {
                Title = "Aprenda em 30 minutos (faça junto)",
                Purpose = "Um atendimento completo de treino. Faça no computador AGORA, clicando junto com o texto.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Minutos 0–5 · Entrar e olhar o Dashboard",
                        Steps = new[]
                        {
                            "Abra o programa e faça login.",
                            "Olhe o Dashboard: números de OS, orçamentos e dinheiro.",
                            "Não se assuste se estiver zerado — oficina nova começa assim."
                        },
                        ScreenshotKey = "dashboard",
                        ScreenshotCaption = "Dashboard = visão geral. Só olhar."
                    },
                    new HelpSection
                    {
                        Heading = "Minutos 5–12 · Cliente + veículo",
                        Steps = new[]
                        {
                            "Menu esquerdo → Clientes → botão de Novo.",
                            "Digite um cliente de teste (ex.: Cliente Treino). Salve.",
                            "Menu → Veículos → Novo. Escolha esse cliente. Placa de teste (ex.: TST1A23). Salve."
                        },
                        ScreenshotKey = "clientes",
                        ScreenshotCaption = "Cliente salvo = base de tudo."
                    },
                    new HelpSection
                    {
                        Heading = "Minutos 12–22 · Orçamento → OS",
                        Steps = new[]
                        {
                            "Menu → Orçamentos → Novo.",
                            "Escolha o cliente e o veículo de teste.",
                            "Adicione 1 serviço ou 1 peça (mesmo inventado no treino).",
                            "Salve. Depois use a opção de converter/aprovar para virar OS (se aparecer).",
                            "Abra Ordens de Serviço e veja a OS criada. Coloque um responsável se pedir."
                        },
                        ScreenshotKey = "orcamentos",
                        ScreenshotCaption = "Orçamento = proposta. OS = serviço em andamento."
                    },
                    new HelpSection
                    {
                        Heading = "Minutos 22–30 · PDV + olhar Kanban",
                        Steps = new[]
                        {
                            "Menu → PDV. Se pedir para abrir caixa, abra com um valor (ex.: 100).",
                            "Faça uma venda de teste de 1 produto (se tiver cadastrado) ou só explore a tela.",
                            "Abra Kanban da Oficina e veja as colunas (Aberta, Em andamento…).",
                            "Parabéns: você já andou o caminho principal do sistema."
                        },
                        Callouts = new[]
                        {
                            new HelpCallout
                            {
                                Kind = "important",
                                Title = "Pronto para o próximo passo",
                                Message = "Agora abra o guia do SEU cargo e a Rotina da manhã. Depois ensine 1 funcionário por vez."
                            }
                        }
                    }
                },
                RelatedTopics = new[] { "comece-aqui", "cargo-gerente", "glossario" }
            };

            t["primeira-semana"] = new HelpTopic
            {
                Title = "Primeira semana — plano dia a dia",
                Purpose = "Não precisa virar expert no dia 1. Siga este plano e a oficina aprende sem curso.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Dia 1 — Fundação",
                        Steps = new[]
                        {
                            "Comece aqui + Tour 30 min",
                            "Configurações: dados da empresa + backup ligado",
                            "Criar usuários: um login por pessoa (sem senha compartilhada)"
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Dia 2 — Recepção",
                        Steps = new[]
                        {
                            "Treinar: cadastrar cliente e veículo de verdade",
                            "Fazer 3 orçamentos reais (mesmo simples)",
                            "Enviar/imprimir 1 orçamento para o cliente"
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Dia 3 — Oficina",
                        Steps = new[]
                        {
                            "Converter orçamentos aprovados em OS",
                            "Eletricista atualiza status no Kanban",
                            "Lançar peças usadas na OS"
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Dia 4 — Caixa e estoque",
                        Steps = new[]
                        {
                            "Abrir/fechar caixa no PDV corretamente",
                            "Vendas de balcão só pelo PDV",
                            "Conferir alertas de estoque baixo"
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Dia 5 — Dinheiro e relatório",
                        Steps = new[]
                        {
                            "Baixar recebimentos no Financeiro",
                            "Gerar 1 relatório de faturamento da semana",
                            "Reunião de 15 min: o que deu errado? Abrir tópico de erros"
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Dia 6–7 — Virar hábito",
                        BulletPoints = new[]
                        {
                            "Usar Rotina da manhã e Fechamento da noite TODO dia",
                            "Cada cargo relê o próprio guia (10 min)",
                            "Dono confere backup e permissões"
                        }
                    }
                },
                Tip = "Na semana 2 a equipe já deve operar sem “anotar no papel”.",
                RelatedTopics = new[] { "rotina-diaria", "treinar-equipe", "erros-evitar" }
            };

            t["glossario"] = new HelpTopic
            {
                Title = "Palavras do sistema (glossário fácil)",
                Purpose = "Se não entender a palavra, para tudo e lê aqui. Sem vergonha.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Palavras que mais aparecem",
                        BulletPoints = new[]
                        {
                            "DASHBOARD — tela inicial com números do dia (quanto vendeu, quantas OS…)",
                            "CLIENTE — pessoa ou empresa dona do carro",
                            "VEÍCULO — o carro/moto (placa, modelo)",
                            "ORÇAMENTO — proposta do que vai fazer e quanto custa (ainda não é serviço)",
                            "OS (Ordem de Serviço) — o serviço JÁ autorizado, em execução na oficina",
                            "KANBAN — quadro com cartõezinhos das OS (quem está parado, quem está andando)",
                            "PDV — caixa do balcão (venda rápida de peça/produto)",
                            "ESTOQUE — quantidade de peças na prateleira",
                            "NF-e — nota fiscal do fornecedor em arquivo XML (entrada de peças)",
                            "FINANCEIRO — contas a pagar e a receber (o dinheiro “de verdade”)",
                            "BACKUP — cópia de segurança dos dados (não desligue isso)",
                            "BAIXA — registrar que uma conta foi paga/recebida (ou saída de estoque, conforme a tela)",
                            "LGPD — regras de privacidade do cliente (consentimento de contato)",
                            "WA.ME — abre o WhatsApp no celular/PC; NÃO é envio automático pela nuvem"
                        },
                        Callouts = new[]
                        {
                            new HelpCallout
                            {
                                Kind = "important",
                                Title = "Diferença que ninguém explica",
                                Message = "ORÇAMENTO = “pode ser”. OS = “está fazendo”. PDV = “vendeu no balcão agora”. Misturar isso é o erro nº 1 dos iniciantes."
                            }
                        }
                    }
                },
                RelatedTopics = new[] { "comece-aqui", "tour-30min" }
            };

            t["cargo-ceo"] = Role(
                "Guia do CEO / Diretor Executivo",
                "Governança corporativa, consolidação financeira multi-filiais, margem líquida, expansão da rede e retorno sobre capital (ROI).",
                new[]
                {
                    "Dashboard Executivo: analise faturamento agregado da rede, ticket médio e margem de contribuição (peças vs serviços)",
                    "Multi-Filiais: monitore volume de ordens de serviço, produtividade e balanceamento de capacidade entre Matriz e Filiais",
                    "Capital de Giro & Estoque: audite a Curva ABC e o capital imobilizado em peças de alto giro vs itens de baixa rotação",
                    "Contratos Corporativos B2B: valide a rentabilidade e o cumprimento dos SLAs de frotas e transportadoras conveniadas",
                    "Conformidade & Auditoria: certifique-se da regularidade fiscal (NFS-e/NFC-e), integridade da licença HMAC e backups da rede"
                },
                new[]
                {
                    "Nunca tome decisões de expansão baseadas apenas em fluxo de caixa imediato sem analisar o DRE consolidado por unidade",
                    "Nunca autorize abertura de nova filial sem definir a infraestrutura de rede segura (VPN/LAN) para sincronização de dados",
                    "Nunca compartilhe credenciais master/admin entre gerentes de unidades distintas"
                },
                "O PRIMOX Enterprise foi arquitetado para fornecer ao conselho e diretoria controle total da rede com alta performance e sem dependência de terceiros.",
                new[] { "modulo-multifilial", "modulo-dashboard", "modulo-relatorios", "modulo-licenca" });

            t["cargo-proprietario"] = Role(
                "Guia do Proprietário / Sócio-Fundador",
                "Gestão executiva da oficina: segurança do caixa, lucro real por serviço, prevenção de desvios e crescimento sustentável.",
                new[]
                {
                    "Manhã: abra o Dashboard, confira o faturamento acumulado do mês e o volume de OS no pátio",
                    "Auditoria de Caixa: confira as sangrias de segurança do PDV e os fechamentos cegos de caixa do turno anterior",
                    "Aprovação Comercial: valide descontos e condições especiais em orçamentos de alto valor",
                    "Controle Anti-Ruptura: revise no módulo Compras os alertas de Ponto de Pedido (ROP) para evitar boxes parados por falta de peças",
                    "Segurança Patrimonial: confirme a execução diária do backup do banco de dados e a calibração de ferramentas de precisão"
                },
                new[]
                {
                    "Nunca permita saída de veículo da oficina sem a OS quitada ou devidamente faturada no módulo de Frotas",
                    "Nunca faça retiradas do caixa da empresa sem lançamento formal de pró-labore no Financeiro",
                    "Nunca desative as rotinas de backup automático corporativo"
                },
                "Oficina lucrativa é aquela com zero retrabalho, estoque sem peças encalhadas e caixa 100% conciliado.",
                new[] { "cargo-ceo", "cargo-gerente", "modulo-financeiro", "modulo-compras", "fazer-backup" });

            t["cargo-gerente"] = Role(
                "Guia do Gerente de Operações / Oficina",
                "Você lidera o chão de fábrica: distribuição de serviços, eliminação de gargalos, produtividade técnica e garantia de qualidade.",
                new[]
                {
                    "Reunião Matinal (Daily 5 min): abra o Kanban da Oficina e alinhe as prioridades de entrega com os eletricistas e técnicos",
                    "Alocação Inteligente de Boxes: distribua as OS considerando a especialidade técnica (injeção, alternador, ar-condicionado, chicotes)",
                    "Eliminação de Gargalos: filtre no Kanban os veículos em 'Aguardando Peça' e cobre agilidade do Almoxarifado/Compras",
                    "Ferramentaria 4.0: garanta que osciloscópios e scanners estejam devidamente sob custódia dos técnicos e calibrados",
                    "Fechamento da Tarde: confira se todas as OS do dia tiveram peças e horas técnicas apontadas antes da liberação ao cliente"
                },
                new[]
                {
                    "Nunca permita técnico trabalhando em veículo sem Ordem de Serviço formalizada no sistema",
                    "Nunca libere entrega de carro sem conferência do checklist de devolução de ferramentas (risco de ferramenta esquecida no motor)",
                    "Nunca altere o status de uma OS sem o consentimento do técnico executor"
                },
                "O Kanban é o termômetro da oficina: se a coluna 'Aguardando Peça' acumular cartões, o gargalo está em suprimentos, não na equipe mecânica.",
                new[] { "modulo-kanban", "modulo-os", "modulo-ferramentas", "treinar-equipe" });

            t["cargo-frotas"] = Role(
                "Guia do Gestor de Frotas B2B",
                "Gestão de frotistas corporativos, transportadoras, contratos com tabela diferenciada e faturamento quinzenal/mensal.",
                new[]
                {
                    "Cadastro de Contratos: vincule o cliente corporativo, tabela de preços negociada e prazos de faturamento periódico",
                    "Recepção de Frotas: registre rigorosamente a quilometragem (KM) e o motorista na abertura de cada orçamento/OS",
                    "Validação de Limite de Crédito: verifique no módulo Frotas se o frotista possui saldo disponível antes de aprovar serviços adicionais",
                    "Fechamento do Ciclo: selecione o período de apuração, agrupe as OS concluídas da frota e gere o Romaneio Consolidado",
                    "Envio de Fatura Analítica: exporte o relatório por placa e KM detalhando peças e serviços para a aprovação financeira da empresa"
                },
                new[]
                {
                    "Nunca atenda veículo corporativo sem registrar a placa exata e a quilometragem real do painel",
                    "Nunca aplique descontos manuais fora da tabela previamente aprovada no contrato B2B",
                    "Nunca libere veículo de frota com contrato bloqueado por inadimplência sem autorização por escrito do Financeiro"
                },
                "Grandes frotas exigem transparência cirúrgica: o relatório analítico por placa e KM fecha negociações de longo prazo.",
                new[] { "modulo-frotas", "passo-frotas-faturamento", "modulo-orcamentos", "modulo-financeiro" });

            t["cargo-comprador"] = Role(
                "Guia do Comprador / Gestão de Compras",
                "Previsão de demanda, compras anti-ruptura via Ponto de Pedido (ROP), cotações com múltiplos distribuidores e negociação.",
                new[]
                {
                    "Monitoramento de ROP: abra a tela de Compras e consulte a lista de itens cujo saldo atingiu o Ponto de Pedido mínimo",
                    "Cotação com Múltiplos Fornecedores: gere cotação para 3 ou mais distribuidores comparando preço unitário e prazo de entrega",
                    "Emissão de Ordem de Compra (OC): formalize o pedido aprovado com condições de pagamento e previsão de entrega",
                    "Acompanhamento de Lead Time: monitore a data prometida de entrega para evitar paradas em serviços agendados",
                    "Alinhamento com Almoxarifado: informe a equipe de recebimento sobre as compras despachadas para conferência de XML"
                },
                new[]
                {
                    "Nunca compre peças críticas 'no escuro' sem consultar o histórico de consumo e giro no Estoque",
                    "Nunca aprove pedidos com fornecedores sem cadastro homologado ou sem documento fiscal válido",
                    "Nunca autorize recebimento de mercadorias sem a respectiva Ordem de Compra registrada no sistema"
                },
                "Comprar antes que a peça acabe é o segredo da oficina de alta produtividade: o cálculo de ROP do PRIMOX faz isso automaticamente.",
                new[] { "modulo-compras", "passo-compras-pedido", "modulo-estoque", "modulo-fornecedores" });

            t["cargo-almoxarife"] = Role(
                "Guia do Almoxarife / Estoquista",
                "Recebimento de mercadorias, conferência cega, endereçamento de prateleiras, etiquetagem e requisições para a oficina.",
                new[]
                {
                    "Entrada de Mercadorias: importe o XML da NF-e de compra e confira fisicamente as quantidades recebidas",
                    "Conversão de Embalagem: certifique-se de converter caixas/pacotes para unidades individuais quando aplicável",
                    "Endereçamento Físico: organize as peças em suas respectivas posições (Rua / Prateleira / Gaveta) cadastradas no PRIMOX",
                    "Etiquetagem Térmica ZPL II: imprima etiquetas com código de barras e QR Code para peças sem identificação de fábrica",
                    "Atendimento à Oficina: só entregue peças para os técnicos mediante baixa apontada na Ordem de Serviço correspondente",
                    "Inventário Rotativo: realize contagens semanais por categoria de produtos para manter a acurácia de estoque em 100%"
                },
                new[]
                {
                    "Nunca entregue peças de balcão ou oficina sem a baixa imediata na OS ou venda no PDV",
                    "Nunca importe a mesma nota fiscal de compra duas vezes (duplica o saldo físico e financeiro)",
                    "Nunca altere saldos manualmente sem apontar a justificativa formal (perda, avaria ou inventário)"
                },
                "Peça no lugar errado é peça perdida. Com o endereçamento e as etiquetas térmicas do PRIMOX, qualquer peça é achada em 10 segundos.",
                new[] { "modulo-estoque", "modulo-nfe", "modulo-etiquetas", "modulo-transferencias" });

            t["cargo-ferramenteiro"] = Role(
                "Guia do Responsável por Ferramentaria 4.0",
                "Custódia de aparelhos de precisão (scanners, osciloscópios, alicates, torquímetros), controle de calibração e anti-extravio.",
                new[]
                {
                    "Check-Out (Empréstimo): selecione o técnico e bipe a ferramenta especial para transferir a responsabilidade formal",
                    "Inspeção Prévia: certifique-se de que pontas de prova, conectores e cabos de alimentação estejam intactos",
                    "Check-In (Devolução): inspecione visualmente o equipamento retornado e registre a devolução no sistema",
                    "Controle de Calibração: consulte semanalmente os alertas de vencimento de aferição de torquímetros e manômetros",
                    "Trava Anti-Extravio: antes da entrega final de um veículo, verifique no sistema se nenhuma ferramenta ficou retida na respectiva OS"
                },
                new[]
                {
                    "Nunca libere equipamentos de alto valor (como scanner de diagnóstico) sem registro formal de check-out no sistema",
                    "Nunca use instrumentos com aferição vencida em laudos técnicos ou perícias automotivas",
                    "Nunca guarde equipamentos com pontas ou cabos danificados sem registrar o chamado de manutenção interna"
                },
                "A trava anti-extravio elimina de vez o prejuízo de esquecer osciloscópios ou ferramentas caras no cofre do motor do cliente.",
                new[] { "modulo-ferramentas", "passo-ferramentas-emprestimo", "modulo-os", "modulo-autoeletrica" });

            t["cargo-recepcao"] = Role(
                "Guia do Consultor de Serviços / Recepção (Service Advisor)",
                "Porta de entrada da oficina: acolhimento, checklist de entrada no veículo, elaboração de orçamentos e comunicação ágil.",
                new[]
                {
                    "Acolhimento & Busca: pesquise por CPF/CNPJ ou telefone antes de criar qualquer novo cadastro no sistema",
                    "Checklist de Entrada: fotografe ou anote avarias pré-existentes, pertences, quilometragem (KM) e nível de combustível",
                    "Relato Técnico do Cliente: anote detalhadamente as queixas elétricas (ex.: 'luz da bateria acende ao ligar o ar', 'falha intermitente')",
                    "Elaboração de Orçamento: lance com clareza os serviços diagnósticos e as peças sugeridas",
                    "Aprovação Rápida: envie a proposta comercial via WhatsApp ou impresso para autorização do cliente",
                    "Conversão em OS: logo após a aprovação formal, converta o orçamento em Ordem de Serviço e notifique o pátio"
                },
                new[]
                {
                    "Nunca cadastre cliente duplicado com variações de grafia no nome",
                    "Nunca autorize desmontagem de painéis ou chicotes sem a aprovação prévia do orçamento pelo cliente",
                    "Nunca omita danos ou arranhões prévios identificados no checklist de recebimento"
                },
                "Um checklist de entrada bem executado é a maior blindagem jurídica e de credibilidade que a oficina pode ter.",
                new[] { "criar-cliente", "registrar-veiculo", "criar-orcamento", "criar-os", "modulo-agendamentos" });

            t["cargo-eletricista"] = Role(
                "Guia do Eletricista Automotivo / Técnico Diagnosticador",
                "Diagnóstico elétrico com Copilot de IA, análise de DTCs, medição de ripple, diagramas e execução técnica de alto padrão.",
                new[]
                {
                    "Consulta ao Kanban: identifique a OS com o seu nome e mova o cartão para 'Em Andamento'",
                    "Copilot de IA: insira o código DTC do scanner (ex.: P0335) e obtenha esquema de pinagem, tensões esperadas e roteiro de testes",
                    "Testes Elétricos: realize teste de carga em bateria, queda de tensão e teste de ripple no alternador (registrando na aba Auto Elétrica Técnica)",
                    "Requisição no Estoque: solicite as peças necessárias ao almoxarifado garantindo que sejam apontadas na OS",
                    "Validação Pós-Reparo: teste o circuito sob carga máxima (farol alto + desembaçador + ar) antes de aprovar",
                    "Devolução & Finalização: faça o check-in das ferramentas na Ferramentaria 4.0 e mova o Kanban para 'Finalizada'"
                },
                new[]
                {
                    "Nunca substitua módulos ou alternadores sem comprovação por teste elétrico documentado",
                    "Nunca finalize a OS sem apontar as peças e componentes realmente instalados no veículo",
                    "Nunca libere o carro com ferramentas da oficina esquecidas no compartimento do motor"
                },
                "O Copilot de IA Dual-Engine economiza horas de bancada ao correlacionar DTCs complexos e diagramas elétricos na hora.",
                new[] { "modulo-ia", "modulo-autoeletrica", "modulo-kanban", "modulo-ferramentas", "passo-ia-diagnostico" });

            t["cargo-caixa"] = Role(
                "Guia do Operador de Caixa / Balcão (PDV)",
                "Operação financeira de frente de loja: abertura com fundo de troco, recebimento de vendas e OS, sangrias e fechamento seguro.",
                new[]
                {
                    "Abertura de Caixa (Manhã): conte o dinheiro físico do fundo de troco (suprimento) e informe o valor exato no PDV",
                    "Vendas Rápidas de Peças: use o leitor de código de barras para lançar fusíveis, lâmpadas e relés com agilidade",
                    "Recebimento de Ordens de Serviço: localize a OS finalizada pela placa ou número e receba via PIX, Cartão (TEF) ou Dinheiro",
                    "Sangrias de Segurança: sempre que o saldo em dinheiro atingir o teto de segurança, registre a sangria e transfira ao cofre",
                    "Fechamento Cego (Fim de Turno): encerre o turno, conte fisicamente o dinheiro e comprovantes sem ver o total do sistema e imprima o fechamento"
                },
                new[]
                {
                    "Nunca deixe o caixa aberto sem vigilância ou utilize a senha de outro operador",
                    "Nunca entregue troco sem conferência rigorosa na frente do cliente",
                    "Nunca realize vendas 'por fora' sem passar pelo PDV (isso gera furo de estoque e sonegação fiscal)"
                },
                "Se houver divergência no fechamento cego de caixa, chame o gerente imediatamente e confira os comprovantes antes de fechar o turno.",
                new[] { "venda-pdv", "modulo-pdv", "fechamento-dia", "modulo-fiscal" });

            t["cargo-financeiro"] = Role(
                "Guia do Analista Financeiro & Fiscal",
                "Gestão de contas a pagar e receber, fluxo de caixa projetado, conciliação bancária e transmissão fiscal (NFS-e e NFC-e).",
                new[]
                {
                    "Contas a Receber: monitore vencimentos do dia e recebimentos de frotistas conveniados, efetuando as baixas bancárias",
                    "Contas a Pagar: programe pagamentos a fornecedores de autopeças e despesas operacionais da empresa",
                    "Central Fiscal: acompanhe a emissão e autorização de NFS-e (ISS de serviços) e NFC-e (balcão) via Gateway Fiscal",
                    "Conciliação Bancária: confira os extratos bancários contra os lançamentos de recebíveis de cartão e PIX do sistema",
                    "Fluxo de Caixa (D+30): avalie os saldos futuros projetados para garantir o capital de giro necessário para compras críticas"
                },
                new[]
                {
                    "Nunca baixe duplicatas ou contas a receber sem o comprovante de depósito ou conciliação em conta corrente",
                    "Nunca deixe notas fiscais em contingência sem retransmitir para validação na Prefeitura ou SEFAZ",
                    "Nunca apague lançamentos financeiros conciliados sem a devida autorização da diretoria"
                },
                "A emissão de NFS-e para mão de obra e NFC-e para peças garante a conformidade contábil e protege a oficina contra contingências fiscais.",
                new[] { "modulo-financeiro", "modulo-fiscal", "usar-financeiro", "passo-fiscal-emissao", "modulo-relatorios" });

            t["treinar-equipe"] = new HelpTopic
            {
                Title = "Como treinar sua equipe (sem pagar curso)",
                Purpose = "Método simples para o dono ensinar caixa, recepção e eletricista usando só esta Ajuda.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Método de 3 passos (por funcionário)",
                        Steps = new[]
                        {
                            "EU FAÇO: você faz o Tour 30 min na frente dele (ele só olha).",
                            "NÓS FAZEMOS: ele clica, você corrige ao lado (1 atendimento real simples).",
                            "ELE FAZ: ele sozinho; você só confere no fim do dia no Dashboard/Kanban."
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Ordem de treinamento (não inverta)",
                        Steps = new[]
                        {
                            "Todo mundo lê o Glossário (15 min).",
                            "Recepção primeiro (cadastro + orçamento).",
                            "Eletricista (OS + Kanban).",
                            "Caixa (PDV + fechar caixa).",
                            "Gerente revisa permissões e backup."
                        },
                        Callouts = new[]
                        {
                            new HelpCallout
                            {
                                Kind = "caution",
                                Title = "Erro clássico de dono",
                                Message = "Treinar todo mundo no mesmo dia. Resultado: ninguém aprende. Treine 1 cargo por vez."
                            }
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Prova de que aprendeu (checklist)",
                        BulletPoints = new[]
                        {
                            "Recepção: cadastra cliente+veículo+orçamento sem ajuda",
                            "Eletricista: atualiza status e lança peça sozinho",
                            "Caixa: abre, vende, fecha caixa batendo com a gaveta",
                            "Ninguém usa senha do outro"
                        }
                    }
                },
                Tip = "Imprima (ou deixe aberto) o guia do cargo no computador de cada um na primeira semana.",
                RelatedTopics = new[] { "comece-aqui", "cargo-recepcao", "cargo-caixa", "cargo-eletricista" }
            };

            t["rotina-diaria"] = new HelpTopic
            {
                Title = "Abrir o dia (manhã) — faça nesta ordem",
                Purpose = "Checklist para a oficina não começar o dia no caos.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Checklist da manhã (15 a 20 minutos)",
                        Steps = new[]
                        {
                            "Abrir o programa com SEU usuário",
                            "Dashboard: ver OS abertas e orçamentos pendentes",
                            "Agendamentos: confirmar quem vem hoje",
                            "Kanban: ver o que está parado / aguardando peça",
                            "Estoque: olhar alertas vermelhos/baixos das peças do dia",
                            "Se tem balcão: PDV → Abrir caixa",
                            "Financeiro: ver o que vence hoje (se for seu cargo)",
                            "Só agora atender cliente novo"
                        },
                        ScreenshotKey = "dashboard",
                        ScreenshotCaption = "Comece sempre pelo Dashboard."
                    }
                },
                Callouts = new[]
                {
                    new HelpCallout
                    {
                        Kind = "important",
                        Title = "Se só tiver tempo para 3 coisas",
                        Message = "1) Abrir caixa  2) Olhar Kanban  3) Confirmar agenda. O resto vem em seguida."
                    }
                },
                RelatedTopics = new[] { "fechamento-dia", "cargo-gerente", "comece-aqui" }
            };

            t["fechamento-dia"] = new HelpTopic
            {
                Title = "Fechar o dia (noite)",
                Purpose = "Terminar o dia sem misturar caixa, OS e dinheiro.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Checklist da noite",
                        Steps = new[]
                        {
                            "Kanban/OS: nada “Em andamento” esquecido sem observação",
                            "PDV: fechar caixa e conferir a gaveta",
                            "Financeiro: baixar o que recebeu hoje",
                            "Dashboard: anotar o que ficou para amanhã",
                            "Backup: confirmar que está ativo (Configurações)"
                        },
                        Callouts = new[]
                        {
                            new HelpCallout
                            {
                                Kind = "danger",
                                Title = "Nunca durma com o caixa aberto",
                                Message = "Mistura o dinheiro de ontem com o de hoje. Sempre feche."
                            }
                        }
                    }
                },
                RelatedTopics = new[] { "rotina-diaria", "cargo-caixa" }
            };

            t["mapa-paginas"] = new HelpTopic
            {
                Title = "O que tem em cada tela",
                Purpose = "Clique na setinha de cada card para abrir. Leia “o que é”, “o que fazer” e “cuidado”.",
                PageGuides = BuildPageGuides(),
                Tip = "Não precisa decorar tudo. Use como enciclopédia quando esquecer.",
                RelatedTopics = new[] { "glossario", "erros-evitar" }
            };

            t["proveito-maximo"] = new HelpTopic
            {
                Title = "Tirar o máximo do sistema",
                Purpose = "Depois que a base funciona, estes hábitos fazem a oficina ganhar tempo e dinheiro.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Hábitos de oficina forte",
                        BulletPoints = new[]
                        {
                            "Orçamento no mesmo atendimento (não “depois eu faço”)",
                            "OS com responsável sempre",
                            "Peça do balcão só pelo PDV",
                            "Entrada de compra pela NF-e quando der",
                            "Relatório toda semana (mesmo que 10 minutos)",
                            "1 usuário por pessoa"
                        }
                    }
                },
                RelatedTopics = new[] { "primeira-semana", "modulo-relatorios" }
            };

            t["erros-evitar"] = new HelpTopic
            {
                Title = "Erros comuns e como corrigir",
                Purpose = "Errou? Respira. Abaixo: o erro, por que dói, e o que fazer agora.",
                Sections = BuildErrorSections(),
                Tip = "Errar uma vez é aprendizado. Repetir o mesmo erro todo dia é falta de rotina.",
                RelatedTopics = new[] { "faq", "treinar-equipe", "suporte" }
            };

            // Passos + módulos (linguagem simples)
            t["primeiro-acesso"] = t["comece-aqui"]; // alias mental — tag antiga se F1 usar

            AddStep(t, "criar-cliente", "Cadastrar cliente",
                "Guardar a pessoa ou empresa no sistema para depois orçar, abrir OS e cobrar.",
                new[]
                {
                    "No menu lateral ESQUERDO, localize e clique em “Clientes”.",
                    "Na parte superior da tela de Clientes, localize o botão “Novo” (ou “Novo Cliente”) e clique nele.",
                    "Na janela que abrir, no campo “Nome”, digite o nome completo. Exemplo: QA_HELP_CLIENTE Silva.",
                    "No campo de telefone, digite um número de teste. Exemplo: (17) 99999-0001.",
                    "Se pedir CPF/CNPJ, preencha só com dados de teste (nunca use documento real de terceiro).",
                    "Revise os dados e clique em “Salvar” (geralmente botão destacado na parte inferior ou superior da janela).",
                    "Feche a janela se ela continuar aberta. O cliente deve aparecer na lista. Use a caixa de pesquisa acima da tabela se não achar de imediato."
                },
                "Antes de cadastrar, use a pesquisa no topo da lista. Cliente duplicado vira bagunça no histórico.",
                "Se já criou duplicado: use só o cadastro mais completo e pare de usar o outro.",
                "clientes");

            AddStep(t, "registrar-veiculo", "Cadastrar veículo",
                "Ligar a placa ao cliente. Sem isso o histórico do carro some.",
                new[]
                {
                    "No menu lateral esquerdo, clique em “Veículos”.",
                    "Clique no botão “Novo” (parte superior da tela).",
                    "Escolha o cliente já cadastrado (campo de seleção / busca de cliente).",
                    "Digite a placa. Exemplo de teste: QAH1A23.",
                    "Preencha modelo/ano quando a tela pedir. Exemplo: Gol 2019.",
                    "Clique em “Salvar”. O veículo deve aparecer na lista vinculado ao cliente."
                },
                "Placa certa é obrigatória. Errar a placa cria um “carro fantasma”.",
                "Placa errada: edite na hora. Não crie outro veículo.",
                "veiculos");

            AddStep(t, "criar-orcamento", "Fazer orçamento",
                "Mostrar ao cliente o que será feito e o preço ANTES de executar.",
                new[]
                {
                    "Menu lateral esquerdo → “Orçamentos”.",
                    "Clique em “Novo” / “Novo Orçamento”.",
                    "Selecione o cliente e o veículo.",
                    "Adicione serviços e/ou peças pelos botões da tela (ex.: Adicionar item / Selecionar produto).",
                    "Revise o total. Clique em “Salvar” ou “Salvar rascunho”, conforme o botão disponível.",
                    "Se o cliente aceitar, use a opção de aprovar/converter para OS (quando aparecer na tela)."
                },
                "Orçamento NÃO é Ordem de Serviço. Orçamento = proposta. OS = serviço autorizado.",
                "Virou OS cedo demais e não começou? Cancele a OS (se permitido) e volte o orçamento.",
                "orcamentos");

            AddStep(t, "criar-os", "Abrir ordem de serviço",
                "Colocar o carro oficialmente na oficina com rastreio de status e peças.",
                new[]
                {
                    "Preferência: abra a OS a partir do orçamento aprovado (botão converter/gerar OS).",
                    "Ou: menu lateral → “Ordens de Serviço” → “Nova OS”.",
                    "Confira cliente, veículo e itens.",
                    "Informe o responsável (eletricista/técnico) quando a tela pedir.",
                    "Salve / Emita conforme os botões da janela.",
                    "Durante o serviço: atualize o status e lance as peças usadas.",
                    "Ao terminar: finalize / entregue conforme o fluxo da tela."
                },
                "Sem responsável = OS órfã no Kanban.",
                "Finalizou sem peça: lance a peça e ajuste estoque com o gerente se necessário.",
                "os");

            AddStep(t, "venda-pdv", "Vender no balcão (PDV)",
                "Venda rápida com baixa de estoque e registro de dinheiro.",
                new[]
                {
                    "Menu lateral → “PDV”.",
                    "Se aparecer “Abrir caixa”, clique e informe o valor inicial da gaveta (ex.: 100,00). Confirme.",
                    "Busque o produto na tela do PDV e adicione ao carrinho.",
                    "Escolha a forma de pagamento (Dinheiro, PIX, Cartão — conforme botões).",
                    "Clique em finalizar/receber e confirme.",
                    "No fim do dia: use o botão de fechar caixa e confira se bate com a gaveta."
                },
                "Sem passar no PDV, o estoque não baixa e o dinheiro some do relatório.",
                "Vendeu errado: cancele/estorne com o gerente e confira o caixa.",
                "pdv");

            AddStep(t, "gerar-relatorio", "Gerar relatório",
                "Ver números do negócio em PDF/Excel.",
                new[]
                {
                    "Menu lateral → “Relatórios”.",
                    "Escolha o tipo de relatório na tela.",
                    "Marque o período (data inicial e final).",
                    "Clique em Gerar / Atualizar.",
                    "Use Exportar PDF ou Excel quando os botões estiverem disponíveis."
                },
                "Período errado = conclusão errada.",
                "Gerou com data errada? Gere de novo com a data certa.",
                "relatorios");

            AddStep(t, "passo-ia-diagnostico", "Diagnosticar com Copilot IA",
                "Utilizar a inteligência artificial especializada para correlacionar falhas elétricas, DTCs e esquemas.",
                new[]
                {
                    "Acesse o menu lateral e clique em “Copilot IA” (ou utilize o atalho de diagnóstico na própria Ordem de Serviço).",
                    "No campo de pesquisa diagnóstica, digite o código de falha DTC lido no scanner (ex.: P0335) e o modelo do veículo.",
                    "O motor Dual-Engine analisa a base RAG automotiva e exibe a descrição técnica, causas prováveis e sintomas associados.",
                    "Consulte o roteiro de testes passo a passo: pinagem da ECU, tensões de referência com multímetro e teste de osciloscópio.",
                    "Execute os testes no veículo e copie o parecer técnico do Copilot diretamente para o laudo da OS na aba de Auto Elétrica Técnica."
                },
                "O Copilot IA guia o teste determinístico, mas a validação física com pontas de prova e multímetro é essencial.",
                "Se o código informado não trouxer resultado: verifique a grafia do DTC (ex.: letra P seguida de 4 dígitos) e confirme o ano/modelo.",
                "ia");

            AddStep(t, "passo-ferramentas-emprestimo", "Empréstimo e devolução de ferramentas",
                "Controlar a custódia de instrumentos caros (scanners, osciloscópios) e evitar esquecimento no veículo.",
                new[]
                {
                    "No menu lateral, selecione o módulo “Ferramentaria 4.0”.",
                    "Clique no botão “Check-Out (Empréstimo)” na barra superior.",
                    "Selecione o técnico responsável e bipe a etiqueta térmica ZPL ou escolha a ferramenta na lista.",
                    "Confirme o empréstimo. O status do equipamento muda imediatamente para “Emprestado” associado ao colaborador.",
                    "Na devolução: clique em “Check-In (Devolução)”, inspecione o estado dos cabos e acessórios e confirme o recebimento.",
                    "Antes de entregar qualquer veículo concluído, certifique-se de que nenhuma ferramenta permaneceu pendente na OS."
                },
                "Equipamentos sob custódia formal reduzem o extravio a zero e aumentam a vida útil dos aparelhos de precisão.",
                "Se devolver com cabo rompido ou avaria: registre o incidente no sistema e encaminhe imediatamente para manutenção interna.",
                "ferramentas");

            AddStep(t, "passo-compras-pedido", "Gerar pedido de compra anti-ruptura",
                "Comprar peças no momento certo com base no Ponto de Pedido (ROP) e cotação de múltiplos fornecedores.",
                new[]
                {
                    "Acesse o menu lateral e clique em “Compras”.",
                    "Consulte a aba “Alertas de Ruptura” para visualizar os produtos cujo saldo físico atingiu o Ponto de Pedido (ROP).",
                    "Selecione os itens críticos e clique em “Criar Pedido de Cotação”.",
                    "Insira os valores cotados com até 3 fornecedores para comparar automaticamente a melhor oferta de preço e prazo.",
                    "Gere a Ordem de Compra (OC) final, confirme a condição de pagamento e exporte o pedido para o fornecedor parceiro."
                },
                "O ROP (Ponto de Pedido) calcula o momento exato de comprar considerando seu consumo diário e o lead time de entrega.",
                "Comprou sem pedido formal: cadastre a OC retroativa para que a importação do XML da NF-e localize o pedido correto.",
                "compras");

            AddStep(t, "passo-transferencia-filial", "Transferir peças entre filiais",
                "Remover peças do estoque de uma unidade e incorporar em outra com rastreabilidade ACID total.",
                new[]
                {
                    "No menu lateral, localize e clique em “Transferências”.",
                    "Clique no botão “Nova Transferência”.",
                    "Selecione a Filial Origem (onde o item está) e a Filial Destino (que vai receber a peça).",
                    "Adicione os produtos e quantidades desejadas.",
                    "Clique em “Despachar (Em Trânsito)”. O sistema reduz o saldo disponível na origem e mantém em status transitório seguro.",
                    "Ao chegar na filial de destino: o responsável confere fisicamente a carga e clica em “Confirmar Recebimento” para creditar o estoque."
                },
                "A transferência ACID garante que nenhuma peça desapareça durante a viagem entre unidades da mesma rede.",
                "Se enviou quantidade errada: recuse a transferência na tela de recebimento e realize o estorno com a gerência.",
                "transferencias");

            AddStep(t, "passo-frotas-faturamento", "Faturar contrato de frota B2B",
                "Consolidar serviços prestados a veículos de uma empresa parceira e gerar a fatura unificada com relatório.",
                new[]
                {
                    "Acesse o menu lateral e clique em “Gestão de Frotas”.",
                    "Selecione a empresa frotista na lista de contratos corporativos ativos.",
                    "Defina o período de corte (ex.: primeira quinzena do mês) e clique em “Buscar Ordens Concluídas”.",
                    "Revise as OS da frota incluídas no lote com placas, serviços executados e quilometragem.",
                    "Clique em “Gerar Faturamento Consolidado”. O sistema emite o lote e gera os títulos a receber no Financeiro.",
                    "Exporte o Romaneio Analítico em PDF/Excel com o detalhamento por veículo e envie ao departamento financeiro do cliente."
                },
                "Faturamentos consolidados transparentes evitam glosas e aceleram a aprovação de pagamento pelos gestores de frota.",
                "Se uma OS da lista foi incluída indevidamente: desmarque o item antes de clicar em gerar a fatura consolidada.",
                "frotas");

            AddStep(t, "passo-fiscal-emissao", "Emitir NFS-e e NFC-e Fiscal",
                "Transmitir notas fiscais de serviço e venda diretamente via Gateway Fiscal integrado.",
                new[]
                {
                    "Acesse “Central Fiscal” no menu lateral (ou acione os botões de emissão na própria OS finalizada ou tela do PDV).",
                    "Para Serviços de Oficina: selecione a Ordem de Serviço concluída e clique em “Emitir NFS-e (Mão de Obra)”.",
                    "Para Venda de Peças: no fechamento do PDV ou OS com peças, clique em “Emitir NFC-e (Balcão)”.",
                    "O sistema valida dados cadastrais, CNPJ/CPF do cliente e alíquotas tributárias (Simples Nacional ou Lucro Presumido).",
                    "Aguarde o retorno da autorização da Prefeitura ou SEFAZ e imprima o DANFE ou envie o XML/PDF por e-mail."
                },
                "A emissão de NFS-e de mão de obra e NFC-e de peças atende integralmente à legislação tributária brasileira.",
                "Se a nota for rejeitada pela SEFAZ: confira a mensagem de retorno (ex.: CEP divergente ou NCM inválido), corrija e retransmita.",
                "fiscal");

            AddStep(t, "passo-etiquetas-zpl", "Imprimir etiquetas térmicas ZPL II",
                "Gerar etiquetas adesivas industriais para identificação de chaves de carros, prateleiras e ferramentas.",
                new[]
                {
                    "Acesse o módulo “Etiquetas” no menu lateral ou clique no ícone de impressora na tela de Peças, OS ou Ferramentas.",
                    "Selecione o modelo desejado: Etiqueta de Chave (com Placa, OS e Cliente), Etiqueta de Prateleira (com Código de Barras e Local) ou Ferramenta (com QR Code).",
                    "Selecione a impressora térmica configurada (Zebra ZD220, Argox OS-214, Elgin L42 ou driver genérico).",
                    "Defina a quantidade de etiquetas a imprimir e clique em “Imprimir”.",
                    "O sistema envia a sequência de comandos ZPL II nativa para a impressora, garantindo código legível por qualquer scanner."
                },
                "Etiquetas ZPL industriais não desbotam com óleo ou calor e garantem identificação imediata das chaves na recepção.",
                "Se a impressão sair desalinhada: acione o comando de calibração automática de gap de etiqueta na impressora térmica.",
                "etiquetas");

            AddMod(t, "modulo-dashboard", "Dashboard Executivo", "Capa do sistema com indicadores em tempo real.",
                new[] { "Faturamento do dia e do mês", "Ordens de serviço por status", "Orçamentos pendentes de aprovação", "Alertas operacionais críticos" },
                new[] { "Abrir o Dashboard de manhã", "Conferir metas e volume de atendimento", "Clicar em Atualizar se necessário", "Navegar direto para os gargalos" },
                "dashboard");

            AddMod(t, "modulo-agendamentos", "Agendamentos & Boxes", "Gestão de horários e alocação de boxes.",
                new[] { "Horários marcados por box", "Dados do cliente e veículo", "Serviço previsto e técnico responsável", "Status de comparecimento" },
                new[] { "Abrir a agenda do dia", "Marcar novos atendimentos", "Confirmar presença com antecedência", "Converter em OS na chegada" },
                "agendamentos");

            AddMod(t, "modulo-orcamentos", "Orçamentos Comerciais", "Propostas transparentes com peças e serviços.",
                new[] { "Lista de orçamentos com status", "Discriminação detalhada de peças e mão de obra", "Emissão de proposta em PDF e WhatsApp", "Conversão direta em Ordem de Serviço" },
                new[] { "Criar novo orçamento", "Adicionar itens e valor de serviço", "Enviar para aprovação do cliente", "Converter em OS autorizada" },
                "orcamentos");

            AddMod(t, "modulo-os", "Ordens de Serviço (OS)", "Execução técnica e rastreabilidade da oficina.",
                new[] { "Status em tempo real", "Eletricista responsável", "Peças requisitadas e aplicadas", "Laudo técnico e checklist de saída" },
                new[] { "Abrir OS a partir do orçamento", "Atribuir técnico executor", "Apontar peças utilizadas", "Finalizar após validação e teste" },
                "os");

            AddMod(t, "modulo-kanban", "Kanban da Oficina", "Quadro visual do fluxo de veículos no pátio.",
                new[] { "Colunas: Aberta, Diagnóstico, Aguardando Peça, Em Andamento, Concluída", "Cartões de OS com placa e responsável", "Tempo de permanência por etapa" },
                new[] { "Visualizar o pátio num relance", "Arrastar cartões conforme avanço técnico", "Priorizar veículos travados em 'Aguardando Peça'", "Equilibrar a carga entre técnicos" },
                "kanban");

            AddMod(t, "modulo-pdv", "PDV / Frente de Caixa", "Caixa de balcão para venda rápida e recebimento de OS.",
                new[] { "Abertura e fechamento com contagem cega", "Vendas diretas com leitor de código de barras", "Recebimento de OS finalizadas", "Suprimentos e sangrias de segurança" },
                new[] { "Abrir caixa com fundo de troco", "Bipar peças ou buscar por nome", "Receber em Dinheiro, PIX ou Cartão (TEF)", "Fechar turno com conferência física" },
                "pdv");

            AddMod(t, "modulo-nfe", "Importar NF-e de Compras",
                "Entrada automatizada de peças via XML da nota fiscal do fornecedor.",
                new[] { "Seleção e leitura de arquivo XML", "Vínculo inteligente com o código interno do estoque", "Conversão de embalagem para unidades individuais", "Lançamento automático no contas a pagar" },
                new[]
                {
                    "Abrir tela de importação de NF-e",
                    "Selecionar o arquivo XML recebido do fornecedor",
                    "Conferir os vínculos de produtos e quantidades",
                    "Confirmar a importação para creditar o estoque",
                    "NOTA: Para emissão de notas de saída (serviços e vendas), utilize a 'Central Fiscal'."
                },
                "nfe");

            AddMod(t, "modulo-clientes", "Clientes", "Cadastro centralizado de pessoas e empresas.",
                new[] { "Busca instantânea por CPF/CNPJ, nome ou telefone", "Ficha cadastral completa com endereço", "Histórico de veículos, orçamentos e OS vinculados", "Classificação de cliente (particular, corporativo, frotista)" },
                new[] { "Buscar antes de cadastrar para evitar duplicidade", "Cadastrar novos dados e contatos", "Atualizar telefone e WhatsApp", "Consultar histórico de atendimentos" },
                "clientes");

            AddMod(t, "modulo-veiculos", "Veículos", "Cadastro de veículos e histórico técnico da placa.",
                new[] { "Placa com padrão Mercosul e antigo", "Marca, modelo, ano e cor", "Vínculo com o cliente proprietário", "Histórico completo de serviços e KM" },
                new[] { "Cadastrar veículo vinculando ao cliente", "Registrar quilometragem atualizada", "Consultar manutenções elétricas anteriores", "Identificar histórico de garantias" },
                "veiculos");

            AddMod(t, "modulo-autoeletrica", "Auto Elétrica Técnica", "Registros e medições técnicas especializadas.",
                new[] { "Teste de queda de tensão e corrente de partida", "Medição de ripple do alternador e fuga de corrente", "Checklist de chicotes, iluminação e fusíveis", "Laudos periciais e fotos do diagnóstico" },
                new[] { "Abrir a aba técnica a partir da OS", "Registrar as medições com multímetro e osciloscópio", "Anotar o diagnóstico e solução adotada", "Salvar e anexar ao histórico do veículo" },
                null);

            AddMod(t, "modulo-ia", "Copilot de IA Dual-Engine",
                "Inteligência Artificial automotiva para apoio em diagnósticos elétricos complexos.",
                new[] { "Interpretação de códigos de falha DTC (OBD-II)", "Esquemas elétricos, pinouts de módulos e sensores", "Tensões de referência e roteiro de testes guiados", "Diagnóstico de anomalias em alternador e bateria" },
                new[] { "Abrir o Copilot IA", "Digitar o código DTC e sintomas do veículo", "Consultar o roteiro de medições passo a passo", "Copiar laudo técnico gerado para a OS" },
                "ia");

            AddMod(t, "modulo-ferramentas", "Ferramentaria 4.0 & Calibração",
                "Controle de custódia de instrumentos caros, aferição preventiva e anti-extravio.",
                new[] { "Inventário de scanners, osciloscópios, alicates e torquímetros", "Check-Out e Check-In de empréstimo por técnico", "Alertas automáticos de calibração periódica", "Trava de segurança anti-extravio em ordens de serviço" },
                new[] { "Cadastrar ferramenta especial com número de série", "Registrar empréstimo para o técnico responsável", "Acompanhar prazos de aferição de torquímetros", "Conferir devolução antes de entregar o veículo" },
                "ferramentas");

            AddMod(t, "modulo-compras", "Compras & Ponto de Pedido (ROP)",
                "Previsão de demanda, alerta anti-ruptura e gestão comparativa de cotações.",
                new[] { "Alertas de itens que atingiram o Ponto de Pedido (ROP)", "Geração automática de cotações multi-fornecedor", "Mapa comparativo de preços e prazos de entrega", "Emissão e controle de Ordens de Compra (OC)" },
                new[] { "Verificar alertas de ruptura do estoque", "Criar pedido de cotação para fornecedores", "Comparar propostas no mapa de preços", "Aprovar a melhor oferta e emitir a OC" },
                "compras");

            AddMod(t, "modulo-estoque", "Estoque & Curva ABC", "Gestão de saldos, endereçamento físico e valorização.",
                new[] { "Saldos em tempo real e valor total em estoque", "Endereçamento físico (Rua, Prateleira, Gaveta)", "Classificação Curva ABC (itens de alto, médio e baixo giro)", "Histórico de movimentações (entradas, saídas, ajustes)" },
                new[] { "Cadastrar novos produtos com NCM e código de barras", "Localizar peças por endereçamento de prateleira", "Acompanhar alertas de estoque mínimo", "Realizar inventários rotativos periódicos" },
                "estoque");

            AddMod(t, "modulo-transferencias", "Transferências Inter-Filiais",
                "Movimentação segura de peças entre filiais com consistência transacional ACID.",
                new[] { "Solicitações de transferência entre unidades", "Bloqueio automático de saldo em trânsito na origem", "Conferência cega e recebimento na filial de destino", "Rastreabilidade e romaneio de remessa" },
                new[] { "Abrir nova transferência definindo origem e destino", "Adicionar itens e quantidades a remeter", "Confirmar envio para status 'Em Trânsito'", "Efetuar o recebimento físico na filial receptora" },
                "transferencias");

            AddMod(t, "modulo-frotas", "Gestão de Frotas B2B",
                "Contratos corporativos com transportadoras, telemetria de KM e faturamento unificado.",
                new[] { "Cadastro de contratos corporativos e limites de crédito", "Vínculo de placas de frotistas e motoristas autorizados", "Tabela de preços com descontos contratuais automáticos", "Geração de faturamento quinzenal/mensal consolidado com romaneio" },
                new[] { "Cadastrar contrato com a empresa frotista", "Vincular a frota de veículos cadastrados", "Abrir OS registrando quilometragem e motorista", "Gerar lote de faturamento com relatório por veículo" },
                "frotas");

            AddMod(t, "modulo-catalogo", "Catálogo de Peças", "Consulta rápida de aplicações e especificações técnicas.",
                new[] { "Busca ágil por código de fábrica, original ou similar", "Tabela de aplicação por modelo de veículo", "Preço de venda e disponibilidade em estoque" },
                new[] { "Consultar aplicação de relés, lâmpadas, alternadores e baterias", "Verificar itens substitutos e equivalentes", "Adicionar diretamente ao orçamento em aberto" },
                null);

            AddMod(t, "modulo-fornecedores", "Fornecedores", "Gestão de distribuidores e fabricantes de autopeças.",
                new[] { "Cadastro completo com CNPJ, contatos e endereço", "Histórico de compras e notas fiscais importadas", "Avaliação de lead time e pontualidade de entrega" },
                new[] { "Cadastrar novos distribuidores e representantes", "Vincular fornecedor ao XML de notas fiscais", "Consultar histórico de cotações anteriores" },
                null);

            AddMod(t, "modulo-funcionarios", "Funcionários & Equipe", "Gestão de equipe, funções e controle de acesso.",
                new[] { "Cadastro de colaboradores por cargo e especialidade", "Comissões por serviço ou peça vendida", "Vínculo de usuário de sistema e nível de permissão" },
                new[] { "Cadastrar novos funcionários da oficina", "Atribuir como técnico responsável em OS", "Definir metas e acompanhar produtividade individual" },
                null);

            AddMod(t, "modulo-financeiro", "Financeiro & Fluxo de Caixa", "Contas a pagar, a receber e conciliação bancária.",
                new[] { "Contas a receber (clientes, cartões, frotas faturadas)", "Contas a pagar (fornecedores, despesas fixas)", "Projeção de fluxo de caixa (D+30)", "DRE gerencial e margem operacional líquida" },
                new[] { "Registrar baixas de recebimento e pagamento", "Lançar despesas operacionais da oficina", "Conciliar extrato bancário com lançamentos", "Acompanhar a saúde financeira no fluxo projetado" },
                "financeiro");

            AddMod(t, "modulo-fiscal", "Central Fiscal (NFS-e / NFC-e)",
                "Emissão e transmissão direta de notas fiscais eletrônicas de serviço e venda.",
                new[] { "NFS-e de prestação de serviço de mão de obra (Prefeitura)", "NFC-e de balcão e peças para consumidor final (SEFAZ)", "Painel de controle de contingência e autorização", "Armazenamento seguro de XMLs e emissão de DANFE" },
                new[] { "Transmitir NFS-e da OS com 1 clique", "Emitir NFC-e diretamente no encerramento do PDV", "Monitorar retornos da SEFAZ e Prefeitura", "Exportar arquivos fiscais para a contabilidade" },
                "fiscal");

            AddMod(t, "modulo-etiquetas", "Impressão de Etiquetas ZPL II",
                "Geração térmica industrial para chaves, prateleiras e ferramentas.",
                new[] { "Layouts ZPL II nativos de alta velocidade", "Etiqueta adesiva de chave com placa, cliente e número da OS", "Etiquetas de prateleira com código de barras Code 128", "Etiquetas duráveis para ferramentas com QR Code" },
                new[] { "Selecionar o modelo de etiqueta a imprimir", "Definir a impressora térmica conectada", "Ajustar a quantidade de etiquetas", "Enviar a impressão direta sem lentidão" },
                "etiquetas");

            AddMod(t, "modulo-relatorios", "Relatórios & Business Intelligence", "Tomada de decisão baseada em dados concretos.",
                new[] { "Relatórios de faturamento, lucro bruto e margem líquida", "Produtividade por eletricista e tempo médio de box", "Curva ABC de vendas e giro de estoque", "Exportação profissional em PDF e planilhas Excel" },
                new[] { "Escolher o relatório desejado no catálogo", "Filtrar por período, filial ou técnico", "Visualizar os gráficos e indicadores na tela", "Exportar em PDF para reuniões ou contabilidade" },
                "relatorios");

            AddMod(t, "modulo-multifilial", "Multi-Filiais Corporativo",
                "Gestão centralizada de redes de auto elétrica e franquias.",
                new[] { "Cadastro de Matriz e múltiplas Filiais", "Visão DRE consolidada da rede e individual por loja", "Troca rápida de unidade de trabalho ativa", "Políticas e parâmetros corporativos unificados" },
                new[] { "Consultar a unidade em operação", "Gerenciar o cadastro das filiais da rede", "Acompanhar o desempenho comparativo entre unidades", "Padronizar tabelas de preços e regras" },
                "multifilial");

            AddMod(t, "modulo-licenca", "Licença & Conformidade HMAC",
                "Segurança, autenticidade do software e amarração ao Hardware ID.",
                new[] { "Plano contratado e módulos Enterprise ativos", "Identificador único de máquina (Hardware ID SHA-256)", "Chave de conformidade criptográfica HMAC", "Registro de auditoria e conformidade técnica" },
                new[] { "Verificar os dados de homologação da licença", "Consultar o Hardware ID desta estação", "Aplicar a chave comercial de ativação", "Confirmar o status da garantia corporativa" },
                "licenca");

            AddMod(t, "modulo-configuracoes", "Configurações do Sistema", "Parâmetros centrais, usuários e segurança de dados.",
                new[] { "Dados cadastrais da empresa e logo para impressões", "Gestão de usuários, senhas e perfis de permissão", "Rotinas de backup automático e restauração de dados", "Configurações de impressoras térmicas e certificados digitais" },
                new[] { "Preencher dados completos da empresa", "Criar usuário com senha para cada funcionário", "Configurar backup diário em mídia segura", "Personalizar mensagens de orçamento e OS" },
                null);

            // --- Tópicos novos Sanitization / Help Center 1.0 ---
            t["primeiros-10min"] = new HelpTopic
            {
                Title = "Primeiros 10 minutos no PRIMOX",
                Purpose = "Se você acabou de abrir o programa pela primeira vez, faça só isto.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Os 10 passos",
                        Steps = new[]
                        {
                            "Entre com seu usuário e senha na tela de Login.",
                            "Olhe o Dashboard (tela inicial com números).",
                            "No menu lateral esquerdo, role e reconheça: Clientes, Veículos, Orçamentos, OS, Agenda, Estoque, PDV, Financeiro.",
                            "Abra Clientes só para ver a lista e a caixa de pesquisa.",
                            "Abra Veículos e localize a pesquisa de placa.",
                            "Abra Ordens de Serviço e veja a lista (pode estar vazia).",
                            "Abra Agendamentos e veja o calendário — NÃO clique aleatoriamente em todos os dias; use os botões da tela.",
                            "Abra Estoque e veja se há produtos.",
                            "Abra Financeiro e Relatórios só para reconhecer onde ficam.",
                            "Volte à Ajuda (menu Ajuda ou tecla F1) e abra o guia do SEU CARGO."
                        }
                    }
                },
                RelatedTopics = new[] { "comece-aqui", "eu-quero", "glossario" }
            };

            t["eu-quero"] = new HelpTopic
            {
                Title = "Eu quero… (comece aqui se estiver perdido)",
                Purpose = "Escolha o que você precisa fazer agora. Depois clique no atalho correspondente.",
                QuickLinks = new[]
                {
                    "criar-cliente", "registrar-veiculo", "criar-orcamento", "criar-os",
                    "venda-pdv", "usar-financeiro", "gerar-relatorio", "fazer-backup",
                    "problemas-resolver", "modulo-nfe"
                },
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Perguntas rápidas",
                        BulletPoints = new[]
                        {
                            "O cliente chegou? → Clientes / Agenda",
                            "Precisa registrar serviço? → Orçamento → OS",
                            "Precisa vender no balcão? → PDV",
                            "Precisa verificar peça? → Estoque",
                            "Precisa cobrar / pagar? → Financeiro",
                            "Precisa ver desempenho? → Relatórios / Dashboard",
                            "Precisa configurar empresa/usuários/backup? → Configurações",
                            "Está com problema? → Problemas e como resolver"
                        }
                    }
                },
                Tip = "Se ainda estiver perdido: abra “COMECE AQUI” e não pule a ordem.",
                RelatedTopics = new[] { "comece-aqui", "primeiros-10min" }
            };

            t["dia-trabalho"] = new HelpTopic
            {
                Title = "Um dia de trabalho no PRIMOX",
                Purpose = "Visão do dia inteiro — manhã, durante o serviço, final e administrativo.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Manhã",
                        Steps = new[]
                        {
                            "Login → Dashboard",
                            "Agendamentos: quem vem hoje",
                            "Clientes/Veículos: conferir cadastros se necessário",
                            "Check-in / abrir OS conforme a rotina da casa",
                            "Se houver balcão: PDV → Abrir caixa"
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Durante o serviço",
                        Steps = new[]
                        {
                            "OS / Kanban: diagnóstico, execução, peças",
                            "Atualizar status quando a etapa mudar",
                            "Lançar peças usadas na OS"
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Final do atendimento",
                        Steps = new[]
                        {
                            "Finalizar OS / entregar",
                            "Receber (Financeiro ou PDV, conforme o caso)",
                            "Conferir se o cliente saiu com tudo registrado"
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Administrativo / noite",
                        Steps = new[]
                        {
                            "Financeiro: baixar o que recebeu",
                            "Fechar caixa no PDV",
                            "Relatório rápido se for dia de conferência",
                            "Confirmar backup em Configurações"
                        }
                    }
                },
                RelatedTopics = new[] { "rotina-diaria", "fechamento-dia", "cargo-gerente" }
            };


            AddStep(t, "usar-financeiro", "Consultar e baixar no Financeiro",
                "Localizar contas e registrar que o dinheiro foi recebido ou pago.",
                new[]
                {
                    "No menu lateral esquerdo, clique em “Financeiro”.",
                    "Localize as abas ou listas de Contas a Receber e Contas a Pagar.",
                    "Use filtros de período / status (ex.: vencidas) se existirem na tela.",
                    "Selecione a conta na tabela.",
                    "Use o botão de baixa / receber / pagar conforme o rótulo real da tela.",
                    "Confirme o valor e a forma de pagamento (ex.: PIX, Dinheiro).",
                    "Verifique se a conta saiu da lista de abertas ou mudou de status."
                },
                "Baixa = registrar que a conta foi quitada. Não é o mesmo que emitir nota fiscal.",
                "Baixou errado: chame o gerente e corrija dentro do sistema (não apague evidência no caderno).",
                "financeiro");

            AddStep(t, "fazer-backup", "Fazer backup",
                "Criar cópia de segurança dos dados da oficina.",
                new[]
                {
                    "Menu lateral → Configurações (ou tela de Backup, se houver atalho).",
                    "Localize a seção de Backup / Restauração.",
                    "Clique em criar/gerar backup e aguarde a confirmação.",
                    "Anote onde o arquivo foi salvo (pasta indicada na mensagem).",
                    "Nunca apague backups antigos todos de uma vez sem orientação."
                },
                "Backup é cópia de segurança. Sem backup, um HD quebrado pode apagar a oficina inteira.",
                "Se a restauração for necessária: pare, chame o responsável e siga o procedimento da tela — não invente.",
                null);

            t["limites-produto"] = new HelpTopic
            {
                Title = "Capacidades & Escopo PRIMOX Enterprise v2.1",
                Purpose = "Conheça o alcance e os padrões da edição corporativa para oficinas de auto elétrica e centros automotivos de grande porte.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Capacidades Corporativas Incluídas (v2.1 Enterprise)",
                        BulletPoints = new[]
                        {
                            "Multi-Filiais & Unidades de Negócio: gestão centralizada de matriz e filiais, consolidação contábil e usuários por filial.",
                            "Transferências Inter-Lojas ACID: remessa e recebimento de peças entre unidades com bloqueio de saldo em trânsito e rastreabilidade total.",
                            "Central Fiscal Integrada: Gateway Fiscal plugável para emissão de NFS-e (ISSQN de mão de obra) e NFC-e (balcão/peças).",
                            "Copilot de IA Dual-Engine: inteligência artificial para correlação de falhas DTC, diagramas de chicote, pinouts e testes de alternador/bateria com ripple.",
                            "Gestão de Frotas B2B: contratos corporativos, telemetria de KM por placa, tabela de preços diferenciada e faturamento quinzenal/mensal agrupado.",
                            "Ferramentaria 4.0: custódia de instrumentos de precisão (scanners, osciloscópios), calibração periódica e trava anti-extravio no cofre do veículo.",
                            "Compras Anti-Ruptura: cálculo automático de Ponto de Pedido (ROP), Curva ABC e mapa comparativo de cotações com múltiplos fornecedores.",
                            "Impressão Térmica ZPL II: geração direta de etiquetas térmicas industriais (Zebra, Argox, Elgin) para chaves de veículos, prateleiras e ferramentas.",
                            "Licenciamento HMAC SHA-256: ativação segura com amarração ao Hardware ID da estação e trilha de auditoria."
                        },
                        Callouts = new[]
                        {
                            new HelpCallout
                            {
                                Kind = "important",
                                Title = "Arquitetura Soberana (Offline-First)",
                                Message = "O PRIMOX opera em arquitetura Desktop Nativa de alta performance. Todas as operações críticas da oficina (OS, Caixa, Estoque e Diagnóstico) funcionam com velocidade instantânea sem lentidão de internet de oficina, preservando seus dados em ambiente seguro e sob controle total da sua empresa."
                            }
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Fronteiras e Boas Práticas Operacionais",
                        BulletPoints = new[]
                        {
                            "A emissão de NFS-e/NFC-e requer certificado digital válido (A1) e credenciamento ativo na Prefeitura / SEFAZ do seu estado.",
                            "A sincronização multi-filiais requer conectividade de rede local (LAN) ou VPN corporativa segura entre as unidades.",
                            "O link de WhatsApp (wa.me) opera diretamente pelo aplicativo oficial no computador/celular sem custos de intermediários.",
                            "As rotinas de backup corporativo devem ser mantidas ativas e configuradas para exportação diária em mídia externa ou servidor de segurança."
                        }
                    }
                },
                Tip = "O sistema foi projetado para escalar de uma oficina individual até redes com dezenas de filiais e frotas corporativas.",
                RelatedTopics = new[] { "modulo-multifilial", "modulo-fiscal", "modulo-frotas", "modulo-ia", "modulo-ferramentas" }
            };

            t["problemas-resolver"] = new HelpTopic
            {
                Title = "Problemas e como resolver",
                Purpose = "O que fazer quando algo não abre, não aparece ou parece “travado”.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Acesso e telas",
                        ErrorFixes = new[]
                        {
                            E("Não consigo entrar", "Você fica fora do sistema.", "Confira usuário/senha. Caps Lock. Peça reset ao administrador. Veja se o programa é a versão instalada correta."),
                            E("Botão não funciona", "A tarefa não completa.", "Veja se o botão está cinza (sem permissão ou falta preencher campo). Leia a mensagem. Tire print."),
                            E("Tela não abre / janela fechou", "Você perde o caminho.", "Reabra pelo menu lateral. Se repetir: anote o módulo e chame suporte com print."),
                            E("Cliente / veículo / OS / produto não aparece", "Você acha que “sumiu”.", "Use a pesquisa. Limpe filtros. Confira se está inativo. Cadastre de novo só se tiver certeza que não existe.")
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Operação",
                        ErrorFixes = new[]
                        {
                            E("Estoque não atualizou", "Saldo mente.", "Confira se a venda passou no PDV ou se a peça foi lançada na OS."),
                            E("Financeiro não aparece / não baixou", "Caixa mental diferente do sistema.", "Atualize a lista (F5). Confira filtros de data. Refaça a baixa se não gravou."),
                            E("PDF/Excel/impressão não gerou", "Você não consegue entregar documento.", "Confira se o relatório gerou na tela antes de exportar. Veja pasta de Downloads/Documentos. Teste outra impressora."),
                            E("Sistema lento ou fechou", "Perde tempo e confiança.", "Feche outras janelas. Reinicie o PRIMOX. Se repetir: anote horário e o que fazia.")
                        }
                    },
                    new HelpSection
                    {
                        Heading = "Emissão Fiscal e Comunicação",
                        ErrorFixes = new[]
                        {
                            E("Nota fiscal rejeitada na SEFAZ", "Documento fiscal não autoriza.", "Abra a Central Fiscal, leia a mensagem de erro retornada (ex.: NCM inválido, CPF tomador divergente), corrija o cadastro e retransmita."),
                            E("Impressora térmica ZPL não imprime", "Etiquetas de chaves ou peças travadas.", "Verifique se o cabo USB/rede está conectado, se o papel/ribbon está abastecido e execute a calibração de gap da impressora."),
                            E("WhatsApp não abre a conversa", "Falha no disparo ao cliente.", "Verifique se o número do cliente possui DDD e 9 dígitos e se o aplicativo de WhatsApp está instalado no computador ou com WhatsApp Web logado no navegador padrão."),
                            E("Backup / restauração de dados", "Segurança da informação.", "Execute a rotina pelo menu Configurações. Mantenha cópias diárias em disco externo ou nuvem própria de segurança.")
                        }
                    }
                },
                RelatedTopics = new[] { "informar-problema", "erros-evitar", "limites-produto", "suporte" }
            };

            t["informar-problema"] = new HelpTopic
            {
                Title = "Como informar um problema ao suporte",
                Purpose = "Quanto melhor a informação, mais rápido resolve.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Passo a passo",
                        Steps = new[]
                        {
                            "Não feche a mensagem de erro imediatamente.",
                            "Leia o título e o texto completo exibido na tela.",
                            "Tire uma captura de tela (Print Screen ou Win+Shift+S).",
                            "Anote: o que você estava fazendo, qual módulo (OS, PDV, Fiscal, Estoque…) e o horário.",
                            "Diga se o erro se repete ao tentar novamente.",
                            "Informe a versão Enterprise v2.1 e o Hardware ID exibido na tela de Licença.",
                            "Envie para o suporte corporativo com os dados e a captura de tela anexada."
                        }
                    }
                },
                RelatedTopics = new[] { "suporte", "problemas-resolver" }
            };

            t["atalhos-teclado"] = new HelpTopic
            {
                Title = "Atalhos de teclado",
                Purpose = "Atalhos que mais ajudam no dia a dia.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        KeyboardShortcuts = new Dictionary<string, string>
                        {
                            ["F1"] = "Abrir esta Ajuda",
                            ["Ctrl+N"] = "Novo registro",
                            ["Ctrl+S"] = "Salvar alterações",
                            ["F5"] = "Atualizar lista",
                            ["Ctrl+F"] = "Pesquisar na tela",
                            ["Esc"] = "Fechar / cancelar janela",
                            ["Delete"] = "Excluir item selecionado"
                        },
                        Callouts = new[]
                        {
                            new HelpCallout { Kind = "caution", Title = "Delete", Message = "Só aperte Delete se tiver certeza. Leia a pergunta na tela antes de confirmar." }
                        }
                    }
                }
            };

            t["faq"] = new HelpTopic
            {
                Title = "Perguntas frequentes",
                Purpose = "Dúvidas que quase todo mundo tem no dia a dia.",
                Sections = new[]
                {
                    new HelpSection { Heading = "Esqueci a senha de acesso", Content = "Peça a um usuário administrador para redefinir sua senha em Configurações → Usuários." },
                    new HelpSection { Heading = "Posso usar em múltiplos computadores?", Content = "Sim! O PRIMOX Enterprise opera em rede local (LAN) ou VPN corporativa, compartilhando a base de dados de forma segura entre todas as estações da oficina e filiais." },
                    new HelpSection { Heading = "Sumiu uma peça do estoque", Content = "Verifique se a peça foi consumida em alguma Ordem de Serviço ou vendida no PDV. Se houver desvio, consulte o histórico de movimentações em Estoque." },
                    new HelpSection { Heading = "O sistema emite nota fiscal eletrônica?", Content = "Sim! Na Central Fiscal você emite NFS-e para os serviços de mão de obra e NFC-e no PDV para a venda de peças, além de importar os XMLs das notas fiscais dos fornecedores." },
                    new HelpSection { Heading = "Preciso pagar curso ou consultoria para a equipe?", Content = "Não! A Escola PRIMOX incluída nesta Ajuda possui trilhas completas do Caixa ao CEO, com método prático de 3 dias para treinar qualquer colaborador sem custos extras." }
                },
                RelatedTopics = new[] { "comece-aqui", "erros-evitar", "limites-produto", "suporte" }
            };

            t["suporte"] = new HelpTopic
            {
                Title = "Chamar suporte corporativo",
                Purpose = "Quando a documentação da Ajuda não resolver.",
                Sections = new[]
                {
                    new HelpSection { Heading = "Antes de acionar o suporte", BulletPoints = new[] { "Consulte os tópicos 'Problemas e Como Resolver' e 'Erros Comuns'", "Anote a sequência exata de cliques que gerou o comportamento", "Tire uma captura de tela nítida do erro", "Tenha em mãos a versão Enterprise v2.1 e seu Hardware ID" } },
                    new HelpSection { Heading = "Canal de Atendimento", Content = "Utilize o canal oficial de suporte corporativo especificado em seu contrato de licenciamento." },
                    new HelpSection { Heading = "Horário de Cobertura", Content = "Segunda a sexta-feira, em horário comercial, com plantão conforme os termos do plano Enterprise." }
                }
            };

            return t;
        }

        private static HelpTopic Role(string title, string purpose, string[] doThis, string[] neverDo, string tip, string[] related) => new()
        {
            Title = title,
            Purpose = purpose,
            Sections = new[]
            {
                new HelpSection { Heading = "Faça sempre (nesta ordem)", Steps = doThis },
                new HelpSection
                {
                    Heading = "Nunca faça",
                    Callouts = Array.ConvertAll(neverDo, x => new HelpCallout { Kind = "danger", Title = "Proibido na prática", Message = x })
                }
            },
            Tip = tip,
            RelatedTopics = related
        };

        private static void AddStep(Dictionary<string, HelpTopic> t, string key, string title, string purpose,
            string[] steps, string important, string ifWrong, string? shot)
        {
            t[key] = new HelpTopic
            {
                Title = title,
                Purpose = purpose,
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Faça assim (clique junto)",
                        Steps = steps,
                        ScreenshotKey = shot,
                        ScreenshotCaption = shot == null ? null : "Exemplo da tela (ilustração).",
                        Callouts = new[]
                        {
                            new HelpCallout { Kind = "important", Title = "Importante", Message = important },
                            new HelpCallout { Kind = "danger", Title = "Se já errou", Message = ifWrong }
                        }
                    }
                },
                RelatedTopics = new[] { "comece-aqui", "erros-evitar", "glossario" }
            };
        }

        private static void AddMod(Dictionary<string, HelpTopic> t, string key, string title, string purpose,
            string[] finds, string[] steps, string? shot)
        {
            t[key] = new HelpTopic
            {
                Title = title,
                Purpose = purpose,
                Sections = new[]
                {
                    new HelpSection { Heading = "O que você vê nesta tela", BulletPoints = finds },
                    new HelpSection
                    {
                        Heading = "Como usar (simples)",
                        Steps = steps,
                        ScreenshotKey = shot,
                        ScreenshotCaption = shot == null ? null : $"Exemplo — {title}"
                    }
                },
                RelatedTopics = new[] { "mapa-paginas", "glossario" }
            };
        }

        private static HelpSection[] BuildErrorSections() => new[]
        {
            new HelpSection
            {
                Heading = "Cadastro",
                ErrorFixes = new[]
                {
                    E("Cliente duplicado", "Histórico e cobrança se perdem.", "Busque por CPF/telefone. Use só um cadastro. Pare de usar o outro."),
                    E("Placa errada", "Serviço fica no carro “fantasma”.", "Edite a placa agora. Não crie outro veículo."),
                    E("Atendeu sem cadastrar", "Não tem como cobrar/garantir direito.", "Cadastre agora e refaça o orçamento/OS vinculando.")
                }
            },
            new HelpSection
            {
                Heading = "Orçamento e OS",
                ErrorFixes = new[]
                {
                    E("Virou OS sem o cliente aceitar", "Peça e tempo jogados fora.", "Se não começou: cancele a OS. Volte o orçamento para aguardando."),
                    E("OS sem peça lançada", "Estoque e lucro mentem.", "Lance as peças na OS ou faça saída no estoque com o gerente."),
                    E("Status esquecido", "Kanban mente, carro some na fila.", "Atualize o status no momento real.")
                }
            },
            new HelpSection
            {
                Heading = "Caixa e estoque",
                ErrorFixes = new[]
                {
                    E("Vendeu fora do PDV", "Dinheiro sem rastreio e estoque alto demais.", "Refaça no PDV ou dê saída + lançamento no financeiro."),
                    E("Caixa não fechou", "Turno misturado.", "Feche agora com observação. Amanhã abra limpo."),
                    E("NF-e importada 2 vezes", "Estoque inflado.", "Ajuste o saldo com o gerente e não reimporte a mesma nota.")
                }
            },
            new HelpSection
            {
                Heading = "Gente e senha",
                ErrorFixes = new[]
                {
                    E("Todo mundo na mesma senha", "Não dá para saber quem errou.", "Crie um usuário por pessoa hoje."),
                    E("Funcionário saiu e ainda entra", "Risco grave.", "Desative o usuário no mesmo dia em Configurações.")
                },
                Callouts = new[]
                {
                    new HelpCallout
                    {
                        Kind = "danger",
                        Title = "Regra final",
                        Message = "Dinheiro, estoque e exclusão: se errou, corrija DENTRO do sistema. Não “acerta no caderno”."
                    }
                }
            }
        };

        private static HelpErrorFix E(string error, string impact, string solution) => new()
        {
            Error = error, Impact = impact, Solution = solution
        };

        private static HelpPageGuide[] BuildPageGuides() => new[]
        {
            G("Dashboard", "Números do dia e indicadores executivos.", new[] { "Faturamento", "OS abertas", "Orçamentos", "Ticket médio" }, new[] { "Olhe todo dia de manhã." }, new[] { "Número estranho? Clique Atualizar." }, null, "modulo-dashboard"),
            G("Agendamentos", "Agenda de horários e boxes.", new[] { "Dia", "Horário", "Cliente", "Eletricista" }, new[] { "Confirme quem vem com antecedência." }, new[] { "Remarcou? Notifique o cliente via WhatsApp." }, null, "modulo-agendamentos"),
            G("Orçamentos", "Proposta comercial transparente.", new[] { "Itens de peças", "Mão de obra", "Total", "Status" }, new[] { "Envie no mesmo atendimento." }, new[] { "Orçamento não é OS autorizada." }, new[] { "Converter sem aprovação formal." }, "modulo-orcamentos"),
            G("Ordens de Serviço", "Execução técnica autorizada.", new[] { "Status", "Responsável técnico", "Peças aplicadas", "Laudo" }, new[] { "Sempre com responsável técnico definido." }, new[] { "Finalize apenas com peças devidamente apontadas." }, null, "modulo-os"),
            G("Kanban da Oficina", "Quadro visual de produtividade.", new[] { "Colunas de status", "Cartões de OS", "Tempo de box" }, new[] { "Priorize veículos na coluna 'Aguardando Peça'." }, new[] { "Cartão sem eletricista responsável." }, null, "modulo-kanban"),
            G("PDV / Caixa", "Caixa de balcão e recebimentos.", new[] { "Venda balcão", "Recebimento de OS", "Abertura/Fechamento", "Sangrias" }, new[] { "Abrir com suprimento e fechar com contagem cega todo dia." }, new[] { "Conferência atenta de troco e comprovantes." }, new[] { "Encerrar o dia com caixa em aberto." }, "modulo-pdv"),
            G("Clientes / Veículos", "Cadastros base e histórico.", new[] { "Busca ágil", "CPF/CNPJ", "Placa", "Histórico de manutenções" }, new[] { "Buscar sempre antes de criar novo cadastro." }, new[] { "Cadastrar placa com grafia errada." }, new[] { "Duplicar clientes com telefones diferentes." }, "modulo-clientes"),
            G("Copilot de IA", "Diagnóstico automotivo inteligente.", new[] { "DTCs OBD-II", "Pinouts de ECU", "Tensões de referência", "Ripple" }, new[] { "Consulte em falhas intermitentes de injeção e chicote." }, new[] { "Pular verificação física com multímetro." }, null, "modulo-ia"),
            G("Ferramentaria 4.0", "Controle de instrumentos de precisão.", new[] { "Scanners", "Osciloscópios", "Empréstimos", "Calibração" }, new[] { "Realizar check-in antes de liberar o veículo da oficina." }, new[] { "Utilizar aparelho com aferição vencida." }, new[] { "Deixar ferramenta especial no motor do cliente." }, "modulo-ferramentas"),
            G("Compras & ROP", "Gestão anti-ruptura e cotações.", new[] { "Ponto de Pedido ROP", "Cotações multi-fornecedor", "Ordens de Compra" }, new[] { "Compre antes que o saldo atinja o estoque mínimo." }, new[] { "Comprar itens caros sem comparar cotações." }, null, "modulo-compras"),
            G("Estoque & NF-e", "Peças, saldos e entrada de compras.", new[] { "Saldo físico", "Alertas mínimos", "Importação XML", "Endereçamento" }, new[] { "Confira alertas de peças críticas toda manhã." }, new[] { "Ajustar saldo sem conferência de prateleira." }, new[] { "Importar o mesmo XML duas vezes." }, "modulo-estoque"),
            G("Transferências", "Remessa e recebimento entre filiais.", new[] { "Origem e destino", "Itens despachados", "Status em trânsito", "Romaneio" }, new[] { "Confira a carga antes de aceitar o recebimento." }, new[] { "Transferir peças sem romaneio formal." }, null, "modulo-transferencias"),
            G("Gestão de Frotas", "Contratos corporativos e faturamento.", new[] { "Frotistas", "Placas vinculadas", "Telemetria KM", "Fatura agrupada" }, new[] { "Registre a quilometragem em todo atendimento." }, new[] { "Liberar veículo de frota com contrato bloqueado." }, null, "modulo-frotas"),
            G("Central Fiscal", "Emissão de NFS-e e NFC-e.", new[] { "NFS-e de mão de obra", "NFC-e balcão", "DANFE", "Retorno SEFAZ" }, new[] { "Acompanhe rejeições tributárias no mesmo dia." }, new[] { "Acumular notas em contingência sem reprocessar." }, null, "modulo-fiscal"),
            G("Etiquetas ZPL II", "Impressão térmica industrial.", new[] { "Etiquetas de chaves", "Peças", "Ferramentas", "Código de barras" }, new[] { "Bipe as etiquetas para validação imediata no pátio." }, new[] { "Etiquetas ilegíveis por falta de calibração." }, null, "modulo-etiquetas"),
            G("Financeiro & DRE", "Gestão do dinheiro e conciliação.", new[] { "Contas a receber", "Contas a pagar", "Fluxo de caixa D+30", "DRE" }, new[] { "Baixe recebimentos no mesmo dia do crédito bancário." }, new[] { "Baixar duplicata sem comprovante bancário." }, new[] { "Apagar lançamento conciliado sem autorização." }, "modulo-financeiro"),
            G("Multi-Filiais", "Rede de oficinas consolidada.", new[] { "Matriz e Filiais", "Consolidação DRE", "Unidade ativa" }, new[] { "Mantenha regras contábeis padronizadas na rede." }, new[] { "Operar sem sincronização de dados entre lojas." }, null, "modulo-multifilial"),
            G("Licença & Segurança", "Conformidade e integridade HMAC.", new[] { "Licença Enterprise", "Hardware ID", "Chave HMAC" }, new[] { "Mantenha backups diários ativos em local seguro." }, new[] { "Compartilhar chaves de licença em estações não homologadas." }, null, "modulo-licenca"),
            G("Configurações", "Empresa, usuários e parâmetros.", new[] { "Dados cadastrais", "Usuários e permissões", "Backup corporativo" }, new[] { "Crie um usuário individual para cada colaborador." }, new[] { "Compartilhar senha de administrador." }, new[] { "Desligar o backup automático." }, "modulo-configuracoes")
        };

        private static HelpPageGuide G(string name, string summary, string[] find, string[]? imp, string[]? cau, string[]? dan, string related) => new()
        {
            PageName = name, Summary = summary, YouWillFind = find, Important = imp, Cautions = cau, Dangers = dan, RelatedTopicTag = related
        };
    }
}
