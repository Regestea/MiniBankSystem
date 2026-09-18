/**
 * Centralized temporary scenic-image configuration — DARK OCEAN theme.
 *
 * Every ocean / coastal image in the UI MUST reference `imageUrls.*`.
 * Do NOT hardcode remote URLs inside JSX/TSX components.
 *
 * These are TEMPORARY development images (Unsplash CDN, ocean/sea/lighthouse
 * mood matching the reference). Replace any URL here later with the real asset —
 * the whole app updates because components read from this single object.
 *
 * Each visual area falls back to a CSS ocean gradient when the remote
 * image is unavailable, so the UI stays deep-blue even offline.
 */

export const imageUrls = {
  /** Auth screens backdrop (login / register side art). */
  loginBackground:
    "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1200&q=60",
  /** Dashboard hero / balance-card scenic layer. */
  dashboardBackground:
    "https://images.unsplash.com/photo-1505118380757-91f5f5632de0?auto=format&fit=crop&w=1200&q=60",
  /** Transfer flow decorative header. */
  transferBackground:
    "https://images.unsplash.com/photo-1439405326854-014607f694d7?auto=format&fit=crop&w=1200&q=60",
  /** Top-up screen decorative header. */
  topupBackground:
    "https://images.unsplash.com/photo-1502680390469-be75c86b636f?auto=format&fit=crop&w=1200&q=60",
  /** Profile hero backdrop. */
  profileBackground:
    "https://images.unsplash.com/photo-1518837695005-2083093ee35b?auto=format&fit=crop&w=1200&q=60",
  /** Transfer-success celebration backdrop / badge layer. */
  successBackground:
    "https://images.unsplash.com/photo-1500375592092-40eb2168fd21?auto=format&fit=crop&w=1200&q=60",
  /** Generic ocean card backdrop (recipients, side panels). */
  oceanCard:
    "https://images.unsplash.com/photo-1559827260-dc66d52bef19?auto=format&fit=crop&w=1200&q=60",
} as const;

export type ImageUrlKey = keyof typeof imageUrls;
