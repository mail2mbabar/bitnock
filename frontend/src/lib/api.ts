import type { AuthResponse, Envelope } from "./types";
import { readEnv } from "./env";

function resolveApiBase() {
  let base = readEnv("API_URL") || readEnv("NEXT_PUBLIC_API_URL") || "http://localhost:5080";
  if (!/^https?:\/\//i.test(base)) {
    base = base.includes(".") ? `https://${base}` : `https://${base}.onrender.com`;
  }
  return base.replace(/\/$/, "");
}

export const serverApi = resolveApiBase();
export const publicApi = typeof window === "undefined" ? serverApi : (readEnv("NEXT_PUBLIC_API_URL") || "");

const failed = <T,>(code: string, message: string): Envelope<T> => ({
  success: false,
  data: null,
  error: { code, message },
  requestId: "",
});

async function readEnvelope<T>(response: Response): Promise<Envelope<T>> {
  const text = await response.text();
  if (!text.trim()) {
    if (response.status === 401) {
      return failed("UNAUTHORIZED", "Please sign in again.");
    }
    return failed("EMPTY_RESPONSE", `The API returned an empty ${response.status} response.`);
  }
  try {
    return JSON.parse(text) as Envelope<T>;
  } catch {
    return failed("BAD_RESPONSE", "The API returned a non-JSON response.");
  }
}

let refreshInFlight: Promise<boolean> | null = null;

async function refreshAccessToken(): Promise<boolean> {
  if (typeof window === "undefined") return false;
  const refreshToken = sessionStorage.getItem("nexus.refreshToken");
  if (!refreshToken) return false;
  if (refreshInFlight) return refreshInFlight;

  refreshInFlight = (async () => {
    const response = await fetch(`${publicApi}/api/v1/auth/refresh`, {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
    const result = await readEnvelope<AuthResponse>(response);
    if (!result.success || !result.data) return false;
    sessionStorage.setItem("nexus.accessToken", result.data.accessToken);
    sessionStorage.setItem("nexus.refreshToken", result.data.refreshToken);
    return true;
  })().finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}

function signOutToLogin() {
  if (typeof window === "undefined") return;
  sessionStorage.removeItem("nexus.accessToken");
  sessionStorage.removeItem("nexus.refreshToken");
  if (!window.location.pathname.startsWith("/login")) {
    window.location.replace("/login");
  }
}

export async function fetchEnvelope<T>(path: string, init?: RequestInit & { revalidate?: number }): Promise<Envelope<T>> {
  const { revalidate: _, ...rest } = init ?? {};
  try {
    const response = await fetch(`${serverApi}${path}`, {
      ...rest,
      cache: "no-store",
      signal: rest.signal ?? AbortSignal.timeout(25_000),
      headers: { Accept: "application/json", ...(rest.headers ?? {}) },
    });
    return await readEnvelope<T>(response);
  } catch {
    return failed("API_UNAVAILABLE", "The publishing API is unavailable.");
  }
}

export async function fetchClient<T>(path: string, init?: RequestInit, didRefresh = false): Promise<Envelope<T>> {
  const token = typeof window !== "undefined" ? sessionStorage.getItem("nexus.accessToken") : null;
  const isForm = init?.body instanceof FormData;
  try {
    const response = await fetch(`${publicApi}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        ...(isForm || !init?.body ? {} : { "Content-Type": "application/json" }),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(init?.headers ?? {}),
      },
    });

    if (response.status === 401 && !didRefresh && !path.includes("/api/v1/auth/")) {
      const refreshed = await refreshAccessToken();
      if (refreshed) {
        return fetchClient<T>(path, init, true);
      }
      signOutToLogin();
      return failed("UNAUTHORIZED", "Please sign in again.");
    }

    return await readEnvelope<T>(response);
  } catch {
    return failed("API_UNAVAILABLE", "The publishing API is unavailable.");
  }
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return "";
  return new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric", year: "numeric" }).format(new Date(value));
}
