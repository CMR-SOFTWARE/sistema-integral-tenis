import type { ReactNode } from 'react';

/** Fade + translate corto al cambiar de sección. El key lo pone el caller (pathname). */
export default function PageTransition({ children }: { children: ReactNode }) {
  return <div className="motion-page">{children}</div>;
}
