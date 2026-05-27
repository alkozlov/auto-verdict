import { cn } from "@/lib/utils";
import type { CarCheckResponse } from "@/lib/api";

type Status = CarCheckResponse["status"];

const CLASS_MAP: Record<Status, string> = {
  Pending: "bg-warn-tint text-warn",
  Processing: "bg-info-tint text-info",
  Completed: "bg-ok-tint text-ok",
  Failed: "bg-bad-tint text-bad",
};

export function StatusBadge({ status }: { status: Status }) {
  return (
    <span
      className={cn(
        "inline-flex shrink-0 items-center rounded-sm px-2.5 py-0.5 text-xs font-medium",
        CLASS_MAP[status]
      )}
    >
      {status}
    </span>
  );
}
