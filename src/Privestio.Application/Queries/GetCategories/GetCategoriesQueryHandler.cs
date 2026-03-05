using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetCategories;

public class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CategoryResponse>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken
    )
    {
        var categories = await _unitOfWork.Categories.GetByOwnerIdAsync(
            request.OwnerId,
            cancellationToken
        );

        return categories
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type.ToString(),
                Icon = c.Icon,
                SortOrder = c.SortOrder,
                IsSystem = c.IsSystem,
                ParentCategoryId = c.ParentCategoryId,
            })
            .ToList();
    }
}
