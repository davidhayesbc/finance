using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string Type,
    Guid OwnerId,
    string? Icon = null,
    int SortOrder = 0,
    Guid? ParentCategoryId = null
) : IRequest<CategoryResponse>;
