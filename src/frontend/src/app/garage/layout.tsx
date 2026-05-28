"use client";

import { useCallback, useEffect, useState } from "react";
import { useNavigate, Outlet } from "react-router-dom";
import { getToken } from "@/lib/auth";
import { api, type MeResponse } from "@/lib/api";
import { GarageContext } from "@/lib/garage-context";
import { Sidebar } from "@/components/Sidebar";
import { MobileNav } from "@/components/MobileNav";

export default function GarageLayout() {
  const navigate = useNavigate();
  const [me, setMe] = useState<MeResponse | null>(null);
  const [ready, setReady] = useState(false);

  const refreshMe = useCallback(async () => {
    try {
      const data = await api.me();
      setMe(data);
    } catch {
      // ignore refresh errors
    }
  }, []);

  useEffect(() => {
    if (!getToken()) {
      navigate("/", { replace: true });
      return;
    }
    api
      .me()
      .then((data) => {
        setMe(data);
        setReady(true);
      })
      .catch(() => {
        navigate("/", { replace: true });
      });
  }, [navigate]);

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-page">
        <p className="text-sm text-dim">Loading…</p>
      </div>
    );
  }

  return (
    <GarageContext.Provider value={{ me, refreshMe }}>
      <div className="flex min-h-screen bg-page">
        <Sidebar me={me} />
        <div className="flex flex-col flex-1 min-w-0">
          <MobileNav me={me} />
          <main className="flex-1 px-4 py-8 sm:px-6 lg:px-8 lg:py-10">
            <Outlet />
          </main>
        </div>
      </div>
    </GarageContext.Provider>
  );
}
