using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using SaverSearch.Api.Middleware;
using SaverSearch.Application;
using SaverSearch.Application.Common.Models;
using SaverSearch.Infrastructure;
using SaverSearch.Infrastructure.Persistence.Contexts;

var builder = WebApplication.CreateBuilder(args);

// Add API layer services (Controllers with customized model validation responses)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            var response = ApiResponse<object>.ErrorResponse("Validation failed.", errors);
            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddEndpointsApiExplorer();

// Swagger Gen setup with XML docs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SaverSearch API",
        Version = "v1",
        Description = "Commercial-grade Cashback & Rewards Comparison platform API."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});

// Health Checks
builder.Services.AddHealthChecks();

// Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add Application layer services
builder.Services.AddApplicationServices();

// Add Infrastructure layer services (DB, caching, scrapers, notifications)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline (Strict Middleware Ordering)

// 1. Exception Handling
app.UseExceptionHandler();

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. Response Compression
app.UseResponseCompression();

// 4. Request Logging
app.UseMiddleware<RequestLoggingMiddleware>();

// 5. Swagger UI (Developer docs)
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

// 6. Routing & Auth
app.UseRouting();

// app.UseAuthentication();
// app.UseAuthorization();

// 7. Map Endpoints
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
