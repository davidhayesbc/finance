using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.PreviewFile;

public class PreviewFileQueryHandler(IFilePreviewService filePreviewService)
    : IRequestHandler<PreviewFileQuery, FilePreviewResponse>
{
    public Task<FilePreviewResponse> Handle(
        PreviewFileQuery request,
        CancellationToken cancellationToken
    ) =>
        filePreviewService.PreviewAsync(
            request.FileStream,
            request.FileName,
            request.MaxSampleRows
        );
}
