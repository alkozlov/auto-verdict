import { CheckCircle2, Clock, Loader2, XCircle } from "lucide-react";
import type { CarCheckResponse } from "@/lib/api";

type Status = CarCheckResponse["status"];

const ICON_MAP: Record<Status, { icon: React.ElementType; className: string }> = {
  Pending:    { icon: Clock,         className: "text-warn" },
  Processing: { icon: Loader2,       className: "text-info animate-spin" },
  Completed:  { icon: CheckCircle2,  className: "text-ok" },
  Failed:     { icon: XCircle,       className: "text-bad" },
};

export function StatusBadge({ status }: { status: Status }) {
  const { icon: Icon, className } = ICON_MAP[status];
  return <Icon className={`h-4 w-4 shrink-0 ${className}`} title={status} aria-label={status} />;
}
