var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddApplicationServices();

builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddIdentityServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Identity Service API v1");
    });
}

app.UseHttpsRedirection();

app.Run();
