"use client";

import { useEffect } from "react";
import type { TocItem } from "@/lib/types";

export function ReadingProgress() {
  useEffect(() => {
    const bar = document.getElementById("reading-progress");
    const onScroll = () => {
      const article = document.getElementById("article-body");
      if (!bar || !article) return;
      const rect = article.getBoundingClientRect();
      const total = article.offsetHeight - window.innerHeight;
      const scrolled = Math.min(1, Math.max(0, -rect.top / Math.max(total, 1)));
      bar.style.transform = `scaleX(${scrolled})`;
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);
  return <div id="reading-progress" className="fixed left-0 top-0 z-50 h-0.5 w-full origin-left scale-x-0 bg-accent" />;
}

export function TableOfContents({ items }: { items: TocItem[] }) {
  if (items.length === 0) return null;
  return (
    <nav aria-label="Table of contents" className="rounded-xl border border-rule p-4">
      <p className="text-xs uppercase tracking-[0.18em] text-muted">Contents</p>
      <ol className="mt-3 space-y-2 text-sm">
        {items.map((item) => (
          <li key={item.id} className={item.level === 3 ? "pl-3" : ""}>
            <a href={`#${item.id}`} className="text-muted hover:text-ink">{item.text}</a>
          </li>
        ))}
      </ol>
    </nav>
  );
}

export function ArticleBody({ html }: { html: string }) {
  useEffect(() => {
    const root = document.getElementById("article-body");
    if (!root) return;
    root.querySelectorAll("pre").forEach((pre) => {
      if (pre.parentElement?.classList.contains("code-wrap")) return;
      const wrap = document.createElement("div");
      wrap.className = "code-wrap relative";
      pre.parentElement?.insertBefore(wrap, pre);
      wrap.appendChild(pre);
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = "Copy";
      button.className = "absolute right-2 top-2 rounded bg-paper px-2 py-1 text-xs";
      button.addEventListener("click", async () => {
        await navigator.clipboard.writeText(pre.innerText);
        button.textContent = "Copied";
        setTimeout(() => (button.textContent = "Copy"), 1500);
      });
      wrap.appendChild(button);
    });
    root.querySelectorAll("h2, h3").forEach((heading) => {
      if (!heading.id) {
        heading.id = heading.textContent?.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "") ?? "";
      }
    });
  }, [html]);

  return <div id="article-body" className="prose-article" dangerouslySetInnerHTML={{ __html: html }} />;
}

export function ShareBar({ title, url }: { title: string; url: string }) {
  const encoded = encodeURIComponent(url);
  const text = encodeURIComponent(title);
  return (
    <div className="flex flex-wrap gap-3 text-sm">
      <a className="underline" href={`https://www.linkedin.com/sharing/share-offsite/?url=${encoded}`}>LinkedIn</a>
      <a className="underline" href={`https://twitter.com/intent/tweet?url=${encoded}&text=${text}`}>X</a>
      <a className="underline" href={`https://www.facebook.com/sharer/sharer.php?u=${encoded}`}>Facebook</a>
      <a className="underline" href={`https://www.reddit.com/submit?url=${encoded}&title=${text}`}>Reddit</a>
      <button type="button" className="underline" onClick={() => navigator.clipboard.writeText(url)}>Copy link</button>
    </div>
  );
}
