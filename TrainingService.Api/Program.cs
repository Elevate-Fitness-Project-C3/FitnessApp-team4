using TrainingService.Infrastructure;
using TrainingService.Application.Programs.Commands.CreateProgram;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(CreateProgramCommand).Assembly));

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => "Training API is running");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();