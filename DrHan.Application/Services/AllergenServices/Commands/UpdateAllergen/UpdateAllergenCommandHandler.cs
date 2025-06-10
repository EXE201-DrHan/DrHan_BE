using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AllergenServices.Commands.UpdateAllergen;

public class UpdateAllergenCommandHandler : IRequestHandler<UpdateAllergenCommand, AppResponse<AllergenDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateAllergenCommandHandler> _logger;

    public UpdateAllergenCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateAllergenCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<AllergenDto>> Handle(UpdateAllergenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var allergen = await _unitOfWork.Repository<Allergen>().FindAsync(a => a.Id == request.Id);
            if (allergen == null)
            {
                return new AppResponse<AllergenDto>()
                    .SetErrorResponse("Id", "Allergen not found");
            }

            // Check if name is being changed and if it conflicts with existing allergen
            if (!string.IsNullOrEmpty(request.Name) && 
                !allergen.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var nameExists = await _unitOfWork.Repository<Allergen>()
                    .ExistsAsync(a => a.Name.ToLower() == request.Name.ToLower() && a.Id != request.Id);

                if (nameExists)
                {
                    return new AppResponse<AllergenDto>()
                        .SetErrorResponse("Name", "An allergen with this name already exists");
                }
            }

            // Map non-null properties from request to entity
            if (request.Name != null) allergen.Name = request.Name;
            if (request.Category != null) allergen.Category = request.Category;
            if (request.ScientificName != null) allergen.ScientificName = request.ScientificName;
            if (request.Description != null) allergen.Description = request.Description;
            if (request.IsFdaMajor.HasValue) allergen.IsFdaMajor = request.IsFdaMajor;
            if (request.IsEuMajor.HasValue) allergen.IsEuMajor = request.IsEuMajor;

            allergen.UpdateAt = DateTime.UtcNow;

            _unitOfWork.Repository<Allergen>().Update(allergen);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var allergenDto = _mapper.Map<AllergenDto>(allergen);

            return new AppResponse<AllergenDto>()
                .SetSuccessResponse(allergenDto, "Success", "Allergen updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allergen with ID {Id}", request.Id);
            return new AppResponse<AllergenDto>()
                .SetErrorResponse("UpdateAllergen", "An error occurred while updating the allergen");
        }
    }
} 