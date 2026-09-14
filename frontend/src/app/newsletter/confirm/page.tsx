"use client";

import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { publicApi } from "@/lib/api";

function ConfirmInner() {
  const params = useSearchParams();
  const [status, setStatus] = useState("Confirming…");
  useEffect(() => {
    const token = params.get("token");
    if (!token) {
      setStatus("Missing token.");
      return;
    }
    fetch(`${publicApi}/api/v1/public/newsletter/confirm`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token }),
    }).then((r) => setStatus(r.ok ? "Subscription confirmed." : "This confirmation link is invalid."));
  }, [params]);
  return <p className="mx-auto max-w-lg px-4 py-20 text-center">{status}</p>;
}

export default function NewsletterConfirmPage() {
  return (
    <Suspense>
      <ConfirmInner />
    </Suspense>
  );
}
