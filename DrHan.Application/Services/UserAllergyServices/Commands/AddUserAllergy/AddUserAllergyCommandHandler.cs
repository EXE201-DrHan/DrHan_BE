using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.UserAllergyServices.Commands.AddUserAllergy;

public class AddUserAllergyCommandHandler : IRequestHandler<AddUserAllergyCommand, AppResponse<UserAllergyDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AddUserAllergyCommandHandler> _logger;

    public AddUserAllergyCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AddUserAllergyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<UserAllergyDto>> Handle(AddUserAllergyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify allergen exists
            var allergen = await _unitOfWork.Repository<Allergen>().FindAsync(a => a.Id == request.AllergenId);
            if (allergen == null)
            {
                return new AppResponse<UserAllergyDto>()
                    .SetErrorResponse("AllergenId", "Allergen not found");
            }

            // Check if user already has this allergy
            var existingAllergy = await _unitOfWork.Repository<UserAllergy>()
                .FindAsync(ua => ua.UserId == request.UserId && ua.AllergenId == request.AllergenId);

            if (existingAllergy != null)
            {
                return new AppResponse<UserAllergyDto>()
                    .SetErrorResponse("AllergenId", "User already has this allergy in their profile");
            }

            var userAllergy = _mapper.Map<UserAllergy>(request);
            userAllergy.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<UserAllergy>().AddAsync(userAllergy);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Reload with allergen data
            var createdAllergy = await _unitOfWork.Repository<UserAllergy>()
                .ListAsync(
                    filter: ua => ua.Id == userAllergy.Id,
                    includeProperties: q => q.Include(ua => ua.Allergen));
            var result = createdAllergy.FirstOrDefault();

            var userAllergyDto = _mapper.Map<UserAllergyDto>(result);

            return new AppResponse<UserAllergyDto>()
                .SetSuccessResponse(userAllergyDto, "Success", "Allergy added to user profile successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding allergy to user {UserId}", request.UserId);
            return new AppResponse<UserAllergyDto>()
                .SetErrorResponse("AddUserAllergy", "An error occurred while adding the allergy to user profile");
        }
    }
} 