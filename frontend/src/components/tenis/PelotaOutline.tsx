/** Pelota line art: outline + costuras. Nunca emoji ni 3D. */
export default function PelotaOutline({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" aria-hidden>
      <circle cx="12" cy="12" r="8" stroke="currentColor" strokeWidth="1.4" />
      <path
        d="M5.6 8c3.6 2.2 3.6 5.8 0 8M18.4 8c-3.6 2.2-3.6 5.8 0 8"
        stroke="currentColor"
        strokeWidth="1.25"
      />
    </svg>
  );
}
