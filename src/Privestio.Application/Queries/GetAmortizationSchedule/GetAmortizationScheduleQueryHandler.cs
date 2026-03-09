using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAmortizationSchedule;

public class GetAmortizationScheduleQueryHandler
    : IRequestHandler<GetAmortizationScheduleQuery, AmortizationScheduleResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAmortizationScheduleQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AmortizationScheduleResponse> Handle(
        GetAmortizationScheduleQuery request,
        CancellationToken cancellationToken
    )
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {request.AccountId} not found.");

        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot view another user's amortization schedule."
            );

        var entries = await _unitOfWork.AmortizationEntries.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );

        return new AmortizationScheduleResponse
        {
            AccountId = request.AccountId,
            Entries = entries.Select(AmortizationEntryMapper.ToResponse).ToList(),
            TotalInterest = entries.Sum(e => e.InterestAmount.Amount),
            TotalPrincipal = entries.Sum(e => e.PrincipalAmount.Amount),
            Currency = account.Currency,
        };
    }
}
