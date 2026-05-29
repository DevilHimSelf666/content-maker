namespace EngineeringDigest.Infrastructure.Knowledge;

public sealed class EmbeddingOptions
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Dimensions { get; set; } = 1536;
}
