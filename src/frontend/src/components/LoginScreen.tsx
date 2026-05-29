import { useTranslation } from "react-i18next";

export function LoginScreen() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen items-center justify-center bg-page px-4">
      <div className="w-full max-w-sm space-y-7 text-center">
        <div className="space-y-3">
          <h1 className="text-3xl font-semibold tracking-tight text-hi">{t("app.name")}</h1>
          <p className="text-xl font-medium text-hi leading-snug">
            {t("home.loginTitle")}
          </p>
          <p className="text-sm text-mid leading-relaxed">
            {t("home.loginBody")}
          </p>
        </div>
        <a
          href="/api/auth/google"
          className="inline-flex h-11 w-full items-center justify-center rounded-md bg-brand px-6 text-sm font-semibold text-page transition-colors hover:bg-brand-hi focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-active"
        >
          {t("auth.continueWithGoogle")}
        </a>
        <p className="text-xs text-dim leading-relaxed">
          {t("home.loginDisclaimer")}
        </p>
      </div>
    </div>
  );
}
