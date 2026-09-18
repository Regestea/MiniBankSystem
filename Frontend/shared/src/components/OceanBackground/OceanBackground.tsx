import type { CSSProperties, ReactNode } from "react";
import React from "react";
import "./OceanBackground.css";

type OceanBackgroundProps = {
  /** Centralized URL, e.g. `imageUrls.dashboardBackground`. */
  imageUrl?: string;
  /** Extra class names (e.g. a module class for radius/padding). */
  className?: string;
  children?: ReactNode;
  /** Accessible label for decorative region; omit for purely decorative art. */
  label?: string;
};

/**
 * Reusable ocean scenic layer with gradient fallback.
 * Remote image is layered OVER the dark-ocean CSS gradient, so a failed
 * image request still leaves a deep-blue surface — never white.
 */
export function OceanBackground({
  imageUrl,
  className = "",
  children,
  label,
}: OceanBackgroundProps): React.JSX.Element {
  const style = imageUrl
    ? ({ "--ocean-image": `url("${imageUrl}")` } as CSSProperties)
    : undefined;

  return (
    <div
      className={`ocean-background ${className}`}
      style={style}
      role={label ? "img" : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
    >
      {children ? <div className="ocean-background__content">{children}</div> : null}
    </div>
  );
}
