using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            Options = new OllamaModelOptions
            {
                Temperature = _options.Temperature,
                NumPredict = _options.MaxOutputTokens,
            },
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

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("api/chat", payload, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Ollama request timed out after {_options.RequestTimeoutSeconds} seconds.",
                ex
            );
        }

        using (response)
        {

            if (!response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                // Self-heal once when Ollama reports a missing model.
                if (
                    response.StatusCode == System.Net.HttpStatusCode.NotFound
                    && responseText.Contains("model", StringComparison.OrdinalIgnoreCase)
                    && responseText.Contains("not found", StringComparison.OrdinalIgnoreCase)
                )
                {
                    var pulled = await TryPullModelAsync(cancellationToken);
                    if (pulled)
                    {
                        HttpResponseMessage retryResponse;
                        try
                        {
                            retryResponse = await _httpClient.PostAsJsonAsync(
                                "api/chat",
                                payload,
                                cancellationToken
                            );
                        }
                        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                        {
                            throw new TimeoutException(
                                $"Ollama request timed out after {_options.RequestTimeoutSeconds} seconds.",
                                ex
                            );
                        }

                        using (retryResponse)
                        {
                            if (retryResponse.IsSuccessStatusCode)
                            {
                                var retryChatResponse =
                                    await retryResponse.Content.ReadFromJsonAsync<OllamaChatResponse>(
                                        JsonOptions,
                                        cancellationToken
                                    );
                                return ParseSuggestions(retryChatResponse?.Message?.Content, maxSuggestions);
                            }

                            responseText = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                            _logger.LogWarning(
                                "Ollama suggestion retry failed after model pull: {StatusCode}, {Body}",
                                (int)retryResponse.StatusCode,
                                responseText
                            );
                            throw new InvalidOperationException(
                                $"Ollama suggestion request failed with status {(int)retryResponse.StatusCode}."
                            );
                        }
                    }
                }

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

            return ParseSuggestions(chatResponse?.Message?.Content, maxSuggestions);
        }
    }

    private async Task<bool> TryPullModelAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ollama model '{Model}' was missing. Attempting to pull it automatically.",
            _options.Model
        );

        using var pullResponse = await _httpClient.PostAsJsonAsync(
            "api/pull",
            new OllamaPullRequest { Model = _options.Model, Stream = false },
            cancellationToken
        );

        if (pullResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ollama model pull completed for '{Model}'.", _options.Model);
            return true;
        }

        var pullBody = await pullResponse.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Ollama model pull failed for '{Model}': {StatusCode}, {Body}",
            _options.Model,
            (int)pullResponse.StatusCode,
            pullBody
        );
        return false;
    }

    private IReadOnlyList<RuleSuggestionDraft> ParseSuggestions(string? content, int maxSuggestions)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var extracted = ExtractJsonObject(content);
        var suggestions =
            TryParseEnvelope(content)?.Suggestions
            ?? TryParseEnvelope(extracted)?.Suggestions
            ?? TryParseSuggestionList(content)
            ?? TryParseSuggestionList(extracted);

        if (suggestions is null)
        {
            _logger.LogWarning("Unable to parse Ollama response content into suggestion envelope.");
            return [];
        }

        return suggestions
            .Where(s =>
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
            .Take(60)
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

    private static List<AiSuggestion>? TryParseSuggestionList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<AiSuggestion>>(json, JsonOptions);
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

        [JsonPropertyName("num_predict")]
        public int NumPredict { get; init; }
    }

    private sealed class OllamaPullRequest
    {
        public string Model { get; init; } = string.Empty;
        public bool Stream { get; init; }
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
