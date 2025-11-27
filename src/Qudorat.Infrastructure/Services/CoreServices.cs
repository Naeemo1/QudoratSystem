using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qudorat.Core.Entities;
using Qudorat.Core.Enums;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;

namespace Qudorat.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly QudoratDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(QudoratDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Send email notification asynchronously
        if (notification.UserId.HasValue)
        {
            var user = await _context.Users.FindAsync(new object[] { notification.UserId.Value }, cancellationToken);
            if (user != null)
            {
                await SendEmailNotificationAsync(user.Email, notification.TitleEnglish, notification.MessageEnglish, cancellationToken);
            }
        }
        else if (notification.ApplicantId.HasValue)
        {
            var applicant = await _context.Applicants.FindAsync(new object[] { notification.ApplicantId.Value }, cancellationToken);
            if (applicant != null)
            {
                var title = applicant.CommunicationLanguage == CommunicationLanguage.Arabic 
                    ? notification.TitleArabic 
                    : notification.TitleEnglish;
                var message = applicant.CommunicationLanguage == CommunicationLanguage.Arabic 
                    ? notification.MessageArabic 
                    : notification.MessageEnglish;
                    
                await SendEmailNotificationAsync(applicant.Email, title, message, cancellationToken);
            }
        }
    }

    public async Task SendEmailNotificationAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual email sending using SMTP or email service provider
        _logger.LogInformation("Email notification sent to {To}: {Subject}", to, subject);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { notificationId }, cancellationToken);
        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class UserService : IUserService
{
    private readonly QudoratDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        QudoratDbContext context, 
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<UserService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User created: {Email} with role {Role}", user.Email, user.Role);
        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateUserStatusAsync(Guid userId, UserStatus status, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var previousStatus = user.Status;
        user.Status = status;
        user.StatusChangedAt = DateTime.UtcNow;

        // If user went offline, notify supervisors
        if (previousStatus == UserStatus.Online && status == UserStatus.Offline)
        {
            await NotifySupervisorsOfStatusChangeAsync(user, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {Email} status changed from {Previous} to {New}", user.Email, previousStatus, status);
    }

    public async Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        user.IsActive = false;
        user.Status = UserStatus.Offline;
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User {Email} deactivated", user.Email);
    }

    private async Task NotifySupervisorsOfStatusChangeAsync(User user, CancellationToken cancellationToken)
    {
        var supervisors = await _context.Users
            .Where(u => u.IsActive && 
                       (u.Role == UserRole.SeniorSpecialist || 
                        u.Role == UserRole.SectionHead || 
                        u.Role == UserRole.Director))
            .ToListAsync(cancellationToken);

        foreach (var supervisor in supervisors)
        {
            await _notificationService.SendNotificationAsync(new Notification
            {
                UserId = supervisor.Id,
                Type = NotificationType.UserWentOffline,
                TitleEnglish = "User Status Change",
                TitleArabic = "تغيير حالة المستخدم",
                MessageEnglish = $"{user.FullName} has changed status to Offline",
                MessageArabic = $"{user.FullName} قام بتغيير الحالة إلى غير متصل"
            }, cancellationToken);
        }
    }
}

public class ApplicantService : IApplicantService
{
    private readonly QudoratDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ApplicantService> _logger;

    public ApplicantService(
        QudoratDbContext context, 
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<ApplicantService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Applicant> CreateOrUpdateApplicantAsync(Applicant applicant, CancellationToken cancellationToken = default)
    {
        var existingApplicant = await _context.Applicants
            .FirstOrDefaultAsync(a => a.EmiratesId == applicant.EmiratesId, cancellationToken);

        if (existingApplicant != null)
        {
            existingApplicant.FirstName = applicant.FirstName;
            existingApplicant.LastName = applicant.LastName;
            existingApplicant.Email = applicant.Email;
            existingApplicant.PhoneNumber = applicant.PhoneNumber;
            existingApplicant.PreferredCommunication = applicant.PreferredCommunication;
            existingApplicant.CommunicationLanguage = applicant.CommunicationLanguage;
            
            await _context.SaveChangesAsync(cancellationToken);
            return existingApplicant;
        }

        await _unitOfWork.Repository<Applicant>().AddAsync(applicant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return applicant;
    }

    public async Task<Applicant?> GetApplicantByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Applicants
            .Include(a => a.Applications)
                .ThenInclude(app => app.Service)
            .Include(a => a.Licenses)
                .ThenInclude(l => l.Service)
            .Include(a => a.Suspensions.Where(s => s.Status == SuspensionStatus.Active))
                .ThenInclude(s => s.Reason)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Applicant?> GetApplicantByEmiratesIdAsync(string emiratesId, CancellationToken cancellationToken = default)
    {
        return await _context.Applicants
            .Include(a => a.Applications)
            .Include(a => a.Licenses)
            .FirstOrDefaultAsync(a => a.EmiratesId == emiratesId, cancellationToken);
    }

    public async Task<IEnumerable<Applicant>> SearchApplicantsAsync(string? name = null, string? emiratesId = null, string? email = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Applicants.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(a => a.FirstName.Contains(name) || a.LastName.Contains(name));
        }

        if (!string.IsNullOrEmpty(emiratesId))
        {
            query = query.Where(a => a.EmiratesId.Contains(emiratesId));
        }

        if (!string.IsNullOrEmpty(email))
        {
            query = query.Where(a => a.Email.Contains(email));
        }

        return await query
            .OrderBy(a => a.FirstName)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicantSuspension> SuspendApplicantAsync(Guid applicantId, List<Guid> serviceIds, Guid reasonId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var applicant = await _context.Applicants.FindAsync(new object[] { applicantId }, cancellationToken)
            ?? throw new InvalidOperationException("Applicant not found");

        var suspension = new ApplicantSuspension
        {
            ApplicantId = applicantId,
            SuspendedServices = string.Join(",", serviceIds),
            ReasonId = reasonId,
            EnabledDate = DateTime.UtcNow,
            Status = SuspensionStatus.Active,
            Notes = notes
        };

        await _context.ApplicantSuspensions.AddAsync(suspension, cancellationToken);
        
        applicant.IsSuspended = true;

        // Add to application history for each affected application
        var affectedApplications = await _context.Applications
            .Where(a => a.ApplicantId == applicantId && 
                       serviceIds.Contains(a.ServiceId) &&
                       a.QudoratStatus != ApplicationStatus.Approved &&
                       a.QudoratStatus != ApplicationStatus.Rejected)
            .ToListAsync(cancellationToken);

        foreach (var app in affectedApplications)
        {
            await _context.ApplicationHistories.AddAsync(new ApplicationHistory
            {
                ApplicationId = app.Id,
                ActionType = ActionType.Suspended,
                ActionDescription = $"Applicant suspended: {notes ?? "No reason provided"}"
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Send notification
        await _notificationService.SendNotificationAsync(new Notification
        {
            ApplicantId = applicantId,
            Type = NotificationType.StatusUpdated,
            TitleEnglish = "Account Suspended",
            TitleArabic = "تم تعليق الحساب",
            MessageEnglish = $"Your account has been suspended for certain services. Please contact support for more information.",
            MessageArabic = $"تم تعليق حسابك لبعض الخدمات. يرجى الاتصال بالدعم لمزيد من المعلومات"
        }, cancellationToken);

        _logger.LogInformation("Applicant {ApplicantId} suspended for services: {Services}", applicantId, string.Join(",", serviceIds));

        return suspension;
    }

    public async Task DeactivateSuspensionAsync(Guid suspensionId, CancellationToken cancellationToken = default)
    {
        var suspension = await _context.ApplicantSuspensions
            .Include(s => s.Applicant)
            .FirstOrDefaultAsync(s => s.Id == suspensionId, cancellationToken)
            ?? throw new InvalidOperationException("Suspension not found");

        suspension.Status = SuspensionStatus.Inactive;
        suspension.DisabledDate = DateTime.UtcNow;

        // Check if applicant has any other active suspensions
        var hasOtherSuspensions = await _context.ApplicantSuspensions
            .AnyAsync(s => s.ApplicantId == suspension.ApplicantId && 
                          s.Id != suspensionId && 
                          s.Status == SuspensionStatus.Active, 
                     cancellationToken);

        if (!hasOtherSuspensions)
        {
            suspension.Applicant.IsSuspended = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Suspension {SuspensionId} deactivated for applicant {ApplicantId}", 
            suspensionId, suspension.ApplicantId);
    }

    public async Task<bool> IsApplicantSuspendedForServiceAsync(Guid applicantId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicantSuspensions
            .AnyAsync(s => s.ApplicantId == applicantId && 
                          s.Status == SuspensionStatus.Active &&
                          s.SuspendedServices.Contains(serviceId.ToString()), 
                     cancellationToken);
    }
}
