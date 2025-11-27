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
public class ApplicantsController : ControllerBase
{
    private readonly IApplicantService _applicantService;
    private readonly QudoratDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ApplicantsController> _logger;

    public ApplicantsController(
        IApplicantService applicantService,
        QudoratDbContext context,
        IMapper mapper,
        ILogger<ApplicantsController> logger)
    {
        _applicantService = applicantService;
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Search applicants
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ApplicantDto>>> SearchApplicants(
        [FromQuery] ApplicantSearchDto search,
        CancellationToken cancellationToken)
    {
        var applicants = await _applicantService.SearchApplicantsAsync(
            search.Name, search.EmiratesId, search.Email, cancellationToken);
        return Ok(_mapper.Map<IEnumerable<ApplicantDto>>(applicants));
    }

    /// <summary>
    /// Get applicant by ID with full details
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicantDetailDto>> GetApplicant(Guid id, CancellationToken cancellationToken)
    {
        var applicant = await _applicantService.GetApplicantByIdAsync(id, cancellationToken);
        if (applicant == null)
            return NotFound();

        return Ok(_mapper.Map<ApplicantDetailDto>(applicant));
    }

    /// <summary>
    /// Get applicant by Emirates ID
    /// </summary>
    [HttpGet("by-eid/{emiratesId}")]
    public async Task<ActionResult<ApplicantDto>> GetApplicantByEmiratesId(string emiratesId, CancellationToken cancellationToken)
    {
        var applicant = await _applicantService.GetApplicantByEmiratesIdAsync(emiratesId, cancellationToken);
        if (applicant == null)
            return NotFound();

        return Ok(_mapper.Map<ApplicantDto>(applicant));
    }

    /// <summary>
    /// Get applicant's applications
    /// </summary>
    [HttpGet("{id:guid}/applications")]
    public async Task<ActionResult<IEnumerable<ApplicationSummaryDto>>> GetApplicantApplications(Guid id, CancellationToken cancellationToken)
    {
        var applications = await _context.Applications
            .Include(a => a.Service)
            .Where(a => a.ApplicantId == id)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<ApplicationSummaryDto>>(applications));
    }

    /// <summary>
    /// Get applicant's licenses
    /// </summary>
    [HttpGet("{id:guid}/licenses")]
    public async Task<ActionResult<IEnumerable<LicenseSummaryDto>>> GetApplicantLicenses(Guid id, CancellationToken cancellationToken)
    {
        var licenses = await _context.Licenses
            .Include(l => l.Service)
            .Where(l => l.ApplicantId == id)
            .OrderByDescending(l => l.IssuedDate)
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<LicenseSummaryDto>>(licenses));
    }

    /// <summary>
    /// Get suspension list
    /// </summary>
    [HttpGet("suspensions")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<IEnumerable<SuspensionDto>>> GetSuspensions(CancellationToken cancellationToken)
    {
        var suspensions = await _context.ApplicantSuspensions
            .Include(s => s.Applicant)
            .Include(s => s.Reason)
            .OrderByDescending(s => s.EnabledDate)
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<SuspensionDto>>(suspensions));
    }

    /// <summary>
    /// Suspend an applicant
    /// </summary>
    [HttpPost("suspensions")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<SuspensionDto>> SuspendApplicant([FromBody] CreateSuspensionDto dto, CancellationToken cancellationToken)
    {
        // Find applicant by email
        var applicant = await _context.Applicants
            .FirstOrDefaultAsync(a => a.Email == dto.ApplicantEmail, cancellationToken);
        
        if (applicant == null)
            return BadRequest("Applicant not found with provided email");

        var suspension = await _applicantService.SuspendApplicantAsync(
            applicant.Id, dto.ServiceIds, dto.ReasonId, dto.Notes, cancellationToken);

        // Reload with navigation properties
        await _context.Entry(suspension).Reference(s => s.Applicant).LoadAsync(cancellationToken);
        await _context.Entry(suspension).Reference(s => s.Reason).LoadAsync(cancellationToken);

        return Ok(_mapper.Map<SuspensionDto>(suspension));
    }

    /// <summary>
    /// Deactivate a suspension
    /// </summary>
    [HttpPut("suspensions/{id:guid}/deactivate")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult> DeactivateSuspension(Guid id, CancellationToken cancellationToken)
    {
        await _applicantService.DeactivateSuspensionAsync(id, cancellationToken);
        return NoContent();
    }
}
