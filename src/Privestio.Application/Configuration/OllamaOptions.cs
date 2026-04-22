namespace Privestio.Application.Configuration;

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1:8b";
    public double Temperature { get; set; } = 0.1;
    public int MaxOutputTokens { get; set; } = 420;
    public int RequestTimeoutSeconds { get; set; } = 180;
}
