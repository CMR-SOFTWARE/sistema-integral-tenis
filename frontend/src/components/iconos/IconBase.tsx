import type { ReactNode } from 'react';

export interface IconProps {
  size?: number;
  className?: string;
  /** Si se pasa, el SVG es decorativo+etiquetado; si no, aria-hidden. */
  title?: string;
}

/** Stroke único del set deportivo: mismo peso, mismos caps, viewBox 24. */
export default function IconBase({
  size = 20,
  className,
  title,
  children,
}: IconProps & { children: ReactNode }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden={title ? undefined : true}
      role={title ? 'img' : undefined}
    >
      {title && <title>{title}</title>}
      {children}
    </svg>
  );
}
