using System.Text;
using System.Text.RegularExpressions;

namespace Blog.Infrastructure.Services;

internal static class SearchLexicon
{
    private static readonly HashSet<string> Stop =
    [
        "a", "an", "the", "and", "or", "of", "to", "in", "on", "for", "with", "from", "is", "are", "was", "be",
        "how", "what", "why", "when", "where", "which", "who", "do", "does", "did", "can", "could", "should",
        "i", "me", "my", "we", "you", "your", "it", "its", "this", "that", "these", "those", "please", "tell",
        "explain", "about", "using", "use", "used", "into", "vs", "versus", "between", "difference", "differ",
        "best", "good", "need", "want", "help", "article", "blog", "video", "guide"
    ];

    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["c#"] = ["csharp", "c sharp", "dotnet", "interview"],
        ["csharp"] = ["c#", "c sharp", "dotnet"],
        ["c sharp"] = ["csharp", "c#"],
        [".net"] = ["dotnet", "csharp", "asp.net"],
        ["dotnet"] = [".net", "csharp", "asp.net core"],
        ["asp"] = ["asp.net", "aspnet", "asp.net core"],
        ["asp.net"] = ["aspnet", "asp.net core", "middleware", "kestrel"],
        ["aspnet"] = ["asp.net core", "asp.net"],
        ["core"] = ["asp.net core", "dotnet"],
        ["ef"] = ["ef core", "entity framework", "orm", "dapper"],
        ["efcore"] = ["ef core", "entity framework"],
        ["entity"] = ["ef core", "entity framework"],
        ["framework"] = ["ef core", "dotnet"],
        ["orm"] = ["ef core", "entity framework", "dapper"],
        ["dapper"] = ["ef core", "sql", "orm"],
        ["sql"] = ["ef core", "database", "query"],
        ["database"] = ["ef core", "sql", "postgresql"],
        ["postgres"] = ["postgresql", "ef core"],
        ["jwt"] = ["authentication", "authorization", "token", "login", "bearer", "security"],
        ["token"] = ["jwt", "authentication", "bearer"],
        ["auth"] = ["jwt", "authentication", "authorization", "login", "security"],
        ["authentication"] = ["jwt", "login", "token"],
        ["authorization"] = ["jwt", "roles", "policy"],
        ["login"] = ["jwt", "authentication", "identity"],
        ["password"] = ["jwt", "authentication", "security"],
        ["secure"] = ["jwt", "security", "authentication"],
        ["security"] = ["jwt", "authentication", "authorization"],
        ["middleware"] = ["pipeline", "asp.net core", "request"],
        ["pipeline"] = ["middleware", "asp.net core", "github actions"],
        ["request"] = ["middleware", "http", "asp.net core"],
        ["http"] = ["api", "asp.net core", "rest"],
        ["api"] = ["asp.net core", "rest", "minimal apis", "controllers"],
        ["rest"] = ["api", "http"],
        ["minimal"] = ["minimal apis", "controllers"],
        ["controller"] = ["controllers", "minimal apis", "asp.net core"],
        ["di"] = ["dependency injection", "lifetimes", "scoped"],
        ["injection"] = ["dependency injection"],
        ["ioc"] = ["dependency injection"],
        ["solid"] = ["principles", "architecture", "srp"],
        ["clean"] = ["clean architecture", "layers", "domain"],
        ["architecture"] = ["clean architecture", "microservices", "solid"],
        ["ddd"] = ["clean architecture", "domain"],
        ["folder"] = ["clean architecture", "structure"],
        ["crud"] = ["clean architecture", "api", "ef core"],
        ["microservice"] = ["microservices", "distributed", "service bus"],
        ["microservices"] = ["microservice", "distributed"],
        ["distributed"] = ["microservices", "service bus"],
        ["queue"] = ["service bus", "messaging", "worker"],
        ["message"] = ["service bus", "event grid", "messaging"],
        ["event"] = ["event grid", "service bus"],
        ["bus"] = ["service bus"],
        ["function"] = ["azure functions", "worker"],
        ["worker"] = ["worker services", "azure functions"],
        ["serverless"] = ["azure functions"],
        ["azure"] = ["app service", "container apps", "functions", "service bus"],
        ["cloud"] = ["azure", "app service"],
        ["hosting"] = ["app service", "container apps", "docker"],
        ["container"] = ["docker", "container apps"],
        ["docker"] = ["container", "dockerfile", "compose"],
        ["kubernetes"] = ["container apps", "docker"],
        ["devops"] = ["github actions", "azure devops", "ci"],
        ["ci"] = ["github actions", "azure devops"],
        ["cd"] = ["github actions", "azure devops"],
        ["github"] = ["github actions"],
        ["actions"] = ["github actions", "azure devops"],
        ["apim"] = ["api management", "gateway"],
        ["gateway"] = ["api management", "apim"],
        ["tenant"] = ["multi-tenant", "ecommerce"],
        ["multitenant"] = ["multi-tenant", "ecommerce"],
        ["ecommerce"] = ["multi-tenant", "storefront"],
        ["blazor"] = ["angular", "frontend"],
        ["angular"] = ["blazor", "frontend", "spa"],
        ["react"] = ["angular", "frontend", "spa"],
        ["vue"] = ["frontend", "spa"],
        ["frontend"] = ["angular", "blazor"],
        ["spa"] = ["angular", "blazor"],
        ["test"] = ["testing", "xunit", "nunit", "mstest", "unit"],
        ["testing"] = ["xunit", "unit test", "integration"],
        ["xunit"] = ["testing", "nunit", "mstest"],
        ["nunit"] = ["testing", "xunit"],
        ["mstest"] = ["testing", "xunit"],
        ["refactor"] = ["refactoring", "clean code"],
        ["refactoring"] = ["refactor", "clean code"],
        ["performance"] = ["ef core", "n+1", "benchmark"],
        ["slow"] = ["performance", "ef core"],
        ["n+1"] = ["ef core", "performance"],
        ["interface"] = ["abstract class", "oop"],
        ["abstract"] = ["interface", "oop"],
        ["oop"] = ["interface", "abstract class", "solid"],
        ["interview"] = ["csharp", "questions", "preparation", "jwt", "middleware"],
        ["question"] = ["interview", "csharp"],
        ["questions"] = ["interview", "csharp"],
        ["job"] = ["interview", "career"],
        ["career"] = ["interview", "job"],
        ["prepare"] = ["interview", "csharp"],
        ["boxing"] = ["csharp", "unboxing"],
        ["delegate"] = ["csharp", "events"],
        ["delegates"] = ["csharp", "events"],
        ["linq"] = ["ienumerable", "iqueryable", "ef core"],
        ["ienumerable"] = ["iqueryable", "linq"],
        ["iqueryable"] = ["ienumerable", "ef core", "linq"],
        ["struct"] = ["class", "csharp"],
        ["class"] = ["struct", "csharp"],
        ["ref"] = ["out", "csharp"],
        ["out"] = ["ref", "csharp"],
        ["version"] = ["api versioning"],
        ["breaking"] = ["api versioning"],
        ["dotnet 10"] = [".net 10", "asp.net core"],
        ["c# 14"] = ["csharp 14", "csharp"],
        ["identity"] = ["jwt", "authentication"],
        ["cookie"] = ["jwt", "authentication"],
        ["bearer"] = ["jwt"],
        ["claim"] = ["jwt", "authorization"],
        ["role"] = ["authorization", "jwt"],
        ["youtube"] = ["abi helpline", "video", "interview"],
        ["video"] = ["abi helpline", "interview"],
        ["helpline"] = ["interview", "abi"],
        ["abi"] = ["interview", "youtube"],
        ["log"] = ["login", "jwt", "authentication"],
        ["signin"] = ["login", "jwt", "authentication"],
        ["sign"] = ["login", "jwt"],
        ["protect"] = ["jwt", "security", "authentication"],
        ["session"] = ["jwt", "authentication", "cookie"],
        ["credential"] = ["jwt", "authentication"],
        ["endpoint"] = ["api", "asp.net core"],
        ["folders"] = ["clean architecture", "structure"],
        ["layers"] = ["clean architecture"],
        ["tutorial"] = ["clean architecture", "crud"],
        ["query"] = ["ef core", "sql", "linq"],
        ["speed"] = ["performance", "ef core"],
        ["lag"] = ["performance", "ef core"],
        ["overkill"] = ["clean architecture", "microservices"],
        ["principle"] = ["solid"],
        ["principles"] = ["solid"],
        ["mock"] = ["testing", "interview"],
        ["claims"] = ["jwt", "authorization"],
        ["policy"] = ["authorization", "jwt"]
    };

    private static readonly (string Needle, string[] Boost)[] Intents =
    [
        ("protect", ["jwt", "security", "authentication"]),
        ("sign in", ["jwt", "login", "authentication"]),
        ("log in", ["jwt", "login"]),
        ("access token", ["jwt", "authentication"]),
        ("unit test", ["testing", "xunit"]),
        ("integration test", ["testing", "asp.net core"]),
        ("folder structure", ["clean architecture"]),
        ("layers", ["clean architecture"]),
        ("from scratch", ["clean architecture", "tutorial"]),
        ("background job", ["worker", "azure functions"]),
        ("message queue", ["service bus"]),
        ("pub sub", ["event grid", "service bus"]),
        ("ci cd", ["github actions", "azure devops"]),
        ("deploy", ["azure", "docker", "app service"]),
        ("host", ["app service", "container apps"]),
        ("n plus 1", ["ef core", "performance"]),
        ("lazy loading", ["ef core"]),
        ("migration", ["ef core"]),
        ("dbcontext", ["ef core"]),
        ("dependency", ["dependency injection"]),
        ("lifetime", ["dependency injection"]),
        ("singleton", ["dependency injection"]),
        ("scoped", ["dependency injection"]),
        ("front end", ["angular", "blazor"]),
        ("full stack", ["interview", "csharp", "sql"]),
        ("mock interview", ["interview"]),
        ("ace interview", ["interview", "csharp"]),
        ("dream job", ["interview", "career"]),
        ("solid principle", ["solid"]),
        ("open closed", ["solid"]),
        ("liskov", ["solid"]),
        ("abstract vs", ["interface", "abstract class"]),
        ("vs interface", ["interface", "abstract class"]),
        ("log users", ["jwt", "login", "authentication"]),
        ("sign users", ["jwt", "login", "authentication"]),
        ("protect api", ["jwt", "security", "authentication"]),
        ("secure api", ["jwt", "security", "authentication"]),
        ("orm vs", ["ef core", "dapper", "sql"]),
        ("sql vs", ["ef core", "dapper"]),
        ("raw sql", ["dapper", "ef core"]),
        ("too slow", ["performance", "ef core"]),
        ("folder layout", ["clean architecture"]),
        ("project structure", ["clean architecture"]),
        ("unit tests", ["testing", "xunit"]),
        ("access control", ["jwt", "authorization"]),
        ("who can access", ["jwt", "authorization"])
    ];

    public static SearchQuery Parse(string raw)
    {
        var original = (raw ?? string.Empty).Trim();
        var normalized = Normalize(original);
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var core = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var related = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(original))
        {
            terms.Add(original);
            terms.Add(normalized);
        }

        foreach (var token in Tokenize(normalized))
        {
            if (token.Length >= 2 && !Stop.Contains(token))
            {
                core.Add(token);
                terms.Add(token);
            }

            if (Aliases.TryGetValue(token, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    terms.Add(alias);
                    related.Add(alias);
                    foreach (var inner in Tokenize(Normalize(alias)))
                    {
                        terms.Add(inner);
                        related.Add(inner);
                    }
                }
            }
        }

        foreach (var (needle, boost) in Intents)
        {
            if (normalized.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in boost)
                {
                    terms.Add(item);
                    related.Add(item);
                }
            }
        }

        terms.RemoveWhere(t => t.Length < 2 || Stop.Contains(t));
        core.RemoveWhere(t => t.Length < 2 || Stop.Contains(t));
        related.RemoveWhere(t => t.Length < 2 || Stop.Contains(t) || core.Contains(t));
        return new SearchQuery(original, normalized, terms.ToArray(), core.ToArray(), related.ToArray());
    }

    public static bool Hits(string hay, string term)
    {
        if (string.IsNullOrWhiteSpace(hay) || string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        if (hay.Equals(term, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (term.Contains(' '))
        {
            return hay.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var word in Tokenize(hay))
        {
            if (word.Equals(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (term.Length >= 4 &&
                (word.StartsWith(term, StringComparison.OrdinalIgnoreCase) ||
                 (word.Length >= 4 && term.StartsWith(word, StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }
        }

        return false;
    }

    public static string Normalize(string value)
    {
        var text = value.Trim().ToLowerInvariant()
            .Replace("c#", " csharp ", StringComparison.Ordinal)
            .Replace(".net", " dotnet ", StringComparison.Ordinal)
            .Replace("asp.net", " aspnet ", StringComparison.Ordinal);
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    public static IEnumerable<string> Tokenize(string normalized) =>
        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static double Similarity(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return 0;
        }

        left = Normalize(left);
        right = Normalize(right);
        if (left == right)
        {
            return 1;
        }

        if (left.Contains(right) || right.Contains(left))
        {
            return 0.82;
        }

        var a = Tokens(left);
        var b = Tokens(right);
        if (a.Count == 0 || b.Count == 0)
        {
            return 0;
        }

        var overlap = a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count();
        return (2.0 * overlap) / (a.Count + b.Count);
    }

    private static List<string> Tokens(string value) =>
        Tokenize(value).Where(t => t.Length > 1 && !Stop.Contains(t)).ToList();
}

internal sealed record SearchQuery(string Original, string Normalized, string[] Terms, string[] Core, string[] Related);
