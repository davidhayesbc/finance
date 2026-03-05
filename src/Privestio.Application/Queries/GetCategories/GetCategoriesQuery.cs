using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetCategories;

public record GetCategoriesQuery(Guid OwnerId) : IRequest<IReadOnlyList<CategoryResponse>>;
