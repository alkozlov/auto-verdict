import { getToken } from "./auth";
import { i18n } from "@/i18n";

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

export interface CreditPackage {
  key: string;
  credits: number;
  label: string;
  price: number | null;
  currency: string | null;
}

export const api = {
  me: () => request<MeResponse>("/me"),

  checks: {
    list: (page = 1, pageSize = 20) =>
      request<CarCheckResponse[]>(`/checks?page=${page}&pageSize=${pageSize}`),
    get: (id: string) => request<CarCheckResponse>(`/checks/${id}`),
    downloadPdf: async (id: string, filename: string): Promise<void> => {
      const token = getToken();
      const headers: Record<string, string> = {};
      if (token) headers["Authorization"] = `Bearer ${token}`;
      const res = await fetch(`/api/checks/${id}/pdf`, { headers });
      if (!res.ok) throw new Error(`${res.status}`);
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    },
    create: async (params: {
      description: string;
      link?: string;
      images?: File[];
    }): Promise<CarCheckResponse> => {
      const token = getToken();
      const form = new FormData();
      form.append("description", params.description);
      form.append("reportLocale", i18n.language);
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

  payments: {
    getPackages: () => request<CreditPackage[]>("/payments/packages"),
    createCheckout: (packageKey: string) =>
      request<{ checkoutUrl: string }>("/payments/checkout", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ package: packageKey }),
      }),
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
