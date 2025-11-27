using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qudorat.Application.DTOs;
using Qudorat.Core.Interfaces;
using Qudorat.Infrastructure.Data;

namespace Qudorat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LicensesController : ControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly QudoratDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<LicensesController> _logger;

    public LicensesController(
        ILicenseService licenseService,
        QudoratDbContext context,
        IMapper mapper,
        ILogger<LicensesController> logger)
    {
        _licenseService = licenseService;
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Search licenses (public search from TAMM)
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LicenseDto>>> SearchLicenses(
        [FromQuery] LicenseSearchDto search,
        CancellationToken cancellationToken)
    {
        var licenses = await _licenseService.SearchLicensesAsync(
            search.Name, search.LicenseNumber, search.IsEntity, cancellationToken);
        return Ok(_mapper.Map<IEnumerable<LicenseDto>>(licenses));
    }

    /// <summary>
    /// Get license by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LicenseDto>> GetLicense(Guid id, CancellationToken cancellationToken)
    {
        var license = await _licenseService.GetLicenseByIdAsync(id, cancellationToken);
        if (license == null)
            return NotFound();

        return Ok(_mapper.Map<LicenseDto>(license));
    }

    /// <summary>
    /// Get license by number
    /// </summary>
    [HttpGet("by-number/{licenseNumber}")]
    [AllowAnonymous]
    public async Task<ActionResult<LicenseDto>> GetLicenseByNumber(string licenseNumber, CancellationToken cancellationToken)
    {
        var license = await _licenseService.GetLicenseByNumberAsync(licenseNumber, cancellationToken);
        if (license == null)
            return NotFound();

        return Ok(_mapper.Map<LicenseDto>(license));
    }

    /// <summary>
    /// Get licenses by applicant
    /// </summary>
    [HttpGet("by-applicant/{applicantId:guid}")]
    public async Task<ActionResult<IEnumerable<LicenseDto>>> GetLicensesByApplicant(Guid applicantId, CancellationToken cancellationToken)
    {
        var licenses = await _licenseService.GetLicensesByApplicantAsync(applicantId, cancellationToken);
        return Ok(_mapper.Map<IEnumerable<LicenseDto>>(licenses));
    }

    /// <summary>
    /// Download certificate
    /// </summary>
    [HttpGet("{id:guid}/certificate")]
    public async Task<ActionResult> DownloadCertificate(Guid id, CancellationToken cancellationToken)
    {
        var license = await _licenseService.GetLicenseByIdAsync(id, cancellationToken);
        if (license == null)
            return NotFound();

        if (string.IsNullOrEmpty(license.CertificatePath))
        {
            // Generate certificate if not exists
            await _licenseService.GenerateCertificateAsync(id, cancellationToken);
            license = await _licenseService.GetLicenseByIdAsync(id, cancellationToken);
        }

        // TODO: Return actual file
        return Ok(new { path = license!.CertificatePath });
    }

    /// <summary>
    /// Download card
    /// </summary>
    [HttpGet("{id:guid}/card")]
    public async Task<ActionResult> DownloadCard(Guid id, CancellationToken cancellationToken)
    {
        var license = await _licenseService.GetLicenseByIdAsync(id, cancellationToken);
        if (license == null)
            return NotFound();

        if (string.IsNullOrEmpty(license.CardPath))
        {
            // Generate card if not exists
            await _licenseService.GenerateCardAsync(id, cancellationToken);
            license = await _licenseService.GetLicenseByIdAsync(id, cancellationToken);
        }

        // TODO: Return actual file
        return Ok(new { path = license!.CardPath });
    }

    /// <summary>
    /// Get expiring licenses
    /// </summary>
    [HttpGet("expiring")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<IEnumerable<LicenseDto>>> GetExpiringLicenses(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTime.UtcNow.AddDays(days);
        
        var licenses = await _context.Licenses
            .Include(l => l.Applicant)
            .Include(l => l.Service)
            .Where(l => l.Status == Core.Enums.LicenseStatus.Active &&
                       l.ExpiryDate <= expiryDate &&
                       l.ExpiryDate > DateTime.UtcNow)
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<LicenseDto>>(licenses));
    }
}

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly QudoratDbContext _context;
    private readonly IMapper _mapper;

    public ServicesController(QudoratDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all active services
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServices(CancellationToken cancellationToken)
    {
        var services = await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.ServiceCode)
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<ServiceDto>>(services));
    }

    /// <summary>
    /// Get service by ID with details
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceDetailDto>> GetService(Guid id, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .Include(s => s.RequiredDocuments.OrderBy(d => d.DisplayOrder))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (service == null)
            return NotFound();

        return Ok(_mapper.Map<ServiceDetailDto>(service));
    }

    /// <summary>
    /// Get service by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceDto>> GetServiceByCode(string code, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.ServiceCode == code, cancellationToken);

        if (service == null)
            return NotFound();

        return Ok(_mapper.Map<ServiceDto>(service));
    }

    /// <summary>
    /// Get services by type
    /// </summary>
    [HttpGet("by-type/{type}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServicesByType(Core.Enums.ServiceType type, CancellationToken cancellationToken)
    {
        var services = await _context.Services
            .Where(s => s.IsActive && s.ServiceType == type)
            .OrderBy(s => s.ServiceCode)
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<ServiceDto>>(services));
    }
}
