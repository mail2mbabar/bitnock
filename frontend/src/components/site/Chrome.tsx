"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { Menu, X } from "lucide-react";
import { BrandLockup } from "@/components/site/BrandLockup";
import { SearchBox } from "@/components/site/SearchBox";
import { ThemeSwitcher } from "@/components/site/ThemeSwitcher";

const links = [
  { href: "/articles", label: "Articles" },
  { href: "/videos", label: "Videos" },
  { href: "/categories/aspnet-core", label: "ASP.NET Core" },
  { href: "/about", label: "About" },
];

export function Header({ siteName }: { siteName: string }) {
  const [open, setOpen] = useState(false);

  return (
    <header className="sticky top-0 z-40 border-b border-rule bg-paper/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl min-w-0 items-center justify-between gap-2 px-4 py-3 sm:gap-4">
        <span className="flex min-w-0 items-center" id="site-brand">
          <BrandLockup siteName={siteName} compact />
        </span>
        <nav className="hidden items-center gap-6 text-sm md:flex" aria-label="Primary">
          {links.map((link) => (
            <Link key={link.href} href={link.href} className="text-muted hover:text-ink">
              {link.label}
            </Link>
          ))}
        </nav>
        <div className="flex items-center gap-2">
          <SearchBox compact />
          <ThemeSwitcher />
          <button type="button" className="rounded-full p-2 md:hidden" aria-expanded={open} aria-label="Open menu" onClick={() => setOpen((v) => !v)}>
            {open ? <X size={18} /> : <Menu size={18} />}
          </button>
        </div>
      </div>
      {open ? (
        <nav className="border-t border-rule px-4 py-3 md:hidden" aria-label="Mobile">
          {links.map((link) => (
            <Link key={link.href} href={link.href} className="block py-2" onClick={() => setOpen(false)}>
              {link.label}
            </Link>
          ))}
          <Link href="/search" className="block py-2" onClick={() => setOpen(false)}>
            Search
          </Link>
        </nav>
      ) : null}
    </header>
  );
}

export function SiteShell({ siteName, youtubeUrl, children }: { siteName: string; youtubeUrl?: string | null; children: React.ReactNode }) {
  const path = usePathname();
  if (path.startsWith("/admin") || path.startsWith("/login")) {
    return <>{children}</>;
  }
  return (
    <>
      <Header siteName={siteName} />
      <main id="main">{children}</main>
      <Footer siteName={siteName} youtubeUrl={youtubeUrl} />
    </>
  );
}

export function Footer({ siteName, youtubeUrl }: { siteName: string; youtubeUrl?: string | null }) {
  const channel = youtubeUrl || "https://www.youtube.com/@ABiHelpline";
  return (
    <footer className="mt-20 border-t border-rule">
      <div className="mx-auto grid max-w-6xl gap-8 px-4 py-12 md:grid-cols-4">
        <div>
          <BrandLockup siteName={siteName} />
          <p className="mt-3 text-sm text-muted">Bitnock is Muhammad Babar’s .NET engineering publication. Tutorials and interview preparations, written for people who ship.</p>
        </div>
        <div>
          <p className="text-sm font-medium">Read</p>
          <ul className="mt-3 space-y-2 text-sm text-muted">
            <li><Link href="/articles">Articles</Link></li>
            <li><Link href="/videos">Videos</Link></li>
            <li><Link href="/search">Search</Link></li>
            <li><Link href="/about">About</Link></li>
            <li><Link href="/author/muhammad-babar">Author</Link></li>
          </ul>
        </div>
        <div>
          <p className="text-sm font-medium">Connect</p>
          <ul className="mt-3 space-y-2 text-sm text-muted">
            <li><a href={channel}>YouTube · ABi Helpline</a></li>
            <li><a href="https://www.linkedin.com/in/dev-mbabar">LinkedIn</a></li>
            <li><a href="https://github.com/mail2mbabar">GitHub</a></li>
            <li><Link href="/contact">Contact</Link></li>
            <li><Link href="/privacy">Privacy</Link></li>
            <li><Link href="/terms">Terms</Link></li>
          </ul>
        </div>
        <div>
          <p className="text-sm font-medium">Subscribe</p>
          <p className="mt-3 text-sm text-muted">One thoughtful article, not a firehose. Tutorials and interview preparations.</p>
          <Link href="/#newsletter" className="mt-3 inline-block text-sm text-accent-2 underline">Join the newsletter</Link>
        </div>
      </div>
      <p className="border-t border-rule px-4 py-4 text-center text-xs text-muted">© {new Date().getFullYear()} {siteName} · Muhammad Babar, Microsoft MVP. All rights reserved.</p>
    </footer>
  );
}
