using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteSinkingFund;

public class DeleteSinkingFundCommandHandler : IRequestHandler<DeleteSinkingFundCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSinkingFundCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSinkingFundCommand request, CancellationToken cancellationToken)
    {
        var fund =
            await _unitOfWork.SinkingFunds.GetByIdAsync(request.SinkingFundId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sinking fund {request.SinkingFundId} not found.");

        if (fund.UserId != request.UserId)
            throw new UnauthorizedAccessException("Cannot delete another user's sinking fund.");

        await _unitOfWork.SinkingFunds.DeleteAsync(request.SinkingFundId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
