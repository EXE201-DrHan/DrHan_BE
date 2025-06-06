using DrHan.Infrastructure.Extensions;
using DrHan.Application.Extensions;
using DrHan.API.Extensions;
using DrHan.API.Middlewares;
using DrHan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddPresentation(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplications(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
using var scope = app.Services.CreateScope();
var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TimeLoggingMiddleware>();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
try
{
    await applicationDbContext.Database.MigrateAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "Error happens during migrations!");
}
app.Run();
