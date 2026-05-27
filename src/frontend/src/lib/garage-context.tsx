"use client";

import { createContext, useContext } from "react";
import type { MeResponse } from "@/lib/api";

interface GarageContextValue {
  me: MeResponse | null;
  refreshMe: () => Promise<void>;
}

export const GarageContext = createContext<GarageContextValue>({
  me: null,
  refreshMe: async () => {},
});

export function useGarage() {
  return useContext(GarageContext);
}
