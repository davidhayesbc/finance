using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdatePriceHistory;

public class UpdatePriceHistoryCommandHandler
    : IRequestHandler<UpdatePriceHistoryCommand, PriceHistoryResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePriceHistoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PriceHistoryResponse?> Handle(
        UpdatePriceHistoryCommand request,
        CancellationToken cancellationToken
    )
    {
        var entry = await _unitOfWork.PriceHistories.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
            return null;

        entry.UpdatePrice(new Money(request.Price, request.Currency), "Manual");
        await _unitOfWork.PriceHistories.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PriceHistoryMapper.ToResponse(entry);
    }
}
