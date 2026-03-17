using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Nop.Core.Infrastructure;
using System.Reflection;

namespace Nop.Plugin.Misc.RequirementApi.Infrastructure;

/// <summary>
/// Represents the registering services on application startup
/// </summary>
public class NopStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add Swagger/OpenAPI services
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Product API",
                Version = "v1",
                Description = "RESTful API for retrieving product details",
                Contact = new OpenApiContact
                {
                    Name = "Vibgyor",
                    Email = "support@vibgyor.com"
                }
            });

            // Include XML comments for better documentation
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var xmlFile = $"{assembly.GetName().Name}.xml";
                
                // Try multiple possible locations for the XML file
                var possiblePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, xmlFile),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Misc.RequirementApi", xmlFile),
                    Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "Misc.RequirementApi", xmlFile)
                };

                foreach (var xmlPath in possiblePaths)
                {
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                        break;
                    }
                }
            }
            catch
            {
                // If XML file is not found, continue without it
            }

            // Add security definition for authorization (if needed)
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    Array.Empty<string>()
                }
            });
        });
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
        // Enable middleware to serve generated Swagger as a JSON endpoint
        application.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.)
        application.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
            c.RoutePrefix = "swagger"; // Swagger UI will be available at /swagger
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.EnableValidator();
            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        });
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 450; // Run after routing but before endpoints
}

