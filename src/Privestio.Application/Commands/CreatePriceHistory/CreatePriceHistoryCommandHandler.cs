using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreatePriceHistory;

public class CreatePriceHistoryCommandHandler
    : IRequestHandler<CreatePriceHistoryCommand, IReadOnlyList<PriceHistoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SecurityResolutionService _securityResolutionService;

    public CreatePriceHistoryCommandHandler(
        IUnitOfWork unitOfWork,
        SecurityResolutionService securityResolutionService
    )
    {
        _unitOfWork = unitOfWork;
        _securityResolutionService = securityResolutionService;
    }

    public async Task<IReadOnlyList<PriceHistoryResponse>> Handle(
        CreatePriceHistoryCommand request,
        CancellationToken cancellationToken
    )
    {
        var resolvedEntries = new List<(PriceHistoryEntry Entry, Security Security)>();
        foreach (var entry in request.Entries)
        {
            var security = await _securityResolutionService.ResolveOrCreateAsync(
                entry.Symbol,
                null,
                entry.Currency,
                preferSymbolAsDisplay: false,
                cancellationToken: cancellationToken
            );
            resolvedEntries.Add((entry, security));
        }

        var keys = resolvedEntries.Select(e => (e.Security.Id, e.Entry.AsOfDate)).ToList();

        var existing = await _unitOfWork.PriceHistories.GetExistingKeysAsync(
            keys,
            cancellationToken
        );

        var newEntries = resolvedEntries
            .Where(e => !existing.Contains((e.Security.Id, e.Entry.AsOfDate)))
            .Select(e => new PriceHistory(
                e.Security.Id,
                e.Security.DisplaySymbol,
                e.Entry.Symbol,
                new Money(e.Entry.Price, e.Entry.Currency),
                e.Entry.AsOfDate,
                e.Entry.Source
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
