import PelotaOutline from '../tenis/PelotaOutline';

/** Carga: skeleton + pelota chica. No es un spinner genérico ni una pelota gigante. */
export default function CargaTenis({ label = 'Cargando…' }: { label?: string }) {
  return (
    <div className="motion-carga">
      <PelotaOutline className="motion-cargaPelota" />
      <div className="motion-skeleton" />
      <span className="vacio">{label}</span>
    </div>
  );
}
