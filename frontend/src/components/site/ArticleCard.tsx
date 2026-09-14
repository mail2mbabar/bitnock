import Link from "next/link";
import type { ArticleListItem } from "@/lib/types";
import { formatDate } from "@/lib/api";
import { publicAssetUrl } from "@/lib/env";

export function ArticleCard({ article, featured = false }: { article: ArticleListItem; featured?: boolean }) {
  return (
    <article className={featured ? "grid gap-6 md:grid-cols-[1.4fr_1fr] md:items-end" : "flex flex-col gap-3"}>
      {article.featuredImageUrl ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={publicAssetUrl(article.featuredImageUrl) ?? article.featuredImageUrl} alt="" className="aspect-[16/9] w-full rounded-xl object-cover" />
      ) : (
        <div className="flex aspect-[16/9] items-end rounded-xl bg-paper-2 p-6">
          <p className="font-serif text-2xl leading-tight">{article.categoryName}</p>
        </div>
      )}
      <div>
        <p className="text-xs uppercase tracking-[0.18em] text-muted">{article.categoryName}</p>
        <h2 className={`mt-2 font-serif tracking-tight ${featured ? "text-4xl" : "text-2xl"}`}>
          <Link href={`/articles/${article.slug}`} className="hover:text-accent">{article.title}</Link>
        </h2>
        <p className="mt-3 text-muted">{article.excerpt}</p>
        <p className="mt-4 text-sm text-muted">
          <Link href={`/author/${article.authorSlug}`}>{article.authorName}</Link>
          {" · "}
          {formatDate(article.publishedAt)}
          {" · "}
          {article.readingTimeMinutes} min read
        </p>
      </div>
    </article>
  );
}

export function EmptyState({ title, body }: { title: string; body: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-rule px-6 py-16 text-center">
      <h2 className="font-serif text-2xl">{title}</h2>
      <p className="mt-2 text-muted">{body}</p>
    </div>
  );
}
