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
  listingUrl: string;
  title: string | null;
  make: string | null;
  model: string | null;
  year: number | null;
  mileageKm: number | null;
  price: number | null;
  currency: string | null;
  status: "Pending" | "Processing" | "Completed" | "Failed";
  report: VehicleReport | null;
  failureReason: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface VehicleReport {
  carSummary: string;
  listingFacts: {
    listingUrl: string;
    title: string | null;
    make: string | null;
    model: string | null;
    year: number | null;
    mileageKm: number | null;
    price: number | null;
    currency: string | null;
    sellerType: string | null;
    location: string | null;
    attributes: Record<string, string>;
  };
  modelRisks: string[];
  listingRisks: string[];
  dealRisks: string[];
  estimatedCosts: {
    purchasePrice: number | null;
    registrationFee: number | null;
    insuranceCost: number | null;
    potentialRepairs: number | null;
    total: number | null;
    currency: string;
    notes: string;
  };
  sellerQuestions: string[];
  inspectionChecklist: string[];
  recommendation: string;
  disclaimer: string;
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
    create: (listingUrl: string) =>
      request<CarCheckResponse>("/checks", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ listingUrl }),
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
