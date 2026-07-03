import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { refreshAccessToken } from "@/lib/auth";

export default function AuthCallback() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  useEffect(() => {
    refreshAccessToken().then((ok) => {
      navigate(ok ? "/garage/check" : "/?error=auth_failed", { replace: true });
    });
  }, [navigate]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-page">
      <p className="text-sm text-dim">{t("auth.signingIn")}</p>
    </div>
  );
}
