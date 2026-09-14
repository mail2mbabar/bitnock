"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useId, useMemo, useRef, useState } from "react";
import { Search, X } from "lucide-react";
import { fetchClient } from "@/lib/api";
import { rankVideos, searchTopics } from "@/lib/search";
import type { ArticleListItem } from "@/lib/types";
import { channelVideos, youtubeThumb } from "@/lib/videos";

export function SearchBox({
  compact = false,
  autoFocus = false,
  defaultQuery = "",
  live = true,
}: {
  compact?: boolean;
  autoFocus?: boolean;
  defaultQuery?: string;
  live?: boolean;
}) {
  const router = useRouter();
  const id = useId();
  const root = useRef<HTMLDivElement>(null);
  const [q, setQ] = useState(defaultQuery);
  const [open, setOpen] = useState(false);
  const [expanded, setExpanded] = useState(!compact);
  const [articles, setArticles] = useState<ArticleListItem[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setQ(defaultQuery);
  }, [defaultQuery]);

  const videos = useMemo(() => (q.trim().length >= 2 ? rankVideos(q, channelVideos, 3) : []), [q]);

  useEffect(() => {
    if (!live) return;
    const term = q.trim();
    if (term.length < 2) {
      setArticles([]);
      return;
    }
    const handle = window.setTimeout(async () => {
      setLoading(true);
      const result = await fetchClient<ArticleListItem[]>(`/api/v1/public/search?q=${encodeURIComponent(term)}&pageSize=6`);
      setArticles(result.data ?? []);
      setLoading(false);
      setOpen(true);
    }, 180);
    return () => window.clearTimeout(handle);
  }, [q, live]);

  useEffect(() => {
    const onClick = (event: MouseEvent) => {
      if (!root.current?.contains(event.target as Node)) {
        setOpen(false);
        if (compact && !q.trim()) setExpanded(false);
      }
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, [compact, q]);

  const submit = (event?: React.FormEvent) => {
    event?.preventDefault();
    const term = q.trim();
    if (!term) {
      router.push("/search");
      return;
    }
    setOpen(false);
    router.push(`/search?q=${encodeURIComponent(term)}`);
  };

  if (compact && !expanded) {
    return (
      <button type="button" aria-label="Search Bitnock" className="rounded-full p-2 text-muted hover:bg-paper-2 hover:text-ink" onClick={() => setExpanded(true)}>
        <Search size={18} />
      </button>
    );
  }

  const showPanel = live && open && q.trim().length >= 2;

  return (
    <div ref={root} className={`relative ${compact ? "w-44 sm:w-56 md:w-80" : "w-full"}`}>
      <form onSubmit={submit} className="flex items-center gap-2 rounded-full border border-rule bg-paper px-3 py-1.5 focus-within:border-accent">
        <Search size={16} className="shrink-0 text-muted" />
        <label htmlFor={id} className="sr-only">
          Search articles and videos in plain English
        </label>
        <input
          id={id}
          value={q}
          autoFocus={autoFocus || compact}
          onChange={(event) => {
            setQ(event.target.value);
            if (live) setOpen(true);
          }}
          onFocus={() => live && q.trim().length >= 2 && setOpen(true)}
          onKeyDown={(event) => {
            if (event.key === "Escape") {
              setOpen(false);
              if (compact && !q.trim()) setExpanded(false);
            }
          }}
          placeholder={compact ? "Ask Bitnock…" : "Ask anything: how do I log users in, ORM vs SQL…"}
          className="w-full bg-transparent text-sm outline-none placeholder:text-muted"
        />
        {q ? (
          <button type="button" aria-label="Clear search" onClick={() => setQ("")} className="text-muted">
            <X size={14} />
          </button>
        ) : null}
      </form>
      {showPanel ? (
        <div className="absolute right-0 z-50 mt-2 w-[min(32rem,calc(100vw-2rem))] rounded-2xl border border-rule bg-paper p-3 shadow-lg">
          {loading ? <p className="px-2 py-3 text-sm text-muted">Finding the best match…</p> : null}
          {!loading && articles.length === 0 && videos.length === 0 ? (
            <p className="px-2 py-3 text-sm text-muted">We’ll still rank the closest article. Press Enter to see it.</p>
          ) : null}
          {articles.length > 0 ? <p className="px-2 pb-1 text-[11px] uppercase tracking-[0.16em] text-muted">Best articles</p> : null}
          <ul>
            {articles.map((article, index) => (
              <li key={article.id}>
                <Link href={`/articles/${article.slug}`} className="block rounded-xl px-2 py-2 hover:bg-paper-2" onClick={() => setOpen(false)}>
                  <span className="block text-xs text-muted">{index === 0 ? "Top match · " : ""}{article.categoryName}</span>
                  <span className="font-serif text-lg leading-snug">{article.title}</span>
                </Link>
              </li>
            ))}
          </ul>
          {videos.length > 0 ? (
            <>
              <p className="mt-3 px-2 pb-1 text-[11px] uppercase tracking-[0.16em] text-muted">Videos</p>
              <ul>
                {videos.map((video) => (
                  <li key={video.id}>
                    <Link href={`/videos/${video.id}`} className="flex gap-3 rounded-xl px-2 py-2 hover:bg-paper-2" onClick={() => setOpen(false)}>
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img src={youtubeThumb(video.id)} alt="" className="h-12 w-20 rounded object-cover" />
                      <span className="font-serif leading-snug">{video.title}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            </>
          ) : null}
          <div className="mt-3 flex flex-wrap gap-2 px-2">
            {searchTopics.map((topic) => (
              <button
                key={topic}
                type="button"
                className="rounded-full border border-rule px-2 py-1 text-xs text-muted hover:text-ink"
                onClick={() => {
                  setQ(topic);
                  router.push(`/search?q=${encodeURIComponent(topic)}`);
                }}
              >
                {topic}
              </button>
            ))}
          </div>
          <button type="button" className="mt-2 w-full rounded-xl px-2 py-2 text-left text-sm text-accent-2 hover:bg-paper-2" onClick={() => submit()}>
            See all results for “{q.trim()}”
          </button>
        </div>
      ) : null}
    </div>
  );
}
