using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qudorat.Core.Entities;
using Qudorat.Core.Enums;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;

namespace Qudorat.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly QudoratDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(QudoratDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApplicationStatusReport> GetApplicationStatusReportAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        Guid? serviceId = null, 
        ApplicationStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Service)
            .Include(a => a.AssignedUser)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.SubmittedAt >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(a => a.SubmittedAt <= endDate.Value);
        
        if (serviceId.HasValue)
            query = query.Where(a => a.ServiceId == serviceId.Value);
        
        if (status.HasValue)
            query = query.Where(a => a.QudoratStatus == status.Value);

        var applications = await query.ToListAsync(cancellationToken);

        var report = new ApplicationStatusReport
        {
            TotalApplications = applications.Count,
            StatusCounts = applications
                .GroupBy(a => a.QudoratStatus)
                .ToDictionary(g => g.Key, g => g.Count()),
            ServiceCounts = applications
                .GroupBy(a => a.Service.NameEnglish)
                .ToDictionary(g => g.Key, g => g.Count()),
            Applications = applications.Select(a => new ApplicationReportItem
            {
                RequestNumber = a.RequestNumber,
                ApplicantName = a.Applicant.FullName,
                ServiceName = a.Service.NameEnglish,
                Status = a.QudoratStatus,
                SubmittedAt = a.SubmittedAt,
                CompletedAt = a.ResponseAt,
                AssignedTo = a.AssignedUser?.FullName
            }).ToList()
        };

        return report;
    }

    public async Task<UserPerformanceReport> GetUserPerformanceReportAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        Guid? userId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApplicationHistories
            .Include(h => h.User)
            .Where(h => h.UserId != null && 
                       (h.ActionType == ActionType.Approved || 
                        h.ActionType == ActionType.Rejected || 
                        h.ActionType == ActionType.Returned))
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(h => h.CreatedAt >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(h => h.CreatedAt <= endDate.Value);
        
        if (userId.HasValue)
            query = query.Where(h => h.UserId == userId.Value);

        var histories = await query.ToListAsync(cancellationToken);

        var userGroups = histories
            .Where(h => h.User != null)
            .GroupBy(h => h.UserId!.Value);

        var report = new UserPerformanceReport
        {
            Users = userGroups.Select(g =>
            {
                var user = g.First().User!;
                return new UserPerformanceItem
                {
                    UserId = g.Key,
                    UserName = user.FullName,
                    Role = user.Role,
                    TasksCompleted = g.Count(),
                    TasksApproved = g.Count(h => h.ActionType == ActionType.Approved),
                    TasksRejected = g.Count(h => h.ActionType == ActionType.Rejected),
                    TasksReturned = g.Count(h => h.ActionType == ActionType.Returned),
                    AverageProcessingTimeHours = 0 // Would need to calculate from application data
                };
            }).ToList()
        };

        return report;
    }

    public async Task<SLAComplianceReport> GetSLAComplianceReportAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Applications
            .Where(a => a.SLADeadline.HasValue)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.SubmittedAt >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(a => a.SubmittedAt <= endDate.Value);

        var applications = await query.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var completedApplications = applications.Where(a => 
            a.QudoratStatus == ApplicationStatus.Approved || 
            a.QudoratStatus == ApplicationStatus.Rejected ||
            a.QudoratStatus == ApplicationStatus.AutoRejected).ToList();

        var pendingApplications = applications.Where(a => 
            a.QudoratStatus != ApplicationStatus.Approved && 
            a.QudoratStatus != ApplicationStatus.Rejected &&
            a.QudoratStatus != ApplicationStatus.AutoRejected).ToList();

        var report = new SLAComplianceReport
        {
            TotalApplications = applications.Count,
            CompletedWithinSLA = completedApplications.Count(a => 
                a.ResponseAt.HasValue && a.ResponseAt <= a.SLADeadline),
            CompletedAfterSLA = completedApplications.Count(a => 
                a.ResponseAt.HasValue && a.ResponseAt > a.SLADeadline),
            PendingWithinSLA = pendingApplications.Count(a => 
                a.SLADeadline > now.AddDays(1)),
            PendingNearSLA = pendingApplications.Count(a => 
                a.SLADeadline > now && a.SLADeadline <= now.AddDays(1)),
            PendingBreachedSLA = pendingApplications.Count(a => 
                a.SLADeadline <= now)
        };

        report.SLACompliancePercentage = report.TotalApplications > 0
            ? Math.Round((double)(report.CompletedWithinSLA + report.PendingWithinSLA) / report.TotalApplications * 100, 2)
            : 0;

        return report;
    }

    public async Task<byte[]> ExportReportToExcelAsync<T>(T report, string reportName, CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Implement Excel export using EPPlus or ClosedXML
        _logger.LogInformation("Exporting report {ReportName} to Excel", reportName);
        
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }
}

public class TammIntegrationService : ITammIntegrationService
{
    private readonly QudoratDbContext _context;
    private readonly IApplicationService _applicationService;
    private readonly IApplicantService _applicantService;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<TammIntegrationService> _logger;

    public TammIntegrationService(
        QudoratDbContext context,
        IApplicationService applicationService,
        IApplicantService applicantService,
        ILicenseService licenseService,
        ILogger<TammIntegrationService> logger)
    {
        _context = context;
        _applicationService = applicationService;
        _applicantService = applicantService;
        _licenseService = licenseService;
        _logger = logger;
    }

    public async Task<Application> ReceiveApplicationFromTammAsync(TammApplicationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Receiving application from TAMM: {TammRequestId}", request.TammRequestId);

        // Get service
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.ServiceCode == request.ServiceCode && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Service not found: {request.ServiceCode}");

        // Create or update applicant
        var applicant = await _applicantService.CreateOrUpdateApplicantAsync(new Applicant
        {
            EmiratesId = request.ApplicantEmiratesId,
            FirstName = request.ApplicantFirstName,
            LastName = request.ApplicantLastName,
            Email = request.ApplicantEmail,
            PhoneNumber = request.ApplicantPhone,
            PreferredCommunication = request.PreferredCommunication,
            CommunicationLanguage = request.CommunicationLanguage
        }, cancellationToken);

        // Check if applicant is suspended for this service
        if (await _applicantService.IsApplicantSuspendedForServiceAsync(applicant.Id, service.Id, cancellationToken))
        {
            throw new InvalidOperationException("Applicant is suspended for this service");
        }

        // Check for duplicate applications
        if (await _applicationService.ValidateDuplicateApplicationAsync(applicant.Id, service.Id, cancellationToken))
        {
            throw new InvalidOperationException("Applicant already has a pending application for this service");
        }

        // Create application
        var application = new Application
        {
            TammRequestId = request.TammRequestId,
            ApplicantId = applicant.Id,
            ServiceId = service.Id,
            FormData = request.FormData,
            ServiceCharges = service.ServiceFee,
            PaymentStatus = service.ServiceFee.HasValue && service.ServiceFee > 0 
                ? PaymentStatus.Pending 
                : PaymentStatus.NotRequired
        };

        // Handle documents
        foreach (var doc in request.Documents)
        {
            var filePath = await SaveDocumentAsync(doc, request.TammRequestId, cancellationToken);
            application.Documents.Add(new ApplicationDocument
            {
                FileName = doc.FileName,
                FilePath = filePath,
                FileType = doc.FileType,
                FileSize = doc.FileContent.Length,
                IsApplicantDocument = true
            });
        }

        // Handle staff members for Provider type services
        if (service.ServiceType == ServiceType.Provider && request.StaffMembers?.Any() == true)
        {
            foreach (var staff in request.StaffMembers)
            {
                // Validate staff member license
                var staffApplicant = await _context.Applicants
                    .FirstOrDefaultAsync(a => a.EmiratesId == staff.EmiratesId, cancellationToken);
                
                if (staffApplicant == null)
                {
                    _logger.LogWarning("Staff member not found: {EmiratesId}", staff.EmiratesId);
                    continue;
                }

                // Check if practitioner is already linked to another provider
                var existingLink = await _context.EntityStaffMembers
                    .Include(e => e.Application)
                    .AnyAsync(e => e.ApplicantId == staffApplicant.Id && 
                                  e.IsAccepted == true &&
                                  e.Application.QudoratStatus == ApplicationStatus.Approved, 
                             cancellationToken);

                if (existingLink)
                {
                    throw new InvalidOperationException($"Practitioner {staff.EmiratesId} is already linked to another provider");
                }

                application.StaffMembers.Add(new EntityStaffMember
                {
                    ApplicantId = staffApplicant.Id,
                    PractitionerLicenseNumber = staff.LicenseNumber,
                    IsAccepted = null // Pending response
                });
            }

            // If provider has staff members, set to pending staff action
            if (application.StaffMembers.Any())
            {
                application.QudoratStatus = ApplicationStatus.PendingStaffAction;
            }
        }

        var createdApplication = await _applicationService.CreateApplicationAsync(application, cancellationToken);
        
        _logger.LogInformation("Application created from TAMM: {RequestNumber}", createdApplication.RequestNumber);

        return createdApplication;
    }

    public async Task SendStatusUpdateToTammAsync(Guid applicationId, TammStatus status, string? message = null, CancellationToken cancellationToken = default)
    {
        var application = await _context.Applications.FindAsync(new object[] { applicationId }, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");

        // TODO: Implement actual TAMM API call
        _logger.LogInformation("Sending status update to TAMM for {RequestNumber}: {Status}", 
            application.RequestNumber, status);

        // Update TAMM status in our system
        application.TammStatus = status;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SendLicenseToTammAsync(Guid licenseId, CancellationToken cancellationToken = default)
    {
        var license = await _licenseService.GetLicenseByIdAsync(licenseId, cancellationToken)
            ?? throw new InvalidOperationException("License not found");

        // TODO: Implement actual TAMM API call to send certificate and card
        _logger.LogInformation("Sending license to TAMM: {LicenseNumber}", license.LicenseNumber);
    }

    private async Task<string> SaveDocumentAsync(TammDocument document, string requestId, CancellationToken cancellationToken)
    {
        // TODO: Implement actual file storage (Azure Blob, AWS S3, or local storage)
        var filePath = $"/documents/{requestId}/{Guid.NewGuid()}_{document.FileName}";
        
        _logger.LogInformation("Document saved: {FilePath}", filePath);
        
        await Task.CompletedTask;
        return filePath;
    }
}
