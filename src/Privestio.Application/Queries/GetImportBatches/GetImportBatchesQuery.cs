using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetImportBatches;

public record GetImportBatchesQuery(Guid UserId) : IRequest<IReadOnlyList<ImportBatchResponse>>;
