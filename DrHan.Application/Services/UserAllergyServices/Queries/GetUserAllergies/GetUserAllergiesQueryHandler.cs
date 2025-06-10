using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.UserAllergyServices.Queries.GetUserAllergies;

public class GetUserAllergiesQueryHandler : IRequestHandler<GetUserAllergiesQuery, AppResponse<IEnumerable<UserAllergyDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserAllergiesQueryHandler> _logger;

    public GetUserAllergiesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetUserAllergiesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IEnumerable<UserAllergyDto>>> Handle(GetUserAllergiesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userAllergies = await _unitOfWork.Repository<UserAllergy>()
                .ListAsync(
                    filter: ua => ua.UserId == request.UserId,
                    includeProperties: q => q.Include(ua => ua.Allergen));

            var allergiesDto = _mapper.Map<IEnumerable<UserAllergyDto>>(userAllergies);

            return new AppResponse<IEnumerable<UserAllergyDto>>()
                .SetSuccessResponse(allergiesDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergies for user {UserId}", request.UserId);
            return new AppResponse<IEnumerable<UserAllergyDto>>()
                .SetErrorResponse("GetUserAllergies", "An error occurred while retrieving user allergies");
        }
    }
} 