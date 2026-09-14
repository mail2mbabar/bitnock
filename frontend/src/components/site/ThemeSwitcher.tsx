"use client";

import { useEffect, useRef, useState } from "react";
import { Palette } from "lucide-react";
import { useTheme } from "next-themes";

export const SITE_THEMES = [
  { id: "grey", label: "Grey", hint: "Light grey", swatch: "#f4f5f7", ring: "#3b5bdb" },
  { id: "paper", label: "Paper", hint: "Warm cream", swatch: "#f6f1e8", ring: "#8a3b12" },
  { id: "midnight", label: "Midnight", hint: "Ink dark", swatch: "#12141a", ring: "#8fbfb4" },
  { id: "ocean", label: "Ocean", hint: "Deep navy", swatch: "#0c1620", ring: "#5eb1ff" },
] as const;

function canonicalTheme(theme: string | undefined) {
  if (theme === "dark") return "midnight";
  if (theme === "light" || theme === "system" || !theme) return "grey";
  return theme;
}

export function ThemeSwitcher({ compact = false }: { compact?: boolean }) {
  const { theme, setTheme } = useTheme();
  const [open, setOpen] = useState(false);
  const [mounted, setMounted] = useState(false);
  const root = useRef<HTMLDivElement>(null);

  useEffect(() => setMounted(true), []);
  useEffect(() => {
    if (!mounted || !theme) return;
    const next = canonicalTheme(theme);
    if (next !== theme) setTheme(next);
  }, [mounted, theme, setTheme]);

  useEffect(() => {
    const onPointer = (event: MouseEvent) => {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    };
    window.addEventListener("mousedown", onPointer);
    return () => window.removeEventListener("mousedown", onPointer);
  }, []);

  const active = canonicalTheme(theme);

  return (
    <div ref={root} className="relative">
      <button
        type="button"
        className="rounded-full p-2 text-muted hover:bg-paper-2 hover:text-ink"
        aria-label="Choose color theme"
        aria-expanded={open}
        onClick={() => setOpen((v) => !v)}
      >
        <Palette size={18} />
      </button>
      {open && mounted ? (
        <div className="absolute right-0 z-50 mt-2 w-56 rounded-xl border border-rule bg-paper p-2 shadow-lg">
          <p className="px-2 pb-2 pt-1 text-xs uppercase tracking-[0.16em] text-muted">Theme</p>
          {SITE_THEMES.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => {
                setTheme(item.id);
                setOpen(false);
              }}
              className={`flex w-full items-center gap-3 rounded-lg px-2 py-2 text-left text-sm hover:bg-paper-2 ${active === item.id ? "bg-paper-2" : ""}`}
            >
              <span
                className="h-6 w-6 rounded-full border border-rule"
                style={{ background: item.swatch, boxShadow: active === item.id ? `0 0 0 2px ${item.ring}` : undefined }}
              />
              <span>
                <span className="block font-medium">{item.label}</span>
                {!compact ? <span className="block text-xs text-muted">{item.hint}</span> : null}
              </span>
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}
