using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qudorat.Application.DTOs;
using Qudorat.Core.Entities;
using Qudorat.Core.Enums;
using Qudorat.Core.Interfaces;

namespace Qudorat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITaskAssignmentService _taskAssignmentService;
    private readonly IMapper _mapper;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ITaskAssignmentService taskAssignmentService,
        IMapper mapper,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _taskAssignmentService = taskAssignmentService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllUsersAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();

        return Ok(_mapper.Map<UserDto>(user));
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByEmailAsync(email, cancellationToken);
        if (user == null)
            return NotFound();

        return Ok(_mapper.Map<UserDto>(user));
    }

    /// <summary>
    /// Get users by role
    /// </summary>
    [HttpGet("by-role/{role}")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByRole(UserRole role, CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersByRoleAsync(role, cancellationToken);
        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    /// <summary>
    /// Get available officers for task assignment
    /// </summary>
    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAvailableOfficers(CancellationToken cancellationToken)
    {
        var users = await _taskAssignmentService.GetAvailableOfficersAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        var user = _mapper.Map<User>(dto);
        var createdUser = await _userService.CreateUserAsync(user, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, _mapper.Map<UserDto>(createdUser));
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        var updatedUser = await _userService.UpdateUserAsync(user, cancellationToken);
        return Ok(_mapper.Map<UserDto>(updatedUser));
    }

    /// <summary>
    /// Update user status (Online/Offline)
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusDto dto, CancellationToken cancellationToken)
    {
        await _userService.UpdateUserStatusAsync(id, dto.Status, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deactivate user
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeactivateUserAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get task count for a user
    /// </summary>
    [HttpGet("{id:guid}/task-count")]
    public async Task<ActionResult<int>> GetTaskCount(Guid id, CancellationToken cancellationToken)
    {
        var count = await _taskAssignmentService.GetAssignedTaskCountAsync(id, cancellationToken);
        return Ok(count);
    }
}
