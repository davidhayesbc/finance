using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeletePriceHistory;

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
