using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.UserAllergyServices.Commands.RemoveUserAllergy;

public class RemoveUserAllergyCommandHandler : IRequestHandler<RemoveUserAllergyCommand, AppResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveUserAllergyCommandHandler> _logger;

    public RemoveUserAllergyCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemoveUserAllergyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppResponse<bool>> Handle(RemoveUserAllergyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the user allergy record
            var userAllergy = await _unitOfWork.Repository<UserAllergy>()
                .FindAsync(ua => ua.Id == request.UserAllergyId && ua.UserId == request.UserId);

            if (userAllergy == null)
            {
                return new AppResponse<bool>()
                    .SetErrorResponse("UserAllergyId", "User allergy not found or you don't have permission to remove it");
            }

            // Remove the user allergy
            _unitOfWork.Repository<UserAllergy>().Delete(userAllergy);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new AppResponse<bool>()
                .SetSuccessResponse(true, "Success", "Allergy removed from user profile successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing allergy {UserAllergyId} from user {UserId}", request.UserAllergyId, request.UserId);
            return new AppResponse<bool>()
                .SetErrorResponse("RemoveUserAllergy", "An error occurred while removing the allergy from user profile");
        }
    }
} 