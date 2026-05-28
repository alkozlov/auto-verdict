"use client";

import { useEffect, useState } from "react";
import { X } from "lucide-react";
import { api, type CreditPackage } from "@/lib/api";
import { cn } from "@/lib/utils";

interface Props {
  onClose: () => void;
}

export function PurchaseCreditsModal({ onClose }: Props) {
  const [packages, setPackages] = useState<CreditPackage[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [buyingKey, setBuyingKey] = useState<string | null>(null);

  useEffect(() => {
    api.payments.getPackages().then(setPackages).catch(() => {});
  }, []);

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [onClose]);

  async function handleBuy(pkg: CreditPackage) {
    setError(null);
    setBuyingKey(pkg.key);
    setLoading(true);
    try {
      const { checkoutUrl } = await api.payments.createCheckout(pkg.key);
      window.location.href = checkoutUrl;
    } catch {
      setError("Failed to start checkout. Please try again.");
      setLoading(false);
      setBuyingKey(null);
    }
  }

  return (
    <>
      <div
        className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Top up credits"
        className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-2xl border border-white/6 bg-[#0E1116] p-6 shadow-2xl"
      >
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-base font-[650] text-hi">Top up credits</h2>
          <button
            onClick={onClose}
            aria-label="Close"
            className="flex h-7 w-7 items-center justify-center rounded-md text-dim hover:text-hi transition-colors"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <p className="mb-5 text-sm text-dim">
          Each credit lets you run one full car analysis. Credits never expire.
        </p>

        <div className="grid grid-cols-2 gap-3">
          {packages.length === 0
            ? [null, null].map((_, i) => (
                <div
                  key={i}
                  className="h-[140px] rounded-xl border border-white/6 bg-surface animate-pulse"
                />
              ))
            : packages.map((pkg) => {
                const isBest = pkg.credits > 1;
                const isBuying = buyingKey === pkg.key && loading;
                return (
                  <div
                    key={pkg.key}
                    className={cn(
                      "relative flex flex-col rounded-xl border p-4",
                      isBest
                        ? "border-brand/40 bg-brand/5"
                        : "border-white/6 bg-surface"
                    )}
                  >
                    {isBest && (
                      <span className="absolute -top-2.5 left-1/2 -translate-x-1/2 rounded-full bg-brand px-2.5 py-0.5 text-[10px] font-semibold text-page">
                        Best value
                      </span>
                    )}
                    <p className="text-[22px] font-[700] text-hi leading-none">
                      {pkg.credits}
                    </p>
                    <p className="mt-0.5 text-xs text-dim">
                      {pkg.credits === 1 ? "check" : "checks"}
                    </p>
                    <p className="mt-3 text-sm font-semibold text-mid">
                      {pkg.pricePln} PLN
                    </p>
                    <button
                      onClick={() => handleBuy(pkg)}
                      disabled={loading}
                      className={cn(
                        "mt-3 rounded-lg px-3 py-2 text-xs font-semibold transition-all",
                        isBest
                          ? "bg-brand text-page hover:brightness-105"
                          : "border border-white/10 text-hi hover:bg-white/[0.06]",
                        loading && "opacity-50 cursor-not-allowed"
                      )}
                    >
                      {isBuying ? "Opening…" : "Buy"}
                    </button>
                  </div>
                );
              })}
        </div>

        {error && (
          <p className="mt-4 text-xs text-bad">{error}</p>
        )}

        <p className="mt-5 text-[11px] text-dim/60">
          Secure checkout via Lemon Squeezy. Credits are added instantly after payment.
        </p>
      </div>
    </>
  );
}
