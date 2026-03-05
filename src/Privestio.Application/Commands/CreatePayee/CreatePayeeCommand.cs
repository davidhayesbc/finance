using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreatePayee;

public record CreatePayeeCommand(string DisplayName, Guid OwnerId, Guid? DefaultCategoryId = null)
    : IRequest<PayeeResponse>;
