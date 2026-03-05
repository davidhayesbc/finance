using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryResponse> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken
    )
    {
        var type = Enum.Parse<CategoryType>(request.Type);

        var category = new Category(
            request.Name,
            type,
            request.OwnerId,
            request.ParentCategoryId,
            request.Icon,
            request.SortOrder
        );

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type.ToString(),
            Icon = category.Icon,
            SortOrder = category.SortOrder,
            IsSystem = category.IsSystem,
            ParentCategoryId = category.ParentCategoryId,
        };
    }
}
