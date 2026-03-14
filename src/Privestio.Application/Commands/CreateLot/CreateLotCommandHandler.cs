using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateLot;

public class CreateLotCommandHandler : IRequestHandler<CreateLotCommand, LotResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateLotCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LotResponse> Handle(
        CreateLotCommand request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            throw new InvalidOperationException("Holding not found.");

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Holding not found.");

        var lot = new Lot(
            request.HoldingId,
            request.AcquiredDate,
            request.Quantity,
            new Money(request.UnitCost, request.Currency),
            request.Source,
            request.Notes
        );

        await _unitOfWork.Lots.AddAsync(lot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LotMapper.ToResponse(lot);
    }
}
