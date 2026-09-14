"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { AgentCredential } from "@/lib/types";

export default function AgentsPage() {
  const [items, setItems] = useState<AgentCredential[]>([]);
  const [secret, setSecret] = useState<string | null>(null);
  const load = () => fetchClient<AgentCredential[]>("/api/v1/admin/agents").then((r) => setItems(r.data ?? []));
  useEffect(() => { void load(); }, []);
  return (
    <div>
      <h1 className="font-serif text-3xl">Agents</h1>
      <p className="mt-2 text-sm text-muted">Secrets are shown once. They are hashed at rest and never written to application logs.</p>
      <button className="mt-4 rounded-lg bg-ink px-4 py-2 text-paper" onClick={async () => {
        const result = await fetchClient<{ apiKey: string }>("/api/v1/admin/agents", {
          method: "POST",
          body: JSON.stringify({
            name: "Blog Publisher Agent",
            scopes: ["articles.read", "articles.create", "articles.update", "articles.publish", "articles.schedule", "articles.validate", "media.upload", "media.read", "categories.read", "tags.read", "series.read", "rules.read"],
          }),
        });
        setSecret(result.data?.apiKey ?? null);
        void load();
      }}>Create API credential</button>
      {secret ? <p className="mt-4 break-all rounded-xl border border-rule p-4 text-sm">Copy now: {secret}</p> : null}
      <table className="mt-6 w-full text-left text-sm">
        <thead><tr className="border-b border-rule"><th className="py-2">Name</th><th>Status</th><th>Scopes</th><th>Last used</th><th></th></tr></thead>
        <tbody>
          {items.map((a) => (
            <tr key={a.id} className="border-b border-rule">
              <td className="py-3">{a.name}<div className="text-xs text-muted">{a.keyPrefix}</div></td>
              <td>{a.status}</td>
              <td className="max-w-xs truncate">{a.scopes.join(" ")}</td>
              <td>{a.lastUsedAt ? new Date(a.lastUsedAt).toLocaleString() : "never"}</td>
              <td className="space-x-2">
                <button onClick={async () => { const r = await fetchClient<{ apiKey: string }>(`/api/v1/admin/agents/${a.id}/rotate`, { method: "POST" }); setSecret(r.data?.apiKey ?? null); void load(); }}>Rotate</button>
                <button onClick={async () => { await fetchClient(`/api/v1/admin/agents/${a.id}/revoke`, { method: "POST" }); void load(); }}>Revoke</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
