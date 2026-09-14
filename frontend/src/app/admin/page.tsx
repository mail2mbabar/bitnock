"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { AnalyticsSummary } from "@/lib/types";

export default function AdminHome() {
  const [data, setData] = useState<AnalyticsSummary | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    fetchClient<AnalyticsSummary>("/api/v1/admin/dashboard").then((r) => {
      if (r.data) setData(r.data);
      else setError(r.error?.message ?? "Could not load the dashboard.");
    });
  }, []);
  if (error) {
    return (
      <div>
        <h1 className="font-serif text-3xl">Overview</h1>
        <p className="mt-4 text-muted">{error}</p>
        <Link href="/login" className="mt-4 inline-block text-sm text-accent-2 underline">Sign in</Link>
      </div>
    );
  }
  if (!data) return <p>Loading dashboard…</p>;
  const cards = [
    ["Total", data.totalArticles],
    ["Published", data.published],
    ["Drafts", data.drafts],
    ["Scheduled", data.scheduled],
    ["Pending", data.pendingReview],
    ["AI generated", data.aiGenerated],
    ["Views", data.totalViews],
  ];
  return (
    <div>
      <h1 className="font-serif text-3xl">Overview</h1>
      <div className="mt-6 grid gap-4 sm:grid-cols-3 lg:grid-cols-4">
        {cards.map(([label, value]) => (
          <div key={String(label)} className="rounded-xl border border-rule p-4">
            <p className="text-xs uppercase tracking-wide text-muted">{label}</p>
            <p className="mt-2 font-serif text-3xl">{value}</p>
          </div>
        ))}
      </div>
      <div className="mt-10 grid gap-8 lg:grid-cols-2">
        <section>
          <h2 className="font-serif text-2xl">Top articles</h2>
          <ul className="mt-4 space-y-3">
            {data.topArticles.map((a) => (
              <li key={a.id}><Link href={`/admin/articles/${a.id}`}>{a.title}</Link> <span className="text-sm text-muted">{a.viewCount} views</span></li>
            ))}
          </ul>
        </section>
        <section>
          <h2 className="font-serif text-2xl">Upcoming</h2>
          <ul className="mt-4 space-y-3">
            {data.upcoming.map((a) => (
              <li key={a.id}>{a.title} · {a.status}</li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  );
}
