using Microsoft.EntityFrameworkCore;
using SaverSearch.Api.Middleware;
using SaverSearch.Infrastructure;
using SaverSearch.Infrastructure.Persistence.Contexts;

var builder = WebApplication.CreateBuilder(args);

// Add API layer services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Gen setup
builder.Services.AddSwaggerGen();

// Health Checks
builder.Services.AddHealthChecks();

// Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add Infrastructure layer services (DB, caching, scrapers, notifications)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SaverSearch API v1");
        c.RoutePrefix = "swagger";
    });

    // Run migrations on startup in Development only
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SaverSearchDbContext>();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}

app.UseHttpsRedirection();

// Map routes
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
