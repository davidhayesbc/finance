using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateTag;

public record CreateTagCommand(string Name, Guid OwnerId) : IRequest<TagResponse>;
