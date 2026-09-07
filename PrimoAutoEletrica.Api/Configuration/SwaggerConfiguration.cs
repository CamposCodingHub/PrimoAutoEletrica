using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace PrimoAutoEletrica.Api.Configuration
{
    /// <summary>
    /// Configuração centralizada para Swagger/OpenAPI
    /// </summary>
    public static class SwaggerConfiguration
    {
        /// <summary>
        /// Adiciona serviços Swagger com configuração completa
        /// </summary>
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                // Informações básicas da API
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "PrimoAutoEletrica API",
                    Version = "v1.0.0",
                    Description = "API REST para gerenciamento de oficina mecânica com recursos de orçamento, ordem de serviço, estoque e financeiro",
                    Contact = new OpenApiContact
                    {
                        Name = "Suporte PrimoAutoEletrica",
                        Email = "suporte@primoautoeletrica.com.br",
                        Url = new Uri("https://www.primoautoeletrica.com.br")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    },
                    TermsOfService = new Uri("https://www.primoautoeletrica.com.br/termos")
                });

                // Adicionar documentação XML
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                // Adicionar documentação XML do projeto principal
                var mainProjectXml = Path.Combine(AppContext.BaseDirectory, "PrimoAutoEletrica.xml");
                if (File.Exists(mainProjectXml))
                {
                    options.IncludeXmlComments(mainProjectXml);
                }

                // Autenticação (preparação para OAuth2/JWT)
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Description = "Entre com o token JWT. Exemplo: Bearer {seu_token}"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });

                // Tags operacionais
                options.TagActionsBy(api =>
                {
                    if (api.GroupName != null)
                        return new[] { api.GroupName };

                    var controllerActionDescriptor = api.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    return new[] { controllerActionDescriptor?.ControllerName ?? "General" };
                });

                // Ordenar tags
                options.OrderActionsBy((apiDescriptionA, apiDescriptionB) =>
                    $"{apiDescriptionA.ActionDescriptor.RouteValues["controller"]}_{apiDescriptionA.HttpMethod}".CompareTo(
                    $"{apiDescriptionB.ActionDescriptor.RouteValues["controller"]}_{apiDescriptionB.HttpMethod}"));

                // Schemas customizados
                options.SchemaFilter<SwaggerSchemaFilter>();
                options.OperationFilter<SwaggerOperationFilter>();

                // Configurar responsáveis por endpoint
                options.OperationFilter<AuthorizeCheckOperationFilter>();
            });

            return services;
        }

        /// <summary>
        /// Configura o middleware Swagger UI
        /// </summary>
        public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
        {
            app.UseSwagger(options =>
            {
                options.SerializeAsV2 = false;
                options.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    swagger.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                    };
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "PrimoAutoEletrica API v1.0.0");
                options.RoutePrefix = string.Empty;
                options.DisplayOperationId();
                options.EnableDeepLinking();
                options.DisplayRequestDuration();
                options.Filter("", "Swagger");
                options.DefaultModelsExpandDepth(2);
                options.DefaultModelExpandDepth(2);
                options.EnableValidator();
                options.ShowExtensions();

                // Configurar tema
                options.InjectStylesheet("/swagger-ui.css");
                options.InjectJavascript("/swagger-ui.js");
            });

            return app;
        }
    }

    /// <summary>
    /// Filtro para adicionar schema customizado ao Swagger
    /// </summary>
    public class SwaggerSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // Adicionar exemplos customizados
            if (context.Type == typeof(Models.Orcamento))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["id"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                    ["cliente"] = new Microsoft.OpenApi.Any.OpenApiString("João Silva"),
                    ["valor"] = new Microsoft.OpenApi.Any.OpenApiDouble(1500.00),
                    ["data"] = new Microsoft.OpenApi.Any.OpenApiString(DateTime.Now.ToString("yyyy-MM-dd"))
                };
            }
        }
    }

    /// <summary>
    /// Filtro para adicionar informações customizadas aos operations
    /// </summary>
    public class SwaggerOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Adicionar exemplos de resposta
            if (context.ApiDescription.HttpMethod == "GET" && context.ApiDescription.RelativePath == "api/health")
            {
                operation.Summary = "Verificar saúde da API";
                operation.Description = "Retorna o status atual da API e suas dependências";
                operation.Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Health" } };

                operation.Responses["200"].Description = "API está saudável e operacional";
            }
        }
    }

    /// <summary>
    /// Filtro para verificar autorização
    /// </summary>
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var authAttributes = context.MethodInfo
                .GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .Distinct();

            if (authAttributes.Any())
            {
                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] { }
                        }
                    }
                };
            }
        }
    }
}
