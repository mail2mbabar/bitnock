"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { BrandLockup } from "@/components/site/BrandLockup";
import { ThemeSwitcher } from "@/components/site/ThemeSwitcher";

const nav = [
  { href: "/admin", label: "Overview" },
  { href: "/admin/articles", label: "Articles" },
  { href: "/admin/articles/new", label: "Create" },
  { href: "/admin/calendar", label: "Calendar" },
  { href: "/admin/categories", label: "Categories" },
  { href: "/admin/tags", label: "Tags" },
  { href: "/admin/authors", label: "Authors" },
  { href: "/admin/media", label: "Media" },
  { href: "/admin/analytics", label: "Analytics" },
  { href: "/admin/agents", label: "Agents" },
  { href: "/admin/audit-logs", label: "Audit logs" },
  { href: "/admin/settings", label: "Settings" },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const path = usePathname();
  const router = useRouter();
  useEffect(() => {
    if (!sessionStorage.getItem("nexus.accessToken")) {
      router.replace("/login");
    }
  }, [router]);

  return (
    <div className="min-h-screen bg-paper">
      <div className="grid md:grid-cols-[14rem_1fr]">
        <aside className="border-r border-rule p-4">
          <div className="flex items-center justify-between gap-2">
            <BrandLockup compact />
            <ThemeSwitcher compact />
          </div>
          <nav className="mt-6 grid gap-1 text-sm">
            {nav.map((item) => (
              <Link key={item.href} href={item.href} id={`nav-${item.label.toLowerCase().replace(/\s/g, "-")}`} className={`rounded-lg px-3 py-2 ${path === item.href ? "bg-paper-2" : "text-muted hover:bg-paper-2"}`}>
                {item.label}
              </Link>
            ))}
          </nav>
          <button className="mt-8 text-sm text-muted" onClick={() => { sessionStorage.clear(); router.push("/login"); }}>Log out</button>
        </aside>
        <div className="p-6 md:p-10">{children}</div>
      </div>
    </div>
  );
}
