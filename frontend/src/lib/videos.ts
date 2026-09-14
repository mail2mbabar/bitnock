export const YOUTUBE_CHANNEL_URL = "https://www.youtube.com/@ABiHelpline";
export const YOUTUBE_CHANNEL_NAME = "ABi Helpline";

export type ChannelVideo = {
  id: string;
  title: string;
  duration: string;
  topic: string;
  summary: string;
  articleSlug: string;
};

export const channelVideos: ChannelVideo[] = [
  {
    id: "6Rpto5pUxpw",
    title: "How to Land Your Dream Job: Interview Tips You Can't Miss",
    duration: "7:15",
    topic: "Career",
    summary: "How I prepare for .NET interviews: stories, trade-offs, and the questions that actually decide offers.",
    articleSlug: "how-to-land-your-dream-job-interview-tips",
  },
  {
    id: "0J_T5qRynSI",
    title: "Clean Architecture | Full Tutorial API, CRUD, Folder Structure, Project Setup .NET 8",
    duration: "1:13:01",
    topic: "Architecture",
    summary: "A full walkthrough: solution structure, domain, application, infrastructure, and a working CRUD API.",
    articleSlug: "clean-architecture-dotnet-8-api-crud-tutorial",
  },
  {
    id: "aVsG9TydyzE",
    title: "JWT in ASP.NET: Authentication and Authorization Simplified",
    duration: "7:00",
    topic: "Security",
    summary: "Access tokens, claims, middleware order, and the authorization mistakes that show up in interviews.",
    articleSlug: "jwt-authentication-and-authorization-in-aspnet",
  },
  {
    id: "xUqjEzRbOAw",
    title: "Microservices: Key Concepts and Benefits",
    duration: "27:37",
    topic: "Architecture",
    summary: "When a microservice earns its keep, what you give up, and how I explain the pattern in interviews.",
    articleSlug: "microservices-key-concepts-and-benefits-for-dotnet",
  },
  {
    id: "Ct6WgZx8_B4",
    title: "Top 10 .NET Code Refactoring Techniques",
    duration: "19:29",
    topic: "C#",
    summary: "Practical refactors I use on ASP.NET Core codebases: extract, invert, name, and stop copying.",
    articleSlug: "top-10-dotnet-code-refactoring-techniques",
  },
  {
    id: "ivJYcCelZ8A",
    title: "EF Core in 15 Minutes: The All-in-One Guide",
    duration: "15:24",
    topic: "EF Core",
    summary: "DbContext, tracking, projections, migrations, and the interview questions that follow.",
    articleSlug: "ef-core-in-15-minutes-all-in-one-guide",
  },
  {
    id: "NaGwNiUQRzI",
    title: "Unit Testing Essentials: ASP.NET Core with MSTest, xUnit, NUnit",
    duration: "18:55",
    topic: "Testing",
    summary: "What to test, which runner to pick, and how I structure tests so they survive a refactor.",
    articleSlug: "unit-testing-aspnet-core-mstest-xunit-nunit",
  },
  {
    id: "h4SvC_9_lL0",
    title: "Understanding Middleware in ASP.NET Core",
    duration: "8:54",
    topic: "ASP.NET Core",
    summary: "The pipeline, short-circuiting, custom middleware, and why order is a production bug.",
    articleSlug: "understanding-middleware-in-aspnet-core",
  },
  {
    id: "D1AJJ8PrZr0",
    title: "Abstract Class vs Interface in C#",
    duration: "11:31",
    topic: "C#",
    summary: "The classic OOP question, answered with rules you can remember under interview pressure.",
    articleSlug: "abstract-class-vs-interface-in-csharp",
  },
  {
    id: "vACP_yp_qW4",
    title: "SOLID Principles in ASP.NET",
    duration: "18:15",
    topic: "Architecture",
    summary: "SOLID as something you can point at in an ASP.NET Core service, not a poster on the wall.",
    articleSlug: "solid-principles-in-aspnet",
  },
  {
    id: "834ybOzNhmw",
    title: "Top 10 C# Interview Questions and Answers",
    duration: "25:03",
    topic: "C#",
    summary: "ref vs out, IEnumerable vs IQueryable, boxing, delegates, structs, and the rest of the classic ten.",
    articleSlug: "top-10-csharp-interview-questions-and-answers",
  },
  {
    id: "UtdyV98Grko",
    title: "Full Stack .NET Interview: C#, Front End, .NET, and SQL",
    duration: "25:18",
    topic: "Interview",
    summary: "A mock-style pass across C#, ASP.NET, SQL, and the frontend questions juniors actually get.",
    articleSlug: "fullstack-dotnet-interview-csharp-frontend-sql",
  },
];

export function youtubeThumb(id: string, quality: "hq" | "max" = "hq") {
  return quality === "max"
    ? `https://i.ytimg.com/vi/${id}/maxresdefault.jpg`
    : `https://i.ytimg.com/vi/${id}/hqdefault.jpg`;
}

export function youtubeWatch(id: string) {
  return `https://www.youtube.com/watch?v=${id}`;
}

export function youtubeEmbed(id: string) {
  return `https://www.youtube.com/embed/${id}`;
}

export function getVideo(id: string) {
  return channelVideos.find((video) => video.id === id) ?? null;
}
