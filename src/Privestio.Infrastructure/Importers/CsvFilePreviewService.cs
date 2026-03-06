using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Infrastructure.Importers;

public class CsvFilePreviewService : IFilePreviewService
{
    public async Task<FilePreviewResponse> PreviewAsync(
        Stream fileStream,
        string fileName,
        int maxSampleRows
    )
    {
        var extension = Path.GetExtension(fileName).ToUpperInvariant();

        return extension switch
        {
            ".CSV" => await PreviewCsvAsync(fileStream, maxSampleRows),
            _ => new FilePreviewResponse
            {
                FileFormat = extension.TrimStart('.'),
                DetectedColumns = [],
                SampleRows = [],
                TotalRows = 0,
            },
        };
    }

    private static async Task<FilePreviewResponse> PreviewCsvAsync(Stream stream, int maxSampleRows)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        var rows = new List<IReadOnlyList<string>>();
        var totalRows = 0;

        while (await csv.ReadAsync())
        {
            totalRows++;
            if (rows.Count < maxSampleRows)
            {
                var row = new List<string>();
                for (var i = 0; i < headers.Length; i++)
                {
                    row.Add(csv.GetField(i) ?? string.Empty);
                }
                rows.Add(row);
            }
        }

        return new FilePreviewResponse
        {
            FileFormat = "CSV",
            DetectedColumns = headers.ToList(),
            SampleRows = rows,
            TotalRows = totalRows,
        };
    }
}
