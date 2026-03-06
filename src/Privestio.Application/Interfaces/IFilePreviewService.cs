using Privestio.Contracts.Responses;

namespace Privestio.Application.Interfaces;

public interface IFilePreviewService
{
    Task<FilePreviewResponse> PreviewAsync(Stream fileStream, string fileName, int maxSampleRows);
}
