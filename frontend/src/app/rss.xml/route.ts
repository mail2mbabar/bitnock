import { serverApi } from "@/lib/api";

export async function GET() {
  const response = await fetch(`${serverApi}/rss.xml`, { next: { revalidate: 120 } });
  const xml = await response.text();
  return new Response(xml, { headers: { "Content-Type": "application/rss+xml; charset=utf-8" } });
}
