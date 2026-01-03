namespace Quote.Domain.Enums;

public enum DisputeStatus
{
    Open = 1,
    UnderReview = 2,
    Resolved = 3,
    Closed = 4
}

public enum DisputeReason
{
    WorkQuality = 1,
    NonCompletion = 2,
    PaymentIssue = 3,
    Communication = 4,
    Other = 5
}

public enum DisputeResolutionType
{
    FullRefund = 1,
    PartialRefund = 2,
    NoRefund = 3,
    Dismissed = 4
}
