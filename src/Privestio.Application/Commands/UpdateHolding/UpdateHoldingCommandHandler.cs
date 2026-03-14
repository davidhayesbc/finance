using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdateHolding;

public class UpdateHoldingCommandHandler : IRequestHandler<UpdateHoldingCommand, HoldingResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateHoldingCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HoldingResponse> Handle(
        UpdateHoldingCommand request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            throw new InvalidOperationException("Holding not found.");

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Holding not found.");

        holding.RenameSecurity(request.SecurityName);
        holding.Update(
            request.Quantity,
            new Money(request.AverageCostPerUnit, request.Currency),
            request.Notes
        );

        await _unitOfWork.Holdings.UpdateAsync(holding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return HoldingMapper.ToResponse(holding);
    }
}
