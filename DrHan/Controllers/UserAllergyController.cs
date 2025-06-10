using DrHan.Application.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MediatR;
using DrHan.Application.Services.UserAllergyServices.Commands.AddUserAllergy;
using DrHan.Application.Services.UserAllergyServices.Queries.GetUserAllergyProfile;
using DrHan.Application.Services.UserAllergyServices.Queries.GetUserAllergies;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserAllergyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserAllergyController> _logger;

    public UserAllergyController(IMediator mediator, ILogger<UserAllergyController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's allergy profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetMyAllergyProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var query = new GetUserAllergyProfileQuery { UserId = userId.Value };
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user allergy profile");
            return StatusCode(500, "An error occurred while retrieving the allergy profile");
        }
    }

    /// <summary>
    /// Get user's allergy profile by user ID (Admin only)
    /// </summary>
    [HttpGet("profile/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserAllergyProfile(int userId)
    {
        try
        {
            var query = new GetUserAllergyProfileQuery { UserId = userId };
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergy profile for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving the allergy profile");
        }
    }

    /// <summary>
    /// Get current user's allergies
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyAllergies()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var query = new GetUserAllergiesQuery { UserId = userId.Value };
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user allergies");
            return StatusCode(500, "An error occurred while retrieving allergies");
        }
    }

    /// <summary>
    /// Add an allergy to current user's profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddAllergy([FromBody] CreateUserAllergyDto createUserAllergyDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var command = new AddUserAllergyCommand
            {
                UserId = userId.Value,
                AllergenId = createUserAllergyDto.AllergenId,
                Severity = createUserAllergyDto.Severity,
                DiagnosisDate = createUserAllergyDto.DiagnosisDate,
                DiagnosedBy = createUserAllergyDto.DiagnosedBy,
                LastReactionDate = createUserAllergyDto.LastReactionDate,
                AvoidanceNotes = createUserAllergyDto.AvoidanceNotes,
                Outgrown = createUserAllergyDto.Outgrown,
                OutgrownDate = createUserAllergyDto.OutgrownDate,
                NeedsVerification = createUserAllergyDto.NeedsVerification
            };
            
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return CreatedAtAction("GetUserAllergyById", new { id = response.Data!.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user allergy");
            return StatusCode(500, "An error occurred while adding the allergy");
        }
    }

    // TODO: Implement additional CQRS commands and queries for the following endpoints:
    // - GetUserAllergyById
    // - UpdateAllergy  
    // - RemoveAllergy
    // - HasAllergy
    // - GetSevereAllergies
    // - GetOutgrownAllergies

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
} 