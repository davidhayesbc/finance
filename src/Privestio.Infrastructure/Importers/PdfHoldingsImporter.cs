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
        var extractor = new SunLifePdfExtractor();
        var result = extractor.Extract(fileStream);
        return Task.FromResult(result);
    }
}
