using FluentValidation;
using MediatR;
using TrainingService.Application.Exercises.Commands.CreateExercise;
using TrainingService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(CreateExerciseCommand).Assembly));

builder.Services.AddValidatorsFromAssembly(
    typeof(CreateExerciseCommand).Assembly);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();