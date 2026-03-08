using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.ContributeSinkingFund;

public class ContributeSinkingFundCommandHandler
    : IRequestHandler<ContributeSinkingFundCommand, SinkingFundResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public ContributeSinkingFundCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SinkingFundResponse> Handle(
        ContributeSinkingFundCommand request,
        CancellationToken cancellationToken
    )
    {
        var fund =
            await _unitOfWork.SinkingFunds.GetByIdAsync(request.SinkingFundId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sinking fund {request.SinkingFundId} not found.");

        if (fund.UserId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot contribute to another user's sinking fund."
            );

        fund.RecordContribution(new Money(request.Amount, request.Currency));

        await _unitOfWork.SinkingFunds.UpdateAsync(fund, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SinkingFundMapper.ToResponse(fund);
    }
}
