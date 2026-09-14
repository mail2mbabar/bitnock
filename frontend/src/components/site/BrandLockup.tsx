import Link from "next/link";

export const AUTHOR_PHOTO = "/authors/muhammad-babar.jpg";
export const BITNOCK_MARK = "/logo-mark.png";

export function BrandLockup({
  siteName = "Bitnock",
  href = "/",
  compact = false,
}: {
  siteName?: string;
  href?: string;
  compact?: boolean;
}) {
  const size = compact ? "h-8 w-8" : "h-10 w-10";

  return (
    <Link href={href} className="flex items-center gap-2.5" aria-label={`${siteName} home`}>
      <span className="relative shrink-0">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={AUTHOR_PHOTO}
          alt="Muhammad Babar"
          className={`${size} rounded-full object-cover object-[50%_12%] ring-1 ring-rule`}
        />
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={BITNOCK_MARK}
          alt=""
          className={`absolute -bottom-0.5 -right-0.5 ${compact ? "h-3.5 w-3.5" : "h-4 w-4"} rounded-[4px] ring-2 ring-paper`}
        />
      </span>
      <span className="leading-tight">
        <span className={`block font-serif tracking-tight ${compact ? "text-lg" : "text-xl"}`}>{siteName}</span>
        {compact ? null : <span className="hidden text-[10px] uppercase tracking-[0.16em] text-muted sm:block">Muhammad Babar</span>}
      </span>
    </Link>
  );
}

export function HomepageBrand({ siteName = "Bitnock" }: { siteName?: string }) {
  return (
    <div className="flex items-center gap-5">
      <span className="relative shrink-0">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={AUTHOR_PHOTO}
          alt="Muhammad Babar"
          className="h-24 w-24 rounded-full object-cover object-[50%_12%] ring-1 ring-rule md:h-28 md:w-28"
        />
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={BITNOCK_MARK}
          alt="Bitnock"
          className="absolute -bottom-1 -right-1 h-9 w-9 rounded-lg ring-4 ring-paper md:h-10 md:w-10"
        />
      </span>
      <div>
        <p className="text-xs uppercase tracking-[0.22em] text-muted">Muhammad Babar · Microsoft MVP</p>
        <h1 className="mt-2 font-serif text-5xl leading-[1.05] tracking-tight md:text-6xl">{siteName}</h1>
      </div>
    </div>
  );
}
