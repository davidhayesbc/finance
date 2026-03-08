using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdateSinkingFund;

public class UpdateSinkingFundCommandHandler
    : IRequestHandler<UpdateSinkingFundCommand, SinkingFundResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSinkingFundCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SinkingFundResponse> Handle(
        UpdateSinkingFundCommand request,
        CancellationToken cancellationToken
    )
    {
        var fund =
            await _unitOfWork.SinkingFunds.GetByIdAsync(request.SinkingFundId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sinking fund {request.SinkingFundId} not found.");

        if (fund.UserId != request.UserId)
            throw new UnauthorizedAccessException("Cannot update another user's sinking fund.");

        fund.Rename(request.Name);
        fund.UpdateTarget(new Money(request.TargetAmount, request.Currency), request.DueDate);
        fund.UpdateNotes(request.Notes);

        await _unitOfWork.SinkingFunds.UpdateAsync(fund, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SinkingFundMapper.ToResponse(fund);
    }
}
