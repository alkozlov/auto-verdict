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
  vehicleIdentifier: string;
  status: "Pending" | "Processing" | "Completed" | "Failed";
  report: VehicleReport | null;
  failureReason: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface VehicleReport {
  vehicleIdentifier: string;
  verdict: string;
  ownership: { ownersCount: number; commercialUseDetected: boolean; notes: string | null };
  mileage: { inconsistencyDetected: boolean; lastRecordedKm: number | null; notes: string | null };
  accidents: { totalCount: number; severeDamageDetected: boolean; notes: string | null };
  service: { regularMaintenanceConfirmed: boolean; lastServiceDate: string | null; notes: string | null };
  legal: { pledgeDetected: boolean; stolenDetected: boolean; wantedDetected: boolean; notes: string | null };
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
    create: (vehicleIdentifier: string, documentStorageKey: string) =>
      request<CarCheckResponse>("/checks", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ vehicleIdentifier, documentStorageKey }),
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
