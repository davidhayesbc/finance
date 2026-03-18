using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.AddHoldingSecurityIdentifier;

public class AddHoldingSecurityIdentifierCommandHandler
    : IRequestHandler<AddHoldingSecurityIdentifierCommand, SecurityIdentifierResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddHoldingSecurityIdentifierCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SecurityIdentifierResponse> Handle(
        AddHoldingSecurityIdentifierCommand request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            throw new InvalidOperationException("Holding not found.");

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Holding not found.");

        var security = await _unitOfWork.Securities.GetByIdAsync(
            holding.SecurityId,
            cancellationToken
        );
        if (security is null)
            throw new InvalidOperationException("Security not found.");

        if (
            !Enum.TryParse<SecurityIdentifierType>(
                request.IdentifierType,
                true,
                out var identifierType
            )
        )
            throw new InvalidOperationException("Invalid identifier type.");

        var identifier = security.AddOrUpdateIdentifier(
            identifierType,
            request.Value,
            request.IsPrimary
        );

        await _unitOfWork.Securities.UpdateAsync(security, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SecurityIdentifierMapper.ToResponse(identifier);
    }
}
