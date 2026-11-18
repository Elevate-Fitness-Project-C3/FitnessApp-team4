
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IdentityService.Common.OpenApi;

internal sealed class BearerSecurityDocumentTransformer
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[
            JwtBearerDefaults.AuthenticationScheme] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT access token."
            };

        foreach (var path in document.Paths.Values)
        {
            if (path.Operations is null)
                continue;

            foreach (var operation in path.Operations.Values)
            {
                if (operation.Security is null || operation.Security.Count == 0)
                    continue;

                operation.Security.Clear();

                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(
                        JwtBearerDefaults.AuthenticationScheme,
                        document)] = []
                });
            }
        }

        return Task.CompletedTask;
    }
}

internal sealed class BearerSecurityOperationTransformer
    : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata =
            context.Description.ActionDescriptor.EndpointMetadata;

        // Anonymous endpoints must not require bearer authentication.
        if (metadata.OfType<IAllowAnonymous>().Any())
            return Task.CompletedTask;

        // Only protected endpoints require bearer authentication.
        if (!metadata.OfType<IAuthorizeData>().Any())
            return Task.CompletedTask;

        // Mark the operation for the document transformer.
        operation.Security ??= [];

        operation.Security.Add(new OpenApiSecurityRequirement());

        return Task.CompletedTask;
    }
}
