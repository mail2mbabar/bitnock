import Link from "next/link";
import type { Author } from "@/lib/types";

const fallbackPhoto = "/authors/muhammad-babar.jpg";
const mvpBadge = "/mvp/microsoft-mvp-badge.png";

export function AuthorIdentity({
  author,
  compact = false,
}: {
  author?: Author | null;
  compact?: boolean;
}) {
  const name = author?.displayName ?? "Muhammad Babar";
  const slug = author?.slug ?? "muhammad-babar";
  const photo = author?.avatarUrl || fallbackPhoto;
  const bio = author?.bio ?? "Microsoft MVP and Senior Full Stack .NET Developer in Manchester.";

  if (compact) {
    return (
      <Link href={`/author/${slug}`} className="flex items-center gap-3">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={photo} alt={name} className="h-10 w-10 rounded-full object-cover" />
        <span>
          <span className="block text-sm font-medium">{name}</span>
          <span className="block text-xs text-muted">Microsoft MVP · .NET</span>
        </span>
      </Link>
    );
  }

  return (
    <div className="flex flex-col gap-5 sm:flex-row sm:items-start">
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={photo} alt={name} className="h-28 w-28 rounded-2xl object-cover shadow-sm" />
      <div>
        <p className="text-xs uppercase tracking-[0.18em] text-muted">Microsoft MVP · Aug 2026 – Aug 2027</p>
        <h2 className="mt-1 font-serif text-3xl">{name}</h2>
        <p className="mt-3 text-sm text-muted">{bio}</p>
        <div className="mt-4 flex flex-wrap items-center gap-4 text-sm">
          <Link href={`/author/${slug}`} className="underline">Profile</Link>
          {author?.gitHubUrl ? <a href={author.gitHubUrl}>GitHub</a> : <a href="https://github.com/mail2mbabar">GitHub</a>}
          {author?.linkedInUrl ? <a href={author.linkedInUrl}>LinkedIn</a> : <a href="https://www.linkedin.com/in/dev-mbabar">LinkedIn</a>}
          <a href="https://www.youtube.com/@ABiHelpline">YouTube · ABi Helpline</a>
          <a href="https://www.credly.com/badges/7c0a51ad-031c-40b9-9c05-b52e15a2d384">Credly badge</a>
        </div>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={mvpBadge} alt="Microsoft Most Valuable Professional" className="mt-5 h-16 w-auto" />
      </div>
    </div>
  );
}
