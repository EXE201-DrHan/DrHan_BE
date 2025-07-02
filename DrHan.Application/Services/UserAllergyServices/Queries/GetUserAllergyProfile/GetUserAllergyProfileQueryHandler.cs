using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.UserAllergyServices.Queries.GetUserAllergyProfile;

public class GetUserAllergyProfileQueryHandler : IRequestHandler<GetUserAllergyProfileQuery, AppResponse<UserAllergyProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserAllergyProfileQueryHandler> _logger;

    public GetUserAllergyProfileQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetUserAllergyProfileQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<UserAllergyProfileDto>> Handle(GetUserAllergyProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userAllergies = await _unitOfWork.Repository<UserAllergy>()
                .ListAsync(
                    filter: ua => ua.UserId == request.UserId,
                    includeProperties: q => q.Include(ua => ua.Allergen));

            var allergiesDto = _mapper.Map<List<UserAllergyDto>>(userAllergies);



            var profile = new UserAllergyProfileDto
            {
                UserId = request.UserId,
                Allergies = allergiesDto,
                TotalAllergies = allergiesDto.Count,
                SevereAllergies = allergiesDto.Count(a => a.Severity?.ToLower() == "severe"),
                OutgrownAllergies = allergiesDto.Count(a => a.Outgrown == true)
            };

            return new AppResponse<UserAllergyProfileDto>()
                .SetSuccessResponse(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergy profile for user {UserId}", request.UserId);
            return new AppResponse<UserAllergyProfileDto>()
                .SetErrorResponse("GetUserAllergyProfile", "An error occurred while retrieving the allergy profile");
        }
    }
}