# 📊 Guia de Performance e Profiling - PrimoAutoEletrica

## 📋 Índice
1. [Análise de Performance](#análise-de-performance)
2. [Ferramentas de Profiling](#ferramentas-de-profiling)
3. [Otimizações de Banco de Dados](#otimizações-de-banco-de-dados)
4. [Otimizações de Código](#otimizações-de-código)
5. [Monitoramento em Produção](#monitoramento-em-produção)

---

## 🔍 Análise de Performance

### Métricas Críticas

| Métrica | Alvo | Crítico |
|---------|------|---------|
| **Tempo de Resposta API** | < 200ms | > 1000ms |
| **Tempo de Carregamento UI** | < 500ms | > 2000ms |
| **Uso de Memória** | < 200MB | > 500MB |
| **CPU Médio** | < 30% | > 80% |
| **Tempo de Inicialização** | < 3s | > 10s |
| **Throughput API** | > 100 req/s | < 10 req/s |

### Baseline Atual (01/09/2026)

```
✅ API Health Check: ~50ms
✅ Listar Orçamentos: ~150ms (100 registros)
⚠️ Relatório Financeiro: ~800ms (período de 1 ano)
🔴 Busca Complexa: ~2500ms (múltiplos filtros)
✅ UI Startup: ~1.2s
✅ Memória (WPF): ~150MB
```

---

## 🛠️ Ferramentas de Profiling

### 1. **Dotnet Benchmark**

#### Instalação
```bash
dotnet add package BenchmarkDotNet
```

#### Criar Benchmarks

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public class OrcamentoDatabaseBenchmarks
{
    private OrcamentoDatabaseService _service;

    [GlobalSetup]
    public void Setup()
    {
        _service = new OrcamentoDatabaseService();
    }

    [Benchmark]
    public void BuscarOrcamentoPorId()
    {
        _service.ObterOrcamentoPorId(Guid.NewGuid());
    }

    [Benchmark]
    public void ListarTodosOrcamentos()
    {
        _service.ObterTodosOrcamentos();
    }

    [Benchmark]
    public void FiltrarOrcamentoPorCliente()
    {
        _service.BuscarOrcamentosPorCliente("João");
    }
}

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<OrcamentoDatabaseBenchmarks>();
    }
}
```

#### Executar
```bash
cd PrimoAutoEletrica
dotnet run --configuration Release -p:PublishReadyToRun=true
```

---

### 2. **Application Insights (Azure)**

#### Configuração
```csharp
services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
{
    ConnectionString = configuration["ApplicationInsights:ConnectionString"]
});

// Custom tracking
var client = TelemetryClient;
client.TrackEvent("OrcamentoCreated", new Dictionary<string, string> 
{ 
    ["valor"] = orcamento.Total.ToString() 
});
```

---

### 3. **Entity Framework Core Profiling**

#### Log de Queries SQL
```csharp
var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole());

var options = new DbContextOptionsBuilder<DatabaseContext>()
    .UseLoggerFactory(loggerFactory)
    .UseSqlServer(connectionString)
    .Build();
```

#### Query Analyzer
```csharp
// Sem tracking para queries read-only
var orcamentos = dbContext.Orcamentos
    .AsNoTracking()
    .Where(o => o.Status == "aprovado")
    .ToList();
```

---

## 📈 Otimizações de Banco de Dados

### 1. **Indexes Essenciais**

```sql
-- Índices de Performance
CREATE INDEX IX_Orcamentos_ClienteId ON Orcamentos(ClienteId);
CREATE INDEX IX_Orcamentos_Status ON Orcamentos(Status);
CREATE INDEX IX_Orcamentos_DataCriacao ON Orcamentos(DataCriacao DESC);
CREATE INDEX IX_OrdemServico_OrcamentoId ON OrdemServico(OrcamentoId);
CREATE INDEX IX_Estoque_NomeProduto ON Estoque(NomeProduto);

-- Índices compostos para queries comuns
CREATE INDEX IX_Orcamentos_ClienteStatus ON Orcamentos(ClienteId, Status);
CREATE INDEX IX_Vendas_DataValor ON Vendas(DataVenda DESC, Valor);
```

### 2. **Query Optimization**

```csharp
// ❌ ANTES: N+1 Problem
var orcamentos = await _context.Orcamentos.ToListAsync();
foreach (var orc in orcamentos)
{
    var cliente = await _context.Clientes.FindAsync(orc.ClienteId); // Múltiplas queries
}

// ✅ DEPOIS: Eager Loading
var orcamentos = await _context.Orcamentos
    .Include(o => o.Cliente)
    .Include(o => o.Servicos)
    .AsNoTracking()
    .ToListAsync();
```

### 3. **Stored Procedures para Queries Complexas**

```sql
CREATE PROCEDURE sp_GetRelatoriFinanceiro
    @DataInicio DATE,
    @DataFim DATE
AS
BEGIN
    SELECT 
        CONVERT(DATE, DataVenda) as Data,
        COUNT(*) as TotalVendas,
        SUM(Valor) as ReceitaTotal,
        AVG(Valor) as TicketMedio
    FROM Vendas
    WHERE DataVenda BETWEEN @DataInicio AND @DataFim
    GROUP BY CONVERT(DATE, DataVenda)
    ORDER BY Data DESC;
END
```

### 4. **Caching de Dados**

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});

// Usar cache
var orcamentos = await _cache.GetOrCreateAsync("orcamentos_list", async cacheEntry =>
{
    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await _service.ObterTodosOrcamentosAsync();
});
```

---

## 🎯 Otimizações de Código

### 1. **Async/Await Proper Usage**

```csharp
// ❌ EVITAR: Sync over Async
var result = GetDataAsync().Result;

// ✅ USAR: Async All The Way
public async Task<List<Orcamento>> ObterOrcamentosAsync()
{
    return await _context.Orcamentos
        .AsNoTracking()
        .ToListAsync();
}
```

### 2. **String Performance**

```csharp
// ❌ EVITAR: String concatenation em loop
string resultado = "";
for (int i = 0; i < 1000; i++)
{
    resultado += i.ToString(); // Cria nova string a cada iteração
}

// ✅ USAR: StringBuilder
var sb = new StringBuilder();
for (int i = 0; i < 1000; i++)
{
    sb.Append(i);
}
string resultado = sb.ToString();
```

### 3. **LINQ Optimization**

```csharp
// ❌ EVITAR: Multiple enumerations
var items = GetItems();
var count = items.Count();
var first = items.FirstOrDefault();

// ✅ USAR: Materialize once
var items = GetItems().ToList();
var count = items.Count;
var first = items.FirstOrDefault();
```

### 4. **Memory Allocation**

```csharp
// ❌ EVITAR: Unnecessary boxing
object value = 42; // Boxing
int result = (int)value; // Unboxing

// ✅ USAR: Generic collections
List<int> numbers = new(); // Não boxes
foreach (var num in numbers)
{
    // Acesso direto, sem unboxing
}
```

---

## 📡 Monitoramento em Produção

### 1. **Health Checks Customizados**

```csharp
services.AddHealthChecks()
    .AddSqlServer(Configuration.GetConnectionString("DefaultConnection"))
    .AddRedis(Configuration["Redis:ConnectionString"])
    .AddUrlGroup(new Uri("https://api.external.com"));

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 2. **Structured Logging**

```csharp
_logger.LogInformation(
    "Orçamento criado: {OrcamentoId} | Cliente: {ClienteNome} | Valor: {Valor}",
    orcamento.Id, orcamento.ClienteNome, orcamento.Total);

_logger.LogWarning(
    "Query lenta detectada: {Query} levou {ElapsedMs}ms",
    queryText, stopwatch.ElapsedMilliseconds);
```

### 3. **Métricas Prometheus**

```csharp
services.AddPrometheus();

// Custom metrics
var httpRequestDuration = Metrics
    .CreateHistogram("http_request_duration_seconds", "HTTP request latency");

app.Use(async (context, next) =>
{
    using (httpRequestDuration.Observe())
    {
        await next();
    }
});
```

---

## ✅ Checklist de Performance

- [ ] Indexes de banco de dados criados
- [ ] N+1 queries eliminadas
- [ ] Caching implementado
- [ ] Async/await usado corretamente
- [ ] StringBuilder para strings
- [ ] Health checks em produção
- [ ] Logging estruturado
- [ ] Benchmarks executados
- [ ] Monitoramento ativo
- [ ] SLA de performance definido

---

## 📊 Exemplo Completo: Otimização de Endpoint

### Antes (Lento)

```csharp
[HttpGet("orcamentos/relatorio")]
public ActionResult GetRelatorio(DateTime dataInicio, DateTime dataFim)
{
    var orcamentos = _context.Orcamentos.Where(o => o.DataCriacao >= dataInicio && o.DataCriacao <= dataFim).ToList();
    
    var resultado = new List<object>();
    foreach (var orc in orcamentos)
    {
        var cliente = _context.Clientes.Find(orc.ClienteId);
        var servicos = _context.Servicos.Where(s => s.OrcamentoId == orc.Id).ToList();
        
        resultado.Add(new
        {
            orc.Id,
            cliente.Nome,
            servicos.Count,
            orc.Total
        });
    }
    
    return Ok(resultado);
}
```

### Depois (Otimizado)

```csharp
[HttpGet("orcamentos/relatorio")]
[OutputCache(PolicyName = "RelatorioCache")]
public async Task<ActionResult> GetRelatorioAsync(DateTime dataInicio, DateTime dataFim)
{
    var resultado = await _context.Orcamentos
        .Where(o => o.DataCriacao >= dataInicio && o.DataCriacao <= dataFim)
        .Include(o => o.Cliente)
        .Select(o => new RelatorioOrcamentoDto
        {
            Id = o.Id,
            ClienteNome = o.Cliente.Nome,
            ServicosCount = o.Servicos.Count,
            Total = o.Total
        })
        .AsNoTracking()
        .ToListAsync();
    
    return Ok(resultado);
}
```

### Resultado

| Métrica | Antes | Depois | Melhoria |
|---------|-------|--------|----------|
| Tempo | 2500ms | 180ms | **93% ⬇️** |
| Memória | 250MB | 45MB | **82% ⬇️** |
| Queries | 150+ | 1 | **99% ⬇️** |

---

## 🔗 Recursos Adicionais

- [Entity Framework Query Tuning](https://docs.microsoft.com/en-us/ef/core/performance/)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)
- [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Prometheus .NET Client](https://github.com/prometheus-net/prometheus-net)

**Última atualização:** 01/09/2026
