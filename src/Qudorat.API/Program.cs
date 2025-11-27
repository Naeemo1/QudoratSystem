using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models;
using Qudorat.Application.Mappings;
using Qudorat.Application.Validators;
using Qudorat.Infrastructure;
using Qudorat.API.BackgroundJobs;
using Qudorat.API.Middleware;
using FluentValidation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/qudorat-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Qudorat API", 
        Version = "v1",
        Description = "API for Qudorat System - Public Health Practitioners and Service Providers Registration"
    });
    
    c.AddSecurityDefinition("Negotiate", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "negotiate",
        Description = "Windows Authentication"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Negotiate"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

// Configure Windows Authentication (SSO)
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("SystemAdmin", "Director"));
    options.AddPolicy("RequireSupervisor", policy => policy.RequireRole("SectionHead", "Director", "SeniorSpecialist"));
    options.AddPolicy("RequireReviewer", policy => policy.RequireRole("Officer", "Specialist", "SeniorSpecialist", "SectionHead", "Director"));
});

// Configure Hangfire for background jobs
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Qudorat API v1"));
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Request logging middleware
app.UseSerilogRequestLogging();

app.MapControllers();

// Hangfire Dashboard
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Configure Hangfire recurring jobs
RecurringJob.AddOrUpdate<TaskAssignmentJob>(
    "task-assignment",
    job => job.ExecuteAsync(CancellationToken.None),
    "*/3 * * * *"); // Every 3 minutes

RecurringJob.AddOrUpdate<SLAMonitoringJob>(
    "sla-monitoring",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 * * * *"); // Every hour

RecurringJob.AddOrUpdate<LicenseExpiryJob>(
    "license-expiry-check",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 8 * * *"); // Daily at 8 AM

app.MapHealthChecks("/health");

// Apply migrations on startup (optional, can be removed for production)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Qudorat.Infrastructure.Data.QudoratDbContext>();
    try
    {
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
    }
}

Log.Information("Qudorat API started");

app.Run();
