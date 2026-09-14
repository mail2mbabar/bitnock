"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { AnalyticsSummary } from "@/lib/types";

export default function AnalyticsPage() {
  const [data, setData] = useState<AnalyticsSummary | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    fetchClient<AnalyticsSummary>("/api/v1/admin/dashboard").then((r) => {
      if (r.data) setData(r.data);
      else setError(r.error?.message ?? "Could not load analytics.");
    });
  }, []);
  if (error) return <p className="text-muted">{error}</p>;
  if (!data) return <p>Loading…</p>;
  const max = Math.max(1, ...data.viewsByDay.map((d) => d.views));
  return (
    <div>
      <h1 className="font-serif text-3xl">Analytics</h1>
      <p className="mt-2 text-muted">First-party page views only. Swap this service for an external provider later.</p>
      <div className="mt-8 flex h-40 items-end gap-2">
        {data.viewsByDay.map((d) => (
          <div key={d.date} className="flex-1 bg-accent-2" style={{ height: `${(d.views / max) * 100}%` }} title={`${d.date}: ${d.views}`} />
        ))}
      </div>
    </div>
  );
}
