namespace EngineeringDigest.Application.Articles;

public sealed record ArticleQualityScore(
    decimal TechnicalDepth,
    decimal Relevance,
    decimal Readability,
    decimal PracticalValue,
    string Reason);
