using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ElfBeauty.BreweryApi.Api.Swagger;

/// Only shows the API key lock icon on endpoints that actually require it, keeping Swagger truthful
/// for [AllowAnonymous] actions such as /health. Adds the requirement per-operation (rather than
/// document-wide) so an empty override on anonymous endpoints isn't dropped by JSON serialization.
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAllowAnonymous = context.MethodInfo.DeclaringType is not null &&
            (context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ||
             context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any());

        if (hasAllowAnonymous)
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                },
                Array.Empty<string>()
            }
        });
    }
}
