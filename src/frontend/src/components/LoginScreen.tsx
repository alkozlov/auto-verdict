export function LoginScreen() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-page px-4">
      <div className="w-full max-w-sm space-y-7 text-center">
        <div className="space-y-3">
          <h1 className="text-3xl font-semibold tracking-tight text-hi">AutoVerdict</h1>
          <p className="text-xl font-medium text-hi leading-snug">
            Avoid expensive used-car mistakes.
          </p>
          <p className="text-sm text-mid leading-relaxed">
            Paste listing details, seller messages, an Otomoto link, or photos.
            Get a structured AI risk analysis before you contact the seller.
          </p>
        </div>
        <a
          href="/api/auth/google"
          className="inline-flex h-11 w-full items-center justify-center rounded-md bg-brand px-6 text-sm font-semibold text-page transition-colors hover:bg-brand-hi focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-active"
        >
          Continue with Google
        </a>
        <p className="text-xs text-dim leading-relaxed">
          AI-assisted screening only. Always verify documents and arrange a professional
          inspection before buying.
        </p>
      </div>
    </div>
  );
}
