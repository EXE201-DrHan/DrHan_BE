using DrHan.Infrastructure.Extensions;
using DrHan.Application.Extensions;
using DrHan.API.Extensions;
using DrHan.API.Middlewares;
using DrHan.Infrastructure.Persistence;
using DrHan.Infrastructure.Seeders;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Serilog;
using Serilog.Events;
using System.Text;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/drhan-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        encoding: Encoding.UTF8,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
// Add services to the container.
try
{
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.AddPresentation(builder.Configuration);
    builder.Services.AddApplications(builder.Configuration);
    
    // Add Redis services with error handling
    try
    {
        builder.Services.AddRedisServices(builder.Configuration);
        Log.Information("Redis services added successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to add Redis services, continuing without Redis");
    }
    
    builder.Services.AddHangfireWithFallback(builder.Configuration);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to configure services");
    throw;
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
builder.Services.AddSwaggerGen();
var app = builder.Build();
var scope = app.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode > 499
            ? LogEventLevel.Error
            : LogEventLevel.Information;
});
// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DrHan API v1");
    c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
    
    // Optional: Require authentication in production
    //if (!app.Environment.IsDevelopment())
    //{
    //    c.SupportedSubmitMethods(); // Disable "Try it out" in production
    //}
});

// Configure middlewares
app.UseMiddleware<ErrorHandlingMiddleware>();
//app.UseMiddleware<TimeLoggingMiddleware>();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();  
app.UseAuthorization();

// Add Hangfire Dashboard (only in development)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapControllers();

//using (var scope = app.Services.CreateScope())
//{
//    var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

//    try
//    {
//        // Get database settings from configuration
//        var autoMigrate = app.Configuration.GetValue<bool>("DatabaseSettings:AutoMigrate", false);
//        var autoSeed = app.Configuration.GetValue<bool>("DatabaseSettings:AutoSeed", false);

//        if (autoMigrate)
//        {
//            logger.LogInformation("AutoMigrate is enabled. Starting database migration...");
//            await applicationDbContext.Database.MigrateAsync();
//            logger.LogInformation("Database migration completed successfully.");
//        }
//        else
//        {
//            logger.LogInformation("AutoMigrate is disabled. Skipping database migration.");
//        }

//        if (autoSeed)
//        {
//            var shouldClearAndReseed = app.Configuration.GetValue<bool>("ClearAndReseedData", false);

//            if (shouldClearAndReseed)
//            {
//                logger.LogInformation("ClearAndReseedData flag is set. Performing data reset...");
//                var dataManagementService = scope.ServiceProvider.GetRequiredService<DrHan.Infrastructure.Seeders.DataManagementService>();
//                await dataManagementService.ResetAllDataAsync();
//            }
//            else
//            {
//                logger.LogInformation("AutoSeed is enabled. Ensuring data exists...");
//                var dataManagementService = scope.ServiceProvider.GetRequiredService<DrHan.Infrastructure.Seeders.DataManagementService>();
//                await dataManagementService.EnsureDataAsync();
//            }
//        }
//        else
//        {
//            logger.LogInformation("AutoSeed is disabled. Skipping data seeding.");
//        }
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "Error occurred during database initialization or seeding!");
//        if (app.Environment.IsDevelopment())
//        {
//            throw;
//        }
//    }
//}
app.Run();
