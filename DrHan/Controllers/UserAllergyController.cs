using DrHan.Application.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MediatR;
using DrHan.Application.Services.UserAllergyServices.Commands.AddUserAllergy;
using DrHan.Application.Services.UserAllergyServices.Commands.RemoveUserAllergy;
using DrHan.Application.Services.UserAllergyServices.Commands.UpdateUserAllergy;
using DrHan.Application.Services.UserAllergyServices.Queries.GetUserAllergyProfile;
using DrHan.Application.Services.UserAllergyServices.Queries.GetUserAllergies;
using DrHan.Application.Interfaces.Services.AuthenticationServices;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserAllergyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserAllergyController> _logger;
    private readonly IUserContext _userContext;

    public UserAllergyController(IMediator mediator, ILogger<UserAllergyController> logger, IUserContext userContext)
    {
        _mediator = mediator;
        _logger = logger;
        _userContext = userContext;
    }

    /// <summary>
    /// Get current user's allergy profile
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetMyAllergyProfile()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
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
    //[HttpGet]
    //[Authorize]
    //public async Task<IActionResult> GetMyAllergies()
    //{
    //    try
    //    {
    //        var userId = _userContext.GetCurrentUserId();
    //        if (userId == null)
    //            return Unauthorized("User ID not found in token");

    //        var query = new GetUserAllergiesQuery { UserId = userId.Value };
    //        var response = await _mediator.Send(query);
            
    //        if (!response.IsSucceeded)
    //            return BadRequest(response);
                
    //        return Ok(response);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving user allergies");
    //        return StatusCode(500, "An error occurred while retrieving allergies");
    //    }
    //}

    /// <summary>
    /// Add an allergy to current user's profile
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddAllergy([FromBody] CreateUserAllergyDto createUserAllergyDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userContext.GetCurrentUserId();
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
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user allergy");
            return StatusCode(500, "An error occurred while adding the allergy");
        }
    }

    /// <summary>
    /// Update an allergy in current user's profile
    /// </summary>
    [HttpPut("profile/{allergenId}")]
    [Authorize]
    public async Task<IActionResult> UpdateAllergy(int allergenId, [FromBody] UpdateUserAllergyDto updateUserAllergyDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            // Find the user allergy record by userId and allergenId
            var existingAllergy = await _mediator.Send(new GetUserAllergiesQuery { UserId = userId.Value });
            if (!existingAllergy.IsSucceeded)
                return BadRequest(existingAllergy);

            var userAllergy = existingAllergy.Data?.FirstOrDefault(ua => ua.AllergenId == allergenId);
            if (userAllergy == null)
                return NotFound($"Allergy with allergen ID {allergenId} not found for current user");

            var command = new UpdateUserAllergyCommand
            {
                UserId = userId.Value,
                UserAllergyId = userAllergy.Id,
                Severity = updateUserAllergyDto.Severity,
                DiagnosisDate = updateUserAllergyDto.DiagnosisDate,
                DiagnosedBy = updateUserAllergyDto.DiagnosedBy,
                LastReactionDate = updateUserAllergyDto.LastReactionDate,
                AvoidanceNotes = updateUserAllergyDto.AvoidanceNotes,
                Outgrown = updateUserAllergyDto.Outgrown,
                OutgrownDate = updateUserAllergyDto.OutgrownDate,
                NeedsVerification = updateUserAllergyDto.NeedsVerification
            };
            
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user allergy for allergen {AllergenId}", allergenId);
            return StatusCode(500, "An error occurred while updating the allergy");
        }
    }

    /// <summary>
    /// Remove an allergy from current user's profile
    /// </summary>
    [HttpDelete("profile/{allergenId}")]
    [Authorize]
    public async Task<IActionResult> RemoveAllergy(int allergenId)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            // Find the user allergy record by userId and allergenId
            var existingAllergy = await _mediator.Send(new GetUserAllergiesQuery { UserId = userId.Value });
            if (!existingAllergy.IsSucceeded)
                return BadRequest(existingAllergy);

            var userAllergy = existingAllergy.Data?.FirstOrDefault(ua => ua.AllergenId == allergenId);
            if (userAllergy == null)
                return NotFound($"Allergy with allergen ID {allergenId} not found for current user");

            var command = new RemoveUserAllergyCommand
            {
                UserId = userId.Value,
                UserAllergyId = userAllergy.Id
            };
            
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user allergy for allergen {AllergenId}", allergenId);
            return StatusCode(500, "An error occurred while removing the allergy");
        }
    }

    // TODO: Implement additional CQRS commands and queries for the following endpoints:
    // - GetUserAllergyById
    // - HasAllergy
    // - GetSevereAllergies
    // - GetOutgrownAllergies
} 