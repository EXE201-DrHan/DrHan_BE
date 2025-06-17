using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using MediatR;

namespace DrHan.Application.Services.MealPlanServices.Commands.AddMealEntry;

public record AddMealEntryCommand(AddMealEntryDto MealEntry) : IRequest<AppResponse<MealEntryDto>>; 