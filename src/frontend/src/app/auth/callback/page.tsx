"use client";

import { Suspense, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { setToken } from "@/lib/auth";

function CallbackInner() {
  const router = useRouter();
  const params = useSearchParams();

  useEffect(() => {
    const token = params.get("token");
    if (token) {
      setToken(token);
      router.replace("/garage/check");
    } else {
      router.replace("/?error=auth_failed");
    }
  }, [params, router]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-page">
      <p className="text-sm text-dim">Signing you in…</p>
    </div>
  );
}

export default function AuthCallback() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center bg-page">
          <p className="text-sm text-dim">Signing you in…</p>
        </div>
      }
    >
      <CallbackInner />
    </Suspense>
  );
}
