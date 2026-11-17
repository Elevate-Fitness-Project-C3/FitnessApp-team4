using IdentityService.Common;
using IdentityService.Common.Exceptions;
using IdentityService.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration.GetValue<bool>("DevelopmentSetup:GenerateRsaKeys"))
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Automatic RSA generation is restricted to Development.");
    IdentityService.Security.Keys.DevelopmentRsaSetup.Ensure(builder.Configuration);
}
builder.Services.AddOpenApi();
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(x => $"Validation.{x.Key}", _ => new[] { "Invalid request value." });
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
            BuildingBlocks.AspNetCore.Responses.ApiResponse<object>.Fail("Validation failed.", StatusCodes.Status400BadRequest, errors));
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
_ = app.Services.GetRequiredService<IdentityService.Security.Keys.RsaKeyProvider>();
await DatabaseInitializer.InitializeAsync(app);
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Identity Service API v1");
    });
}
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
