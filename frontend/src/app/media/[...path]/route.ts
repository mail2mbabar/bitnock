import { NextRequest } from "next/server";
import { proxyToApi } from "@/lib/proxy";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

type Ctx = { params: Promise<{ path: string[] }> };

async function handle(req: NextRequest, ctx: Ctx) {
  const { path } = await ctx.params;
  return proxyToApi(req, `/media/${path.join("/")}`);
}

export const GET = handle;
export const HEAD = handle;
