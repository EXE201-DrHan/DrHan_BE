using DrHan.Infrastructure.Extensions;
using DrHan.Application.Extensions;
using DrHan.API.Extensions;
using DrHan.API.Middlewares;
using DrHan.Infrastructure.Persistence;
using DrHan.Infrastructure.Seeders;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);

builder.AddPresentation(builder.Configuration);
builder.Services.AddApplications(builder.Configuration);
builder.Services.AddRedisServices(builder.Configuration);
builder.Services.AddHangfireWithFallback(
    builder.Configuration,
    builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>()
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
builder.Services.AddSwaggerGen();
var app = builder.Build();
var scope = app.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure middlewares
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TimeLoggingMiddleware>();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();  
app.UseAuthorization();
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
