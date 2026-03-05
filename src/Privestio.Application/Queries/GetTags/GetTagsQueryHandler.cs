using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetTags;

public class GetTagsQueryHandler : IRequestHandler<GetTagsQuery, IReadOnlyList<TagResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTagsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TagResponse>> Handle(
        GetTagsQuery request,
        CancellationToken cancellationToken
    )
    {
        var tags = await _unitOfWork.Tags.GetByOwnerIdAsync(request.OwnerId, cancellationToken);

        return tags.Select(t => new TagResponse { Id = t.Id, Name = t.Name }).ToList();
    }
}
