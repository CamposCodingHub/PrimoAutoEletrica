# API PrimoAutoEletrica - Documentação Completa

## 📋 Índice
- [Visão Geral](#visão-geral)
- [Autenticação](#autenticação)
- [Endpoints](#endpoints)
- [Exemplos de Uso](#exemplos-de-uso)
- [Tratamento de Erros](#tratamento-de-erros)
- [Rate Limiting](#rate-limiting)
- [Versionamento](#versionamento)

---

## 🎯 Visão Geral

A **API PrimoAutoEletrica** é um serviço REST que fornece acesso aos dados e funcionalidades da aplicação de gestão de oficina mecânica.

### Base URL
```
https://api.primoautoeletrica.com.br/api
```

### Versão Atual
- **v1.0.0** (Estável)

### Ambiente de Desenvolvimento
```
http://localhost:5000/api
```

---

## 🔐 Autenticação

### Métodos Suportados

#### 1. **JWT Bearer Token** (Recomendado)
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### 2. **API Key** (Deprecated)
```http
X-API-Key: your-api-key-here
```

### Obter Token

**Endpoint:**
```http
POST /auth/login
Content-Type: application/json

{
  "usuario": "admin",
  "senha": "senha123"
}
```

**Resposta (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "usuario": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "nome": "Administrador",
    "email": "admin@primoautoeletrica.com.br",
    "role": "Admin"
  }
}
```

---

## 📚 Endpoints

### Health Check

#### Verificar Status da API
```http
GET /api/health
```

**Resposta (200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2026-09-01T10:30:00Z",
  "version": "1.0.0",
  "message": "API PrimoAutoEletrica funcionando corretamente"
}
```

---

### Orçamentos

#### Listar Todos os Orçamentos
```http
GET /api/orcamentos
Authorization: Bearer {token}
```

**Query Parameters:**
- `skip` (int): Número de registros a pular (padrão: 0)
- `take` (int): Número de registros a retornar (padrão: 10, máx: 100)
- `status` (string): Filtrar por status (em-andamento, aprovado, rejeitado)
- `dataInicio` (date): Filtrar por data de início (yyyy-MM-dd)
- `dataFim` (date): Filtrar por data de fim (yyyy-MM-dd)

**Exemplo:**
```http
GET /api/orcamentos?skip=0&take=20&status=aprovado
```

**Resposta (200 OK):**
```json
{
  "total": 150,
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "cliente": "João Silva",
      "veiculo": "VW Gol 2020",
      "valor": 1500.00,
      "status": "aprovado",
      "dataCreacao": "2026-09-01T10:00:00Z",
      "dataAprovacao": "2026-09-01T11:00:00Z"
    }
  ]
}
```

#### Obter Orçamento por ID
```http
GET /api/orcamentos/{id}
Authorization: Bearer {token}
```

**Parâmetros:**
- `id` (uuid): ID do orçamento

**Resposta (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "cliente": "João Silva",
  "clienteId": "660e8400-e29b-41d4-a716-446655440000",
  "veiculo": "VW Gol 2020",
  "placa": "ABC1234",
  "servicos": [
    {
      "nome": "Revisão Completa",
      "descricao": "Revisão de motor e transmissão",
      "valor": 800.00,
      "quantidade": 1
    }
  ],
  "subtotal": 1500.00,
  "desconto": 0.00,
  "taxas": 50.00,
  "total": 1550.00,
  "status": "aprovado",
  "observacoes": "Cliente deve buscar em 5 dias",
  "dataCriacao": "2026-09-01T10:00:00Z"
}
```

#### Criar Novo Orçamento
```http
POST /api/orcamentos
Authorization: Bearer {token}
Content-Type: application/json

{
  "clienteId": "660e8400-e29b-41d4-a716-446655440000",
  "veiculoId": "770e8400-e29b-41d4-a716-446655440000",
  "servicos": [
    {
      "nome": "Revisão Completa",
      "descricao": "Revisão de motor",
      "valor": 800.00
    }
  ],
  "observacoes": "Cliente é recorrente"
}
```

**Resposta (201 Created):**
```json
{
  "id": "880e8400-e29b-41d4-a716-446655440000",
  "clienteId": "660e8400-e29b-41d4-a716-446655440000",
  "status": "em-andamento",
  "dataCriacao": "2026-09-01T10:30:00Z"
}
```

---

### Ordem de Serviço

#### Listar Ordens de Serviço
```http
GET /api/ordens-servico
Authorization: Bearer {token}
```

**Query Parameters:**
- `skip`, `take`, `status`, `dataInicio`, `dataFim`

#### Obter Ordem de Serviço por ID
```http
GET /api/ordens-servico/{id}
Authorization: Bearer {token}
```

#### Criar Ordem de Serviço
```http
POST /api/ordens-servico
Authorization: Bearer {token}
Content-Type: application/json

{
  "orcamentoId": "550e8400-e29b-41d4-a716-446655440000",
  "mecanico": "Carlos Monteiro",
  "dataInicio": "2026-09-02T08:00:00Z",
  "observacoes": "Colocar aviso de carro na oficina"
}
```

---

### Estoque

#### Listar Produtos
```http
GET /api/estoque/produtos
Authorization: Bearer {token}
```

#### Verificar Disponibilidade
```http
GET /api/estoque/disponibilidade?produtoId={produtoId}&quantidade={quantidade}
Authorization: Bearer {token}
```

**Resposta:**
```json
{
  "produtoId": "990e8400-e29b-41d4-a716-446655440000",
  "disponivel": true,
  "quantidadeEmEstoque": 50,
  "quantidadeSolicitada": 10,
  "sugestaoFornecedor": "Fornecedor X"
}
```

---

### Financeiro

#### Listar Movimentações Financeiras
```http
GET /api/financeiro/movimentacoes
Authorization: Bearer {token}
```

#### Gerar Relatório Financeiro
```http
GET /api/financeiro/relatorio?dataInicio=2026-09-01&dataFim=2026-09-30
Authorization: Bearer {token}
```

**Resposta:**
```json
{
  "periodo": {
    "dataInicio": "2026-09-01",
    "dataFim": "2026-09-30"
  },
  "resumo": {
    "receitaTotal": 50000.00,
    "despesaTotal": 25000.00,
    "lucroLiquido": 25000.00,
    "margem": 50.00
  },
  "detalhes": []
}
```

---

## 💡 Exemplos de Uso

### cURL

#### Obter Token
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"usuario":"admin","senha":"senha123"}'
```

#### Listar Orçamentos
```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
curl -X GET http://localhost:5000/api/orcamentos \
  -H "Authorization: Bearer $TOKEN"
```

### JavaScript/TypeScript

```typescript
// Função para obter token
async function getToken() {
  const response = await fetch('http://localhost:5000/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      usuario: 'admin',
      senha: 'senha123'
    })
  });
  
  const data = await response.json();
  return data.token;
}

// Listar orçamentos
async function listarOrcamentos() {
  const token = await getToken();
  
  const response = await fetch('http://localhost:5000/api/orcamentos', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  return response.json();
}
```

### C# (.NET)

```csharp
// Usando HttpClient
var httpClient = new HttpClient();

// Obter token
var loginResponse = await httpClient.PostAsJsonAsync(
  "http://localhost:5000/api/auth/login",
  new { usuario = "admin", senha = "senha123" }
);

var loginData = await loginResponse.Content.ReadAsAsync<dynamic>();
var token = loginData.token;

// Listar orçamentos
httpClient.DefaultRequestHeaders.Authorization = 
  new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

var response = await httpClient.GetAsync("http://localhost:5000/api/orcamentos");
var orcamentos = await response.Content.ReadAsAsync<List<Orcamento>>();
```

---

## ⚠️ Tratamento de Erros

### Código de Status HTTP

| Código | Descrição |
|--------|-----------|
| 200 | OK - Requisição bem-sucedida |
| 201 | Created - Recurso criado com sucesso |
| 204 | No Content - Sem conteúdo na resposta |
| 400 | Bad Request - Requisição inválida |
| 401 | Unauthorized - Autenticação necessária |
| 403 | Forbidden - Acesso negado |
| 404 | Not Found - Recurso não encontrado |
| 409 | Conflict - Conflito (ex: duplicação) |
| 429 | Too Many Requests - Limite de requisições excedido |
| 500 | Internal Server Error - Erro no servidor |
| 503 | Service Unavailable - Serviço indisponível |

### Formato de Erro

```json
{
  "code": "RESOURCE_NOT_FOUND",
  "message": "Orçamento com ID 550e8400... não encontrado",
  "timestamp": "2026-09-01T10:30:00Z",
  "traceId": "0HN1GD7A2GTAH:00000001",
  "details": [
    {
      "field": "id",
      "issue": "Recurso não existe no banco de dados"
    }
  ]
}
```

### Exemplos de Erro

#### 401 - Não Autenticado
```json
{
  "code": "UNAUTHORIZED",
  "message": "Token expirado ou inválido",
  "timestamp": "2026-09-01T10:30:00Z"
}
```

#### 400 - Requisição Inválida
```json
{
  "code": "VALIDATION_ERROR",
  "message": "Dados de entrada inválidos",
  "details": [
    {"field": "valor", "issue": "Valor deve ser maior que 0"},
    {"field": "cliente", "issue": "Cliente é obrigatório"}
  ]
}
```

---

## 🚦 Rate Limiting

### Limites por Endpoint

| Endpoint | Limite | Janela |
|----------|--------|--------|
| Geral | 1000 req | 1 hora |
| Auth | 5 tentativas | 15 min |
| Relatórios | 100 req | 1 hora |

### Headers de Rate Limiting

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 998
X-RateLimit-Reset: 1693553400
```

---

## 📦 Versionamento

### Versão Atual
- **v1.0.0** - Estável

### Plano Futuro
- **v1.1.0** - Autenticação OAuth2
- **v2.0.0** - Nova estrutura de dados

### Como Usar Versão Específica

```http
GET /api/v1/orcamentos
GET /api/v2/orcamentos  (Future)
```

---

## 📖 Leitura Adicional

- [Swagger UI](http://localhost:5000/swagger)
- [OpenAPI Specification](http://localhost:5000/swagger/v1/swagger.json)
- [Postman Collection](./postman-collection.json)

---

**Última atualização:** 01/09/2026
