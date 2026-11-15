
using IdentityService.Common.Exceptions;
using IdentityService.Common.Extensions;
using IdentityService.Extensions;
using IdentityService.Persistence.Seeding;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services
    .AddApplicationServices(typeof(Program).Assembly)
    .AddPersistence(builder.Configuration)
    .AddIdentityServices();
// Register centralized exception handling.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var app = builder.Build();
// Handle exceptions raised during HTTP request processing.
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
app.MapControllers();
app.Run();
