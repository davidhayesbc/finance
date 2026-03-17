using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateHolding;

public class CreateHoldingCommandHandler : IRequestHandler<CreateHoldingCommand, HoldingResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SecurityResolutionService _securityResolutionService;

    public CreateHoldingCommandHandler(
        IUnitOfWork unitOfWork,
        SecurityResolutionService securityResolutionService
    )
    {
        _unitOfWork = unitOfWork;
        _securityResolutionService = securityResolutionService;
    }

    public async Task<HoldingResponse> Handle(
        CreateHoldingCommand request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Account not found.");

        var security = await _securityResolutionService.ResolveOrCreateAsync(
            request.Symbol,
            request.SecurityName,
            request.Currency,
            cancellationToken: cancellationToken
        );

        var existing = await _unitOfWork.Holdings.GetByAccountIdAndSecurityIdAsync(
            request.AccountId,
            security.Id,
            cancellationToken
        );
        if (existing is not null)
            throw new InvalidOperationException("Holding already exists for symbol.");

        var holding = new Holding(
            request.AccountId,
            security.Id,
            security.DisplaySymbol,
            security.Name,
            request.Quantity,
            new Money(request.AverageCostPerUnit, request.Currency),
            request.Notes
        );
        holding.RebindSecurity(security);

        await _unitOfWork.Holdings.AddAsync(holding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return HoldingMapper.ToResponse(holding);
    }
}
