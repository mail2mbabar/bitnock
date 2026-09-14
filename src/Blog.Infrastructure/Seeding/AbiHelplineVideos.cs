namespace Blog.Infrastructure.Seeding;

internal sealed record AbiHelplineVideo(
    string Id,
    string Title,
    string Duration,
    string Topic,
    string ArticleSlug,
    string ArticleTitle,
    string Subtitle,
    string Excerpt,
    string CategorySlug,
    string[] Tags,
    string[] Technologies,
    Domain.Enums.Difficulty Difficulty,
    Domain.Enums.ArticleContentType ContentType,
    bool Featured,
    int SeriesPart,
    string Badge,
    string AccentHex);

internal static class AbiHelplineVideos
{
    public const string ChannelUrl = "https://www.youtube.com/@ABiHelpline";
    public const string ChannelName = "ABi Helpline";
    public const string SeriesSlug = "abi-helpline";

    public static string WatchUrl(string id) => $"https://www.youtube.com/watch?v={id}";

    public static string Embed(string id, string title) =>
        $"""
        <iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/{id}" title="{title}" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen loading="lazy"></iframe>
        """;

    public static IReadOnlyList<AbiHelplineVideo> All { get; } =
    [
        new("6Rpto5pUxpw", "How to Land Your Dream Job: Interview Tips You Can't Miss", "7:15", "Career",
            "how-to-land-your-dream-job-interview-tips",
            "How to land a .NET job: interview tips I actually use",
            "Stories, trade-offs, and the questions that decide offers — not a list of buzzwords.",
            "How I prepare candidates (and myself) for .NET interviews: what to practise, what to skip, and how to talk about production work.",
            "software-engineering", ["Interview", "Career", "C#"], ["C#", "ASP.NET Core"], Domain.Enums.Difficulty.Beginner, Domain.Enums.ArticleContentType.Guide, false, 12, "CAREER", "F59E0B"),
        new("0J_T5qRynSI", "Clean Architecture | Full Tutorial API, CRUD, Folder Structure, Project Setup .NET 8", "1:13:01", "Architecture",
            "clean-architecture-dotnet-8-api-crud-tutorial",
            "Clean Architecture in .NET: API, CRUD, folder structure",
            "The full tutorial written up: solution setup, layers, and a CRUD API you can defend in an interview.",
            "A detailed companion to my ABi Helpline Clean Architecture walkthrough — Domain, Application, Infrastructure, and ASP.NET Core wired the way I ship.",
            "clean-architecture", ["Clean Architecture", "ASP.NET Core", "Interview"], ["ASP.NET Core", "EF Core", "C#"], Domain.Enums.Difficulty.Intermediate, Domain.Enums.ArticleContentType.Tutorial, true, 1, "TUTORIAL", "3B5BDB"),
        new("aVsG9TydyzE", "JWT in ASP.NET: Authentication and Authorization Simplified", "7:00", "Security",
            "jwt-authentication-and-authorization-in-aspnet",
            "JWT authentication and authorization in ASP.NET Core",
            "Tokens, claims, middleware order, and the interview answers that do not leak secrets.",
            "How JWT actually works in ASP.NET Core: issuing tokens, validating them, and authorizing endpoints without cargo-cult configuration.",
            "security", ["JWT", "ASP.NET Core", "Security", "Interview"], ["ASP.NET Core", "JWT"], Domain.Enums.Difficulty.Intermediate, Domain.Enums.ArticleContentType.Tutorial, true, 2, "SECURITY", "DC2626"),
        new("xUqjEzRbOAw", "Microservices: Key Concepts and Benefits", "27:37", "Architecture",
            "microservices-key-concepts-and-benefits-for-dotnet",
            "Microservices for .NET interviews: concepts, benefits, and the cost",
            "Independent deployability is the point. Distributed pain is the price. Know both.",
            "The microservice talking points I want in a .NET interview: boundaries, data, messaging, and when a modular monolith is the honest answer.",
            "microservices", ["Microservices", "Architecture", "Interview"], ["ASP.NET Core", "Azure Service Bus"], Domain.Enums.Difficulty.Intermediate, Domain.Enums.ArticleContentType.Architecture, false, 3, "ARCHITECTURE", "7C3AED"),
        new("Ct6WgZx8_B4", "Top 10 .NET Code Refactoring Techniques", "19:29", "C#",
            "top-10-dotnet-code-refactoring-techniques",
            "Top 10 .NET refactoring techniques I use in production",
            "Refactor for the next reader. Interviews notice whether you can improve code without breaking it.",
            "Ten refactors that show up in ASP.NET Core code reviews and interviews: extract method, invert ifs, replace magic, and stop duplicating.",
            "csharp", ["Refactoring", "C#", "Interview"], ["C#", "ASP.NET Core"], Domain.Enums.Difficulty.Intermediate, Domain.Enums.ArticleContentType.Guide, false, 4, "TOP 10", "0F766E"),
        new("ivJYcCelZ8A", "EF Core in 15 Minutes: The All-in-One Guide", "15:24", "EF Core",
            "ef-core-in-15-minutes-all-in-one-guide",
            "EF Core in 15 minutes: the all-in-one interview guide",
            "DbContext, tracking, LINQ, migrations, and the questions that follow the demo.",
            "A compact but detailed EF Core briefing: how I explain the ORM, what I configure, and the performance traps interviewers love.",
            "ef-core", ["EF Core", "Interview", "SQL"], ["EF Core", "SQL Server"], Domain.Enums.Difficulty.Beginner, Domain.Enums.ArticleContentType.Tutorial, false, 5, "EF CORE", "2563EB"),
        new("NaGwNiUQRzI", "Unit Testing Essentials: ASP.NET Core with MSTest, xUnit, NUnit", "18:55", "Testing",
            "unit-testing-aspnet-core-mstest-xunit-nunit",
            "Unit testing ASP.NET Core with xUnit, NUnit, and MSTest",
            "Pick a runner, test behaviour, and keep the suite faster than the build excuse.",
            "How I unit-test ASP.NET Core services: xUnit as the default, what belongs in a test, and a repository example from real interview practice.",
            "testing", ["Testing", "xUnit", "Interview"], ["xUnit", "ASP.NET Core", "C#"], Domain.Enums.Difficulty.Intermediate, Domain.Enums.ArticleContentType.Tutorial, false, 6, "TESTING", "CA8A04"),
        new("h4SvC_9_lL0", "Understanding Middleware in ASP.NET Core", "8:54", "ASP.NET Core",
            "understanding-middleware-in-aspnet-core",
            "Understanding middleware in ASP.NET Core",
            "The request pipeline is a linked list of decisions. Order is a production bug.",
            "Middleware, Use vs Run vs Map, short-circuiting, and the interview diagram I draw on a whiteboard.",
            "aspnet-core", ["ASP.NET Core", "Middleware", "Interview"], ["ASP.NET Core"], Domain.Enums.Difficulty.Beginner, Domain.Enums.ArticleContentType.Tutorial, false, 7, "PIPELINE", "0891B2"),
        new("D1AJJ8PrZr0", "Abstract Class vs Interface in C#", "11:31", "C#",
            "abstract-class-vs-interface-in-csharp",
            "Abstract class vs interface in C#",
            "The OOP classic. Answer it with a rule, then a production example.",
            "When I reach for an interface, when an abstract class is honest, and how default interface methods changed the conversation.",
            "csharp", ["C#", "OOP", "Interview"], ["C#"], Domain.Enums.Difficulty.Beginner, Domain.Enums.ArticleContentType.Guide, false, 8, "OOP", "DB2777"),
        new("vACP_yp_qW4", "SOLID Principles in ASP.NET", "18:15", "Architecture",
            "solid-principles-in-aspnet",
            "SOLID principles in ASP.NET Core, with code",
            "Five letters that only count if you can point at a class that violates them.",
            "SOLID applied to ASP.NET Core services, controllers, and DI — the version I want in a senior interview.",
            "architecture", ["SOLID", "ASP.NET Core", "Interview"], ["ASP.NET Core", "C#"], Domain.Enums.Difficulty.Intermediate, Domain.Enums.ArticleContentType.Architecture, false, 9, "SOLID", "4F46E5"),
        new("834ybOzNhmw", "Top 10 C# Interview Questions and Answers", "25:03", "C#",
            "top-10-csharp-interview-questions-and-answers",
            "Top 10 C# interview questions and answers",
            "The ABi Helpline classic: ref/out, IEnumerable vs IQueryable, boxing, delegates, structs, and the rest.",
            "Detailed written answers to the ten C# questions I cover on ABi Helpline — with code you can say out loud in an interview.",
            "csharp", ["C#", "Interview"], ["C#"], Domain.Enums.Difficulty.Beginner, Domain.Enums.ArticleContentType.Guide, true, 10, "TOP 10", "E11D48"),
        new("UtdyV98Grko", "Full Stack .NET Interview: C#, Front End, .NET, and SQL", "25:18", "Interview",
            "fullstack-dotnet-interview-csharp-frontend-sql",
            "Full-stack .NET interview: C#, frontend, .NET, and SQL",
            "A mock-style briefing across the four piles of questions beginners actually get.",
            "C#, ASP.NET, SQL, and frontend interview questions in one place — the companion to my full-stack mock on ABi Helpline.",
            "software-engineering", ["Interview", "C#", "SQL", "ASP.NET Core"], ["C#", "SQL", "ASP.NET Core"], Domain.Enums.Difficulty.Beginner, Domain.Enums.ArticleContentType.Guide, false, 11, "FULL STACK", "16A34A"),
    ];
}
