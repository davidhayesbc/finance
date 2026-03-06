using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.PreviewFile;

public record PreviewFileQuery(Stream FileStream, string FileName, int MaxSampleRows = 5)
    : IRequest<FilePreviewResponse>;
