export function ProcessingBar() {
  return (
    <div className="flex items-center gap-4 rounded-xl border border-info/20 bg-info-tint px-5 py-4">
      {/* Circular spinner */}
      <div className="shrink-0 h-5 w-5 animate-spin rounded-full border-2 border-info/25 border-t-info" />
      <div className="min-w-0">
        <p className="text-sm font-semibold text-hi">Analysis in progress</p>
        <p className="text-xs text-dim mt-0.5">
          AutoVerdict is analyzing the car. This usually takes 15–30 seconds.
        </p>
      </div>
    </div>
  );
}
