import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { channelVideos, getVideo, youtubeEmbed, youtubeWatch, YOUTUBE_CHANNEL_URL } from "@/lib/videos";

type Props = { params: Promise<{ id: string }> };

export async function generateStaticParams() {
  return channelVideos.map((video) => ({ id: video.id }));
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const video = getVideo(id);
  if (!video) return { title: "Video" };
  return {
    title: video.title,
    description: video.summary,
    openGraph: { type: "video.other", title: video.title, description: video.summary },
  };
}

export default async function VideoWatchPage({ params }: Props) {
  const { id } = await params;
  const video = getVideo(id);
  if (!video) notFound();
  const others = channelVideos.filter((item) => item.id !== video.id).slice(0, 6);

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <nav className="text-sm text-muted">
        <Link href="/">Home</Link> / <Link href="/videos">Videos</Link>
      </nav>
      <div className="mt-6 overflow-hidden rounded-2xl border border-rule bg-ink">
        <div className="video-frame">
          <iframe
            src={youtubeEmbed(video.id)}
            title={video.title}
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
            allowFullScreen
          />
        </div>
      </div>
      <p className="mt-6 text-xs uppercase tracking-[0.18em] text-muted">{video.topic} · {video.duration} · ABi Helpline</p>
      <h1 className="mt-3 font-serif text-3xl md:text-5xl">{video.title}</h1>
      <p className="mt-4 max-w-3xl text-lg text-muted">{video.summary}</p>
      <p className="mt-5 flex flex-wrap gap-4 text-sm">
        <Link className="underline" href={`/articles/${video.articleSlug}`}>Read the detailed guide</Link>
        <a className="underline" href={youtubeWatch(video.id)}>Open on YouTube</a>
        <a className="underline" href={YOUTUBE_CHANNEL_URL}>Subscribe to ABi Helpline</a>
      </p>

      <section className="mt-16">
        <h2 className="font-serif text-2xl">More from the channel</h2>
        <ul className="mt-6 grid gap-4 md:grid-cols-2">
          {others.map((item) => (
            <li key={item.id} className="border-b border-rule pb-4">
              <p className="text-xs text-muted">{item.topic} · {item.duration}</p>
              <Link href={`/videos/${item.id}`} className="mt-1 block font-serif text-xl">{item.title}</Link>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
