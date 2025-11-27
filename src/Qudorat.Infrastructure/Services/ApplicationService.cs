using Microsoft.EntityFrameworkCore;
using Qudorat.Core.Entities;
using Qudorat.Core.Enums;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;

namespace Qudorat.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly QudoratDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ApplicationService(QudoratDbContext context, IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<Application> CreateApplicationAsync(Application application, CancellationToken cancellationToken = default)
    {
        application.RequestNumber = await GenerateRequestNumberAsync(cancellationToken);
        application.SubmittedAt = DateTime.UtcNow;
        application.QudoratStatus = ApplicationStatus.PendingAssignment;
        application.TammStatus = TammStatus.Pending;

        var slaConfig = await _context.SLAConfigurations.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
        if (slaConfig != null)
        {
            application.SLADeadline = CalculateWorkingDaysDeadline(DateTime.UtcNow, slaConfig.SLATotalDays);
        }

        await _unitOfWork.Repository<Application>().AddAsync(application, cancellationToken);
        
        // Add history
        await AddHistoryAsync(application, null, ActionType.Submitted, "Application submitted from TAMM", cancellationToken: cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return application;
    }

    public async Task<Application?> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Service)
            .Include(a => a.AssignedUser)
            .Include(a => a.Documents)
            .Include(a => a.Histories.OrderByDescending(h => h.CreatedAt))
                .ThenInclude(h => h.User)
            .Include(a => a.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.User)
            .Include(a => a.Comments)
                .ThenInclude(c => c.Reason)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Application?> GetApplicationByRequestNumberAsync(string requestNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.RequestNumber == requestNumber, cancellationToken);
    }

    public async Task<IEnumerable<Application>> GetApplicationsByApplicantAsync(Guid applicantId, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .Include(a => a.Service)
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Application>> GetAssignedApplicationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Service)
            .Where(a => a.AssignedUserId == userId && !a.IsArchived)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Application>> GetUnassignedApplicationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Service)
            .Where(a => a.AssignedUserId == null && 
                       a.QudoratStatus == ApplicationStatus.PendingAssignment && 
                       !a.IsArchived)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Application> ApproveApplicationAsync(Guid applicationId, Guid userId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var application = await GetApplicationByIdAsync(applicationId, cancellationToken) 
            ?? throw new InvalidOperationException("Application not found");
        
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var previousStatus = application.QudoratStatus;
        application.ApprovalCount++;
        application.LastActionByRole = user.Role;

        // Check workflow matrix
        var (isFinal, newStatus) = DetermineWorkflowResult(application, ActionType.Approved, user.Role);

        if (isFinal)
        {
            application.QudoratStatus = newStatus;
            application.TammStatus = newStatus == ApplicationStatus.Approved ? TammStatus.Approved : TammStatus.InProgress;
            application.ResponseAt = DateTime.UtcNow;
            application.AssignedUserId = null;
        }
        else
        {
            application.QudoratStatus = ApplicationStatus.InProgress;
            application.TammStatus = TammStatus.InProgress;
            application.AssignedUserId = null; // Will be reassigned to next level
        }

        if (!string.IsNullOrEmpty(comment))
        {
            var appComment = new ApplicationComment
            {
                ApplicationId = applicationId,
                UserId = userId,
                Comment = comment,
                IsInternal = true
            };
            await _context.ApplicationComments.AddAsync(appComment, cancellationToken);
        }

        await AddHistoryAsync(application, user, ActionType.Approved, "Application approved", previousStatus, application.QudoratStatus, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send notification
        if (application.QudoratStatus == ApplicationStatus.Approved)
        {
            await SendApprovalNotificationAsync(application, cancellationToken);
        }

        return application;
    }

    public async Task<Application> RejectApplicationAsync(Guid applicationId, Guid userId, Guid reasonId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var application = await GetApplicationByIdAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");
        
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var previousStatus = application.QudoratStatus;
        application.RejectionCount++;
        application.LastActionByRole = user.Role;

        // First rejection closes the request
        var (isFinal, newStatus) = DetermineWorkflowResult(application, ActionType.Rejected, user.Role);

        if (isFinal || application.RejectionCount >= 2)
        {
            application.QudoratStatus = ApplicationStatus.Rejected;
            application.TammStatus = TammStatus.Rejected;
            application.ResponseAt = DateTime.UtcNow;
            application.AssignedUserId = null;
        }
        else
        {
            application.QudoratStatus = ApplicationStatus.InProgress;
            application.TammStatus = TammStatus.InProgress;
            application.AssignedUserId = null; // Will be reassigned to next level
        }

        var appComment = new ApplicationComment
        {
            ApplicationId = applicationId,
            UserId = userId,
            Comment = comment ?? "Application rejected",
            IsInternal = false,
            ReasonId = reasonId
        };
        await _context.ApplicationComments.AddAsync(appComment, cancellationToken);

        await AddHistoryAsync(application, user, ActionType.Rejected, "Application rejected", previousStatus, application.QudoratStatus, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (application.QudoratStatus == ApplicationStatus.Rejected)
        {
            await SendRejectionNotificationAsync(application, cancellationToken);
        }

        return application;
    }

    public async Task<Application> ReturnApplicationAsync(Guid applicationId, Guid userId, Guid reasonId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var application = await GetApplicationByIdAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");
        
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var previousStatus = application.QudoratStatus;
        application.ReturnCount++;

        var slaConfig = await _context.SLAConfigurations.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
        
        // Auto-reject after max returns
        if (application.ReturnCount >= (slaConfig?.MaxReturnCount ?? 3))
        {
            application.QudoratStatus = ApplicationStatus.AutoRejected;
            application.TammStatus = TammStatus.Rejected;
            application.ResponseAt = DateTime.UtcNow;
            application.AssignedUserId = null;
            
            await AddHistoryAsync(application, user, ActionType.AutoRejected, 
                $"Application auto-rejected after {application.ReturnCount} returns", 
                previousStatus, application.QudoratStatus, cancellationToken);
        }
        else
        {
            application.QudoratStatus = ApplicationStatus.ReturnedForInfo;
            application.TammStatus = TammStatus.RequiresMoreInformation;
            application.AssignedUserId = null;
            
            await AddHistoryAsync(application, user, ActionType.Returned, 
                "Application returned for additional information", 
                previousStatus, application.QudoratStatus, cancellationToken);
        }

        application.LastActionByRole = user.Role;

        var appComment = new ApplicationComment
        {
            ApplicationId = applicationId,
            UserId = userId,
            Comment = comment ?? "Additional information required",
            IsInternal = false,
            ReasonId = reasonId
        };
        await _context.ApplicationComments.AddAsync(appComment, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await SendReturnNotificationAsync(application, cancellationToken);

        return application;
    }

    public async Task<Application> ReassignApplicationAsync(Guid applicationId, Guid fromUserId, Guid toUserId, Guid reasonId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var application = await GetApplicationByIdAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");

        var fromUser = await _context.Users.FindAsync(new object[] { fromUserId }, cancellationToken);
        var toUser = await _context.Users.FindAsync(new object[] { toUserId }, cancellationToken)
            ?? throw new InvalidOperationException("Target user not found");

        var previousUserId = application.AssignedUserId;
        application.AssignedUserId = toUserId;

        if (!string.IsNullOrEmpty(comment))
        {
            var appComment = new ApplicationComment
            {
                ApplicationId = applicationId,
                UserId = fromUserId,
                Comment = comment,
                IsInternal = true,
                ReasonId = reasonId
            };
            await _context.ApplicationComments.AddAsync(appComment, cancellationToken);
        }

        await AddHistoryAsync(application, fromUser, ActionType.Reassigned, 
            $"Reassigned from {fromUser?.FullName ?? "Unassigned"} to {toUser.FullName}", 
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify new assignee
        await _notificationService.SendNotificationAsync(new Notification
        {
            UserId = toUserId,
            ApplicationId = applicationId,
            Type = NotificationType.TaskAssigned,
            TitleEnglish = "New Task Assigned",
            TitleArabic = "مهمة جديدة مخصصة",
            MessageEnglish = $"Application {application.RequestNumber} has been assigned to you",
            MessageArabic = $"تم تخصيص الطلب {application.RequestNumber} لك"
        }, cancellationToken);

        return application;
    }

    public async Task<Application> LockApplicationAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var application = await GetApplicationByIdAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        // Check if user already has max tasks
        var slaConfig = await _context.SLAConfigurations.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
        var assignedCount = await _context.Applications.CountAsync(a => a.AssignedUserId == userId && !a.IsArchived, cancellationToken);
        
        if (assignedCount >= (slaConfig?.MaxTasksPerOfficer ?? 10))
        {
            throw new InvalidOperationException($"User already has maximum ({slaConfig?.MaxTasksPerOfficer ?? 10}) tasks assigned. Please complete or release existing tasks.");
        }

        application.AssignedUserId = userId;
        application.QudoratStatus = ApplicationStatus.InProgress;
        application.TammStatus = TammStatus.InProgress;

        await AddHistoryAsync(application, user, ActionType.Locked, $"Manually locked by {user.FullName}", cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return application;
    }

    public async Task<Application> ReleaseApplicationAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var application = await GetApplicationByIdAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);

        application.AssignedUserId = null;
        application.QudoratStatus = ApplicationStatus.PendingAssignment;

        await AddHistoryAsync(application, user, ActionType.Released, $"Released by {user?.FullName ?? "System"}", cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return application;
    }

    public async Task<Application> ReopenApplicationAsync(Guid applicationId, Guid userId, ApplicationStatus newStatus, CancellationToken cancellationToken = default)
    {
        var application = await GetApplicationByIdAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var previousStatus = application.QudoratStatus;
        application.QudoratStatus = newStatus;
        application.TammStatus = newStatus switch
        {
            ApplicationStatus.Approved => TammStatus.Approved,
            ApplicationStatus.Rejected => TammStatus.Rejected,
            _ => TammStatus.InProgress
        };

        await AddHistoryAsync(application, user, ActionType.Reopened, 
            $"Status changed from {previousStatus} to {newStatus}", 
            previousStatus, newStatus, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify applicant of status change
        await _notificationService.SendNotificationAsync(new Notification
        {
            ApplicantId = application.ApplicantId,
            ApplicationId = applicationId,
            Type = NotificationType.StatusUpdated,
            TitleEnglish = "Application Status Updated",
            TitleArabic = "تم تحديث حالة الطلب",
            MessageEnglish = $"The status of Request #{application.RequestNumber} has been updated from '{previousStatus}' to '{newStatus}' following a review action.",
            MessageArabic = $"تم تحديث حالة الطلب #{application.RequestNumber} من '{previousStatus}' إلى '{newStatus}' بعد إجراء مراجعة"
        }, cancellationToken);

        return application;
    }

    public async Task ArchiveApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _context.Applications.FindAsync(new object[] { applicationId }, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");

        application.IsArchived = true;
        application.ArchivedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ValidateDuplicateApplicationAsync(Guid applicantId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Applications.AnyAsync(a => 
            a.ApplicantId == applicantId && 
            a.ServiceId == serviceId && 
            a.QudoratStatus != ApplicationStatus.Rejected &&
            a.QudoratStatus != ApplicationStatus.AutoRejected &&
            a.QudoratStatus != ApplicationStatus.Approved,
            cancellationToken);
    }

    private async Task<string> GenerateRequestNumberAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow;
        var prefix = $"QUD-{today:yyyyMMdd}";
        var count = await _context.Applications
            .CountAsync(a => a.RequestNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}-{(count + 1):D4}";
    }

    private static DateTime CalculateWorkingDaysDeadline(DateTime startDate, int workingDays)
    {
        var deadline = startDate;
        var daysAdded = 0;
        
        while (daysAdded < workingDays)
        {
            deadline = deadline.AddDays(1);
            if (deadline.DayOfWeek != DayOfWeek.Friday && deadline.DayOfWeek != DayOfWeek.Saturday)
            {
                daysAdded++;
            }
        }
        
        return deadline;
    }

    private static (bool IsFinal, ApplicationStatus NewStatus) DetermineWorkflowResult(Application application, ActionType action, UserRole role)
    {
        // Section Head or Director actions are always final
        if (role == UserRole.SectionHead || role == UserRole.Director)
        {
            return action == ActionType.Approved 
                ? (true, ApplicationStatus.Approved)
                : (true, ApplicationStatus.Rejected);
        }

        // First rejection is final
        if (action == ActionType.Rejected && application.RejectionCount == 0)
        {
            return (true, ApplicationStatus.Rejected);
        }

        // Two approvals needed
        if (action == ActionType.Approved && application.ApprovalCount >= 2)
        {
            return (true, ApplicationStatus.Approved);
        }

        // Two rejections needed (after initial approval)
        if (action == ActionType.Rejected && application.RejectionCount >= 2)
        {
            return (true, ApplicationStatus.Rejected);
        }

        return (false, ApplicationStatus.InProgress);
    }

    private async Task AddHistoryAsync(Application application, User? user, ActionType actionType, string description, 
        ApplicationStatus? previousStatus = null, ApplicationStatus? newStatus = null, CancellationToken cancellationToken = default)
    {
        var history = new ApplicationHistory
        {
            ApplicationId = application.Id,
            UserId = user?.Id,
            ActionType = actionType,
            ActionDescription = description,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            UserRole = user?.Role
        };
        
        await _context.ApplicationHistories.AddAsync(history, cancellationToken);
    }

    private async Task SendApprovalNotificationAsync(Application application, CancellationToken cancellationToken)
    {
        await _notificationService.SendNotificationAsync(new Notification
        {
            ApplicantId = application.ApplicantId,
            ApplicationId = application.Id,
            Type = NotificationType.ApplicationApproved,
            TitleEnglish = "Application Approved",
            TitleArabic = "تمت الموافقة على الطلب",
            MessageEnglish = $"Your application {application.RequestNumber} has been approved. You will receive your license shortly.",
            MessageArabic = $"تمت الموافقة على طلبك {application.RequestNumber}. ستتلقى رخصتك قريباً"
        }, cancellationToken);
    }

    private async Task SendRejectionNotificationAsync(Application application, CancellationToken cancellationToken)
    {
        await _notificationService.SendNotificationAsync(new Notification
        {
            ApplicantId = application.ApplicantId,
            ApplicationId = application.Id,
            Type = NotificationType.ApplicationRejected,
            TitleEnglish = "Application Rejected",
            TitleArabic = "تم رفض الطلب",
            MessageEnglish = $"Your application {application.RequestNumber} has been rejected. Please review the comments for more details.",
            MessageArabic = $"تم رفض طلبك {application.RequestNumber}. يرجى مراجعة التعليقات لمزيد من التفاصيل"
        }, cancellationToken);
    }

    private async Task SendReturnNotificationAsync(Application application, CancellationToken cancellationToken)
    {
        await _notificationService.SendNotificationAsync(new Notification
        {
            ApplicantId = application.ApplicantId,
            ApplicationId = application.Id,
            Type = NotificationType.ApplicationReturned,
            TitleEnglish = "Additional Information Required",
            TitleArabic = "مطلوب معلومات إضافية",
            MessageEnglish = $"Your application {application.RequestNumber} requires additional information. Please check and resubmit.",
            MessageArabic = $"يتطلب طلبك {application.RequestNumber} معلومات إضافية. يرجى المراجعة وإعادة التقديم"
        }, cancellationToken);
    }
}
