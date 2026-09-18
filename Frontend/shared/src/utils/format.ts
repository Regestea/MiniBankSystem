import { CURRENCY_SYMBOL } from "../constants";

/** Format a USD amount, e.g. 12845.32 → "$12,845.32". */
export function formatCurrency(amount: number): string {
  const sign = amount < 0 ? "-" : "";
  const abs = Math.abs(amount);
  const formatted = abs.toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
  return `${sign}${CURRENCY_SYMBOL}${formatted}`;
}

/** Format a signed transaction amount, e.g. -250 → "-$250.00". */
export function formatSignedAmount(amount: number): string {
  const sign = amount < 0 ? "-" : "+";
  const abs = Math.abs(amount);
  const formatted = abs.toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
  return `${sign}${CURRENCY_SYMBOL}${formatted}`;
}

export function formatDateTime(date: Date = new Date()): { date: string; time: string } {
  const yyyy = date.getFullYear();
  const mm = String(date.getMonth() + 1).padStart(2, "0");
  const dd = String(date.getDate()).padStart(2, "0");
  const hh = String(date.getHours()).padStart(2, "0");
  const min = String(date.getMinutes()).padStart(2, "0");
  return { date: `${yyyy}-${mm}-${dd}`, time: `${hh}:${min}` };
}

/** "2025-08-24" → "Aug 24, 2025". Falls back to the raw string. */
export function formatDisplayDate(isoDate: string): string {
  const d = new Date(`${isoDate}T00:00:00`);
  if (Number.isNaN(d.getTime())) return isoDate;
  return d.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

/** "Sarah Miller" → "S***h M***r" style masked name (mirrors preview-by-number API). */
export function maskName(fullName: string): string {
  return fullName
    .split(" ")
    .map((part) => {
      if (part.length <= 2) return `${part[0] ?? ""}***`;
      return `${part[0]}***${part[part.length - 1]}`;
    })
    .join(" ");
}

export function getInitials(fullName: string): string {
  return fullName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? "")
    .join("");
}

export function getGreeting(hour: number = new Date().getHours()): string {
  if (hour < 12) return "Good morning";
  if (hour < 18) return "Good afternoon";
  return "Good evening";
}
