namespace EngineeringDigest.Application.Articles;

public sealed record VideoClassification(bool IsRelevant, decimal Score, string Reason);
