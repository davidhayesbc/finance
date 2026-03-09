using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.GenerateAmortizationSchedule;

public class GenerateAmortizationScheduleCommandHandler
    : IRequestHandler<GenerateAmortizationScheduleCommand, AmortizationScheduleResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AmortizationScheduleService _amortizationService;

    public GenerateAmortizationScheduleCommandHandler(
        IUnitOfWork unitOfWork,
        AmortizationScheduleService amortizationService
    )
    {
        _unitOfWork = unitOfWork;
        _amortizationService = amortizationService;
    }

    public async Task<AmortizationScheduleResponse> Handle(
        GenerateAmortizationScheduleCommand request,
        CancellationToken cancellationToken
    )
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {request.AccountId} not found.");

        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot generate amortization for another user's account."
            );

        var entries = _amortizationService.GenerateSchedule(
            request.AccountId,
            request.PrincipalAmount,
            request.AnnualInterestRate,
            request.TermMonths,
            request.MonthlyPaymentAmount,
            request.StartDate,
            request.Currency
        );

        await _unitOfWork.AmortizationEntries.DeleteByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );
        await _unitOfWork.AmortizationEntries.AddRangeAsync(entries, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AmortizationScheduleResponse
        {
            AccountId = request.AccountId,
            Entries = entries.Select(AmortizationEntryMapper.ToResponse).ToList(),
            TotalInterest = entries.Sum(e => e.InterestAmount.Amount),
            TotalPrincipal = entries.Sum(e => e.PrincipalAmount.Amount),
            Currency = request.Currency,
        };
    }
}
