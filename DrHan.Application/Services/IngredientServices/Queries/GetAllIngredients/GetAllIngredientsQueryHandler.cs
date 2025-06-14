using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.IngredientServices.Queries.GetAllIngredients;

public class GetAllIngredientsQueryHandler : IRequestHandler<GetAllIngredientsQuery, AppResponse<IPaginatedList<IngredientDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllIngredientsQueryHandler> _logger;

    public GetAllIngredientsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetAllIngredientsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IPaginatedList<IngredientDto>>> Handle(GetAllIngredientsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var pagination = new PaginationRequest(request.Page, request.Size);
            
            var ingredients = await _unitOfWork.Repository<Ingredient>().ListAsyncWithPaginated(
                filter: i => (string.IsNullOrEmpty(request.Search) || i.Name.Contains(request.Search) || i.Description.Contains(request.Search)) &&
                            (string.IsNullOrEmpty(request.Category) || i.Category == request.Category),
                orderBy: q => q.OrderBy(i => i.Name),
                pagination: pagination);

            var ingredientDtos = _mapper.Map<IPaginatedList<IngredientDto>>(ingredients);

            return new AppResponse<IPaginatedList<IngredientDto>>()
                .SetSuccessResponse(ingredientDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all ingredients");
            return new AppResponse<IPaginatedList<IngredientDto>>()
                .SetErrorResponse("GetAllIngredients", "An error occurred while retrieving ingredients");
        }
    }
} 