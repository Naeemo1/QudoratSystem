using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qudorat.Application.DTOs;
using Qudorat.Core.Entities;
using Qudorat.Core.Enums;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;

namespace Qudorat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public NotificationsController(INotificationService notificationService, IMapper mapper)
    {
        _notificationService = notificationService;
        _mapper = mapper;
    }

    /// <summary>
    /// Get notifications for a user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
        [FromQuery] Guid userId,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly, cancellationToken);
        return Ok(_mapper.Map<IEnumerable<NotificationDto>>(notifications));
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllAsRead([FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly QudoratDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, QudoratDbContext context, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get application status report
    /// </summary>
    [HttpGet("application-status")]
    public async Task<ActionResult<ApplicationStatusReport>> GetApplicationStatusReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? serviceId,
        [FromQuery] ApplicationStatus? status,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetApplicationStatusReportAsync(
            startDate, endDate, serviceId, status, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Get user performance report
    /// </summary>
    [HttpGet("user-performance")]
    public async Task<ActionResult<UserPerformanceReport>> GetUserPerformanceReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetUserPerformanceReportAsync(
            startDate, endDate, userId, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Get SLA compliance report
    /// </summary>
    [HttpGet("sla-compliance")]
    public async Task<ActionResult<SLAComplianceReport>> GetSLAComplianceReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetSLAComplianceReportAsync(startDate, endDate, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Export report to Excel
    /// </summary>
    [HttpGet("export/{reportType}")]
    public async Task<ActionResult> ExportReport(
        string reportType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        object report = reportType.ToLower() switch
        {
            "application-status" => await _reportService.GetApplicationStatusReportAsync(startDate, endDate, null, null, cancellationToken),
            "user-performance" => await _reportService.GetUserPerformanceReportAsync(startDate, endDate, null, cancellationToken),
            "sla-compliance" => await _reportService.GetSLAComplianceReportAsync(startDate, endDate, cancellationToken),
            _ => throw new InvalidOperationException("Invalid report type")
        };

        var bytes = await _reportService.ExportReportToExcelAsync(report, reportType, cancellationToken);
        
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{reportType}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly QudoratDbContext _context;
    private readonly IMapper _mapper;

    public DashboardController(QudoratDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var dashboard = new DashboardDto
        {
            PendingAssignment = await _context.Applications
                .CountAsync(a => a.QudoratStatus == ApplicationStatus.PendingAssignment && !a.IsArchived, cancellationToken),
            
            InProgress = await _context.Applications
                .CountAsync(a => a.QudoratStatus == ApplicationStatus.InProgress && !a.IsArchived, cancellationToken),
            
            CompletedToday = await _context.Applications
                .CountAsync(a => a.ResponseAt.HasValue && 
                                a.ResponseAt.Value.Date == today &&
                                (a.QudoratStatus == ApplicationStatus.Approved || 
                                 a.QudoratStatus == ApplicationStatus.Rejected), cancellationToken),
            
            NearingSLA = await _context.Applications
                .CountAsync(a => !a.IsArchived &&
                                a.SLADeadline.HasValue &&
                                a.SLADeadline > now &&
                                a.SLADeadline <= now.AddDays(1) &&
                                a.QudoratStatus == ApplicationStatus.InProgress, cancellationToken),
            
            BreachedSLA = await _context.Applications
                .CountAsync(a => !a.IsArchived &&
                                a.SLADeadline.HasValue &&
                                a.SLADeadline <= now &&
                                a.QudoratStatus == ApplicationStatus.InProgress, cancellationToken),
            
            RecentTasks = await _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Service)
                .Include(a => a.AssignedUser)
                .Where(a => !a.IsArchived)
                .OrderByDescending(a => a.SubmittedAt)
                .Take(10)
                .Select(a => new TaskSummaryDto(
                    a.Id,
                    a.RequestNumber,
                    a.Applicant.FirstName + " " + a.Applicant.LastName,
                    a.Service.NameEnglish,
                    a.QudoratStatus,
                    a.SubmittedAt,
                    a.SLADeadline,
                    a.AssignedUser != null ? a.AssignedUser.FirstName + " " + a.AssignedUser.LastName : null
                ))
                .ToListAsync(cancellationToken)
        };

        return Ok(dashboard);
    }

    /// <summary>
    /// Get KPIs
    /// </summary>
    [HttpGet("kpis")]
    public async Task<ActionResult<KPIDto>> GetKPIs(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var applications = await _context.Applications
            .Include(a => a.Service)
            .ToListAsync(cancellationToken);

        var completedApplications = applications
            .Where(a => a.QudoratStatus == ApplicationStatus.Approved || 
                       a.QudoratStatus == ApplicationStatus.Rejected ||
                       a.QudoratStatus == ApplicationStatus.AutoRejected)
            .ToList();

        var kpi = new KPIDto
        {
            TotalApplicationsToday = applications.Count(a => a.SubmittedAt.Date == today),
            TotalApplicationsThisWeek = applications.Count(a => a.SubmittedAt >= weekStart),
            TotalApplicationsThisMonth = applications.Count(a => a.SubmittedAt >= monthStart),
            
            AverageProcessingTimeHours = completedApplications
                .Where(a => a.ResponseAt.HasValue)
                .Select(a => (a.ResponseAt!.Value - a.SubmittedAt).TotalHours)
                .DefaultIfEmpty(0)
                .Average(),
            
            SLACompliancePercentage = completedApplications.Count > 0
                ? Math.Round((double)completedApplications.Count(a => a.ResponseAt <= a.SLADeadline) / completedApplications.Count * 100, 2)
                : 0,
            
            ApprovedCount = applications.Count(a => a.QudoratStatus == ApplicationStatus.Approved),
            RejectedCount = applications.Count(a => a.QudoratStatus == ApplicationStatus.Rejected || a.QudoratStatus == ApplicationStatus.AutoRejected),
            ReturnedCount = applications.Count(a => a.QudoratStatus == ApplicationStatus.ReturnedForInfo),
            
            ApplicationsByService = applications
                .GroupBy(a => a.Service.NameEnglish)
                .ToDictionary(g => g.Key, g => g.Count()),
            
            ApplicationsByStatus = applications
                .GroupBy(a => a.QudoratStatus.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return Ok(kpi);
    }
}

[ApiController]
[Route("api/[controller]")]
public class TammController : ControllerBase
{
    private readonly ITammIntegrationService _tammService;
    private readonly IMapper _mapper;
    private readonly ILogger<TammController> _logger;

    public TammController(ITammIntegrationService tammService, IMapper mapper, ILogger<TammController> logger)
    {
        _tammService = tammService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Receive application from TAMM platform
    /// </summary>
    [HttpPost("applications")]
    [AllowAnonymous] // TAMM integration uses API key authentication
    public async Task<ActionResult<ApplicationDto>> ReceiveApplication(
        [FromBody] TammApplicationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receiving application from TAMM: {TammRequestId}", request.TammRequestId);

        var application = await _tammService.ReceiveApplicationFromTammAsync(request, cancellationToken);
        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Webhook for TAMM status updates
    /// </summary>
    [HttpPost("webhook/status")]
    [AllowAnonymous]
    public async Task<ActionResult> StatusUpdateWebhook(
        [FromBody] TammStatusUpdate update,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Status update webhook from TAMM for request: {TammRequestId}", update.TammRequestId);
        
        // Handle status updates from TAMM (e.g., payment confirmation)
        await Task.CompletedTask;
        
        return Ok();
    }
}

public class TammStatusUpdate
{
    public string TammRequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReasonCodesController : ControllerBase
{
    private readonly QudoratDbContext _context;
    private readonly IMapper _mapper;

    public ReasonCodesController(QudoratDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all reason codes
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReasonCodeDto>>> GetReasonCodes(
        [FromQuery] ReasonType? type,
        CancellationToken cancellationToken)
    {
        var query = _context.ReasonCodes.Where(r => r.IsActive).AsQueryable();

        if (type.HasValue)
            query = query.Where(r => r.ReasonType == type.Value);

        var reasons = await query.ToListAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<ReasonCodeDto>>(reasons));
    }

    /// <summary>
    /// Get reason codes by type
    /// </summary>
    [HttpGet("by-type/{type}")]
    public async Task<ActionResult<IEnumerable<ReasonCodeDto>>> GetReasonCodesByType(ReasonType type, CancellationToken cancellationToken)
    {
        var reasons = await _context.ReasonCodes
            .Where(r => r.IsActive && r.ReasonType == type)
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<ReasonCodeDto>>(reasons));
    }
}
