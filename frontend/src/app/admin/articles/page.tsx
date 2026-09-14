"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { ArticleListItem } from "@/lib/types";

export default function AdminArticlesPage() {
  const [items, setItems] = useState<ArticleListItem[]>([]);
  const [status, setStatus] = useState("");
  useEffect(() => {
    const q = status ? `?status=${status}` : "";
    fetchClient<ArticleListItem[]>(`/api/v1/admin/articles${q}`).then((r) => setItems(r.data ?? []));
  }, [status]);
  return (
    <div>
      <div className="flex items-center justify-between">
        <h1 className="font-serif text-3xl">Articles</h1>
        <Link href="/admin/articles/new" className="rounded-lg bg-ink px-4 py-2 text-sm text-paper">New article</Link>
      </div>
      <label className="mt-4 block text-sm" htmlFor="status">Status</label>
      <select id="status" value={status} onChange={(e) => setStatus(e.target.value)} className="mt-1 rounded-lg border border-rule bg-paper px-3 py-2">
        <option value="">All</option>
        {["Draft", "PendingReview", "Scheduled", "Published", "Rejected", "Archived"].map((s) => <option key={s}>{s}</option>)}
      </select>
      <table className="mt-6 w-full text-left text-sm">
        <thead><tr className="border-b border-rule text-muted"><th className="py-2">Title</th><th>Status</th><th>Updated</th></tr></thead>
        <tbody>
          {items.map((a) => (
            <tr key={a.id} className="border-b border-rule">
              <td className="py-3"><Link href={`/admin/articles/${a.id}`}>{a.title}</Link></td>
              <td>{a.status}</td>
              <td>{new Date(a.updatedAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
