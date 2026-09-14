"use client";

import { useEffect, useState } from "react";
import { fetchClient, publicApi } from "@/lib/api";
import type { MediaAsset } from "@/lib/types";

export default function MediaAdmin() {
  const [items, setItems] = useState<MediaAsset[]>([]);
  const load = () => fetchClient<MediaAsset[]>("/api/v1/admin/media?pageSize=60").then((r) => setItems(r.data ?? []));
  useEffect(() => { void load(); }, []);
  return (
    <div>
      <h1 className="font-serif text-3xl">Media</h1>
      <p className="mt-2 max-w-2xl text-sm text-muted">Editorial figures for articles — architecture, APIs, Azure, CI/CD, and language. Upload more, then attach a file as an article featured image in the editor.</p>
      <input className="mt-4" type="file" accept="image/*" onChange={async (e) => {
        const file = e.target.files?.[0];
        if (!file) return;
        const body = new FormData();
        body.append("file", file);
        body.append("altText", file.name);
        const token = sessionStorage.getItem("nexus.accessToken");
        await fetch(`${publicApi}/api/v1/admin/media`, { method: "POST", headers: token ? { Authorization: `Bearer ${token}` } : {}, body });
        void load();
      }} />
      <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {items.map((m) => (
          <figure key={m.id} className="rounded-xl border border-rule p-3">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={m.url} alt={m.altText ?? ""} className="aspect-video w-full rounded-lg object-cover" />
            <figcaption className="mt-2 truncate text-xs text-muted">{m.title || m.fileName}</figcaption>
            {m.altText ? <p className="mt-1 line-clamp-2 text-xs text-muted">{m.altText}</p> : null}
          </figure>
        ))}
      </div>
    </div>
  );
}
