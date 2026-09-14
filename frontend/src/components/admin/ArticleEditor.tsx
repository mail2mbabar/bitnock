"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchClient } from "@/lib/api";
import type { ArticleDetail, Category, ValidationResult } from "@/lib/types";

type Props = { articleId?: string };

const empty = {
  title: "",
  slug: "",
  subtitle: "",
  excerpt: "",
  content: "",
  categorySlug: "software-engineering",
  tags: "C#",
  technologies: ".NET 10",
  difficulty: "Intermediate",
  contentType: "Guide",
  isFeatured: false,
  metaTitle: "",
  metaDescription: "",
  canonicalUrl: "",
  ogTitle: "",
  ogDescription: "",
};

export function ArticleEditor({ articleId }: Props) {
  const router = useRouter();
  const [form, setForm] = useState(empty);
  const [rowVersion, setRowVersion] = useState<number | undefined>();
  const [status, setStatus] = useState("Draft");
  const [message, setMessage] = useState("");
  const [preview, setPreview] = useState(false);
  const [validation, setValidation] = useState<ValidationResult | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [dirty, setDirty] = useState(false);

  useEffect(() => {
    fetchClient<Category[]>("/api/v1/admin/categories").then((r) => setCategories(r.data ?? []));
    if (!articleId) return;
    fetchClient<ArticleDetail>(`/api/v1/admin/articles/${articleId}`).then((r) => {
      if (!r.data) return;
      const a = r.data;
      setStatus(a.status);
      setRowVersion(a.rowVersion);
      setForm({
        title: a.title,
        slug: a.slug,
        subtitle: a.subtitle ?? "",
        excerpt: a.excerpt,
        content: a.content,
        categorySlug: a.categorySlug,
        tags: a.tags.map((t) => t.name).join(", "),
        technologies: a.technologies.map((t) => t.name).join(", "),
        difficulty: a.difficulty,
        contentType: a.contentType,
        isFeatured: a.isFeatured,
        metaTitle: a.metaTitle ?? "",
        metaDescription: a.metaDescription ?? "",
        canonicalUrl: a.canonicalUrl ?? "",
        ogTitle: a.ogTitle ?? "",
        ogDescription: a.ogDescription ?? "",
      });
    });
  }, [articleId]);

  useEffect(() => {
    const onLeave = (event: BeforeUnloadEvent) => {
      if (dirty) event.preventDefault();
    };
    window.addEventListener("beforeunload", onLeave);
    return () => window.removeEventListener("beforeunload", onLeave);
  }, [dirty]);

  const payload = useMemo(() => ({
    title: form.title,
    slug: form.slug || undefined,
    subtitle: form.subtitle,
    excerpt: form.excerpt,
    content: form.content,
    categorySlug: form.categorySlug,
    tags: form.tags.split(",").map((t) => t.trim()).filter(Boolean),
    technologies: form.technologies.split(",").map((t) => t.trim()).filter(Boolean),
    difficulty: form.difficulty,
    contentType: form.contentType,
    isFeatured: form.isFeatured,
    metaTitle: form.metaTitle,
    metaDescription: form.metaDescription,
    canonicalUrl: form.canonicalUrl,
    ogTitle: form.ogTitle,
    ogDescription: form.ogDescription,
    rowVersion,
  }), [form, rowVersion]);

  async function save() {
    const path = articleId ? `/api/v1/admin/articles/${articleId}` : "/api/v1/admin/articles";
    const method = articleId ? "PUT" : "POST";
    const result = await fetchClient<ArticleDetail>(path, { method, body: JSON.stringify(payload) });
    if (!result.success || !result.data) {
      setMessage(result.error?.message ?? "Save failed");
      return result.data;
    }
    setRowVersion(result.data.rowVersion);
    setStatus(result.data.status);
    setDirty(false);
    setMessage("Saved");
    if (!articleId) router.replace(`/admin/articles/${result.data.id}`);
    return result.data;
  }

  useEffect(() => {
    if (!articleId || !dirty) return;
    const handle = setTimeout(() => { void save(); }, 4000);
    return () => clearTimeout(handle);
  }, [payload, articleId, dirty]);

  function set<K extends keyof typeof empty>(key: K, value: (typeof empty)[K]) {
    setForm((f) => ({ ...f, [key]: value }));
    setDirty(true);
  }

  async function action(path: string, body?: unknown) {
    await save();
    if (!articleId) return;
    const result = await fetchClient<ArticleDetail>(`/api/v1/admin/articles/${articleId}/${path}`, {
      method: "POST",
      body: body ? JSON.stringify(body) : undefined,
    });
    if (result.data) {
      setStatus(result.data.status);
      setMessage(result.data.status);
    } else {
      setMessage(result.error?.message ?? "Action failed");
    }
  }

  return (
    <div className="grid gap-8 lg:grid-cols-[1.4fr_0.8fr]">
      <div>
        <label className="text-sm" htmlFor="title">Title</label>
        <input id="title" value={form.title} onChange={(e) => set("title", e.target.value)} className="mt-1 w-full rounded-lg border border-rule bg-paper px-3 py-2 font-serif text-2xl" />
        <label className="mt-4 block text-sm" htmlFor="subtitle">Subtitle</label>
        <input id="subtitle" value={form.subtitle} onChange={(e) => set("subtitle", e.target.value)} className="mt-1 w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        <label className="mt-4 block text-sm" htmlFor="excerpt">Excerpt</label>
        <textarea id="excerpt" value={form.excerpt} onChange={(e) => set("excerpt", e.target.value)} className="mt-1 h-20 w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        <div className="mt-4 flex gap-2 text-sm">
          <button type="button" className={!preview ? "underline" : ""} onClick={() => setPreview(false)}>Edit</button>
          <button type="button" className={preview ? "underline" : ""} onClick={() => setPreview(true)}>Preview</button>
        </div>
        {preview ? (
          <pre className="mt-3 overflow-auto whitespace-pre-wrap rounded-xl border border-rule p-4 text-sm">{form.content}</pre>
        ) : (
          <>
            <label className="mt-3 block text-sm" htmlFor="content">Markdown</label>
            <textarea id="content" value={form.content} onChange={(e) => set("content", e.target.value)} className="mt-1 min-h-[28rem] w-full rounded-xl border border-rule bg-paper p-4 font-mono text-sm" />
          </>
        )}
      </div>
      <aside className="space-y-4">
        <p className="text-sm text-muted">Status: {status} {message ? `· ${message}` : ""}</p>
        <button className="w-full rounded-lg bg-ink py-2 text-paper" onClick={() => void save()}>Save draft</button>
        <button className="w-full rounded-lg border border-rule py-2" onClick={() => void action("publish")}>Publish</button>
        <button className="w-full rounded-lg border border-rule py-2" onClick={() => {
          const at = prompt("Schedule UTC (ISO)", new Date(Date.now() + 86400000).toISOString());
          if (at) void action("schedule", { scheduledAt: at });
        }}>Schedule</button>
        <button className="w-full rounded-lg border border-rule py-2" onClick={() => void action("submit")}>Submit for review</button>
        <button className="w-full rounded-lg border border-rule py-2" onClick={() => void action("approve")}>Approve</button>
        <button className="w-full rounded-lg border border-rule py-2" onClick={() => {
          const reason = prompt("Rejection reason");
          if (reason) void action("reject", { reason });
        }}>Reject</button>
        <button className="w-full rounded-lg border border-rule py-2" onClick={async () => {
          if (!articleId) return;
          const result = await fetchClient<{ previewUrl: string }>(`/api/v1/admin/articles/${articleId}/preview`, { method: "POST" });
          if (result.data) window.open(result.data.previewUrl, "_blank");
        }}>Preview unpublished</button>
        <button className="w-full rounded-lg border border-rule py-2" onClick={async () => {
          if (!articleId) return;
          setValidation((await fetchClient<ValidationResult>(`/api/v1/admin/articles/${articleId}/validate`, { method: "POST" })).data);
        }}>Validate</button>
        {validation ? (
          <div className="rounded-xl border border-rule p-3 text-sm">
            <p>Score {validation.score} · SEO {validation.seoScore} · Ready {String(validation.readyToPublish)}</p>
            {validation.errors.map((e) => <p key={e.code} className="text-accent">{e.message}</p>)}
            {validation.warnings.map((e) => <p key={e.code}>{e.message}</p>)}
          </div>
        ) : null}
        <label className="block text-sm" htmlFor="slug">Slug</label>
        <input id="slug" value={form.slug} onChange={(e) => set("slug", e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        <label className="block text-sm" htmlFor="category">Category</label>
        <select id="category" value={form.categorySlug} onChange={(e) => set("categorySlug", e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2">
          {categories.map((c) => <option key={c.id} value={c.slug}>{c.name}</option>)}
        </select>
        <label className="block text-sm" htmlFor="tags">Tags</label>
        <input id="tags" value={form.tags} onChange={(e) => set("tags", e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        <label className="block text-sm" htmlFor="techs">Technologies</label>
        <input id="techs" value={form.technologies} onChange={(e) => set("technologies", e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        <label className="block text-sm" htmlFor="difficulty">Difficulty</label>
        <select id="difficulty" value={form.difficulty} onChange={(e) => set("difficulty", e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2">
          {["Beginner", "Intermediate", "Advanced", "Expert"].map((d) => <option key={d}>{d}</option>)}
        </select>
        <label className="block text-sm" htmlFor="type">Content type</label>
        <select id="type" value={form.contentType} onChange={(e) => set("contentType", e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2">
          {["Tutorial", "Guide", "Opinion", "Reference", "News", "CaseStudy", "Architecture", "CodeExample", "DeepDive"].map((d) => <option key={d}>{d}</option>)}
        </select>
        <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isFeatured} onChange={(e) => set("isFeatured", e.target.checked)} /> Featured</label>
        <label className="block text-sm" htmlFor="seo-title">SEO title</label>
        <input id="seo-title" value={form.metaTitle} onChange={(e) => set("metaTitle", e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        <label className="block text-sm" htmlFor="seo-desc">Meta description</label>
        <textarea id="seo-desc" value={form.metaDescription} onChange={(e) => set("metaDescription", e.target.value)} className="h-24 w-full rounded-lg border border-rule bg-paper px-3 py-2" />
        <label className="block text-sm" htmlFor="canonical">Canonical URL</label>
        <input id="canonical" value={form.canonicalUrl} onChange={(e) => set("canonicalUrl", e.target.value)} className="w-full rounded-lg border border-rule bg-paper px-3 py-2" />
      </aside>
    </div>
  );
}
