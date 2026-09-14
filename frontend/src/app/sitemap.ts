import type { MetadataRoute } from "next";
import { fetchEnvelope, serverApi } from "@/lib/api";
import type { ArticleListItem, Author, Category, Series, Tag } from "@/lib/types";
import { channelVideos } from "@/lib/videos";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const origin = process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000";
  const articles = await fetchEnvelope<ArticleListItem[]>("/api/v1/public/articles?pageSize=50");
  const categories = await fetchEnvelope<Category[]>("/api/v1/public/categories");
  const tags = await fetchEnvelope<Tag[]>("/api/v1/public/tags");
  const series = await fetchEnvelope<Series[]>("/api/v1/public/series");
  const authors = await fetchEnvelope<Author[]>("/api/v1/public/authors");
  return [
    { url: origin, changeFrequency: "daily", priority: 1 },
    { url: `${origin}/articles`, changeFrequency: "hourly", priority: 0.9 },
    { url: `${origin}/videos`, changeFrequency: "weekly", priority: 0.85 },
    ...channelVideos.map((v) => ({ url: `${origin}/videos/${v.id}`, changeFrequency: "monthly" as const, priority: 0.7 })),
    ...(articles.data ?? []).map((a) => ({ url: `${origin}/articles/${a.slug}`, lastModified: a.updatedAt, changeFrequency: "weekly" as const, priority: 0.8 })),
    ...(categories.data ?? []).map((c) => ({ url: `${origin}/categories/${c.slug}`, changeFrequency: "weekly" as const, priority: 0.6 })),
    ...(tags.data ?? []).map((t) => ({ url: `${origin}/tags/${t.slug}`, changeFrequency: "weekly" as const, priority: 0.5 })),
    ...(series.data ?? []).map((s) => ({ url: `${origin}/series/${s.slug}`, changeFrequency: "weekly" as const, priority: 0.6 })),
    ...(authors.data ?? []).map((a) => ({ url: `${origin}/author/${a.slug}`, changeFrequency: "weekly" as const, priority: 0.5 })),
  ];
}

export const dynamic = "force-dynamic";
void serverApi;
