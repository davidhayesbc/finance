using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateHolding;

public class CreateHoldingCommandHandler : IRequestHandler<CreateHoldingCommand, HoldingResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateHoldingCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HoldingResponse> Handle(
        CreateHoldingCommand request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Account not found.");

        var existing = await _unitOfWork.Holdings.GetByAccountIdAndSymbolAsync(
            request.AccountId,
            request.Symbol,
            cancellationToken
        );
        if (existing is not null)
            throw new InvalidOperationException("Holding already exists for symbol.");

        var holding = new Holding(
            request.AccountId,
            request.Symbol,
            request.SecurityName,
            request.Quantity,
            new Money(request.AverageCostPerUnit, request.Currency),
            request.Notes
        );

        await _unitOfWork.Holdings.AddAsync(holding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return HoldingMapper.ToResponse(holding);
    }
}
