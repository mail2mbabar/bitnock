"use client";

import { useState } from "react";
import { publicApi } from "@/lib/api";

export function NewsletterForm() {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<"idle" | "ok" | "error">("idle");

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const response = await fetch(`${publicApi}/api/v1/public/newsletter`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email }),
    });
    setStatus(response.ok ? "ok" : "error");
  }

  return (
    <form id="newsletter" onSubmit={onSubmit} className="rounded-2xl border border-rule bg-paper-2 p-6">
      <h2 className="font-serif text-2xl">The weekday brief</h2>
      <p className="mt-2 text-sm text-muted">Architecture notes, ASP.NET Core internals, and the occasional strongly-typed opinion.</p>
      <label className="mt-4 block text-sm" htmlFor="newsletter-email">Email</label>
      <div className="mt-1 flex gap-2">
        <input id="newsletter-email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        <button type="submit" className="rounded-lg bg-ink px-4 py-2 text-sm text-paper">Subscribe</button>
      </div>
      {status === "ok" ? <p className="mt-2 text-sm">Check your inbox to confirm.</p> : null}
      {status === "error" ? <p className="mt-2 text-sm text-accent">Could not subscribe. Try again.</p> : null}
    </form>
  );
}
