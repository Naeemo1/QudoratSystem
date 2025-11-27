using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qudorat.Core.Entities;
using Qudorat.Core.Enums;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;

namespace Qudorat.Infrastructure.Services;

public class TaskAssignmentService : ITaskAssignmentService
{
    private readonly QudoratDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TaskAssignmentService> _logger;

    public TaskAssignmentService(
        QudoratDbContext context, 
        INotificationService notificationService,
        ILogger<TaskAssignmentService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task AssignPendingTasksAsync(CancellationToken cancellationToken = default)
    {
        var slaConfig = await _context.SLAConfigurations.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
        var maxTasksPerOfficer = slaConfig?.MaxTasksPerOfficer ?? 10;

        // Get pending applications
        var pendingApplications = await _context.Applications
            .Where(a => a.QudoratStatus == ApplicationStatus.PendingAssignment && 
                       a.AssignedUserId == null && 
                       !a.IsArchived)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);

        if (!pendingApplications.Any())
        {
            _logger.LogDebug("No pending applications to assign");
            return;
        }

        // Get available officers (online and not at max capacity)
        var availableOfficers = await GetAvailableOfficersWithCapacityAsync(maxTasksPerOfficer, cancellationToken);

        if (!availableOfficers.Any())
        {
            _logger.LogWarning("No available officers for task assignment");
            return;
        }

        var officerIndex = 0;
        var assignedCount = 0;

        foreach (var application in pendingApplications)
        {
            // Find next available officer with capacity
            var assignedOfficer = FindNextAvailableOfficer(availableOfficers, ref officerIndex, maxTasksPerOfficer);
            
            if (assignedOfficer == null)
            {
                _logger.LogWarning("All officers at max capacity");
                break;
            }

            application.AssignedUserId = assignedOfficer.UserId;
            application.QudoratStatus = ApplicationStatus.InProgress;
            application.TammStatus = TammStatus.InProgress;

            // Track assignment in history
            await _context.ApplicationHistories.AddAsync(new ApplicationHistory
            {
                ApplicationId = application.Id,
                ActionType = ActionType.Assigned,
                ActionDescription = $"Auto-assigned to {assignedOfficer.UserName}",
                NewStatus = ApplicationStatus.InProgress,
                PreviousStatus = ApplicationStatus.PendingAssignment
            }, cancellationToken);

            // Update officer's task count
            assignedOfficer.TaskCount++;
            assignedCount++;

            // Send notification to officer
            await _notificationService.SendNotificationAsync(new Notification
            {
                UserId = assignedOfficer.UserId,
                ApplicationId = application.Id,
                Type = NotificationType.TaskAssigned,
                TitleEnglish = "New Task Assigned",
                TitleArabic = "مهمة جديدة مخصصة",
                MessageEnglish = $"Application {application.RequestNumber} has been assigned to you",
                MessageArabic = $"تم تخصيص الطلب {application.RequestNumber} لك"
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Assigned {Count} applications to officers", assignedCount);
    }

    public async Task<IEnumerable<User>> GetAvailableOfficersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.IsActive && 
                       u.Status == UserStatus.Online && 
                       !u.IsDeleted &&
                       (u.Role == UserRole.Officer || 
                        u.Role == UserRole.Specialist || 
                        u.Role == UserRole.SeniorSpecialist))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAssignedTaskCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .CountAsync(a => a.AssignedUserId == userId && 
                            !a.IsArchived &&
                            a.QudoratStatus != ApplicationStatus.Approved &&
                            a.QudoratStatus != ApplicationStatus.Rejected &&
                            a.QudoratStatus != ApplicationStatus.AutoRejected, 
                       cancellationToken);
    }

    private async Task<List<OfficerCapacity>> GetAvailableOfficersWithCapacityAsync(int maxTasks, CancellationToken cancellationToken)
    {
        var officers = await _context.Users
            .Where(u => u.IsActive && 
                       u.Status == UserStatus.Online && 
                       !u.IsDeleted &&
                       (u.Role == UserRole.Officer || 
                        u.Role == UserRole.Specialist || 
                        u.Role == UserRole.SeniorSpecialist))
            .Select(u => new OfficerCapacity
            {
                UserId = u.Id,
                UserName = u.FirstName + " " + u.LastName,
                Role = u.Role,
                TaskCount = _context.Applications.Count(a => 
                    a.AssignedUserId == u.Id && 
                    !a.IsArchived &&
                    a.QudoratStatus != ApplicationStatus.Approved &&
                    a.QudoratStatus != ApplicationStatus.Rejected &&
                    a.QudoratStatus != ApplicationStatus.AutoRejected)
            })
            .Where(o => o.TaskCount < maxTasks)
            .OrderBy(o => o.TaskCount)
            .ThenBy(o => o.Role) // Officers first, then specialists
            .ToListAsync(cancellationToken);

        return officers;
    }

    private static OfficerCapacity? FindNextAvailableOfficer(List<OfficerCapacity> officers, ref int currentIndex, int maxTasks)
    {
        var startIndex = currentIndex;
        
        do
        {
            var officer = officers[currentIndex];
            currentIndex = (currentIndex + 1) % officers.Count;
            
            if (officer.TaskCount < maxTasks)
            {
                return officer;
            }
        } 
        while (currentIndex != startIndex);

        return null;
    }

    private class OfficerCapacity
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int TaskCount { get; set; }
    }
}
