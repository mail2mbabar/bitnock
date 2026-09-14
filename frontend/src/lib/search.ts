import type { ChannelVideo } from "@/lib/videos";

const stop = new Set([
  "a", "an", "the", "and", "or", "of", "to", "in", "on", "for", "with", "from", "is", "are", "was", "be",
  "how", "what", "why", "when", "where", "which", "who", "do", "does", "did", "can", "could", "should",
  "i", "me", "my", "we", "you", "your", "it", "its", "this", "that", "these", "those", "please", "tell",
  "explain", "about", "using", "use", "used", "into", "vs", "versus", "between", "difference", "differ",
  "best", "good", "need", "want", "help", "article", "blog", "video", "guide",
]);

const aliases: Record<string, string[]> = {
  jwt: ["authentication", "authorization", "token", "login", "bearer", "security"],
  token: ["jwt", "authentication", "bearer"],
  auth: ["jwt", "authentication", "login", "security"],
  authentication: ["jwt", "login", "token"],
  authorization: ["jwt", "roles"],
  login: ["jwt", "authentication", "identity"],
  log: ["login", "jwt", "authentication"],
  sign: ["login", "jwt"],
  signin: ["login", "jwt"],
  secure: ["jwt", "security", "authentication"],
  protect: ["jwt", "security", "authentication"],
  security: ["jwt", "authentication"],
  password: ["jwt", "authentication"],
  middleware: ["pipeline", "asp.net core"],
  pipeline: ["middleware", "asp.net core"],
  api: ["asp.net core", "rest"],
  ef: ["ef core", "entity framework", "orm", "dapper"],
  orm: ["ef core", "dapper", "sql"],
  dapper: ["ef core", "sql", "orm"],
  sql: ["ef core", "database"],
  slow: ["performance", "ef core"],
  performance: ["ef core"],
  clean: ["clean architecture", "folders"],
  architecture: ["clean architecture", "microservices"],
  folder: ["clean architecture"],
  folders: ["clean architecture"],
  crud: ["clean architecture", "api"],
  microservice: ["microservices"],
  microservices: ["distributed", "service bus"],
  queue: ["service bus", "messaging"],
  event: ["event grid", "service bus"],
  azure: ["app service", "functions", "container apps"],
  docker: ["container", "container apps"],
  test: ["testing", "xunit", "nunit", "mstest"],
  testing: ["xunit", "unit"],
  interview: ["csharp", "questions", "jwt", "middleware"],
  question: ["interview", "csharp"],
  job: ["interview", "career"],
  interface: ["abstract class", "oop"],
  abstract: ["interface", "oop"],
  solid: ["principles", "architecture"],
  di: ["dependency injection"],
  injection: ["dependency injection"],
};

const intents: [string, string[]][] = [
  ["log in", ["jwt", "login", "authentication"]],
  ["log users", ["jwt", "login", "authentication"]],
  ["sign in", ["jwt", "login"]],
  ["access token", ["jwt", "authentication"]],
  ["protect api", ["jwt", "security"]],
  ["secure api", ["jwt", "security"]],
  ["folder structure", ["clean architecture"]],
  ["from scratch", ["clean architecture"]],
  ["orm vs", ["ef core", "dapper", "sql"]],
  ["abstract vs", ["interface", "abstract class"]],
  ["vs interface", ["interface", "abstract class"]],
  ["unit test", ["testing", "xunit"]],
  ["dream job", ["interview"]],
  ["message queue", ["service bus"]],
];

export function normalizeSearch(value: string) {
  const text = value
    .trim()
    .toLowerCase()
    .replaceAll("c#", " csharp ")
    .replaceAll("asp.net", " aspnet ")
    .replaceAll(".net", " dotnet ");
  return text.replace(/[^a-z0-9]+/g, " ").replace(/\s+/g, " ").trim();
}

export function expandSearchTerms(query: string) {
  const original = query.trim();
  const normalized = normalizeSearch(original);
  const terms = new Set<string>();
  const core = new Set<string>();
  if (!normalized) return { original, normalized, terms, core };

  for (const token of normalized.split(" ").filter(Boolean)) {
    if (token.length >= 2 && !stop.has(token)) {
      core.add(token);
      terms.add(token);
    }
    for (const alias of aliases[token] ?? []) {
      terms.add(alias);
      for (const inner of normalizeSearch(alias).split(" ").filter(Boolean)) {
        if (inner.length >= 2 && !stop.has(inner)) terms.add(inner);
      }
    }
  }

  for (const [needle, boost] of intents) {
    if (normalized.includes(needle)) {
      for (const item of boost) terms.add(item);
    }
  }

  return { original, normalized, terms, core };
}

function hits(hay: string, term: string) {
  if (!hay || !term) return false;
  if (hay === term) return true;
  if (term.includes(" ")) return hay.includes(term);
  return hay.split(" ").some((word) => {
    if (word === term) return true;
    if (term.length >= 4 && (word.startsWith(term) || (word.length >= 4 && term.startsWith(word)))) return true;
    return false;
  });
}

export function scoreVideo(query: string, video: ChannelVideo) {
  const parsed = expandSearchTerms(query);
  if (!parsed.normalized) return 0;
  const hay = normalizeSearch(`${video.title} ${video.topic} ${video.summary} ${video.articleSlug.replaceAll("-", " ")}`);
  let score = 0;
  if (hay.includes(parsed.normalized)) score += 80;
  if (parsed.original && hay.includes(parsed.original.toLowerCase())) score += 40;
  for (const term of parsed.terms) {
    const t = normalizeSearch(term);
    if (t.length < 2) continue;
    if (!hits(hay, t)) continue;
    score += parsed.core.has(term) || parsed.core.has(t) ? 18 : 8;
  }
  return score;
}

export function rankVideos(query: string, videos: ChannelVideo[], limit = 4) {
  if (!query.trim()) return [];
  const ranked = videos
    .map((video) => ({ video, score: scoreVideo(query, video) }))
    .sort((a, b) => b.score - a.score);
  const top = ranked[0]?.score ?? 0;
  const floor = top <= 0 ? 1 : Math.max(4, top * 0.18);
  const matches = ranked.filter((x) => x.score >= floor);
  return (matches.length > 0 ? matches : ranked.filter((x) => x.score > 0)).slice(0, limit).map((x) => x.video);
}

export const searchStarters = [
  "How do I secure a .NET API?",
  "Difference between abstract class and interface",
  "Prepare for a C# interview",
  "EF Core is slow",
  "Azure queue vs events",
  "Clean architecture folders",
];

export const searchTopics = ["C# interview", "JWT login", "Clean Architecture", "EF Core", "middleware", "Azure vs", "unit testing", "microservices"];
