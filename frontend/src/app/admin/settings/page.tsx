"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { SiteSettings } from "@/lib/types";

export default function SettingsPage() {
  const [settings, setSettings] = useState<SiteSettings | null>(null);
  const [mode, setMode] = useState("RequireApproval");
  useEffect(() => {
    fetch("/api/v1/public/settings".replace("/api", `${process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080"}/api`))
      .then((r) => r.json())
      .then((r) => { setSettings(r.data); setMode(r.data.publishingMode); });
  }, []);
  return (
    <div className="max-w-xl">
      <h1 className="font-serif text-3xl">Settings</h1>
      <p className="mt-3 text-muted">Site name, logo, and social links are configured in environment / appsettings so the brand can change without a rewrite.</p>
      {settings ? <pre className="mt-4 overflow-auto rounded-xl border border-rule p-4 text-xs">{JSON.stringify(settings, null, 2)}</pre> : null}
      <label className="mt-6 block text-sm" htmlFor="mode">Publishing mode</label>
      <select id="mode" value={mode} onChange={(e) => setMode(e.target.value)} className="mt-1 w-full rounded-lg border border-rule bg-paper px-3 py-2">
        <option>RequireApproval</option>
        <option>AutoPublish</option>
      </select>
      <button className="mt-4 rounded-lg bg-ink px-4 py-2 text-paper" onClick={() => fetchClient("/api/v1/admin/publishing-mode", { method: "PUT", body: JSON.stringify({ mode }) })}>Save mode</button>
    </div>
  );
}
