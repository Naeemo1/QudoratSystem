using Qudorat.Core.Interfaces;

namespace Qudorat.API.BackgroundJobs;

public class TaskAssignmentJob
{
    private readonly ITaskAssignmentService _taskAssignmentService;
    private readonly ILogger<TaskAssignmentJob> _logger;

    public TaskAssignmentJob(ITaskAssignmentService taskAssignmentService, ILogger<TaskAssignmentJob> logger)
    {
        _taskAssignmentService = taskAssignmentService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting task assignment job");
        
        try
        {
            await _taskAssignmentService.AssignPendingTasksAsync(cancellationToken);
            _logger.LogInformation("Task assignment job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in task assignment job");
            throw;
        }
    }
}

public class SLAMonitoringJob
{
    private readonly ISLAService _slaService;
    private readonly ILogger<SLAMonitoringJob> _logger;

    public SLAMonitoringJob(ISLAService slaService, ILogger<SLAMonitoringJob> logger)
    {
        _slaService = slaService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SLA monitoring job");
        
        try
        {
            await _slaService.MonitorSLAAsync(cancellationToken);
            _logger.LogInformation("SLA monitoring job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SLA monitoring job");
            throw;
        }
    }
}

public class LicenseExpiryJob
{
    private readonly ILicenseService _licenseService;
    private readonly ILogger<LicenseExpiryJob> _logger;

    public LicenseExpiryJob(ILicenseService licenseService, ILogger<LicenseExpiryJob> logger)
    {
        _licenseService = licenseService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting license expiry check job");
        
        try
        {
            await _licenseService.CheckExpiringLicensesAsync(cancellationToken);
            _logger.LogInformation("License expiry check job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in license expiry check job");
            throw;
        }
    }
}
