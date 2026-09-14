"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { Tag } from "@/lib/types";

export default function TagsAdmin() {
  const [items, setItems] = useState<Tag[]>([]);
  const [name, setName] = useState("");
  const load = () => fetchClient<Tag[]>("/api/v1/admin/tags").then((r) => setItems(r.data ?? []));
  useEffect(() => { void load(); }, []);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    await fetchClient("/api/v1/admin/tags", { method: "POST", body: JSON.stringify({ name }) });
    setName("");
    await load();
  }

  return (
    <div>
      <h1 className="font-serif text-3xl">Tags</h1>
      <form className="mt-4 flex gap-2" onSubmit={onSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} className="rounded-lg border border-rule bg-paper px-3 py-2" placeholder="Tag" />
        <button className="rounded-lg bg-ink px-4 py-2 text-paper">Add</button>
      </form>
      <ul className="mt-6 columns-2 gap-8">
        {items.map((t) => <li key={t.id}>{t.name}</li>)}
      </ul>
    </div>
  );
}
