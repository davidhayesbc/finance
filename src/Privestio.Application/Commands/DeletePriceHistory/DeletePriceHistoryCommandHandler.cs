using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeletePriceHistory;

/// <summary>
/// Handles deletion of a price history entry.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Ownership model:</strong> PriceHistory entries are global shared reference data
/// (market prices), not per-user private data. Any authenticated user may manage price entries
/// because prices are objective facts shared across all portfolios. This is analogous to
/// exchange rates and security master data.
/// </para>
/// <para>
/// Per-user authorisation is not applied here by design. If multi-tenant isolation is needed
/// in the future, price entries should be scoped by household or tenant rather than individual user.
/// </para>
/// </remarks>
public class DeletePriceHistoryCommandHandler : IRequestHandler<DeletePriceHistoryCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeletePriceHistoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeletePriceHistoryCommand request,
        CancellationToken cancellationToken
    )
    {
        var entry = await _unitOfWork.PriceHistories.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
            return false;

        await _unitOfWork.PriceHistories.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
