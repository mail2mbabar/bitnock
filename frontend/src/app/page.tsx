import Link from "next/link";
import { ArticleCard } from "@/components/site/ArticleCard";
import { AuthorIdentity } from "@/components/site/AuthorIdentity";
import { HomepageBrand } from "@/components/site/BrandLockup";
import { NewsletterForm } from "@/components/site/NewsletterForm";
import { fetchEnvelope } from "@/lib/api";
import type { ArticleListItem, Author, Category, SiteSettings } from "@/lib/types";
import { YOUTUBE_CHANNEL_URL, channelVideos, youtubeThumb } from "@/lib/videos";

export default async function HomePage() {
  const [settings, articles, featured, categories, authors] = await Promise.all([
    fetchEnvelope<SiteSettings>("/api/v1/public/settings", { revalidate: 0 }),
    fetchEnvelope<ArticleListItem[]>("/api/v1/public/articles?pageSize=8"),
    fetchEnvelope<ArticleListItem[]>("/api/v1/public/articles?featuredOnly=true&pageSize=1"),
    fetchEnvelope<Category[]>("/api/v1/public/categories"),
    fetchEnvelope<Author[]>("/api/v1/public/authors"),
  ]);

  const latest = articles.data ?? [];
  const lead = featured.data?.[0] ?? latest[0];
  const rest = latest.filter((a) => a.id !== lead?.id).slice(0, 6);
  const popular = [...latest].sort((a, b) => b.viewCount - a.viewCount).slice(0, 5);
  const author = authors.data?.[0];

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <section>
        <HomepageBrand siteName={settings.data?.siteName ?? "Bitnock"} />
        <p className="mt-5 max-w-2xl text-lg text-muted">{settings.data?.siteDescription}</p>
      </section>

      {lead ? (
        <section className="mt-12 border-t border-rule pt-10">
          <p className="text-xs uppercase tracking-[0.18em] text-muted">Featured</p>
          <div className="mt-6"><ArticleCard article={lead} featured /></div>
        </section>
      ) : null}

      <section className="mt-16 border-t border-rule pt-10">
        <div className="flex items-end justify-between gap-4">
          <div>
            <p className="text-xs uppercase tracking-[0.18em] text-muted">ABi Helpline</p>
            <h2 className="mt-2 font-serif text-3xl">Videos</h2>
          </div>
          <Link href="/videos" className="text-sm underline">All {channelVideos.length} videos</Link>
        </div>
        <p className="mt-3 max-w-2xl text-muted">
          Every tutorial from <a className="underline" href={YOUTUBE_CHANNEL_URL}>youtube.com/@ABiHelpline</a> — C#, ASP.NET Core, interviews — with a written guide next to the player.
        </p>
        <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {channelVideos.slice(0, 4).map((video) => (
            <Link key={video.id} href={`/videos/${video.id}`} className="group overflow-hidden rounded-2xl border border-rule">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={youtubeThumb(video.id)} alt={video.title} className="aspect-video w-full object-cover" />
              <span className="block p-3 font-serif text-lg leading-snug group-hover:text-accent">{video.title}</span>
            </Link>
          ))}
        </div>
      </section>

      <section className="mt-16 grid gap-12 lg:grid-cols-[1.6fr_0.8fr]">
        <div>
          <h2 className="font-serif text-3xl">Latest</h2>
          <div className="mt-8 grid gap-10">
            {rest.map((article) => <ArticleCard key={article.id} article={article} />)}
          </div>
          <Link href="/articles" className="mt-8 inline-block text-sm underline">All articles</Link>
        </div>
        <aside className="space-y-10">
          <div>
            <h2 className="font-serif text-2xl">Popular</h2>
            <ol className="mt-4 space-y-4">
              {popular.map((article, index) => (
                <li key={article.id} className="border-b border-rule pb-4">
                  <span className="text-xs text-muted">{String(index + 1).padStart(2, "0")}</span>
                  <Link href={`/articles/${article.slug}`} className="mt-1 block font-serif text-xl">{article.title}</Link>
                </li>
              ))}
            </ol>
          </div>
          <div>
            <h2 className="font-serif text-2xl">Topics</h2>
            <div className="mt-4 flex flex-wrap gap-2">
              {(categories.data ?? []).slice(0, 12).map((category) => (
                <Link key={category.id} href={`/categories/${category.slug}`} className="rounded-full border border-rule px-3 py-1 text-sm hover:bg-paper-2">
                  {category.name}
                </Link>
              ))}
            </div>
          </div>
          {author ? (
            <div>
              <h2 className="font-serif text-2xl">Author</h2>
              <div className="mt-4">
                <AuthorIdentity author={author} />
              </div>
            </div>
          ) : null}
          <NewsletterForm />
        </aside>
      </section>
    </div>
  );
}
