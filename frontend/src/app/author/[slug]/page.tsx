import { notFound } from "next/navigation";
import { ArticleCard } from "@/components/site/ArticleCard";
import { AuthorIdentity } from "@/components/site/AuthorIdentity";
import { fetchEnvelope } from "@/lib/api";
import type { ArticleListItem, Author } from "@/lib/types";

export default async function AuthorPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const author = await fetchEnvelope<Author>(`/api/v1/public/authors/${slug}`);
  if (!author.data) notFound();
  const articles = await fetchEnvelope<ArticleListItem[]>(`/api/v1/public/articles?author=${slug}&pageSize=40`);
  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <AuthorIdentity author={author.data} />
      <p className="mt-6 text-sm text-muted">{author.data.articleCount} articles · Manchester, United Kingdom</p>
      <p className="mt-2 text-sm">
        <a className="underline" href="/resume/Muhammad-Babar-Microsoft-MVP-CV.pdf">Download CV</a>
        {" · "}
        <a className="underline" href="https://www.youtube.com/@ABiHelpline">ABi Helpline on YouTube</a>
        {" · "}
        <a className="underline" href="https://www.credly.com/badges/7c0a51ad-031c-40b9-9c05-b52e15a2d384">Verify MVP on Credly</a>
      </p>
      <div className="mt-10 grid gap-10">{(articles.data ?? []).map((a) => <ArticleCard key={a.id} article={a} />)}</div>
    </div>
  );
}
