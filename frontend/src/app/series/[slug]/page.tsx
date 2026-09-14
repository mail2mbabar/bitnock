import Link from "next/link";
import { notFound } from "next/navigation";
import { fetchEnvelope, formatDate } from "@/lib/api";
import type { SeriesDetail } from "@/lib/types";

export default async function SeriesPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const series = await fetchEnvelope<SeriesDetail>(`/api/v1/public/series/${slug}`);
  if (!series.data) notFound();
  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="font-serif text-4xl">{series.data.name}</h1>
      <p className="mt-3 text-muted">{series.data.description}</p>
      <ol className="mt-10 space-y-4">
        {series.data.articles.filter((a) => a.status === "Published").map((article) => (
          <li key={article.id} className="border-b border-rule pb-4">
            <p className="text-xs text-muted">Part {article.order}</p>
            <Link href={`/articles/${article.slug}`} className="font-serif text-2xl">{article.title}</Link>
            <p className="text-sm text-muted">{formatDate(article.publishedAt)}</p>
          </li>
        ))}
      </ol>
    </div>
  );
}
