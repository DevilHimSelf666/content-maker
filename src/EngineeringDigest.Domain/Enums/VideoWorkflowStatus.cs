namespace EngineeringDigest.Domain.Enums;

public enum VideoWorkflowStatus
{
    Discovered = 0,
    TranscriptReady = 1,
    Classified = 2,
    ArticleDrafted = 3,
    PendingReview = 4,
    Approved = 5,
    Published = 6,
    Rejected = 7,
    NotRelevant = 8,
    Failed = 9
}
