"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { Category } from "@/lib/types";

export default function CategoriesAdmin() {
  const [items, setItems] = useState<Category[]>([]);
  const [name, setName] = useState("");
  const load = () => fetchClient<Category[]>("/api/v1/admin/categories").then((r) => setItems(r.data ?? []));
  useEffect(() => { void load(); }, []);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    await fetchClient("/api/v1/admin/categories", {
      method: "POST",
      body: JSON.stringify({ name, sortOrder: items.length + 1, isActive: true }),
    });
    setName("");
    await load();
  }

  return (
    <div>
      <h1 className="font-serif text-3xl">Categories</h1>
      <form className="mt-4 flex gap-2" onSubmit={onSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Name" className="rounded-lg border border-rule bg-paper px-3 py-2" />
        <button className="rounded-lg bg-ink px-4 py-2 text-paper">Add</button>
      </form>
      <ul className="mt-6 space-y-2">
        {items.map((c) => (
          <li key={c.id} className="flex items-center justify-between border-b border-rule py-2">
            <span>{c.name} · {c.slug}</span>
            <button className="text-sm text-accent" onClick={async () => { await fetchClient(`/api/v1/admin/categories/${c.id}`, { method: "DELETE" }); void load(); }}>Delete</button>
          </li>
        ))}
      </ul>
    </div>
  );
}
