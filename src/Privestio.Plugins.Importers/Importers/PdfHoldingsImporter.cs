using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.Importers;

public class PdfHoldingsImporter : IHoldingsImporter
{
    public string FileFormat => "PDF";

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<HoldingsParseResult> ParseAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default
    )
    {
        // Buffer into a seekable MemoryStream so we can probe, then rewind for extraction.
        var memoryStream = EnsureSeekable(fileStream);

        HoldingsParseResult result;
        if (WorldsourcePdfExtractor.IsWorldsourcePdf(memoryStream))
        {
            memoryStream.Position = 0;
            var extractor = new WorldsourcePdfExtractor();
            result = extractor.Extract(memoryStream);
        }
        else
        {
            memoryStream.Position = 0;
            var extractor = new SunLifePdfExtractor();
            result = extractor.Extract(memoryStream);
        }

        return Task.FromResult(result);
    }

    private static MemoryStream EnsureSeekable(Stream stream)
    {
        if (stream is MemoryStream ms && ms.CanSeek)
            return ms;

        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Position = 0;
        return buffer;
    }
}
