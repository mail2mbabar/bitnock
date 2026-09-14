import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArticleBody, ReadingProgress, ShareBar, TableOfContents } from "@/components/article/ArticleExtras";
import { ArticleCard } from "@/components/site/ArticleCard";
import { NewsletterForm } from "@/components/site/NewsletterForm";
import { fetchEnvelope, formatDate, serverApi } from "@/lib/api";
import type { ArticleDetail } from "@/lib/types";

type Props = { params: Promise<{ slug: string }> };

async function load(slug: string) {
  const article = await fetchEnvelope<ArticleDetail>(`/api/v1/public/articles/${slug}`, { revalidate: 30 });
  if (!article.data) return null;
  const html = await fetchEnvelope<{ html: string }>(`/api/v1/public/articles/${slug}/html`, { revalidate: 30 });
  return { article: article.data, html: html.data?.html ?? "" };
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const data = await load(slug);
  if (!data) return { title: "Article" };
  const { article } = data;
  const title = article.metaTitle ?? article.title;
  const description = article.metaDescription ?? article.excerpt;
  return {
    title,
    description,
    alternates: { canonical: article.canonicalUrl ?? `/articles/${article.slug}` },
    openGraph: {
      type: "article",
      title: article.ogTitle ?? title,
      description: article.ogDescription ?? description,
      images: article.ogImage || article.featuredImageUrl ? [article.ogImage ?? article.featuredImageUrl!] : undefined,
    },
    twitter: { card: "summary_large_image", title, description },
  };
}

export default async function ArticlePage({ params }: Props) {
  const { slug } = await params;
  const data = await load(slug);
  if (!data) notFound();
  const { article, html } = data;
  const url = `${process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000"}/articles/${article.slug}`;
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Article",
    headline: article.title,
    description: article.excerpt,
    image: article.featuredImageUrl,
    datePublished: article.publishedAt,
    dateModified: article.updatedAt,
    author: { "@type": "Person", name: article.authorName, url: `${process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000"}/author/${article.authorSlug}` },
    publisher: {
      "@type": "Organization",
      name: "Bitnock",
      logo: { "@type": "ImageObject", url: `${process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000"}/logo.png` },
      founder: { "@type": "Person", name: "Muhammad Babar" },
    },
    mainEntityOfPage: url,
  };

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <ReadingProgress />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
      <nav aria-label="Breadcrumb" className="text-sm text-muted">
        <Link href="/">Home</Link> / <Link href="/articles">Articles</Link> / <Link href={`/categories/${article.categorySlug}`}>{article.categoryName}</Link>
      </nav>
      <div className="mt-6 grid gap-10 lg:grid-cols-[minmax(0,1fr)_16rem]">
        <article>
          <p className="text-xs uppercase tracking-[0.18em] text-muted">{article.categoryName} · {article.difficulty} · {article.contentType}</p>
          <h1 className="mt-3 font-serif text-4xl tracking-tight md:text-5xl">{article.title}</h1>
          {article.subtitle ? <p className="mt-4 text-xl text-muted">{article.subtitle}</p> : <p className="mt-4 text-xl text-muted">{article.excerpt}</p>}
          <p className="mt-5 flex flex-wrap items-center gap-3 text-sm text-muted">
            {article.authorAvatarUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={article.authorAvatarUrl} alt="" className="h-8 w-8 rounded-full object-cover" />
            ) : null}
            <span>
              <Link href={`/author/${article.authorSlug}`}>{article.authorName}</Link>
              {" · Microsoft MVP"}
              {" · "}Published {formatDate(article.publishedAt)}
              {article.updatedAt !== article.publishedAt ? ` · Updated ${formatDate(article.updatedAt)}` : ""}
              {" · "}{article.readingTimeMinutes} min read
            </span>
          </p>
          <div className="mt-6 flex flex-wrap gap-2">
            {article.tags.map((tag) => (
              <Link key={tag.id} href={`/tags/${tag.slug}`} className="rounded-full border border-rule px-3 py-1 text-xs">{tag.name}</Link>
            ))}
          </div>
          {article.featuredImageUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={article.featuredImageUrl} alt="" className="mt-8 w-full rounded-2xl" />
          ) : null}
          {article.seriesName && article.seriesSlug ? (
            <div className="mt-8 rounded-xl border border-rule p-4">
              <p className="text-sm">Series: <Link href={`/series/${article.seriesSlug}`} className="underline">{article.seriesName}</Link> · Part {article.seriesOrder}</p>
              <ol className="mt-3 space-y-1 text-sm">
                {article.seriesParts.map((part) => (
                  <li key={part.id} className={part.isCurrent ? "font-medium" : "text-muted"}>
                    {part.isPublished ? <Link href={`/articles/${part.slug}`}>{part.order}. {part.title}</Link> : `${part.order}. ${part.title}`}
                  </li>
                ))}
              </ol>
            </div>
          ) : null}
          <details className="mt-8 lg:hidden">
            <summary className="cursor-pointer text-sm font-medium">Table of contents</summary>
            <div className="mt-3"><TableOfContents items={article.tableOfContents} /></div>
          </details>
          <div className="mt-10"><ArticleBody html={html} /></div>
          <div className="mt-10 border-t border-rule pt-6">
            <ShareBar title={article.title} url={url} />
          </div>
          <div className="mt-10 grid gap-4 md:grid-cols-2">
            {article.previous ? <Link href={`/articles/${article.previous.slug}`} className="rounded-xl border border-rule p-4 text-sm">Previous<br /><span className="font-serif text-lg">{article.previous.title}</span></Link> : <div />}
            {article.next ? <Link href={`/articles/${article.next.slug}`} className="rounded-xl border border-rule p-4 text-right text-sm">Next<br /><span className="font-serif text-lg">{article.next.title}</span></Link> : null}
          </div>
          <section className="mt-12 rounded-2xl border border-rule p-6">
            <div className="flex items-start gap-4">
              {article.authorAvatarUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={article.authorAvatarUrl} alt="" className="h-16 w-16 rounded-2xl object-cover" />
              ) : null}
              <div>
                <h2 className="font-serif text-2xl">Written by {article.authorName}</h2>
                <p className="mt-1 text-xs uppercase tracking-[0.16em] text-muted">Microsoft MVP · Developer Technologies · .NET</p>
                <p className="mt-3 text-muted">{article.authorBio}</p>
                <div className="mt-3 flex flex-wrap gap-4 text-sm">
                  {article.authorGitHub ? <a href={article.authorGitHub}>GitHub</a> : null}
                  {article.authorLinkedIn ? <a href={article.authorLinkedIn}>LinkedIn</a> : null}
                  <a href="https://www.credly.com/badges/7c0a51ad-031c-40b9-9c05-b52e15a2d384">Credly</a>
                  <Link href={`/author/${article.authorSlug}`}>All articles</Link>
                </div>
              </div>
            </div>
          </section>
          <div className="mt-10"><NewsletterForm /></div>
          <section className="mt-12">
            <h2 className="font-serif text-2xl">Related</h2>
            <div className="mt-6 grid gap-8 md:grid-cols-2">
              {article.related.map((item) => <ArticleCard key={item.id} article={item} />)}
            </div>
          </section>
          <section className="mt-12 text-sm text-muted">
            Comments will land here once a provider is connected. The article contract is stable enough for an external discussion service.
          </section>
        </article>
        <aside className="hidden lg:block">
          <div className="sticky top-24"><TableOfContents items={article.tableOfContents} /></div>
        </aside>
      </div>
      <TrackView articleId={article.id} />
    </div>
  );
}

function TrackView({ articleId }: { articleId: string }) {
  return (
    <script
      dangerouslySetInnerHTML={{
        __html: `fetch("${serverApi}/api/v1/public/analytics/pageview",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({path:location.pathname,articleId:"${articleId}",referrer:document.referrer,deviceCategory:window.innerWidth<768?"mobile":"desktop"})})`,
      }}
    />
  );
}
