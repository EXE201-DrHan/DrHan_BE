using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.UserAllergyServices.Commands.AddMultipleUserAllergies;

public class AddMultipleUserAllergiesCommandHandler : IRequestHandler<AddMultipleUserAllergiesCommand, AppResponse<BulkUserAllergyResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AddMultipleUserAllergiesCommandHandler> _logger;

    public AddMultipleUserAllergiesCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AddMultipleUserAllergiesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<BulkUserAllergyResponseDto>> Handle(AddMultipleUserAllergiesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = new BulkUserAllergyResponseDto();
            
            // Get all allergens to validate they exist
            var allergens = await _unitOfWork.Repository<Allergen>()
                .ListAsync(filter: a => request.AllergenIds.Contains(a.Id));

            var existingAllergenIds = allergens.Select(a => a.Id).ToHashSet();
            var missingAllergenIds = request.AllergenIds.Where(id => !existingAllergenIds.Contains(id)).ToList();

            // Add errors for missing allergens
            foreach (var missingId in missingAllergenIds)
            {
                response.Errors.Add(new BulkAllergyErrorDto
                {
                    AllergenId = missingId,
                    Error = "Allergen not found"
                });
            }

            // Get existing user allergies to check for duplicates
            var existingUserAllergies = await _unitOfWork.Repository<UserAllergy>()
                .ListAsync(filter: ua => ua.UserId == request.UserId && request.AllergenIds.Contains((int)ua.AllergenId));

            var existingUserAllergyIds = existingUserAllergies.Select(ua => ua.AllergenId).ToHashSet();
            
            // Add errors for duplicate allergies
            foreach (var existingId in existingUserAllergyIds)
            {
                response.Errors.Add(new BulkAllergyErrorDto
                {
                    AllergenId = (int)existingId,
                    Error = "User already has this allergy in their profile"
                });
            }

            // Process valid allergen IDs (those that exist and user doesn't already have)
            var validAllergenIds = request.AllergenIds
                .Where(id => existingAllergenIds.Contains(id) && !existingUserAllergyIds.Contains(id))
                .ToList();
            var successfulAllergies = new List<UserAllergy>();

            foreach (var allergenId in validAllergenIds)
            {
                try
                {
                    var userAllergy = new UserAllergy
                    {
                        UserId = request.UserId,
                        AllergenId = allergenId,
                        Severity = request.Severity,
                        DiagnosisDate = request.DiagnosisDate,
                        DiagnosedBy = request.DiagnosedBy,
                        LastReactionDate = request.LastReactionDate,
                        AvoidanceNotes = request.AvoidanceNotes,
                        Outgrown = request.Outgrown,
                        OutgrownDate = request.OutgrownDate,
                        NeedsVerification = request.NeedsVerification,
                        CreateAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Repository<UserAllergy>().AddAsync(userAllergy);
                    successfulAllergies.Add(userAllergy);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding allergy {AllergenId} to user {UserId}", allergenId, request.UserId);
                    response.Errors.Add(new BulkAllergyErrorDto
                    {
                        AllergenId = allergenId,
                        Error = "Failed to add allergy due to an internal error"
                    });
                }
            }

            // Save changes if there are successful additions
            if (successfulAllergies.Any())
            {
                await _unitOfWork.CompleteAsync(cancellationToken);

                // Reload with allergen data
                var createdAllergyIds = successfulAllergies.Select(ua => ua.Id).ToList();
                var createdAllergies = await _unitOfWork.Repository<UserAllergy>()
                    .ListAsync(
                        filter: ua => createdAllergyIds.Contains(ua.Id),
                        includeProperties: q => q.Include(ua => ua.Allergen));

                response.SuccessfullyAdded = _mapper.Map<List<UserAllergyDto>>(createdAllergies);
            }

            // Set summary statistics
            response.TotalProcessed = request.AllergenIds.Count;
            response.SuccessCount = response.SuccessfullyAdded.Count;
            response.ErrorCount = response.Errors.Count;

            var appResponse = new AppResponse<BulkUserAllergyResponseDto>();

            if (response.SuccessCount > 0 && response.ErrorCount == 0)
            {
                // All successful
                return appResponse.SetSuccessResponse(response, "Success", $"All {response.SuccessCount} allergies added successfully");
            }
            else if (response.SuccessCount > 0 && response.ErrorCount > 0)
            {
                // Partial success
                return appResponse.SetSuccessResponse(response, "PartialSuccess", 
                    $"{response.SuccessCount} allergies added successfully, {response.ErrorCount} failed");
            }
            else
            {
                // All failed
                return appResponse.SetErrorResponse("AddMultipleAllergies",
                    $"Failed to add any allergies. {response.ErrorCount} errors occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple allergies to user {UserId}", request.UserId);
            return new AppResponse<BulkUserAllergyResponseDto>()
                .SetErrorResponse("AddMultipleUserAllergies", "An error occurred while adding multiple allergies to user profile");
        }
    }
} 