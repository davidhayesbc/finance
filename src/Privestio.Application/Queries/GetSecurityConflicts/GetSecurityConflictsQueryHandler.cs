using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Services;

namespace Privestio.Application.Queries.GetSecurityConflicts;

public class GetSecurityConflictsQueryHandler
    : IRequestHandler<GetSecurityConflictsQuery, IReadOnlyList<SecurityConflictResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSecurityConflictsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SecurityConflictResponse>> Handle(
        GetSecurityConflictsQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(request.UserId, cancellationToken);
        if (accounts.Count == 0)
            return [];

        var conflicts = new List<SecurityConflictResponse>();

        foreach (var account in accounts)
        {
            var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(account.Id, cancellationToken);
            foreach (var holding in holdings)
            {
                var normalized = SecuritySymbolMatcher.Normalize(holding.Symbol);
                var candidates = await _unitOfWork.Securities.GetCandidatesBySymbolAsync(
                    normalized,
                    cancellationToken
                );

                if (candidates.Count <= 1)
                    continue;

                conflicts.Add(
                    new SecurityConflictResponse
                    {
                        HoldingId = holding.Id,
                        AccountId = account.Id,
                        AccountName = account.Name,
                        HoldingSymbol = holding.Symbol,
                        HoldingSecurityName = holding.SecurityName,
                        Candidates = candidates
                            .Select(c => new SecurityConflictCandidateResponse
                            {
                                SecurityId = c.Id,
                                DisplaySymbol = c.DisplaySymbol,
                                Name = c.Name,
                                Currency = c.Currency,
                                Exchange = c.Exchange,
                            })
                            .ToList(),
                    }
                );
            }
        }

        return conflicts.AsReadOnly();
    }
}
