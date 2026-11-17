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
            .ToDictionary(
                x => $"Validation.{x.Key}",
                x => x.Value!.Errors
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid request value." : e.ErrorMessage)
                    .ToArray());
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
            IdentityService.Common.Responses.ApiResponse<object>.Fail("Validation failed.", StatusCodes.Status400BadRequest, errors));
    };
});
builder.Services.AddDependencies(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseForwardedHeaders();
app.UseExceptionHandler();
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
app.MapJwks();
app.Run();
