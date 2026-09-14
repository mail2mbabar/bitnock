import { NextRequest } from "next/server";
import { readEnv } from "./env";

const hopByHop = new Set(["connection", "keep-alive", "proxy-authenticate", "proxy-authorization", "te", "trailers", "transfer-encoding", "upgrade", "host", "content-length"]);

function upstream() {
  let baseUrl = readEnv("API_URL") || readEnv("NEXT_PUBLIC_API_URL") || "http://localhost:5080";
  if (!/^https?:\/\//i.test(baseUrl)) {
    baseUrl = baseUrl.includes(".") ? `https://${baseUrl}` : `http://${baseUrl}:8080`;
  }
  return baseUrl.replace(/\/$/, "");
}

export async function proxyToApi(req: NextRequest, apiPath: string) {
  const url = `${upstream()}${apiPath}${req.nextUrl.search}`;
  const headers = new Headers();
  req.headers.forEach((value, key) => {
    if (!hopByHop.has(key.toLowerCase())) headers.set(key, value);
  });

  const init: RequestInit = { method: req.method, headers, redirect: "manual" };
  if (req.method !== "GET" && req.method !== "HEAD") {
    init.body = await req.arrayBuffer();
  }

  const res = await fetch(url, init);
  const out = new Headers();
  res.headers.forEach((value, key) => {
    if (!hopByHop.has(key.toLowerCase())) out.set(key, value);
  });
  return new Response(res.body, { status: res.status, headers: out });
}
