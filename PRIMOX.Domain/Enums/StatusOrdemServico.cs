namespace PRIMOX.Domain.Enums
{
    /// <summary>
    /// Estados do ciclo de vida da Ordem de Serviço no PRIMOX 3.0.
    /// </summary>
    public enum StatusOrdemServico
    {
        Aberta = 1,              // Recepcionada na portaria/balcão
        EmTriagemDvi = 2,        // Inspeção visual / DVI preliminar
        EmDiagnostico = 3,       // Investigação elétrica / DTC / testes
        OrcamentoGerado = 4,     // Peças e serviços precificados, aguardando aprovação
        Aprovada = 5,            // Aprovada pelo cliente
        AguardandoPeca = 6,      // Bloqueio operacional por falta de insumo/peça
        EmExecucao = 7,          // Reparo técnico no box
        ControleQualidade = 8,   // Teste elétrico final e conformidade
        ProntaParaEntrega = 9,   // Veículo liberado para retirada / emissão fiscal
        Finalizada = 10,         // Entregue e faturado
        Cancelada = 11,          // Cancelamento definitivo (estado terminal)
        Rejeitada = 12,          // Orçamento reprovado pelo cliente (estado terminal)
        RetornoGarantia = 13     // Retorno por garantia após finalização
    }

    /// <summary>
    /// Nível de prioridade de atendimento da Ordem de Serviço.
    /// </summary>
    public enum PrioridadeOS
    {
        Baixa = 1,
        Normal = 2,
        Alta = 3,
        Urgente = 4
    }
}
