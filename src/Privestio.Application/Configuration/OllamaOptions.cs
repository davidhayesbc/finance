namespace Privestio.Application.Configuration;

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string DefaultProfile { get; set; } = "Balanced";
    public Dictionary<string, OllamaModelProfileOptions> Profiles { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Legacy single-model settings kept for backward compatibility.
    public string Model { get; set; } = "llama3.1:8b";
    public double Temperature { get; set; } = 0.1;
    public int MaxOutputTokens { get; set; } = 420;
    public int RequestTimeoutSeconds { get; set; } = 180;

    public OllamaResolvedProfile ResolveProfile()
    {
        if (
            !string.IsNullOrWhiteSpace(DefaultProfile)
            && Profiles.TryGetValue(DefaultProfile, out var configuredProfile)
            && !string.IsNullOrWhiteSpace(configuredProfile.Model)
        )
        {
            return new OllamaResolvedProfile(
                string.IsNullOrWhiteSpace(configuredProfile.Name)
                    ? DefaultProfile
                    : configuredProfile.Name,
                configuredProfile.Model,
                configuredProfile.Temperature,
                configuredProfile.MaxOutputTokens
            );
        }

        return new OllamaResolvedProfile(
            "Legacy",
            Model,
            Temperature,
            MaxOutputTokens
        );
    }
}

public class OllamaModelProfileOptions
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.1;
    public int MaxOutputTokens { get; set; } = 260;
}

public sealed record OllamaResolvedProfile(
    string Name,
    string Model,
    double Temperature,
    int MaxOutputTokens
);
