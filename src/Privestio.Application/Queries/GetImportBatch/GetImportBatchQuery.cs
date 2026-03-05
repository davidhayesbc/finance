using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetImportBatch;

public record GetImportBatchQuery(Guid BatchId) : IRequest<ImportBatchResponse?>;
