using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qudorat.Core.Entities;
using Qudorat.Core.Enums;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;

namespace Qudorat.Infrastructure.Services;

public class LicenseService : ILicenseService
{
    private readonly QudoratDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(
        QudoratDbContext context, 
        INotificationService notificationService,
        ILogger<LicenseService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<License> IssueLicenseAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");

        if (application.QudoratStatus != ApplicationStatus.Approved)
        {
            throw new InvalidOperationException("Cannot issue license for non-approved application");
        }

        var slaConfig = await _context.SLAConfigurations.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
        var validityDays = slaConfig?.LicenseValidityDays ?? 365;

        var license = new License
        {
            LicenseNumber = await GenerateLicenseNumberAsync(application.Service.ServiceCode, cancellationToken),
            ApplicationId = applicationId,
            ApplicantId = application.ApplicantId,
            ServiceId = application.ServiceId,
            IssuedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(validityDays),
            Status = LicenseStatus.Active
        };

        await _context.Licenses.AddAsync(license, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Generate certificate and card
        license.CertificatePath = await GenerateCertificateAsync(license.Id, cancellationToken);
        license.CardPath = await GenerateCardAsync(license.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Send notification
        await _notificationService.SendNotificationAsync(new Notification
        {
            ApplicantId = application.ApplicantId,
            ApplicationId = applicationId,
            Type = NotificationType.ApplicationApproved,
            TitleEnglish = "License Issued",
            TitleArabic = "تم إصدار الرخصة",
            MessageEnglish = $"Your license {license.LicenseNumber} has been issued and is valid until {license.ExpiryDate:dd/MM/yyyy}",
            MessageArabic = $"تم إصدار رخصتك {license.LicenseNumber} وهي صالحة حتى {license.ExpiryDate:dd/MM/yyyy}"
        }, cancellationToken);

        _logger.LogInformation("License {LicenseNumber} issued for application {RequestNumber}", 
            license.LicenseNumber, application.RequestNumber);

        return license;
    }

    public async Task<License?> GetLicenseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Licenses
            .Include(l => l.Application)
            .Include(l => l.Applicant)
            .Include(l => l.Service)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<License?> GetLicenseByNumberAsync(string licenseNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Licenses
            .Include(l => l.Application)
            .Include(l => l.Applicant)
            .Include(l => l.Service)
            .FirstOrDefaultAsync(l => l.LicenseNumber == licenseNumber, cancellationToken);
    }

    public async Task<IEnumerable<License>> GetLicensesByApplicantAsync(Guid applicantId, CancellationToken cancellationToken = default)
    {
        return await _context.Licenses
            .Include(l => l.Service)
            .Where(l => l.ApplicantId == applicantId)
            .OrderByDescending(l => l.IssuedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<License>> SearchLicensesAsync(string? name = null, string? licenseNumber = null, bool? isEntity = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Licenses
            .Include(l => l.Applicant)
            .Include(l => l.Service)
            .Where(l => l.Status == LicenseStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(l => 
                l.Applicant.FirstName.Contains(name) || 
                l.Applicant.LastName.Contains(name));
        }

        if (!string.IsNullOrEmpty(licenseNumber))
        {
            query = query.Where(l => l.LicenseNumber.Contains(licenseNumber));
        }

        if (isEntity.HasValue)
        {
            var serviceType = isEntity.Value ? ServiceType.Provider : ServiceType.Individual;
            query = query.Where(l => l.Service.ServiceType == serviceType);
        }

        return await query
            .OrderByDescending(l => l.IssuedDate)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task CheckExpiringLicensesAsync(CancellationToken cancellationToken = default)
    {
        var slaConfig = await _context.SLAConfigurations.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
        var notificationDays = slaConfig?.RenewalNotificationDays ?? 30;

        var expiringDate = DateTime.UtcNow.AddDays(notificationDays);

        var expiringLicenses = await _context.Licenses
            .Include(l => l.Applicant)
            .Include(l => l.Service)
            .Where(l => l.Status == LicenseStatus.Active &&
                       l.ExpiryDate <= expiringDate &&
                       l.ExpiryDate > DateTime.UtcNow &&
                       !l.RenewalNotificationSent)
            .ToListAsync(cancellationToken);

        foreach (var license in expiringLicenses)
        {
            await _notificationService.SendNotificationAsync(new Notification
            {
                ApplicantId = license.ApplicantId,
                Type = NotificationType.LicenseExpiring,
                TitleEnglish = "License Expiring Soon",
                TitleArabic = "الرخصة ستنتهي قريباً",
                MessageEnglish = $"Your license {license.LicenseNumber} for {license.Service.NameEnglish} will expire on {license.ExpiryDate:dd/MM/yyyy}. Please submit a renewal request.",
                MessageArabic = $"رخصتك {license.LicenseNumber} لـ {license.Service.NameArabic} ستنتهي في {license.ExpiryDate:dd/MM/yyyy}. يرجى تقديم طلب تجديد"
            }, cancellationToken);

            license.RenewalNotificationSent = true;
            license.RenewalNotificationSentAt = DateTime.UtcNow;
        }

        // Update expired licenses
        var expiredLicenses = await _context.Licenses
            .Where(l => l.Status == LicenseStatus.Active && l.ExpiryDate < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var license in expiredLicenses)
        {
            license.Status = LicenseStatus.Expired;
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Processed {ExpiringCount} expiring licenses and {ExpiredCount} expired licenses", 
            expiringLicenses.Count, expiredLicenses.Count);
    }

    public async Task<string> GenerateCertificateAsync(Guid licenseId, CancellationToken cancellationToken = default)
    {
        var license = await GetLicenseByIdAsync(licenseId, cancellationToken)
            ?? throw new InvalidOperationException("License not found");

        // In a real implementation, this would generate a PDF certificate
        // For now, we'll just return a placeholder path
        var certificatePath = $"/certificates/{license.LicenseNumber}_certificate.pdf";
        
        // TODO: Implement actual PDF generation using a library like QuestPDF, iTextSharp, or similar
        
        _logger.LogInformation("Certificate generated for license {LicenseNumber}: {Path}", 
            license.LicenseNumber, certificatePath);

        return certificatePath;
    }

    public async Task<string> GenerateCardAsync(Guid licenseId, CancellationToken cancellationToken = default)
    {
        var license = await GetLicenseByIdAsync(licenseId, cancellationToken)
            ?? throw new InvalidOperationException("License not found");

        // In a real implementation, this would generate a digital card image
        // For now, we'll just return a placeholder path
        var cardPath = $"/cards/{license.LicenseNumber}_card.png";
        
        // TODO: Implement actual card image generation
        
        _logger.LogInformation("Card generated for license {LicenseNumber}: {Path}", 
            license.LicenseNumber, cardPath);

        return cardPath;
    }

    private async Task<string> GenerateLicenseNumberAsync(string serviceCode, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow;
        var prefix = serviceCode.Replace("/", "-");
        var year = today.Year;
        
        var count = await _context.Licenses
            .CountAsync(l => l.LicenseNumber.StartsWith($"{prefix}-{year}"), cancellationToken);
        
        return $"{prefix}-{year}-{(count + 1):D5}";
    }
}
