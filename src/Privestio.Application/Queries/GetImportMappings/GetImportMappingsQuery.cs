using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetImportMappings;

public record GetImportMappingsQuery(Guid UserId) : IRequest<IReadOnlyList<ImportMappingResponse>>;
