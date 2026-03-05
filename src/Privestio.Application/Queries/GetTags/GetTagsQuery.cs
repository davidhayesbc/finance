using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetTags;

public record GetTagsQuery(Guid OwnerId) : IRequest<IReadOnlyList<TagResponse>>;
