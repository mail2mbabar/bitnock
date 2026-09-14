"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { AuditLog } from "@/lib/types";

export default function AuditLogsPage() {
  const [items, setItems] = useState<AuditLog[]>([]);
  useEffect(() => { fetchClient<AuditLog[]>("/api/v1/admin/audit-logs").then((r) => setItems(r.data ?? [])); }, []);
  return (
    <div>
      <h1 className="font-serif text-3xl">Audit logs</h1>
      <table className="mt-6 w-full text-left text-sm">
        <thead><tr className="border-b border-rule"><th className="py-2">When</th><th>Actor</th><th>Action</th><th>Resource</th></tr></thead>
        <tbody>
          {items.map((l) => (
            <tr key={l.id} className="border-b border-rule">
              <td className="py-2">{new Date(l.timestamp).toLocaleString()}</td>
              <td>{l.actorName} ({l.actorType})</td>
              <td>{l.action}</td>
              <td>{l.resourceType} {l.resourceId}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
