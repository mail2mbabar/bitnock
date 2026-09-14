namespace Blog.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string Author = "Author";
    public const string Agent = "Agent";

    public static readonly string[] All = [Admin, Editor, Author, Agent];
}

public static class Permissions
{
    public const string CanCreateArticle = "CanCreateArticle";
    public const string CanEditArticle = "CanEditArticle";
    public const string CanPublishArticle = "CanPublishArticle";
    public const string CanManageMedia = "CanManageMedia";
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageAgentKeys = "CanManageAgentKeys";
    public const string CanManageTaxonomy = "CanManageTaxonomy";
    public const string CanViewAuditLogs = "CanViewAuditLogs";
    public const string CanManageSettings = "CanManageSettings";
    public const string CanViewAnalytics = "CanViewAnalytics";
    public const string CanImportExport = "CanImportExport";
    public const string CanApproveArticles = "CanApproveArticles";
}

public static class AgentScopes
{
    public const string ArticlesRead = "articles.read";
    public const string ArticlesCreate = "articles.create";
    public const string ArticlesUpdate = "articles.update";
    public const string ArticlesDelete = "articles.delete";
    public const string ArticlesPublish = "articles.publish";
    public const string ArticlesSchedule = "articles.schedule";
    public const string ArticlesValidate = "articles.validate";
    public const string MediaUpload = "media.upload";
    public const string MediaRead = "media.read";
    public const string CategoriesRead = "categories.read";
    public const string CategoriesWrite = "categories.write";
    public const string TagsRead = "tags.read";
    public const string TagsWrite = "tags.write";
    public const string SeriesRead = "series.read";
    public const string AnalyticsRead = "analytics.read";
    public const string RulesRead = "rules.read";

    public static readonly string[] All =
    [
        ArticlesRead, ArticlesCreate, ArticlesUpdate, ArticlesDelete,
        ArticlesPublish, ArticlesSchedule, ArticlesValidate,
        MediaUpload, MediaRead,
        CategoriesRead, CategoriesWrite,
        TagsRead, TagsWrite,
        SeriesRead, AnalyticsRead, RulesRead
    ];

    public static readonly string[] DefaultPublisher =
    [
        ArticlesRead, ArticlesCreate, ArticlesUpdate,
        ArticlesPublish, ArticlesSchedule, ArticlesValidate,
        MediaUpload, MediaRead,
        CategoriesRead, TagsRead, SeriesRead, RulesRead
    ];
}

public static class AuditActions
{
    public const string ArticleCreated = "article.created";
    public const string ArticleUpdated = "article.updated";
    public const string ArticleDeleted = "article.deleted";
    public const string ArticlePublished = "article.published";
    public const string ArticleUnpublished = "article.unpublished";
    public const string ArticleScheduled = "article.scheduled";
    public const string ArticleArchived = "article.archived";
    public const string ArticleSubmitted = "article.submitted";
    public const string ArticleApproved = "article.approved";
    public const string ArticleRejected = "article.rejected";
    public const string ArticleSeoUpdated = "article.seo.updated";
    public const string ArticleCategoryChanged = "article.category.changed";
    public const string MediaUploaded = "media.uploaded";
    public const string MediaDeleted = "media.deleted";
    public const string AgentCreated = "agent.created";
    public const string AgentRevoked = "agent.revoked";
    public const string AgentRotated = "agent.rotated";
    public const string AgentDisabled = "agent.disabled";
    public const string AgentEnabled = "agent.updated";
    public const string LoginSucceeded = "auth.login.succeeded";
    public const string LoginFailed = "auth.login.failed";
}
