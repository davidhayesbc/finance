using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.CorrectHoldingSecurity;

public class CorrectHoldingSecurityCommandHandler
    : IRequestHandler<CorrectHoldingSecurityCommand, HoldingResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SecurityResolutionService _securityResolutionService;

    public CorrectHoldingSecurityCommandHandler(
        IUnitOfWork unitOfWork,
        SecurityResolutionService securityResolutionService
    )
    {
        _unitOfWork = unitOfWork;
        _securityResolutionService = securityResolutionService;
    }

    public async Task<HoldingResponse> Handle(
        CorrectHoldingSecurityCommand request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            throw new InvalidOperationException("Holding not found.");

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Holding not found.");

        var identifiers = BuildIdentifiers(request.Cusip, request.Isin);
        var resolvedSecurity = await _securityResolutionService.ResolveOrCreateAsync(
            request.Symbol,
            request.SecurityName,
            account.Currency,
            source: request.Source,
            exchange: request.Exchange,
            identifiers: identifiers,
            cancellationToken: cancellationToken
        );

        holding.RebindSecurity(resolvedSecurity);
        await _unitOfWork.Holdings.UpdateAsync(holding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return HoldingMapper.ToResponse(holding);
    }

    private static IReadOnlyDictionary<SecurityIdentifierType, string>? BuildIdentifiers(
        string? cusip,
        string? isin
    )
    {
        Dictionary<SecurityIdentifierType, string>? identifiers = null;

        if (!string.IsNullOrWhiteSpace(cusip))
        {
            identifiers ??= [];
            identifiers[SecurityIdentifierType.Cusip] = cusip.Trim();
        }

        if (!string.IsNullOrWhiteSpace(isin))
        {
            identifiers ??= [];
            identifiers[SecurityIdentifierType.Isin] = isin.Trim();
        }

        return identifiers;
    }
}
