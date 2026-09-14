import { notFound } from "next/navigation";
import { ArticleCard } from "@/components/site/ArticleCard";
import { fetchEnvelope } from "@/lib/api";
import type { ArticleListItem, Category } from "@/lib/types";

export default async function CategoryPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const category = await fetchEnvelope<Category>(`/api/v1/public/categories/${slug}`);
  if (!category.data) notFound();
  const articles = await fetchEnvelope<ArticleListItem[]>(`/api/v1/public/articles?category=${slug}&pageSize=20`);
  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <h1 className="font-serif text-4xl">{category.data.name}</h1>
      <p className="mt-3 text-muted">{category.data.description}</p>
      <div className="mt-10 grid gap-10">{(articles.data ?? []).map((a) => <ArticleCard key={a.id} article={a} />)}</div>
    </div>
  );
}
