"use client";

import { useEffect, useState } from "react";
import { fetchClient } from "@/lib/api";
import type { Author } from "@/lib/types";

export default function AuthorsAdmin() {
  const [items, setItems] = useState<Author[]>([]);
  useEffect(() => {
    fetchClient<Author[]>("/api/v1/admin/authors").then((r) => setItems(r.data ?? [])).catch(() => undefined);
  }, []);
  return (
    <div>
      <h1 className="font-serif text-3xl">Authors</h1>
      <ul className="mt-6 space-y-3">
        {items.map((a) => (
          <li key={a.id} className="border-b border-rule pb-3">
            <p className="font-medium">{a.displayName}</p>
            <p className="text-sm text-muted">{a.bio}</p>
          </li>
        ))}
      </ul>
    </div>
  );
}
