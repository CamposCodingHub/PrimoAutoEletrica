namespace PrimoAutoEletrica.Models.Common
{
    /// <summary>
    /// Proveniência e classificação de fontes de conhecimento no PRIMOX.
    /// Elimina a ambiguidade entre dados reais da oficina, dados sintéticos de laboratório e manuais de fábrica.
    /// </summary>
    public enum KnowledgeSourceKind
    {
        /// <summary>Dados sintéticos gerados para laboratório, testes ou demonstração.</summary>
        Synthetic = 0,

        /// <summary>Conhecimento técnico curado e redigido por especialistas do projeto PRIMOX.</summary>
        CuratedTechnical = 1,

        /// <summary>Literatura técnica de bancada validada e revisada.</summary>
        ValidatedTechnical = 2,

        /// <summary>Caso real solucionado na oficina com Ordem de Serviço vinculada e medições físicas.</summary>
        PRIMOXVerifiedCase = 3,

        /// <summary>Manual ou boletim técnico oficial emitido pela montadora (OEM).</summary>
        OEMLicensed = 4,

        /// <summary>Relato empírico informado pelo técnico sem medição anexada.</summary>
        UserReported = 5,

        /// <summary>Hipótese dedutiva gerada em tempo de execução por modelo generativo (IA).</summary>
        AIHypothesis = 6
    }
}
