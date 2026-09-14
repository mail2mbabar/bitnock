import Link from "next/link";
import { fetchEnvelope } from "@/lib/api";
import type { Author } from "@/lib/types";
import { AuthorIdentity } from "@/components/site/AuthorIdentity";

const experience = [
  {
    role: "Senior Full Stack .NET Engineer",
    org: "DataServe Corp",
    dates: "Jan 2025 – Present",
    detail: "Azure, .NET 8/10, multi-tenant US ecommerce and healthcare claims. Angular admin, Functions, Service Bus, OpenAI-assisted operations.",
  },
  {
    role: "Senior Full Stack .NET Engineer",
    org: "Systems Limited",
    dates: "Sep 2023 – Jan 2025",
    detail: "TDAP trade platform: microservices, CQRS, 30+ official forms, Angular officer workflows, SignalR, Redis, Elasticsearch.",
  },
  {
    role: "Senior Full Stack .NET Engineer",
    org: "Calrom Pakistan",
    dates: "Aug 2022 – Sep 2023",
    detail: "Airline booking APIs and Vue/React fronts. Peak-traffic production support with Grafana and Prometheus.",
  },
  {
    role: "Senior Software Engineer",
    org: "Punjab Information Technology Board",
    dates: "Sep 2020 – Aug 2022",
    detail: "Electronic filing and SharePoint document automation for government departments.",
  },
  {
    role: "Software Engineer",
    org: "Fauji Foundation",
    dates: "Aug 2016 – Aug 2020",
    detail: "eOffice and office automation on ASP.NET, DevExpress, and SQL Server.",
  },
];

export default async function AboutPage() {
  const authors = await fetchEnvelope<Author[]>("/api/v1/public/authors");
  const author = authors.data?.[0];

  return (
    <div className="mx-auto max-w-3xl px-4 py-12">
      <p className="text-xs uppercase tracking-[0.22em] text-muted">About</p>
      <h1 className="mt-3 font-serif text-4xl md:text-5xl">Bitnock is Muhammad Babar’s .NET publication</h1>
      <p className="mt-4 text-lg text-muted">
        Engineering notes on C#, ASP.NET Core, Azure, and the operational details that keep systems alive. Written from eleven years of shipping, not from a template.
      </p>

      <div className="mt-10 rounded-2xl border border-rule bg-paper-2/50 p-6">
        <AuthorIdentity author={author} />
      </div>

      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src="/mvp/mvp-announcement.png" alt="Muhammad Babar accepted into the Microsoft MVP Program" className="mt-8 w-full rounded-2xl" />

      <section className="mt-12">
        <h2 className="font-serif text-3xl">Recognition</h2>
        <p className="mt-3 text-muted">
          Microsoft Most Valuable Professional, Developer Technologies · .NET, August 2026 – August 2027. Official Credly badge authorised for professional use.
        </p>
        <p className="mt-3 text-sm">
          <a className="underline" href="https://www.credly.com/badges/7c0a51ad-031c-40b9-9c05-b52e15a2d384">Verify on Credly</a>
          {" · "}
          <a className="underline" href="/resume/Muhammad-Babar-Microsoft-MVP-CV.pdf">Download CV (PDF)</a>
        </p>
      </section>

      <section className="mt-12">
        <h2 className="font-serif text-3xl">ABi Helpline on YouTube</h2>
        <p className="mt-3 text-muted">
          I publish free C#, ASP.NET Core, EF Core, JWT, Clean Architecture, and interview videos on{" "}
          <a className="underline" href="https://www.youtube.com/@ABiHelpline">youtube.com/@ABiHelpline</a>.
          Every video is also on <Link className="underline" href="/videos">Bitnock Videos</Link> with a long-form written guide.
        </p>
      </section>

      <section className="mt-12">
        <h2 className="font-serif text-3xl">What I write about</h2>
        <p className="mt-3 text-muted">
          Production ASP.NET Core, modern C#, EF Core, Azure hosting and messaging, interview preparation, and honest comparisons — App Service vs Container Apps, Functions vs workers, Angular vs Blazor, Actions vs Azure DevOps. The companion notes to ABi Helpline live next to the engineering essays. The CMS behind this site is API-first so a future agent can draft and publish through scoped credentials.
        </p>
      </section>

      <section className="mt-12">
        <h2 className="font-serif text-3xl">Experience</h2>
        <ol className="mt-6 space-y-6">
          {experience.map((item) => (
            <li key={item.org} className="border-b border-rule pb-6">
              <p className="text-xs uppercase tracking-[0.16em] text-muted">{item.dates}</p>
              <p className="mt-1 font-medium">{item.role} · {item.org}</p>
              <p className="mt-2 text-sm text-muted">{item.detail}</p>
            </li>
          ))}
        </ol>
      </section>

      <section className="mt-12">
        <h2 className="font-serif text-3xl">Education</h2>
        <ul className="mt-4 space-y-3 text-muted">
          <li>MSc Data Science, University of Salford, United Kingdom · Jan 2025 – Apr 2026</li>
          <li>BS Telecommunication Engineering, COMSATS University · 2012 – 2016</li>
        </ul>
      </section>

      <section className="mt-12">
        <h2 className="font-serif text-3xl">Based in Manchester</h2>
        <p className="mt-3 text-muted">
          Email <a className="underline" href="mailto:mail2mbabar@gmail.com">mail2mbabar@gmail.com</a>
          {" · "}
          <Link className="underline" href="/contact">Contact</Link>
        </p>
      </section>
    </div>
  );
}
