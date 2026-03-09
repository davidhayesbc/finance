using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateValuation;

public class CreateValuationCommandHandler
    : IRequestHandler<CreateValuationCommand, ValuationResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateValuationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValuationResponse> Handle(
        CreateValuationCommand request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Account not found.");

        var estimatedValue = new Money(request.Amount, request.Currency);
        var valuation = new Valuation(
            request.AccountId,
            estimatedValue,
            request.EffectiveDate,
            request.Source,
            request.Notes
        );

        await _unitOfWork.Valuations.AddAsync(valuation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ValuationMapper.ToResponse(valuation);
    }
}
