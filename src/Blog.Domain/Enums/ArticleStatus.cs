namespace Blog.Domain.Enums;

public enum ArticleStatus
{
    Draft = 0,
    PendingReview = 1,
    Scheduled = 2,
    Published = 3,
    Rejected = 4,
    Archived = 5
}

public enum ContentFormat
{
    Markdown = 0,
    Html = 1
}

public enum ArticleContentType
{
    Tutorial = 0,
    Guide = 1,
    Opinion = 2,
    Reference = 3,
    News = 4,
    CaseStudy = 5,
    Architecture = 6,
    CodeExample = 7,
    DeepDive = 8
}

public enum Difficulty
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
    Expert = 3
}

public enum ActorType
{
    User = 0,
    Agent = 1,
    System = 2
}

public enum PublishingMode
{
    AutoPublish = 0,
    RequireApproval = 1
}

public enum NewsletterStatus
{
    PendingConfirmation = 0,
    Subscribed = 1,
    Unsubscribed = 2
}

public enum AgentCredentialStatus
{
    Active = 0,
    Disabled = 1,
    Revoked = 2,
    Expired = 3
}
