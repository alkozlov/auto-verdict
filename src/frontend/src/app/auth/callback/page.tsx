import { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { setToken } from "@/lib/auth";

export default function AuthCallback() {
  const navigate = useNavigate();
  const [params] = useSearchParams();

  useEffect(() => {
    const token = params.get("token");
    if (token) {
      setToken(token);
      navigate("/garage/check", { replace: true });
    } else {
      navigate("/?error=auth_failed", { replace: true });
    }
  }, [params, navigate]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-page">
      <p className="text-sm text-dim">Signing you in…</p>
    </div>
  );
}
