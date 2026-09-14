import { notFound } from "next/navigation";
import { ArticleBody } from "@/components/article/ArticleExtras";
import { fetchEnvelope, serverApi } from "@/lib/api";
import type { ArticleDetail } from "@/lib/types";

export default async function PreviewPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ token?: string }>;
}) {
  const { id } = await params;
  const { token } = await searchParams;
  if (!token) notFound();
  const result = await fetch(`${serverApi}/api/v1/public/preview/${id}?token=${encodeURIComponent(token)}`, { cache: "no-store" });
  const envelope = (await result.json()) as { data?: ArticleDetail };
  if (!envelope.data) notFound();
  const html = await fetchEnvelope<{ html: string }>(`/api/v1/public/articles/${envelope.data.slug}/html`).catch(() => null);
  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <p className="text-xs uppercase tracking-[0.18em] text-accent">Unpublished preview</p>
      <h1 className="mt-3 font-serif text-4xl">{envelope.data.title}</h1>
      <div className="mt-8">
        <ArticleBody html={html?.data?.html ?? envelope.data.content} />
      </div>
    </div>
  );
}
