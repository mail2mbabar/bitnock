export function readEnv(name: string, fallback = "") {
  const value = process.env[name];
  return value && value.length > 0 ? value : fallback;
}

export function publicAssetUrl(url?: string | null): string | undefined {
  if (!url) {
    return undefined;
  }

  const media = url.indexOf("/media/");
  if (media >= 0) {
    return url.slice(media);
  }

  return url;
}
