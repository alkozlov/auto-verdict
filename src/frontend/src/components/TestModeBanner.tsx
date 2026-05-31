import { TriangleAlert } from "lucide-react";

const testMode = import.meta.env.VITE_TEST_MODE === "true";

export function TestModeBanner() {
  if (!testMode) return null;

  return (
    <div className="w-full bg-amber-400 py-3 px-5 flex items-center justify-center gap-2">
      <TriangleAlert className="h-5 w-5 text-amber-900 shrink-0" />
      <p className="text-amber-900 font-bold text-sm text-center">
        TEST MODE &mdash; AI analysis is simulated. No real AI service will be used.
      </p>
    </div>
  );
}
