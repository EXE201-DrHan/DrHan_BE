using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using MediatR;

namespace DrHan.Application.Services.MealPlanServices.Queries.GetUserMealPlans;

public record GetUserMealPlansQuery(PaginationRequest Pagination) : IRequest<AppResponse<PaginatedList<MealPlanDto>>>; 