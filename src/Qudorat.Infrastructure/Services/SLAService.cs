using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qudorat.Core.Entities;
using Qudorat.Core.Enums;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;

namespace Qudorat.Infrastructure.Services;

public class SLAService : ISLAService
{
    private readonly QudoratDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SLAService> _logger;

    public SLAService(
        QudoratDbContext context, 
        INotificationService notificationService,
        ILogger<SLAService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task MonitorSLAAsync(CancellationToken cancellationToken = default)
    {
        await SendEscalationAlertsAsync(cancellationToken);
    }

    public async Task<DateTime> CalculateSLADeadlineAsync(DateTime startDate, int slaDays, CancellationToken cancellationToken = default)
    {
        // Get holidays if configured (could be from database)
        var holidays = await GetHolidaysAsync(cancellationToken);
        
        var deadline = startDate;
        var daysAdded = 0;
        
        while (daysAdded < slaDays)
        {
            deadline = deadline.AddDays(1);
            
            // Skip weekends (Friday & Saturday in UAE)
            if (deadline.DayOfWeek != DayOfWeek.Friday && 
                deadline.DayOfWeek != DayOfWeek.Saturday &&
                !holidays.Contains(deadline.Date))
            {
                daysAdded++;
            }
        }
        
        return deadline;
    }

    public async Task SendEscalationAlertsAsync(CancellationToken cancellationToken = default)
    {
        var slaConfig = await _context.SLAConfigurations.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
        if (slaConfig == null)
        {
            _logger.LogWarning("No active SLA configuration found");
            return;
        }

        var now = DateTime.UtcNow;

        // Get all in-progress applications with assigned users
        var applications = await _context.Applications
            .Include(a => a.AssignedUser)
            .Include(a => a.Applicant)
            .Where(a => a.AssignedUserId != null &&
                       !a.IsArchived &&
                       a.QudoratStatus == ApplicationStatus.InProgress &&
                       a.SLADeadline.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var application in applications)
        {
            var workingDaysSinceSubmission = CalculateWorkingDays(application.SubmittedAt, now);
            
            // Day 3: Alert to Specialist and Senior Specialist
            if (workingDaysSinceSubmission >= slaConfig.EscalationToSpecialistDays && 
                workingDaysSinceSubmission < slaConfig.EscalationToSectionHeadDays)
            {
                await SendSpecialistEscalationAsync(application, slaConfig, cancellationToken);
            }
            // Day 4: Alert to Section Head and Director
            else if (workingDaysSinceSubmission >= slaConfig.EscalationToSectionHeadDays && 
                     workingDaysSinceSubmission < slaConfig.SLATotalDays)
            {
                await SendSectionHeadEscalationAsync(application, slaConfig, cancellationToken);
            }
            // Day 5: Final reminder to assigned user
            else if (workingDaysSinceSubmission >= slaConfig.SLATotalDays)
            {
                await SendFinalReminderAsync(application, cancellationToken);
            }
        }

        _logger.LogInformation("SLA monitoring completed. Processed {Count} applications", applications.Count);
    }

    private async Task SendSpecialistEscalationAsync(Application application, SLAConfiguration config, CancellationToken cancellationToken)
    {
        // Get specialists and senior specialists
        var escalationUsers = await _context.Users
            .Where(u => u.IsActive && 
                       !u.IsDeleted &&
                       (u.Role == UserRole.Specialist || u.Role == UserRole.SeniorSpecialist))
            .ToListAsync(cancellationToken);

        foreach (var user in escalationUsers)
        {
            // Check if notification already sent today
            var alreadySent = await _context.Notifications
                .AnyAsync(n => n.ApplicationId == application.Id &&
                              n.UserId == user.Id &&
                              n.Type == NotificationType.SLAWarning &&
                              n.CreatedAt.Date == DateTime.UtcNow.Date, 
                         cancellationToken);

            if (!alreadySent)
            {
                await _notificationService.SendNotificationAsync(new Notification
                {
                    UserId = user.Id,
                    ApplicationId = application.Id,
                    Type = NotificationType.SLAWarning,
                    TitleEnglish = "SLA Warning - Application Pending",
                    TitleArabic = "تحذير SLA - الطلب معلق",
                    MessageEnglish = $"Application {application.RequestNumber} has been pending for {config.EscalationToSpecialistDays} working days. Assigned to: {application.AssignedUser?.FullName ?? "Unassigned"}",
                    MessageArabic = $"الطلب {application.RequestNumber} معلق لمدة {config.EscalationToSpecialistDays} أيام عمل. مخصص لـ: {application.AssignedUser?.FullName ?? "غير مخصص"}"
                }, cancellationToken);
            }
        }
    }

    private async Task SendSectionHeadEscalationAsync(Application application, SLAConfiguration config, CancellationToken cancellationToken)
    {
        // Get section heads and directors
        var escalationUsers = await _context.Users
            .Where(u => u.IsActive && 
                       !u.IsDeleted &&
                       (u.Role == UserRole.SectionHead || u.Role == UserRole.Director))
            .ToListAsync(cancellationToken);

        foreach (var user in escalationUsers)
        {
            // Check if notification already sent today
            var alreadySent = await _context.Notifications
                .AnyAsync(n => n.ApplicationId == application.Id &&
                              n.UserId == user.Id &&
                              n.Type == NotificationType.SLAEscalation &&
                              n.CreatedAt.Date == DateTime.UtcNow.Date, 
                         cancellationToken);

            if (!alreadySent)
            {
                await _notificationService.SendNotificationAsync(new Notification
                {
                    UserId = user.Id,
                    ApplicationId = application.Id,
                    Type = NotificationType.SLAEscalation,
                    TitleEnglish = "SLA Escalation - Urgent Action Required",
                    TitleArabic = "تصعيد SLA - مطلوب إجراء عاجل",
                    MessageEnglish = $"Application {application.RequestNumber} has been pending for {config.EscalationToSectionHeadDays} working days. Assigned to: {application.AssignedUser?.FullName ?? "Unassigned"}. Immediate attention required.",
                    MessageArabic = $"الطلب {application.RequestNumber} معلق لمدة {config.EscalationToSectionHeadDays} أيام عمل. مخصص لـ: {application.AssignedUser?.FullName ?? "غير مخصص"}. يتطلب اهتماماً فورياً"
                }, cancellationToken);
            }
        }
    }

    private async Task SendFinalReminderAsync(Application application, CancellationToken cancellationToken)
    {
        if (application.AssignedUserId.HasValue)
        {
            // Check if notification already sent today
            var alreadySent = await _context.Notifications
                .AnyAsync(n => n.ApplicationId == application.Id &&
                              n.UserId == application.AssignedUserId &&
                              n.Type == NotificationType.SLAEscalation &&
                              n.CreatedAt.Date == DateTime.UtcNow.Date, 
                         cancellationToken);

            if (!alreadySent)
            {
                await _notificationService.SendNotificationAsync(new Notification
                {
                    UserId = application.AssignedUserId,
                    ApplicationId = application.Id,
                    Type = NotificationType.SLAEscalation,
                    TitleEnglish = "Final SLA Reminder - Deadline Reached",
                    TitleArabic = "تذكير SLA النهائي - تم الوصول إلى الموعد النهائي",
                    MessageEnglish = $"Application {application.RequestNumber} has reached the SLA deadline. Please complete review immediately.",
                    MessageArabic = $"وصل الطلب {application.RequestNumber} إلى الموعد النهائي لـ SLA. يرجى إكمال المراجعة فوراً"
                }, cancellationToken);
            }
        }
    }

    private static int CalculateWorkingDays(DateTime startDate, DateTime endDate)
    {
        var workingDays = 0;
        var currentDate = startDate.Date;
        
        while (currentDate < endDate.Date)
        {
            currentDate = currentDate.AddDays(1);
            if (currentDate.DayOfWeek != DayOfWeek.Friday && currentDate.DayOfWeek != DayOfWeek.Saturday)
            {
                workingDays++;
            }
        }
        
        return workingDays;
    }

    private async Task<HashSet<DateTime>> GetHolidaysAsync(CancellationToken cancellationToken)
    {
        // Could be loaded from database
        var holidayConfig = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == "PublicHolidays", cancellationToken);

        if (holidayConfig != null && !string.IsNullOrEmpty(holidayConfig.Value))
        {
            return holidayConfig.Value
                .Split(',')
                .Select(d => DateTime.TryParse(d.Trim(), out var date) ? date : (DateTime?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .ToHashSet();
        }

        return new HashSet<DateTime>();
    }
}
