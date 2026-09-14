"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useMemo, useState } from "react";
import { ArticleCard, EmptyState } from "@/components/site/ArticleCard";
import { SearchBox } from "@/components/site/SearchBox";
import { fetchClient } from "@/lib/api";
import { rankVideos, searchStarters } from "@/lib/search";
import type { ArticleListItem } from "@/lib/types";
import { channelVideos, youtubeThumb } from "@/lib/videos";

function SearchResults() {
  const params = useSearchParams();
  const router = useRouter();
  const q = params.get("q") ?? "";
  const [articles, setArticles] = useState<ArticleListItem[]>([]);
  const [loading, setLoading] = useState(false);

  const videos = useMemo(() => (q.trim() ? rankVideos(q, channelVideos, 4) : []), [q]);

  useEffect(() => {
    const term = q.trim();
    if (!term) {
      setArticles([]);
      return;
    }
    let cancelled = false;
    setLoading(true);
    fetchClient<ArticleListItem[]>(`/api/v1/public/search?q=${encodeURIComponent(term)}&pageSize=20`).then((result) => {
      if (!cancelled) {
        setArticles(result.data ?? []);
        setLoading(false);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [q]);

  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <p className="text-xs uppercase tracking-[0.22em] text-muted">Search</p>
      <h1 className="mt-3 font-serif text-4xl">Ask Bitnock anything</h1>
      <p className="mt-3 max-w-2xl text-muted">
        Type a question in plain English: “how do I log users in”, “ORM vs SQL”, “interview prep”. We rank the closest article even when you don’t use the exact keywords.
      </p>
      <div className="mt-6">
        <SearchBox autoFocus={!q} defaultQuery={q} live={false} />
      </div>
      {!q ? (
        <div className="mt-8">
          <p className="text-sm text-muted">Try one of these</p>
          <div className="mt-3 flex flex-wrap gap-2">
            {searchStarters.map((item) => (
              <button
                key={item}
                type="button"
                className="rounded-full border border-rule px-3 py-1.5 text-sm hover:bg-paper-2"
                onClick={() => router.push(`/search?q=${encodeURIComponent(item)}`)}
              >
                {item}
              </button>
            ))}
          </div>
        </div>
      ) : null}

      {q && loading ? <p className="mt-10 text-sm text-muted">Ranking the best matches…</p> : null}

      {q && !loading && articles.length === 0 && videos.length === 0 ? (
        <div className="mt-10">
          <EmptyState title="Nothing close enough" body="Try a shorter phrase, or pick a starter above." />
        </div>
      ) : null}

      {articles.length > 0 ? (
        <section className="mt-12">
          <h2 className="font-serif text-2xl">{q ? "Best articles" : "Articles"}</h2>
          <div className="mt-6 grid gap-10">
            {articles.map((article, index) => (
              <div key={article.id}>
                {index === 0 && q ? (
                  <p className="mb-3 text-xs uppercase tracking-[0.16em] text-accent-2">Top match for “{q}”</p>
                ) : null}
                <ArticleCard article={article} featured={index === 0} />
              </div>
            ))}
          </div>
        </section>
      ) : null}

      {videos.length > 0 ? (
        <section className="mt-10">
          <h2 className="font-serif text-2xl">Matching videos</h2>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            {videos.map((video) => (
              <Link key={video.id} href={`/videos/${video.id}`} className="overflow-hidden rounded-2xl border border-rule">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={youtubeThumb(video.id)} alt="" className="aspect-video w-full object-cover" />
                <span className="block p-3 font-serif leading-snug">{video.title}</span>
              </Link>
            ))}
          </div>
        </section>
      ) : null}
    </div>
  );
}

export default function SearchPage() {
  return (
    <Suspense fallback={<div className="mx-auto max-w-4xl px-4 py-10 font-serif text-3xl">Search</div>}>
      <SearchResults />
    </Suspense>
  );
}
