using AuditService.Health;
using AuditService.Persistence;
using AuditService.Messaging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Use portable structured output without requiring Windows Event Log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
// SQL command/update exceptions can contain values from rejected rows.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None);
builder.Logging.AddFilter("MassTransit", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
builder.Services.AddControllers();
builder.Services.AddHttpClient<AuditService.Features.AuditEvents.Reports.AuditReportAuthorization>(client => client.Timeout = TimeSpan.FromSeconds(5))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<AuditService.Features.AuditEvents.Reports.AuditCursorCodec>();
builder.Services.AddScoped<AuditService.Features.AuditEvents.Reports.AuditReportQuery>();
builder.Services.AddScoped<AuditService.Features.AuditEvents.Reports.AuditReportAccessFilter>();
var connection = AuditDatabaseConfiguration.GetConnectionString(builder.Configuration);
builder.Services.AddDbContext<AuditDbContext>(options => options
    .UseSqlServer(connection, sql => sql.CommandTimeout(5))
    .EnableSensitiveDataLogging(false)
    .EnableDetailedErrors(false));
builder.Services.AddHealthChecks().AddCheck<AuditDatabaseReadinessCheck>(
    "audit-database", tags: ["ready"], timeout: TimeSpan.FromSeconds(10));
builder.Services.AddAuditMessaging(builder.Configuration, builder.Environment.IsDevelopment());

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
    {
        context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        return;
    }
    var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    var persistence = exception is Microsoft.Data.SqlClient.SqlException or DbUpdateException;
    context.Response.StatusCode = persistence ? 503 : 500;
    if (persistence)
    {
        var sql = exception as Microsoft.Data.SqlClient.SqlException ?? exception!.InnerException as Microsoft.Data.SqlClient.SqlException;
        app.Logger.LogError("Audit request failed. TraceId={TraceId} ExceptionType={ExceptionType} SqlError={SqlError}",
            traceId, exception!.GetType().Name, sql?.Number);
    }
    else
        app.Logger.LogError(exception, "Audit request failed. TraceId={TraceId}", traceId);
    await context.Response.WriteAsJsonAsync(new { Success = false, StatusCode = context.Response.StatusCode, Message = "The audit operation is temporarily unavailable." });
}));
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapHealthChecks("/health/messaging", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("masstransit"),
    ResultStatusCodes =
    {
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable
    }
});
// Migrations are applied by a separate deployment identity, never by the web process.
app.Run();
