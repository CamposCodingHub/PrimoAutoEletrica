using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Utilities
{
    /// <summary>
    /// Utilitário para análise e limpeza de código morto e arquivos não referenciados
    /// Executa em modo de auditoria (nunca deleta sem confirmação)
    /// </summary>
    public class CodeAuditService
    {
        private readonly string _projectPath;
        private readonly List<string> _auditLog;
        private readonly List<string> _filesNotReferenced;

        public CodeAuditService(string projectPath)
        {
            _projectPath = projectPath;
            _auditLog = new List<string>();
            _filesNotReferenced = new List<string>();
        }

        /// <summary>
        /// Executa auditoria completa do projeto
        /// </summary>
        public async Task<CodeAuditReport> RunAuditAsync()
        {
            var report = new CodeAuditReport
            {
                ExecutionTime = DateTime.Now,
                TotalFiles = 0,
                UnusedFiles = new List<string>(),
                UnusedClasses = new List<string>(),
                UnusedMethods = new List<string>(),
                EmptyFiles = new List<string>(),
                DuplicateClasses = new List<string>()
            };

            try
            {
                // Varrer todos os arquivos C#
                var csFiles = Directory.GetFiles(_projectPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                    .ToList();

                report.TotalFiles = csFiles.Count;
                _auditLog.Add($"Total de arquivos C# encontrados: {csFiles.Count}");

                // Análise 1: Arquivos vazios ou praticamente vazios
                AnalyzeEmptyFiles(csFiles, report);

                // Análise 2: Namespaces/Classes não utilizados
                await AnalyzeUnusedTypesAsync(csFiles, report);

                // Análise 3: Métodos private não chamados
                AnalyzeUnusedPrivateMethods(csFiles, report);

                // Análise 4: Classes duplicadas ou muito semelhantes
                AnalyzeDuplicateClasses(csFiles, report);

                // Análise 5: Usando statements não utilizados
                AnalyzeUnusedUsingStatements(csFiles, report);

                report.SuccessMessage = "Auditoria concluída com sucesso";
                report.AuditLog = _auditLog;

                return report;
            }
            catch (Exception ex)
            {
                report.ErrorMessage = $"Erro durante auditoria: {ex.Message}";
                _auditLog.Add($"ERRO: {ex.Message}");
                return report;
            }
        }

        /// <summary>
        /// Identifica arquivos vazios ou com apenas comentários
        /// </summary>
        private void AnalyzeEmptyFiles(List<string> files, CodeAuditReport report)
        {
            _auditLog.Add("\n=== ANÁLISE 1: Arquivos Vazios ===");

            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    var codeLines = lines
                        .Where(l => !string.IsNullOrWhiteSpace(l) 
                            && !l.Trim().StartsWith("//") 
                            && !l.Trim().StartsWith("/*")
                            && !l.Trim().StartsWith("*"))
                        .Count();

                    if (codeLines < 5)
                    {
                        report.EmptyFiles.Add(file);
                        _auditLog.Add($"Arquivo vazio (apenas {codeLines} linhas de código): {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    _auditLog.Add($"Erro ao ler arquivo {file}: {ex.Message}");
                }
            }

            _auditLog.Add($"Total de arquivos vazios: {report.EmptyFiles.Count}");
        }

        /// <summary>
        /// Analisa tipos (classes, interfaces) não utilizados
        /// </summary>
        private async Task AnalyzeUnusedTypesAsync(List<string> files, CodeAuditReport report)
        {
            _auditLog.Add("\n=== ANÁLISE 2: Tipos Não Utilizados ===");

            var allContent = await Task.Run(() => 
                string.Join("\n", files.Select(f => File.ReadAllText(f))));

            var classPattern = new Regex(@"(?:public|private|internal)?\s+(?:partial\s+)?class\s+(\w+)");
            var matches = classPattern.Matches(allContent);

            foreach (Match match in matches)
            {
                var className = match.Groups[1].Value;
                var usageCount = Regex.Matches(allContent, $@"\b{className}\b").Count;

                // Se a classe aparece apenas 1 vez (sua definição), pode estar não utilizada
                if (usageCount <= 1 && !IsBaseClassOrInterface(className, allContent))
                {
                    report.UnusedClasses.Add(className);
                    _auditLog.Add($"Classe possível não utilizada: {className} (referências: {usageCount})");
                }
            }

            _auditLog.Add($"Total de classes potencialmente não utilizadas: {report.UnusedClasses.Count}");
        }

        /// <summary>
        /// Analisa métodos private que nunca são chamados
        /// </summary>
        private void AnalyzeUnusedPrivateMethods(List<string> files, CodeAuditReport report)
        {
            _auditLog.Add("\n=== ANÁLISE 3: Métodos Private Não Chamados ===");

            var allContent = string.Join("\n", files.Select(f => File.ReadAllText(f)));

            // Padrão: private [tipo] NomeMetodo(
            var privateMethodPattern = new Regex(@"private\s+\w+\s+(\w+)\s*\(");
            var matches = privateMethodPattern.Matches(allContent);

            var unusedCount = 0;
            foreach (Match match in matches)
            {
                var methodName = match.Groups[1].Value;
                
                // Contar referências (mais de 1 já é a definição)
                var usageCount = Regex.Matches(allContent, $@"\b{methodName}\s*\(").Count;

                if (usageCount == 1) // Apenas a definição
                {
                    report.UnusedMethods.Add($"{methodName}()");
                    unusedCount++;

                    if (unusedCount <= 10) // Mostrar apenas os primeiros 10
                        _auditLog.Add($"Método private nunca chamado: {methodName}()");
                }
            }

            _auditLog.Add($"Total de métodos private potencialmente não utilizados: {unusedCount}");
        }

        /// <summary>
        /// Identifica classes duplicadas ou muito semelhantes
        /// </summary>
        private void AnalyzeDuplicateClasses(List<string> files, CodeAuditReport report)
        {
            _auditLog.Add("\n=== ANÁLISE 4: Classes Duplicadas/Semelhantes ===");

            var classes = new Dictionary<string, List<string>>();

            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var classPattern = new Regex(@"(?:public|private|internal)?\s+(?:partial\s+)?class\s+(\w+)");
                    
                    foreach (Match match in classPattern.Matches(content))
                    {
                        var className = match.Groups[1].Value;
                        if (!classes.ContainsKey(className))
                            classes[className] = new List<string>();
                        classes[className].Add(file);
                    }
                }
                catch (Exception ex)
                {
                    _auditLog.Add($"Erro ao analisar {file}: {ex.Message}");
                }
            }

            var duplicates = classes.Where(c => c.Value.Count > 1).ToList();
            
            foreach (var dup in duplicates)
            {
                report.DuplicateClasses.Add($"{dup.Key} (em {dup.Value.Count} arquivos)");
                _auditLog.Add($"Classe duplicada: {dup.Key} encontrada em {dup.Value.Count} arquivos");
                foreach (var file in dup.Value)
                    _auditLog.Add($"  - {Path.GetFileName(file)}");
            }

            _auditLog.Add($"Total de classes duplicadas: {duplicates.Count}");
        }

        /// <summary>
        /// Encontra using statements não utilizados
        /// </summary>
        private void AnalyzeUnusedUsingStatements(List<string> files, CodeAuditReport report)
        {
            _auditLog.Add("\n=== ANÁLISE 5: Using Statements Não Utilizados ===");

            var unusedUsings = 0;

            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var usingPattern = new Regex(@"using\s+([\w.]+);");
                    
                    foreach (Match match in usingPattern.Matches(content))
                    {
                        var namespaceName = match.Groups[1].Value;
                        var shortName = namespaceName.Split('.').Last();
                        
                        // Verificar se a classe/namespace é utilizado
                        var usagePattern = new Regex($@"\b{shortName}\b");
                        var usageMatches = usagePattern.Matches(content);
                        
                        if (usageMatches.Count <= 1) // Apenas o próprio using
                        {
                            if (unusedUsings < 5)
                                _auditLog.Add($"Using não utilizado em {Path.GetFileName(file)}: {namespaceName}");
                            unusedUsings++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _auditLog.Add($"Erro ao analisar {file}: {ex.Message}");
                }
            }

            _auditLog.Add($"Total de using statements não utilizados: {unusedUsings}");
        }

        private bool IsBaseClassOrInterface(string typeName, string content)
        {
            // Verifica se é herdado por outras classes
            return Regex.IsMatch(content, $@":\s*{typeName}");
        }
    }

    /// <summary>
    /// Relatório de auditoria de código morto
    /// </summary>
    public class CodeAuditReport
    {
        public DateTime ExecutionTime { get; set; }
        public int TotalFiles { get; set; }
        public List<string> UnusedFiles { get; set; } = new();
        public List<string> UnusedClasses { get; set; } = new();
        public List<string> UnusedMethods { get; set; } = new();
        public List<string> EmptyFiles { get; set; } = new();
        public List<string> DuplicateClasses { get; set; } = new();
        public List<string> AuditLog { get; set; } = new();
        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("╔════════════════════════════════════════════╗");
            sb.AppendLine("║     RELATÓRIO DE AUDITORIA DE CÓDIGO       ║");
            sb.AppendLine("╚════════════════════════════════════════════╝");
            sb.AppendLine($"\nData: {ExecutionTime:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Total de arquivos analisados: {TotalFiles}");
            sb.AppendLine($"\n📊 RESULTADOS:");
            sb.AppendLine($"  • Arquivos vazios: {EmptyFiles.Count}");
            sb.AppendLine($"  • Classes não utilizadas: {UnusedClasses.Count}");
            sb.AppendLine($"  • Métodos private não chamados: {UnusedMethods.Count}");
            sb.AppendLine($"  • Classes duplicadas: {DuplicateClasses.Count}");
            sb.AppendLine($"\n✅ {SuccessMessage}");

            if (!string.IsNullOrEmpty(ErrorMessage))
                sb.AppendLine($"❌ {ErrorMessage}");

            return sb.ToString();
        }
    }
}
