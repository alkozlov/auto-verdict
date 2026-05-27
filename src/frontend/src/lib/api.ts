import { getToken } from "./auth";

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>),
  };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(`/api${path}`, { ...options, headers });
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`${res.status}: ${text || res.statusText}`);
  }
  return res.json();
}

export interface MeResponse {
  id: string;
  email: string;
  displayName: string | null;
  credits: number;
}

export interface CarCheckResponse {
  checkId: string;
  title: string | null;
  listingUrl: string | null;
  status: "Pending" | "Processing" | "Completed" | "Failed";
  report: string | null;
  failureReason: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface FileUploadResponse {
  storageKey: string;
  contentType: string;
  fileSizeBytes: number;
}

export const api = {
  me: () => request<MeResponse>("/me"),

  checks: {
    list: (page = 1, pageSize = 20) =>
      request<CarCheckResponse[]>(`/checks?page=${page}&pageSize=${pageSize}`),
    get: (id: string) => request<CarCheckResponse>(`/checks/${id}`),
    create: async (params: {
      description: string;
      link?: string;
      images?: File[];
    }): Promise<CarCheckResponse> => {
      const token = getToken();
      const form = new FormData();
      form.append("description", params.description);
      if (params.link) form.append("link", params.link);
      params.images?.forEach((img, i) => form.append(`image${i}`, img));
      const headers: Record<string, string> = {};
      if (token) headers["Authorization"] = `Bearer ${token}`;
      const res = await fetch("/api/checks", { method: "POST", headers, body: form });
      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(`${res.status}: ${text || res.statusText}`);
      }
      return res.json();
    },
  },

  uploads: {
    upload: async (file: File): Promise<FileUploadResponse> => {
      const token = getToken();
      const form = new FormData();
      form.append("file", file);
      const headers: Record<string, string> = {};
      if (token) headers["Authorization"] = `Bearer ${token}`;
      const res = await fetch("/api/uploads", {
        method: "POST",
        headers,
        body: form,
      });
      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(`${res.status}: ${text || res.statusText}`);
      }
      return res.json();
    },
  },
};
