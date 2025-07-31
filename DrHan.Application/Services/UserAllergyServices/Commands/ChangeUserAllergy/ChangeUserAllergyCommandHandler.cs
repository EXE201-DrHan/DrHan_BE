using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.UserAllergyServices.Commands.ChangeUserAllergy;

public class ChangeUserAllergyCommandHandler : IRequestHandler<ChangeUserAllergyCommand, AppResponse<UserAllergyDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ChangeUserAllergyCommandHandler> _logger;

    public ChangeUserAllergyCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ChangeUserAllergyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<UserAllergyDto>> Handle(ChangeUserAllergyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify new allergen exists
            var newAllergen = await _unitOfWork.Repository<Allergen>().FindAsync(a => a.Id == request.NewAllergenId);
            if (newAllergen == null)
            {
                return new AppResponse<UserAllergyDto>()
                    .SetErrorResponse("NewAllergenId", "New allergen not found");
            }

            // Check if user already has the new allergen
            var existingNewAllergy = await _unitOfWork.Repository<UserAllergy>()
                .FindAsync(ua => ua.UserId == request.UserId && ua.AllergenId == request.NewAllergenId);

            if (existingNewAllergy != null)
            {
                return new AppResponse<UserAllergyDto>()
                    .SetErrorResponse("NewAllergenId", "User already has this allergy in their profile");
            }

            // Find the current user allergy record
            var currentUserAllergy = await _unitOfWork.Repository<UserAllergy>()
                .FindAsync(ua => ua.UserId == request.UserId && ua.AllergenId == request.CurrentAllergenId);

            if (currentUserAllergy == null)
            {
                return new AppResponse<UserAllergyDto>()
                    .SetErrorResponse("CurrentAllergenId", "Current allergy not found for this user");
            }

            // Create new allergy with updated information
            var newUserAllergy = new UserAllergy
            {
                UserId = request.UserId,
                AllergenId = request.NewAllergenId,
                Severity = request.Severity ?? currentUserAllergy.Severity,
                DiagnosisDate = request.DiagnosisDate ?? currentUserAllergy.DiagnosisDate,
                DiagnosedBy = request.DiagnosedBy ?? currentUserAllergy.DiagnosedBy,
                LastReactionDate = request.LastReactionDate ?? currentUserAllergy.LastReactionDate,
                AvoidanceNotes = request.AvoidanceNotes ?? currentUserAllergy.AvoidanceNotes,
                Outgrown = request.Outgrown ?? currentUserAllergy.Outgrown,
                OutgrownDate = request.OutgrownDate ?? currentUserAllergy.OutgrownDate,
                NeedsVerification = request.NeedsVerification ?? currentUserAllergy.NeedsVerification,
                CreateAt = DateTime.Now
            };

            // Remove old allergy and add new one in a transaction
            _unitOfWork.Repository<UserAllergy>().Delete(currentUserAllergy);
            await _unitOfWork.Repository<UserAllergy>().AddAsync(newUserAllergy);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Reload with allergen data
            var createdAllergy = await _unitOfWork.Repository<UserAllergy>()
                .ListAsync(
                    filter: ua => ua.Id == newUserAllergy.Id,
                    includeProperties: q => q.Include(ua => ua.Allergen));
            var result = createdAllergy.FirstOrDefault();

            var userAllergyDto = _mapper.Map<UserAllergyDto>(result);

            _logger.LogInformation("User {UserId} changed allergy from {OldAllergenId} to {NewAllergenId}", 
                request.UserId, request.CurrentAllergenId, request.NewAllergenId);

            return new AppResponse<UserAllergyDto>()
                .SetSuccessResponse(userAllergyDto, "Success", $"Allergy changed from {currentUserAllergy.Allergen?.Name} to {newAllergen.Name} successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing allergy for user {UserId} from {CurrentAllergenId} to {NewAllergenId}", 
                request.UserId, request.CurrentAllergenId, request.NewAllergenId);
            return new AppResponse<UserAllergyDto>()
                .SetErrorResponse("ChangeUserAllergy", "An error occurred while changing the allergy");
        }
    }
} 