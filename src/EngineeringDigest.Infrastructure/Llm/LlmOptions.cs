namespace EngineeringDigest.Infrastructure.Llm;

public sealed class LlmOptions
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal RelevanceThreshold { get; set; } = 0.65m;
}
