using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.IngredientServices.Queries.GetIngredientById;

public class GetIngredientByIdQueryHandler : IRequestHandler<GetIngredientByIdQuery, AppResponse<IngredientDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIngredientByIdQueryHandler> _logger;

    public GetIngredientByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetIngredientByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IngredientDto>> Handle(GetIngredientByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var ingredient = await _unitOfWork.Repository<Ingredient>().FindAsync(i => i.Id == request.Id);

            if (ingredient == null)
                return new AppResponse<IngredientDto>().SetErrorResponse("Ingredient", "Ingredient not found");

            var dto = _mapper.Map<IngredientDto>(ingredient);
            return new AppResponse<IngredientDto>().SetSuccessResponse(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingredient by id {Id}", request.Id);
            return new AppResponse<IngredientDto>().SetErrorResponse("GetIngredientById", "An error occurred while retrieving the ingredient");
        }
    }
} 