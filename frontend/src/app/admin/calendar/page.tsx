"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { AnalyticsSummary } from "@/lib/types";

export default function CalendarPage() {
  const [data, setData] = useState<AnalyticsSummary | null>(null);
  useEffect(() => {
    fetchClient<AnalyticsSummary>("/api/v1/admin/dashboard").then((r) => setData(r.data));
  }, []);
  return (
    <div>
      <h1 className="font-serif text-3xl">Publishing calendar</h1>
      <p className="mt-2 text-muted">Today, tomorrow, and the upcoming queue.</p>
      <ul className="mt-6 space-y-3">
        {(data?.upcoming ?? []).map((item) => (
          <li key={item.id} className="rounded-xl border border-rule p-4">
            <p className="text-xs uppercase text-muted">{item.status}</p>
            <p className="font-serif text-xl">{item.title}</p>
            <p className="text-sm text-muted">{item.at ? new Date(item.at).toUTCString() : "unscheduled"}</p>
          </li>
        ))}
      </ul>
    </div>
  );
}
