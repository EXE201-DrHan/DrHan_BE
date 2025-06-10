using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AllergenServices.Commands.DeleteAllergen;

public class DeleteAllergenCommandHandler : IRequestHandler<DeleteAllergenCommand, AppResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAllergenCommandHandler> _logger;

    public DeleteAllergenCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteAllergenCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppResponse<bool>> Handle(DeleteAllergenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var allergen = await _unitOfWork.Repository<Allergen>().FindAsync(a => a.Id == request.Id);
            if (allergen == null)
            {
                return new AppResponse<bool>()
                    .SetErrorResponse("Id", "Allergen not found");
            }

            // Check if allergen is referenced by any user allergies
            var hasUserAllergies = await _unitOfWork.Repository<Domain.Entities.Users.UserAllergy>()
                .ExistsAsync(ua => ua.AllergenId == request.Id);

            if (hasUserAllergies)
            {
                return new AppResponse<bool>()
                    .SetErrorResponse("Delete", "Cannot delete allergen because it is referenced by user allergy records");
            }

            _unitOfWork.Repository<Allergen>().Delete(allergen);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new AppResponse<bool>()
                .SetSuccessResponse(true, "Success", "Allergen deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting allergen with ID {Id}", request.Id);
            return new AppResponse<bool>()
                .SetErrorResponse("DeleteAllergen", "An error occurred while deleting the allergen");
        }
    }
} 