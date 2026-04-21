using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;

namespace Privestio.Infrastructure.Ai;

public class OllamaRuleSuggestionService : IOllamaRuleSuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaRuleSuggestionService> _logger;

    public OllamaRuleSuggestionService(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaRuleSuggestionService> logger
    )
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RuleSuggestionDraft>> SuggestRulesAsync(
        IReadOnlyList<RuleSuggestionInputRow> rows,
        int maxSuggestions,
        CancellationToken cancellationToken = default
    )
    {
        if (rows.Count == 0)
            return [];

        var payload = new OllamaChatRequest
        {
            Model = _options.Model,
            Stream = false,
            Options = new OllamaModelOptions { Temperature = _options.Temperature },
            Messages =
            [
                new OllamaMessage
                {
                    Role = "system",
                    Content =
                        "You are a finance rule suggestion assistant. Propose reusable categorization RULES only. "
                        + "Do not categorize individual rows. Focus on recurring merchant descriptors and stable amount ranges.",
                },
                new OllamaMessage
                {
                    Role = "user",
                    Content = BuildUserPrompt(rows, maxSuggestions),
                },
            ],
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "api/chat",
            payload,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Ollama suggestion request failed: {StatusCode}, {Body}",
                (int)response.StatusCode,
                responseText
            );
            throw new InvalidOperationException(
                $"Ollama suggestion request failed with status {(int)response.StatusCode}."
            );
        }

        var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            JsonOptions,
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(chatResponse?.Message?.Content))
            return [];

        var content = chatResponse.Message.Content;
        var envelope = TryParseEnvelope(content) ?? TryParseEnvelope(ExtractJsonObject(content));
        if (envelope?.Suggestions is null)
        {
            _logger.LogWarning("Unable to parse Ollama response content into suggestion envelope.");
            return [];
        }

        return envelope
            .Suggestions.Where(s =>
                !string.IsNullOrWhiteSpace(s.Name)
                && !string.IsNullOrWhiteSpace(s.DescriptionContains)
                && !string.IsNullOrWhiteSpace(s.SuggestedCategoryName)
            )
            .Select(s =>
                new RuleSuggestionDraft(
                    s.Name.Trim(),
                    s.DescriptionContains.Trim(),
                    s.MinAmount,
                    s.MaxAmount,
                    s.SuggestedCategoryName.Trim(),
                    (s.Rationale ?? string.Empty).Trim()
                )
            )
            .Take(Math.Max(1, maxSuggestions))
            .ToList();
    }

    private static string BuildUserPrompt(IReadOnlyList<RuleSuggestionInputRow> rows, int maxSuggestions)
    {
        var rowLines = rows
            .Take(300)
            .Select(r => $"- Description: {r.Description}; Amount: {r.Amount}")
            .ToList();

        return
            "Given the imported transaction sample below, suggest up to "
            + maxSuggestions
            + " reusable categorization rules. Output STRICT JSON with this shape: "
            + "{\"suggestions\":[{\"name\":\"...\",\"descriptionContains\":\"...\",\"minAmount\":number|null,\"maxAmount\":number|null,\"suggestedCategoryName\":\"...\",\"rationale\":\"...\"}]}. "
            + "Do not include markdown or any text outside JSON."
            + Environment.NewLine
            + Environment.NewLine
            + string.Join(Environment.NewLine, rowLines);
    }

    private static AiSuggestionEnvelope? TryParseEnvelope(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<AiSuggestionEnvelope>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
            return content;

        return content[start..(end + 1)];
    }

    private sealed class OllamaChatRequest
    {
        public string Model { get; init; } = string.Empty;
        public bool Stream { get; init; }
        public OllamaModelOptions Options { get; init; } = new();
        public List<OllamaMessage> Messages { get; init; } = [];
    }

    private sealed class OllamaModelOptions
    {
        public double Temperature { get; init; }
    }

    private sealed class OllamaMessage
    {
        public string Role { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        public OllamaMessage? Message { get; init; }
    }

    private sealed class AiSuggestionEnvelope
    {
        public List<AiSuggestion>? Suggestions { get; init; }
    }

    private sealed class AiSuggestion
    {
        public string Name { get; init; } = string.Empty;
        public string DescriptionContains { get; init; } = string.Empty;
        public decimal? MinAmount { get; init; }
        public decimal? MaxAmount { get; init; }
        public string SuggestedCategoryName { get; init; } = string.Empty;
        public string? Rationale { get; init; }
    }
}
