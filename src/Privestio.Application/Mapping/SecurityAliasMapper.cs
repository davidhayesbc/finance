using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class SecurityAliasMapper
{
    public static SecurityAliasResponse ToResponse(SecurityAlias alias) =>
        new()
        {
            Id = alias.Id,
            SecurityId = alias.SecurityId,
            Symbol = alias.Symbol,
            Source = alias.Source,
            IsPrimary = alias.IsPrimary,
        };
}
