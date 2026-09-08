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

            t["cargo-gerente"] = Role(
                "Guia do Dono / Gerente",
                "Você manda no sistema: usuários, dinheiro, estoque e se a equipe está usando certo.",
                new[]
                {
                    "Configurações → dados da empresa e backup automático",
                    "Criar 1 usuário por funcionário (caixa, recepção, oficina)",
                    "Todo dia: Dashboard de manhã + Fechamento à noite",
                    "Toda semana: 1 relatório de faturamento",
                    "Treinar a equipe com o tópico “Como treinar sua equipe”"
                },
                new[]
                {
                    "Nunca compartilhe a senha de administrador",
                    "Se alguém sair da empresa: desative o usuário no mesmo dia",
                    "Não deixe caixa aberto de um dia para o outro"
                },
                "Se a equipe “não usa o sistema”, o problema quase sempre é falta de rotina matinal — não falta de inteligência.",
                new[] { "treinar-equipe", "modulo-configuracoes", "rotina-diaria", "proveito-maximo" });

            t["cargo-recepcao"] = Role(
                "Guia da Recepção / Atendimento",
                "Você é a porta de entrada. Se o cadastro vier errado, a oficina inteira sofre.",
                new[]
                {
                    "Cliente chegou → BUSCAR no sistema antes de cadastrar de novo",
                    "Se não existir → cadastrar Cliente",
                    "Cadastrar/atualizar Veículo (placa certa)",
                    "Abrir Orçamento com peças/serviços",
                    "Enviar ou imprimir o orçamento",
                    "Se o cliente aceitar → converter em OS e avisar a oficina"
                },
                new[]
                {
                    "Não invente cliente duplicado (“João”, “Joao”, “JOAO SILVA”)",
                    "Não abra OS sem o cliente aceitar (salvo regra da casa)",
                    "Sempre confirme telefone/WhatsApp"
                },
                "Sua meta: orçamento pronto em poucos minutos, com placa e telefone certos.",
                new[] { "criar-cliente", "registrar-veiculo", "criar-orcamento", "glossario" });

            t["cargo-caixa"] = Role(
                "Guia do Caixa / Balcão (PDV)",
                "Você cuida do dinheiro do balcão e da baixa de peça na hora.",
                new[]
                {
                    "Manhã: PDV → Abrir caixa (conte o troco na mão e digite o valor)",
                    "Venda: buscar produto → quantidade → forma de pagamento → Finalizar",
                    "Não venda “por fora” do sistema",
                    "Noite: fechar caixa e conferir se bateu com o dinheiro da gaveta",
                    "Se não bater: anote e chame o gerente (não “ajuste calado”)"
                },
                new[]
                {
                    "Não feche o caixa com venda suspensa aberta",
                    "Não use o PDV se o caixa não estiver aberto",
                    "Troco errado = prejuízo: confira antes de finalizar"
                },
                "Se vendeu e o estoque não baixou, a venda não passou no PDV. Refaça do jeito certo.",
                new[] { "venda-pdv", "modulo-pdv", "fechamento-dia", "erros-evitar" });

            t["cargo-eletricista"] = Role(
                "Guia do Eletricista / Oficina",
                "Você executa o serviço. O sistema precisa saber em que etapa o carro está.",
                new[]
                {
                    "Olhe o Kanban (ou lista de OS) no começo do dia",
                    "Pegue a OS do seu nome / da sua vez",
                    "Mude o status quando começar (Em andamento)",
                    "Se faltar peça: status Aguardando peça + avise a recepção",
                    "Lance as peças que usou na OS",
                    "Quando terminar: Finalizada / Entregue (conforme a tela)",
                    "Se fizer diagnóstico elétrico: registre em Auto Elétrica Técnica"
                },
                new[]
                {
                    "Não deixe OS “Em andamento” sem dono",
                    "Não finalize sem lançar peça — o estoque mente e o lucro some",
                    "Não faça serviço sem OS (exceto emergência definida pelo gerente)"
                },
                "Kanban não é burocracia: é para ninguém esquecer carro parado sem peça.",
                new[] { "criar-os", "modulo-os", "modulo-kanban", "modulo-autoeletrica" });

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

            AddMod(t, "modulo-dashboard", "Dashboard", "Capa do sistema com números do dia.",
                new[] { "Faturamento", "OS", "Orçamentos" }, new[] { "Abrir", "Olhar", "Atualizar se precisar", "Ir no problema" }, "dashboard");
            AddMod(t, "modulo-agendamentos", "Agendamentos", "Quem vem e a que horas.",
                new[] { "Horários", "Cliente", "Serviço" }, new[] { "Abrir agenda", "Marcar horário", "Confirmar com cliente" }, "agendamentos");
            AddMod(t, "modulo-orcamentos", "Orçamentos", "Propostas comerciais.",
                new[] { "Lista", "Itens", "PDF", "Converter OS" }, new[] { "Criar", "Enviar", "Aprovar", "Converter" }, "orcamentos");
            AddMod(t, "modulo-os", "Ordens de Serviço", "Serviço em execução.",
                new[] { "Status", "Responsável", "Peças" }, new[] { "Abrir", "Atribuir", "Atualizar", "Finalizar" }, "os");
            AddMod(t, "modulo-kanban", "Kanban da Oficina", "Quadro visual das OS.",
                new[] { "Colunas", "Cartões" }, new[] { "Abrir", "Priorizar parado", "Atualizar status" }, "kanban");
            AddMod(t, "modulo-pdv", "PDV", "Caixa do balcão.",
                new[] { "Abrir/fechar caixa", "Venda", "Pagamento" }, new[] { "Abrir caixa", "Vender", "Fechar caixa" }, "pdv");
            AddMod(t, "modulo-nfe", "Importar NF-e",
                "Entrada de peças pela nota XML do FORNECEDOR. Isto NÃO emite nota fiscal de saída.",
                new[] { "Seleção de XML", "Itens da nota", "Histórico de importação", "Confirmação de entrada" },
                new[]
                {
                    "Abra a tela de Importar NF-e pelo menu (quando disponível) ou atalho da oficina.",
                    "Clique para escolher o arquivo XML da nota do fornecedor.",
                    "Confera os itens e o vínculo com produtos do estoque.",
                    "Confirme a importação somente se os dados estiverem corretos.",
                    "ATENÇÃO: emitir NF-e de venda/serviço ainda NÃO está disponível neste PRIMOX 1.0.0."
                },
                "nfe");

            AddMod(t, "modulo-clientes", "Clientes", "Cadastro de quem traz o carro.",
                new[] { "Busca", "Dados", "Histórico" }, new[] { "Buscar", "Cadastrar", "Atualizar" }, "clientes");
            AddMod(t, "modulo-veiculos", "Veículos", "Cadastro das placas.",
                new[] { "Placa", "Modelo", "Cliente" }, new[] { "Cadastrar", "Vincular cliente" }, "veiculos");
            AddMod(t, "modulo-autoeletrica", "Auto Elétrica Técnica", "Anotações técnicas do diagnóstico.",
                new[] { "Medições", "Observações" }, new[] { "Abrir", "Registrar", "Salvar" }, null);
            AddMod(t, "modulo-estoque", "Estoque", "Peças na prateleira.",
                new[] { "Saldos", "Alertas", "Movimentações" }, new[] { "Cadastrar", "Entrar peça", "Olhar mínimo" }, "estoque");
            AddMod(t, "modulo-catalogo", "Catálogo de Peças", "Consulta rápida de peças.",
                new[] { "Busca", "Aplicação" }, new[] { "Buscar", "Usar no orçamento" }, null);
            AddMod(t, "modulo-fornecedores", "Fornecedores", "De quem você compra.",
                new[] { "Cadastro", "Contato" }, new[] { "Cadastrar", "Usar na NF-e" }, null);
            AddMod(t, "modulo-funcionarios", "Funcionários", "Sua equipe no sistema.",
                new[] { "Cadastro", "Função" }, new[] { "Cadastrar", "Atribuir na OS" }, null);
            AddMod(t, "modulo-financeiro", "Financeiro", "Contas a pagar e receber.",
                new[] { "A receber", "A pagar", "Saldo" }, new[] { "Baixar recebimento", "Lançar despesa" }, "financeiro");
            AddMod(t, "modulo-relatorios", "Relatórios", "Números para decidir.",
                new[] { "Tipos", "Período", "PDF/Excel" }, new[] { "Escolher", "Filtrar", "Gerar" }, "relatorios");
            AddMod(t, "modulo-configuracoes", "Configurações", "Coração do sistema.",
                new[] { "Empresa", "Usuários", "Backup", "Impressoras" }, new[] { "Preencher empresa", "Criar usuários", "Ligar backup" }, null);

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

            t["cargo-proprietario"] = Role(
                "Guia do Proprietário",
                "Você cuida do negócio: dinheiro, pessoas, backup e se a equipe usa o sistema.",
                new[]
                {
                    "Dashboard: olhar faturamento e OS abertas",
                    "Financeiro + Relatórios: conferir a saúde do caixa",
                    "Estoque: alertas de peça crítica",
                    "Funcionários: um usuário por pessoa",
                    "Configurações: empresa + backup",
                    "Ajuda → Treinar equipe: ensinar 1 cargo por vez"
                },
                new[]
                {
                    "Não compartilhe a senha de administrador",
                    "Não prometa ao cliente emissão de NF-e se ainda não estiver disponível no seu PRIMOX",
                    "Não ignore backup"
                },
                "Se a equipe não usa o sistema, comece pela Rotina da manhã — não por mais telas.",
                new[] { "cargo-gerente", "treinar-equipe", "limites-produto", "fazer-backup" });

            t["cargo-financeiro"] = Role(
                "Guia do Financeiro",
                "Você confere o que entra e o que sai — e registra as baixas.",
                new[]
                {
                    "Menu lateral → Financeiro",
                    "Filtrar contas a receber vencidas / de hoje",
                    "Selecionar a conta e registrar a baixa (pagamento recebido)",
                    "Conferir contas a pagar e lançar despesas quando for o caso",
                    "No fim do período: Relatórios financeiros / Dashboard"
                },
                new[]
                {
                    "Não “acerte no caderno” fora do sistema",
                    "Não baixe conta sem conferir valor e forma de pagamento",
                    "PIX no sistema é forma de pagamento interna — não é gateway automático de banco"
                },
                "Exemplo: conta venceu ontem → filtre vencidas → abra → baixe → confira se sumiu da lista de abertas.",
                new[] { "usar-financeiro", "modulo-financeiro", "gerar-relatorio", "erros-evitar" });

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
                Title = "O que NÃO está disponível neste PRIMOX 1.0.0",
                Purpose = "Leia isto para não esperar função que ainda não existe. Honestidade evita frustração.",
                Sections = new[]
                {
                    new HelpSection
                    {
                        Heading = "Ainda não disponível",
                        BulletPoints = new[]
                        {
                            "Emitir NF-e / NFC-e / NFS-e (autorização SEFAZ) — só existe IMPORTAÇÃO de NF-e de compra",
                            "WhatsApp Business Cloud / SMS Twilio automático — o que existe é abrir o WhatsApp pelo link wa.me",
                            "E-mail SMTP automático — pode abrir o programa de e-mail do Windows (mailto) quando houver botão",
                            "Várias filiais com estoque/caixa separados — multi-filial ainda não está disponível",
                            "Sincronização na nuvem / SaaS",
                            "Gateway de pagamento / PIX automático do banco"
                        },
                        Callouts = new[]
                        {
                            new HelpCallout
                            {
                                Kind = "caution",
                                Title = "Importante",
                                Message = "Se alguém disser que o sistema “já emite nota” ou “já sincroniza filiais”, peça para abrir esta página. O produto atual é desktop de oficina com as funções documentadas na Ajuda."
                            }
                        }
                    }
                },
                RelatedTopics = new[] { "modulo-nfe", "faq", "comece-aqui" }
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
                        Heading = "Integrações e NF-e",
                        ErrorFixes = new[]
                        {
                            E("WhatsApp “não enviou sozinho”", "Expectativa de API automática.", "No 1.0.0 o envio automático não está configurado. Use o botão que abre o WhatsApp (wa.me) quando existir."),
                            E("E-mail não saiu sozinho", "Mesma expectativa.", "Não há SMTP automático. Use mailto ou envie pelo seu e-mail."),
                            E("Não consigo emitir NF-e", "Função inexistente hoje.", "Só a IMPORTAÇÃO de XML de compra está disponível. Emissão virá em decisão futura."),
                            E("Backup / restauração", "Risco de dados.", "Use a tela de backup. Não apague arquivos de backup. Restauração: com responsável e cópia de segurança prévia.")
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
                            "Leia o título e o texto completo.",
                            "Tire um print da tela (Print Screen ou Ferramenta de Captura).",
                            "Anote: o que você estava fazendo, qual módulo (Clientes, OS, PDV…), horário aproximado.",
                            "Diga se consegue repetir o erro (sim/não).",
                            "Informe a versão do PRIMOX (Sobre / instalador 1.0.0).",
                            "Envie para o suporte com o print anexado."
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
                            ["Ctrl+N"] = "Novo",
                            ["Ctrl+S"] = "Salvar",
                            ["F5"] = "Atualizar lista",
                            ["Ctrl+F"] = "Buscar",
                            ["Esc"] = "Fechar / cancelar",
                            ["Delete"] = "Excluir (com confirmação)"
                        },
                        Callouts = new[]
                        {
                            new HelpCallout { Kind = "caution", Title = "Delete", Message = "Só aperte Delete se tiver certeza. Leia a pergunta na tela." }
                        }
                    }
                }
            };

            t["faq"] = new HelpTopic
            {
                Title = "Perguntas frequentes",
                Purpose = "Dúvidas que quase todo mundo tem na primeira semana.",
                Sections = new[]
                {
                    new HelpSection { Heading = "Esqueci a senha", Content = "Peça ao dono/admin em Configurações → Usuários / Funcionários." },
                    new HelpSection { Heading = "Posso usar em 2 PCs?", Content = "Depende da instalação e do banco. Pergunte ao responsável técnico. Não invente “sincronização na nuvem” se ela não estiver contratada." },
                    new HelpSection { Heading = "Sumiu uma peça do estoque", Content = "Alguém vendeu fora do PDV ou esqueceu de lançar na OS. Veja “Erros comuns”." },
                    new HelpSection { Heading = "O sistema emite nota fiscal?", Content = "Neste 1.0.0: importa NF-e de compra. Não emite NF-e/NFC-e/NFS-e." },
                    new HelpSection { Heading = "Preciso de curso?", Content = "Não. Siga: Comece aqui → Tour 30 min → Guia do cargo → Rotina da manhã." }
                },
                RelatedTopics = new[] { "comece-aqui", "erros-evitar", "limites-produto", "suporte" }
            };

            t["suporte"] = new HelpTopic
            {
                Title = "Chamar suporte",
                Purpose = "Quando a Ajuda não resolver.",
                Sections = new[]
                {
                    new HelpSection { Heading = "Antes de chamar", BulletPoints = new[] { "Leia Problemas e Erros comuns", "Anote o que clicou", "Tire um print", "Diga a versão 1.0.0" } },
                    new HelpSection { Heading = "E-mail", Content = "Use o canal do seu contrato / fornecedor PRIMOX." },
                    new HelpSection { Heading = "Horário", Content = "Segunda a sexta, horário comercial (conforme contrato)." }
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
            G("Dashboard", "Números do dia.", new[] { "Faturamento", "OS", "Orçamentos" }, new[] { "Olhe todo dia de manhã." }, new[] { "Número estranho? Clique Atualizar." }, null, "modulo-dashboard"),
            G("Agendamentos", "Agenda de horários.", new[] { "Dia", "Horário", "Cliente" }, new[] { "Confirme quem vem." }, new[] { "Remarcou? Avisar o cliente." }, null, "modulo-agendamentos"),
            G("Orçamentos", "Proposta de preço.", new[] { "Itens", "Total", "Status" }, new[] { "Envie no mesmo atendimento." }, new[] { "Não é OS ainda." }, new[] { "Converter sem aprovação." }, "modulo-orcamentos"),
            G("Ordens de Serviço", "Serviço autorizado.", new[] { "Status", "Responsável", "Peças" }, new[] { "Sempre com responsável." }, new[] { "Finalize com peças lançadas." }, null, "modulo-os"),
            G("Kanban", "Quadro da oficina.", new[] { "Colunas", "Cartões" }, new[] { "Priorize parado sem peça." }, new[] { "Cartão sem dono." }, null, "modulo-kanban"),
            G("PDV", "Caixa do balcão.", new[] { "Venda", "Pagamento", "Caixa" }, new[] { "Abrir e fechar todo dia." }, new[] { "Troco e forma de pagamento." }, new[] { "Dormir com caixa aberto." }, "modulo-pdv"),
            G("Clientes / Veículos", "Cadastros base.", new[] { "Busca", "Placa", "Histórico" }, new[] { "Buscar antes de criar." }, new[] { "Duplicar cadastro." }, null, "modulo-clientes"),
            G("Estoque / NF-e", "Peças e entrada.", new[] { "Saldo", "Alertas", "XML" }, new[] { "Olhe alertas de manhã." }, new[] { "Ajuste sem contar a prateleira." }, new[] { "Importar XML duas vezes." }, "modulo-estoque"),
            G("Financeiro / Relatórios", "Dinheiro e números.", new[] { "A receber", "A pagar", "PDF" }, new[] { "Baixe recebimento no dia." }, new[] { "Período errado no relatório." }, new[] { "Apagar lançamento quitado sem autorização." }, "modulo-financeiro"),
            G("Configurações", "Empresa, usuários, backup.", new[] { "Usuários", "Backup", "Impressoras" }, new[] { "Backup ligado." }, new[] { "Senha de admin compartilhada." }, new[] { "Desligar backup." }, "modulo-configuracoes")
        };

        private static HelpPageGuide G(string name, string summary, string[] find, string[]? imp, string[]? cau, string[]? dan, string related) => new()
        {
            PageName = name, Summary = summary, YouWillFind = find, Important = imp, Cautions = cau, Dangers = dan, RelatedTopicTag = related
        };
    }
}
