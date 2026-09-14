"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { Series } from "@/lib/types";

export default function SeriesAdmin() {
  const [items, setItems] = useState<Series[]>([]);
  const [name, setName] = useState("");
  const load = () => fetchClient<Series[]>("/api/v1/admin/series").then((r) => setItems(r.data ?? []));
  useEffect(() => { void load(); }, []);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    await fetchClient("/api/v1/admin/series", { method: "POST", body: JSON.stringify({ name }) });
    setName("");
    await load();
  }

  return (
    <div>
      <h1 className="font-serif text-3xl">Series</h1>
      <form className="mt-4 flex gap-2" onSubmit={onSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} className="rounded-lg border border-rule bg-paper px-3 py-2" placeholder="Series name" />
        <button className="rounded-lg bg-ink px-4 py-2 text-paper">Add</button>
      </form>
      <ul className="mt-6 space-y-2">{items.map((s) => <li key={s.id}>{s.name} · {s.articleCount} parts</li>)}</ul>
    </div>
  );
}
