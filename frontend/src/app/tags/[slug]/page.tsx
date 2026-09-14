import { notFound } from "next/navigation";
import { ArticleCard } from "@/components/site/ArticleCard";
import { fetchEnvelope } from "@/lib/api";
import type { ArticleListItem, Tag } from "@/lib/types";

export default async function TagPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const tag = await fetchEnvelope<Tag>(`/api/v1/public/tags/${slug}`);
  if (!tag.data) notFound();
  const articles = await fetchEnvelope<ArticleListItem[]>(`/api/v1/public/articles?tag=${slug}&pageSize=20`);
  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <h1 className="font-serif text-4xl">#{tag.data.name}</h1>
      <div className="mt-10 grid gap-10">{(articles.data ?? []).map((a) => <ArticleCard key={a.id} article={a} />)}</div>
    </div>
  );
}
