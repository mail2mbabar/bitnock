import { ArticleCard, EmptyState } from "@/components/site/ArticleCard";
import { fetchEnvelope } from "@/lib/api";
import type { ArticleListItem } from "@/lib/types";

export default async function ArticlesPage({
  searchParams,
}: {
  searchParams: Promise<{ category?: string; tag?: string; technology?: string; difficulty?: string; page?: string }>;
}) {
  const params = await searchParams;
  const query = new URLSearchParams();
  if (params.category) query.set("category", params.category);
  if (params.tag) query.set("tag", params.tag);
  if (params.technology) query.set("technology", params.technology);
  if (params.difficulty) query.set("difficulty", params.difficulty);
  query.set("page", params.page ?? "1");
  query.set("pageSize", "12");
  const result = await fetchEnvelope<ArticleListItem[]>(`/api/v1/public/articles?${query.toString()}`);
  const items = result.data ?? [];

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <h1 className="font-serif text-4xl">Articles</h1>
      <p className="mt-2 text-muted">Guides, architecture notes, and production-minded .NET writing.</p>
      <form className="mt-6 grid gap-3 md:grid-cols-4" method="get">
        <input name="category" placeholder="Category slug" defaultValue={params.category} className="rounded-lg border border-rule bg-paper px-3 py-2" />
        <input name="tag" placeholder="Tag" defaultValue={params.tag} className="rounded-lg border border-rule bg-paper px-3 py-2" />
        <input name="technology" placeholder="Technology" defaultValue={params.technology} className="rounded-lg border border-rule bg-paper px-3 py-2" />
        <select name="difficulty" defaultValue={params.difficulty ?? ""} className="rounded-lg border border-rule bg-paper px-3 py-2">
          <option value="">All levels</option>
          <option>Beginner</option>
          <option>Intermediate</option>
          <option>Advanced</option>
          <option>Expert</option>
        </select>
        <button className="rounded-lg bg-ink px-4 py-2 text-sm text-paper md:col-span-4 md:w-fit">Filter</button>
      </form>
      <div className="mt-10 grid gap-10 md:grid-cols-2">
        {items.length === 0 ? <div className="md:col-span-2"><EmptyState title="No articles" body="Try another filter." /></div> : items.map((article) => <ArticleCard key={article.id} article={article} />)}
      </div>
    </div>
  );
}
