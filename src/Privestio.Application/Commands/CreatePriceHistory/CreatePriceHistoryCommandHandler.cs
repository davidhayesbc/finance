using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreatePriceHistory;

public class CreatePriceHistoryCommandHandler
    : IRequestHandler<CreatePriceHistoryCommand, IReadOnlyList<PriceHistoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePriceHistoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PriceHistoryResponse>> Handle(
        CreatePriceHistoryCommand request,
        CancellationToken cancellationToken
    )
    {
        var keys = request
            .Entries.Select(e => (e.Symbol.ToUpperInvariant().Trim(), e.AsOfDate))
            .ToList();

        var existing = await _unitOfWork.PriceHistories.GetExistingKeysAsync(
            keys,
            cancellationToken
        );

        var newEntries = request
            .Entries.Where(e =>
                !existing.Contains((e.Symbol.ToUpperInvariant().Trim(), e.AsOfDate))
            )
            .Select(e => new PriceHistory(
                e.Symbol,
                new Money(e.Price, e.Currency),
                e.AsOfDate,
                e.Source
            ))
            .ToList();

        if (newEntries.Count > 0)
        {
            await _unitOfWork.PriceHistories.AddRangeAsync(newEntries, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return newEntries.Select(PriceHistoryMapper.ToResponse).ToList().AsReadOnly();
    }
}
