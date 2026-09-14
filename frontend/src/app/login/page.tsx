"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { BrandLockup } from "@/components/site/BrandLockup";
import { fetchClient } from "@/lib/api";
import type { AuthResponse } from "@/lib/types";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@localhost");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const result = await fetchClient<AuthResponse>("/api/v1/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    });
    if (!result.success || !result.data) {
      setError(result.error?.message ?? "Login failed");
      return;
    }
    sessionStorage.setItem("nexus.accessToken", result.data.accessToken);
    sessionStorage.setItem("nexus.refreshToken", result.data.refreshToken);
    router.push("/admin");
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4">
      <BrandLockup />
      <h1 className="mt-8 font-serif text-3xl">Editor sign-in</h1>
      <p className="mt-2 text-sm text-muted">DEVELOPMENT ONLY default: admin@localhost / DevOnly!Nexus2026</p>
      <form onSubmit={onSubmit} className="mt-6 space-y-4">
        <div>
          <label htmlFor="email" className="text-sm">Email</label>
          <input id="email" value={email} onChange={(e) => setEmail(e.target.value)} className="mt-1 w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        </div>
        <div>
          <label htmlFor="password" className="text-sm">Password</label>
          <input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} className="mt-1 w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        </div>
        {error ? <p className="text-sm text-accent">{error}</p> : null}
        <button className="w-full rounded-lg bg-ink py-2 text-paper">Sign in</button>
      </form>
    </div>
  );
}
