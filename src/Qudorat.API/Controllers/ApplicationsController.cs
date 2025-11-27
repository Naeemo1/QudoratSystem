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
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly ILicenseService _licenseService;
    private readonly QudoratDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        IApplicationService applicationService,
        ILicenseService licenseService,
        QudoratDbContext context,
        IMapper mapper,
        ILogger<ApplicationsController> logger)
    {
        _applicationService = applicationService;
        _licenseService = licenseService;
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all applications with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ApplicationDto>>> GetApplications(
        [FromQuery] ApplicationFilterDto filter,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Service)
            .Include(a => a.AssignedUser)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.RequestNumber))
            query = query.Where(a => a.RequestNumber.Contains(filter.RequestNumber));
        
        if (!string.IsNullOrEmpty(filter.ApplicantName))
            query = query.Where(a => a.Applicant.FirstName.Contains(filter.ApplicantName) || 
                                    a.Applicant.LastName.Contains(filter.ApplicantName));
        
        if (filter.ServiceId.HasValue)
            query = query.Where(a => a.ServiceId == filter.ServiceId);
        
        if (filter.ServiceType.HasValue)
            query = query.Where(a => a.Service.ServiceType == filter.ServiceType);
        
        if (filter.QudoratStatus.HasValue)
            query = query.Where(a => a.QudoratStatus == filter.QudoratStatus);
        
        if (filter.TammStatus.HasValue)
            query = query.Where(a => a.TammStatus == filter.TammStatus);
        
        if (filter.AssignedUserId.HasValue)
            query = query.Where(a => a.AssignedUserId == filter.AssignedUserId);
        
        if (filter.FromDate.HasValue)
            query = query.Where(a => a.SubmittedAt >= filter.FromDate);
        
        if (filter.ToDate.HasValue)
            query = query.Where(a => a.SubmittedAt <= filter.ToDate);
        
        if (filter.IsArchived.HasValue)
            query = query.Where(a => a.IsArchived == filter.IsArchived);

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "requestnumber" => pagination.SortDescending ? query.OrderByDescending(a => a.RequestNumber) : query.OrderBy(a => a.RequestNumber),
            "submittedat" => pagination.SortDescending ? query.OrderByDescending(a => a.SubmittedAt) : query.OrderBy(a => a.SubmittedAt),
            "status" => pagination.SortDescending ? query.OrderByDescending(a => a.QudoratStatus) : query.OrderBy(a => a.QudoratStatus),
            _ => query.OrderByDescending(a => a.SubmittedAt)
        };

        var applications = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<ApplicationDto>>(applications);

        return Ok(new PaginatedResult<ApplicationDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize));
    }

    /// <summary>
    /// Get assigned tasks for current user
    /// </summary>
    [HttpGet("assigned")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetAssignedApplications(
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var applications = await _applicationService.GetAssignedApplicationsAsync(userId, cancellationToken);
        return Ok(_mapper.Map<IEnumerable<ApplicationDto>>(applications));
    }

    /// <summary>
    /// Get all unassigned applications (queue)
    /// </summary>
    [HttpGet("unassigned")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetUnassignedApplications(CancellationToken cancellationToken)
    {
        var applications = await _applicationService.GetUnassignedApplicationsAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<ApplicationDto>>(applications));
    }

    /// <summary>
    /// Get application by ID with full details
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationDetailDto>> GetApplication(Guid id, CancellationToken cancellationToken)
    {
        var application = await _applicationService.GetApplicationByIdAsync(id, cancellationToken);
        if (application == null)
            return NotFound();

        var dto = _mapper.Map<ApplicationDetailDto>(application);

        // Get other active applications for the same applicant
        var otherApplications = await _context.Applications
            .Include(a => a.Service)
            .Where(a => a.ApplicantId == application.ApplicantId && 
                       a.Id != id && 
                       a.QudoratStatus != ApplicationStatus.Approved &&
                       a.QudoratStatus != ApplicationStatus.Rejected &&
                       a.QudoratStatus != ApplicationStatus.AutoRejected)
            .ToListAsync(cancellationToken);

        dto = dto with { OtherActiveApplications = _mapper.Map<List<ApplicationSummaryDto>>(otherApplications) };

        return Ok(dto);
    }

    /// <summary>
    /// Get application by request number
    /// </summary>
    [HttpGet("by-number/{requestNumber}")]
    public async Task<ActionResult<ApplicationDto>> GetApplicationByNumber(string requestNumber, CancellationToken cancellationToken)
    {
        var application = await _applicationService.GetApplicationByRequestNumberAsync(requestNumber, cancellationToken);
        if (application == null)
            return NotFound();

        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Approve an application
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "RequireReviewer")]
    public async Task<ActionResult<ApplicationDto>> ApproveApplication(
        Guid id,
        [FromBody] ApplicationActionDto action,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var application = await _applicationService.ApproveApplicationAsync(id, userId, action.Comment, cancellationToken);
        
        // If fully approved, issue license
        if (application.QudoratStatus == ApplicationStatus.Approved)
        {
            await _licenseService.IssueLicenseAsync(id, cancellationToken);
        }

        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Reject an application
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "RequireReviewer")]
    public async Task<ActionResult<ApplicationDto>> RejectApplication(
        Guid id,
        [FromBody] ApplicationActionDto action,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        if (!action.ReasonId.HasValue)
            return BadRequest("Reason is required for rejection");

        var application = await _applicationService.RejectApplicationAsync(id, userId, action.ReasonId.Value, action.Comment, cancellationToken);
        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Return an application for more information
    /// </summary>
    [HttpPost("{id:guid}/return")]
    [Authorize(Policy = "RequireReviewer")]
    public async Task<ActionResult<ApplicationDto>> ReturnApplication(
        Guid id,
        [FromBody] ApplicationActionDto action,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        if (!action.ReasonId.HasValue)
            return BadRequest("Reason is required for return");

        var application = await _applicationService.ReturnApplicationAsync(id, userId, action.ReasonId.Value, action.Comment, cancellationToken);
        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Reassign an application to another user
    /// </summary>
    [HttpPost("{id:guid}/reassign")]
    [Authorize(Policy = "RequireSupervisor")]
    public async Task<ActionResult<ApplicationDto>> ReassignApplication(
        Guid id,
        [FromBody] ReassignApplicationDto dto,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var application = await _applicationService.ReassignApplicationAsync(id, userId, dto.ToUserId, dto.ReasonId, dto.Comment, cancellationToken);
        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Lock an application to current user
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    public async Task<ActionResult<ApplicationDto>> LockApplication(
        Guid id,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var application = await _applicationService.LockApplicationAsync(id, userId, cancellationToken);
        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Release an application back to queue
    /// </summary>
    [HttpPost("{id:guid}/release")]
    [Authorize(Policy = "RequireSupervisor")]
    public async Task<ActionResult<ApplicationDto>> ReleaseApplication(
        Guid id,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var application = await _applicationService.ReleaseApplicationAsync(id, userId, cancellationToken);
        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Reopen a completed application (Section Head/Director only)
    /// </summary>
    [HttpPost("{id:guid}/reopen")]
    [Authorize(Policy = "RequireSupervisor")]
    public async Task<ActionResult<ApplicationDto>> ReopenApplication(
        Guid id,
        [FromBody] ReopenApplicationDto dto,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var application = await _applicationService.ReopenApplicationAsync(id, userId, dto.NewStatus, cancellationToken);
        return Ok(_mapper.Map<ApplicationDto>(application));
    }

    /// <summary>
    /// Archive an application
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult> ArchiveApplication(Guid id, CancellationToken cancellationToken)
    {
        await _applicationService.ArchiveApplicationAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Add internal document to application
    /// </summary>
    [HttpPost("{id:guid}/documents")]
    public async Task<ActionResult<ApplicationDocumentDto>> AddInternalDocument(
        Guid id,
        [FromBody] AddInternalDocumentDto dto,
        CancellationToken cancellationToken)
    {
        var application = await _context.Applications.FindAsync(new object[] { id }, cancellationToken);
        if (application == null)
            return NotFound();

        var document = new ApplicationDocument
        {
            ApplicationId = id,
            FileName = dto.FileName,
            FileType = dto.FileType,
            FilePath = $"/documents/{id}/{Guid.NewGuid()}_{dto.FileName}", // TODO: Save actual file
            FileSize = dto.FileContent.Length,
            IsApplicantDocument = false,
            Description = dto.Description
        };

        await _context.ApplicationDocuments.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(_mapper.Map<ApplicationDocumentDto>(document));
    }

    /// <summary>
    /// Add comment to application
    /// </summary>
    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ApplicationCommentDto>> AddComment(
        Guid id,
        [FromBody] AddCommentDto dto,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var comment = new ApplicationComment
        {
            ApplicationId = id,
            UserId = userId,
            Comment = dto.Comment,
            IsInternal = dto.IsInternal,
            ReasonId = dto.ReasonId
        };

        await _context.ApplicationComments.AddAsync(comment, cancellationToken);
        
        // Add to history
        await _context.ApplicationHistories.AddAsync(new ApplicationHistory
        {
            ApplicationId = id,
            UserId = userId,
            ActionType = ActionType.CommentAdded,
            ActionDescription = dto.IsInternal ? "Internal comment added" : "Comment added"
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        await _context.Entry(comment).Reference(c => c.User).LoadAsync(cancellationToken);
        await _context.Entry(comment).Reference(c => c.Reason).LoadAsync(cancellationToken);

        return Ok(_mapper.Map<ApplicationCommentDto>(comment));
    }

    /// <summary>
    /// Get application history
    /// </summary>
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IEnumerable<ApplicationHistoryDto>>> GetApplicationHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _context.ApplicationHistories
            .Include(h => h.User)
            .Where(h => h.ApplicationId == id)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<ApplicationHistoryDto>>(history));
    }
}
