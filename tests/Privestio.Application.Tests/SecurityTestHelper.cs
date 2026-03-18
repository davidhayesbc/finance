using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Services;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests;

internal static class SecurityTestHelper
{
    public static Security CreateSecurity(
        string symbol,
        string? name = null,
        string currency = "CAD",
        bool isCashEquivalent = false,
        params (string Symbol, string Source, bool IsPrimary)[] aliases
    )
    {
        var security = new Security(
            symbol,
            symbol,
            string.IsNullOrWhiteSpace(name) ? symbol : name,
            currency,
            isCashEquivalent: isCashEquivalent
        );

        foreach (var alias in aliases)
        {
            security.AddOrUpdateAlias(alias.Symbol, alias.Source, alias.IsPrimary);
        }

        return security;
    }

    public static Holding CreateHolding(
        Guid accountId,
        Security security,
        decimal quantity,
        Money averageCostPerUnit,
        string? notes = null
    )
    {
        var holding = new Holding(
            accountId,
            security.Id,
            security.DisplaySymbol,
            security.Name,
            quantity,
            averageCostPerUnit,
            notes
        );
        holding.RebindSecurity(security);
        return holding;
    }

    public static PriceHistory CreatePriceHistory(
        Security security,
        decimal price,
        DateOnly asOfDate,
        string source = "YahooFinance",
        string? providerSymbol = null
    ) =>
        new(
            security.Id,
            security.DisplaySymbol,
            providerSymbol ?? security.DisplaySymbol,
            new Money(price, security.Currency),
            asOfDate,
            source
        );

    public static SecurityResolutionService CreateSecurityResolutionService(
        Mock<IUnitOfWork> unitOfWork,
        IEnumerable<Security>? initialSecurities = null
    )
    {
        var securities = initialSecurities?.ToList() ?? [];
        var repository = new Mock<ISecurityRepository>();

        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (Guid id, CancellationToken _) => securities.FirstOrDefault(s => s.Id == id)
            );

        repository
            .Setup(r => r.GetByAnySymbolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (string symbol, CancellationToken _) =>
                {
                    var normalized = SecuritySymbolMatcher.Normalize(symbol);
                    return securities.FirstOrDefault(s =>
                        s.CanonicalSymbol == normalized
                        || s.DisplaySymbol == normalized
                        || s.Aliases.Any(a => a.Symbol == normalized)
                    );
                }
            );

        repository
            .Setup(
                r =>
                    r.GetByIdentifierAsync(
                        It.IsAny<SecurityIdentifierType>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(
                (SecurityIdentifierType identifierType, string value, CancellationToken _) =>
                {
                    var normalized = value.Trim().ToUpperInvariant();
                    return securities.FirstOrDefault(s =>
                        s.Identifiers.Any(i =>
                            i.IdentifierType == identifierType
                            && string.Equals(i.Value, normalized, StringComparison.Ordinal)
                        )
                    );
                }
            );

        repository
            .Setup(
                r =>
                    r.GetByAliasContextAsync(
                        It.IsAny<string>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(
                (
                    string symbol,
                    string? source,
                    string? exchange,
                    CancellationToken _
                ) =>
                {
                    var normalized = SecuritySymbolMatcher.Normalize(symbol);
                    var normalizedSource = string.IsNullOrWhiteSpace(source)
                        ? null
                        : source.Trim();
                    var normalizedExchange = string.IsNullOrWhiteSpace(exchange)
                        ? null
                        : exchange.Trim().ToUpperInvariant();

                    return securities.FirstOrDefault(s =>
                        s.Aliases.Any(a =>
                            a.Symbol == normalized
                            && (normalizedSource is null || a.Source == normalizedSource)
                            && (normalizedExchange is null || a.Exchange == normalizedExchange)
                        )
                    );
                }
            );

        repository
            .Setup(r => r.AddAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (Security security, CancellationToken _) =>
                {
                    securities.Add(security);
                    return security;
                }
            );

        repository
            .Setup(r => r.UpdateAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Security security, CancellationToken _) => security);

        unitOfWork.Setup(x => x.Securities).Returns(repository.Object);

        return new SecurityResolutionService(unitOfWork.Object);
    }
}
