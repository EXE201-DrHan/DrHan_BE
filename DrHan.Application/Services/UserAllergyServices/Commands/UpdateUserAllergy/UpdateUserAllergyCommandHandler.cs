using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.UserAllergyServices.Commands.UpdateUserAllergy;

public class UpdateUserAllergyCommandHandler : IRequestHandler<UpdateUserAllergyCommand, AppResponse<UserAllergyDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateUserAllergyCommandHandler> _logger;

    public UpdateUserAllergyCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateUserAllergyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<UserAllergyDto>> Handle(UpdateUserAllergyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the user allergy record
            var userAllergy = await _unitOfWork.Repository<UserAllergy>()
                .FindAsync(ua => ua.Id == request.UserAllergyId && ua.UserId == request.UserId);

            if (userAllergy == null)
            {
                return new AppResponse<UserAllergyDto>()
                    .SetErrorResponse("UserAllergyId", "User allergy not found or you don't have permission to update it");
            }

            // Update the user allergy using AutoMapper
            _mapper.Map(request, userAllergy);
            userAllergy.UpdateAt = DateTime.Now;

            _unitOfWork.Repository<UserAllergy>().Update(userAllergy);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Reload with allergen data
            var updatedAllergy = await _unitOfWork.Repository<UserAllergy>()
                .ListAsync(
                    filter: ua => ua.Id == userAllergy.Id,
                    includeProperties: q => q.Include(ua => ua.Allergen));
            var result = updatedAllergy.FirstOrDefault();

            var userAllergyDto = _mapper.Map<UserAllergyDto>(result);

            return new AppResponse<UserAllergyDto>()
                .SetSuccessResponse(userAllergyDto, "Success", "User allergy profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allergy {UserAllergyId} for user {UserId}", request.UserAllergyId, request.UserId);
            return new AppResponse<UserAllergyDto>()
                .SetErrorResponse("UpdateUserAllergy", "An error occurred while updating the user allergy profile");
        }
    }
} 