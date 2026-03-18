using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class SecurityIdentifierMapper
{
    public static SecurityIdentifierResponse ToResponse(SecurityIdentifier identifier) =>
        new()
        {
            Id = identifier.Id,
            SecurityId = identifier.SecurityId,
            IdentifierType = identifier.IdentifierType.ToString(),
            Value = identifier.Value,
            IsPrimary = identifier.IsPrimary,
        };
}
