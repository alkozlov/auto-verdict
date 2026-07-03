// Access token lives in memory only; the session is carried by the HttpOnly
// av_refresh cookie (Path=/api/auth), which JS can never read.
let accessToken: string | null = null;
let refreshPromise: Promise<boolean> | null = null;

// One-time cleanup of the legacy localStorage token.
if (typeof window !== "undefined") {
  localStorage.removeItem("av_token");
}

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

/**
 * Exchange the refresh cookie for a new access token.
 * Single-flight: concurrent callers share one in-flight request.
 * Resolves true on success, false when the session is gone.
 */
export function refreshAccessToken(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = (async () => {
      try {
        const res = await fetch("/api/auth/refresh", { method: "POST" });
        if (!res.ok) {
          accessToken = null;
          return false;
        }
        const data = (await res.json()) as { accessToken: string };
        accessToken = data.accessToken;
        return true;
      } catch {
        accessToken = null;
        return false;
      } finally {
        refreshPromise = null;
      }
    })();
  }
  return refreshPromise;
}

export async function logout(): Promise<void> {
  try {
    await fetch("/api/auth/logout", { method: "POST" });
  } catch {
    // Best effort — clear local state regardless.
  }
  accessToken = null;
}
