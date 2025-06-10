using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AllergenServices.Commands.CreateAllergen;

public class CreateAllergenCommandHandler : IRequestHandler<CreateAllergenCommand, AppResponse<AllergenDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateAllergenCommandHandler> _logger;

    public CreateAllergenCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateAllergenCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<AllergenDto>> Handle(CreateAllergenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if allergen with same name already exists
            var existingAllergen = await _unitOfWork.Repository<Allergen>()
                .FindAsync(a => a.Name.ToLower() == request.Name.ToLower());

            if (existingAllergen != null)
            {
                return new AppResponse<AllergenDto>()
                    .SetErrorResponse("Name", "An allergen with this name already exists");
            }

            var allergen = _mapper.Map<Allergen>(request);
            allergen.CreateAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Allergen>().AddAsync(allergen);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var allergenDto = _mapper.Map<AllergenDto>(allergen);

            return new AppResponse<AllergenDto>()
                .SetSuccessResponse(allergenDto, "Success", "Allergen created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating allergen");
            return new AppResponse<AllergenDto>()
                .SetErrorResponse("CreateAllergen", "An error occurred while creating the allergen");
        }
    }
} 