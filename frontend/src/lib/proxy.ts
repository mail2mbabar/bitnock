import { NextRequest } from "next/server";

const hopByHop = new Set(["connection", "keep-alive", "proxy-authenticate", "proxy-authorization", "te", "trailers", "transfer-encoding", "upgrade", "host", "content-length"]);

function upstream() {
  let baseUrl = process.env.API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";
  if (baseUrl && !/^https?:\/\//i.test(baseUrl)) {
    baseUrl = `https://${baseUrl}`;
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
