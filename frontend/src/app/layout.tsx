import type { Metadata } from "next";
import { IBM_Plex_Mono, IBM_Plex_Sans, Source_Serif_4 } from "next/font/google";
import { ThemeProvider } from "next-themes";
import { SiteShell } from "@/components/site/Chrome";
import { fetchEnvelope } from "@/lib/api";
import { readEnv } from "@/lib/env";
import type { SiteSettings } from "@/lib/types";
import "./globals.css";

export const dynamic = "force-dynamic";
export const revalidate = 0;

const sans = IBM_Plex_Sans({ subsets: ["latin"], weight: ["400", "500", "600"], variable: "--font-sans" });
const serif = Source_Serif_4({ subsets: ["latin"], weight: ["500", "600", "700"], variable: "--font-serif" });
const mono = IBM_Plex_Mono({ subsets: ["latin"], weight: ["400", "500"], variable: "--font-mono" });

export async function generateMetadata(): Promise<Metadata> {
  const settings = await fetchEnvelope<SiteSettings>("/api/v1/public/settings", { revalidate: 0 }).catch(() => null);
  const name = settings?.data?.siteName ?? "Bitnock";
  const description = settings?.data?.siteDescription ?? "Bitnock is Muhammad Babar’s .NET engineering publication. C#, ASP.NET Core, Azure, tutorials and interview preparations.";
  const siteUrl = readEnv("NEXT_PUBLIC_SITE_URL", "http://localhost:3000");
  const metadataBase = new URL(siteUrl.startsWith("http") ? siteUrl : `https://${siteUrl}`);
  return {
    title: { default: name, template: `%s · ${name}` },
    description,
    metadataBase,
    icons: { icon: "/logo-mark.png", apple: "/logo.png" },
    openGraph: { type: "website", siteName: name, description, images: [{ url: "/brand-lockup.png", width: 1280, height: 360, alt: `${name} · Muhammad Babar` }] },
    twitter: { card: "summary_large_image", images: ["/brand-lockup.png"] },
    robots: { index: true, follow: true },
  };
}

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const settings = await fetchEnvelope<SiteSettings>("/api/v1/public/settings", { revalidate: 0 }).catch(() => null);
  const name = settings?.data?.siteName ?? "Bitnock";
  return (
    <html lang="en" suppressHydrationWarning className={`${sans.variable} ${serif.variable} ${mono.variable}`}>
      <body className="min-h-screen bg-paper font-sans text-ink antialiased">
        <ThemeProvider attribute="class" defaultTheme="grey" enableSystem={false} themes={["grey", "paper", "midnight", "ocean"]}>
          <SiteShell siteName={name} youtubeUrl={settings?.data?.social?.youTube}>{children}</SiteShell>
        </ThemeProvider>
      </body>
    </html>
  );
}
