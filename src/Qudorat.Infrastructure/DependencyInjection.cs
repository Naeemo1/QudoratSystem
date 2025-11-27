using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;
using Qudorat.Infrastructure.Repositories;
using Qudorat.Infrastructure.Services;

namespace Qudorat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<QudoratDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(QudoratDbContext).Assembly.FullName)));

        // Add UnitOfWork and Repository
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Add Services
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<ITaskAssignmentService, TaskAssignmentService>();
        services.AddScoped<ISLAService, SLAService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IApplicantService, ApplicantService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ITammIntegrationService, TammIntegrationService>();

        return services;
    }
}
