using MediatR;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.ImportTransactions;

public record ImportTransactionsCommand(
    Stream FileStream,
    string FileName,
    Guid AccountId,
    Guid UserId,
    Guid? MappingId = null,
    ImportPolicy Policy = ImportPolicy.SkipInvalid
) : IRequest<ImportResultResponse>;
