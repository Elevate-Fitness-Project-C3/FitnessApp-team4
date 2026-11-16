using IdentityService.Common;
using IdentityService.Common.Exceptions;
using IdentityService.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(x => $"Validation.{x.Key}", _ => new[] { "Invalid request value." });
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
            IdentityService.Common.Responses.ApiResponse<object>.Fail("Validation failed.", StatusCodes.Status400BadRequest, errors));
    };
});
builder.Services.AddDependencies(builder.Configuration, builder.Environment.IsDevelopment());
// Register centralized exception handling.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
// Handle exceptions raised during HTTP request processing.
app.UseForwardedHeaders();
app.UseExceptionHandler();
// Ensure required identity roles exist before accepting requests.
await using (var scope = app.Services.CreateAsyncScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await IdentityRoleSeeder.SeedAsync(roleManager);
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Identity Service API v1");
    });
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
