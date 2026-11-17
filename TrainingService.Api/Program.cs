using TrainingService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();