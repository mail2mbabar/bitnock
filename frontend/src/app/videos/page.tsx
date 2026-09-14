import Link from "next/link";
import { YOUTUBE_CHANNEL_NAME, YOUTUBE_CHANNEL_URL, channelVideos, youtubeThumb } from "@/lib/videos";

export const metadata = {
  title: "Videos",
  description: "Every ABi Helpline video on Bitnock — C#, ASP.NET Core, interviews, and architecture, with written guides.",
};

export default function VideosPage() {
  return (
    <div className="mx-auto max-w-6xl px-4 py-12">
      <p className="text-xs uppercase tracking-[0.22em] text-muted">ABi Helpline</p>
      <h1 className="mt-3 font-serif text-4xl md:text-5xl">Every video, on Bitnock</h1>
      <p className="mt-4 max-w-2xl text-lg text-muted">
        Muhammad Babar’s YouTube channel {YOUTUBE_CHANNEL_NAME} — C#, ASP.NET Core, EF Core, JWT, Clean Architecture, and interview prep.
        Watch here, then read the long-form notes.
      </p>
      <p className="mt-4 text-sm">
        <a className="underline" href={YOUTUBE_CHANNEL_URL}>Subscribe on YouTube</a>
        {" · "}
        {channelVideos.length} videos
      </p>

      <div className="mt-12 grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
        {channelVideos.map((video) => (
          <article key={video.id} className="flex flex-col overflow-hidden rounded-2xl border border-rule bg-paper-2/40">
            <Link href={`/videos/${video.id}`} className="relative block">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={youtubeThumb(video.id)}
                alt={video.title}
                className="aspect-video w-full object-cover"
              />
              <span className="absolute bottom-2 right-2 rounded bg-ink/85 px-2 py-0.5 text-xs text-paper">{video.duration}</span>
            </Link>
            <div className="flex flex-1 flex-col p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-muted">{video.topic}</p>
              <h2 className="mt-2 font-serif text-xl leading-snug">
                <Link href={`/videos/${video.id}`}>{video.title}</Link>
              </h2>
              <p className="mt-2 flex-1 text-sm text-muted">{video.summary}</p>
              <p className="mt-4 text-sm">
                <Link className="underline" href={`/videos/${video.id}`}>Watch</Link>
                {" · "}
                <Link className="underline" href={`/articles/${video.articleSlug}`}>Read the guide</Link>
              </p>
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}
