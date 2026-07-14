import { useEffect, useState } from 'react';
import { api } from '../../lib/api';
import { fechaCorta, horaCorta } from '../agenda/types';
import type { Cancelacion } from './types';
import s from './CancelacionesPage.module.css';

/** Cancelaciones recientes (mockup): turnos que canceló el profe (a mano o
 *  por bloqueo) + avisos de alumnos, con WhatsApp para coordinar recuperación. */
export default function CancelacionesPage() {
  const [cancelaciones, setCancelaciones] = useState<Cancelacion[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<Cancelacion[]>('/cancelaciones')
      .then(setCancelaciones)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando cancelaciones'));
  }, []);

  if (error) return <div className={s.error}>{error} — ¿está corriendo la API?</div>;
  if (!cancelaciones) return <div className={s.vacio}>Cargando…</div>;

  const textoWhatsapp = (c: Cancelacion) =>
    encodeURIComponent(
      `Hola! Vi la cancelación de la clase del ${fechaCorta(c.fecha)} a las ${horaCorta(c.horaInicio)}. Coordinamos una recuperación?`,
    );

  return (
    <div>
      <div className={s.intro}>
        Clases canceladas por vos (a mano o por bloqueos) y avisos de tus
        alumnos. El aviso del alumno no mueve la plata: la recuperación queda
        a tu criterio.
      </div>

      {cancelaciones.length === 0 && (
        <div className={s.vacioCard}>No hay cancelaciones recientes. 🎾</div>
      )}

      <div className={s.lista}>
        {cancelaciones.map((c, i) => (
          <div key={i} className={s.fila}>
            <span className={c.por === 'Alumno' ? s.dotAlumno : s.dotProfe} />
            <div className={s.cuerpo}>
              <div className={s.filaTitulo}>
                {c.alumnoNombre ?? c.titulo}
                <span className={c.por === 'Alumno' ? s.chipAlumno : s.chipProfe}>
                  {c.por === 'Alumno' ? 'Canceló el alumno' : 'Cancelaste vos'}
                </span>
              </div>
              <div className={s.detalle}>
                {c.alumnoNombre && `${c.titulo} · `}
                {fechaCorta(c.fecha)} {horaCorta(c.horaInicio)} hs
                {c.motivo && ` · ${c.motivo}`}
              </div>
            </div>
            {c.telefono && (
              <a
                className={s.btnWhatsapp}
                href={`https://wa.me/${c.telefono.replace(/\D/g, '')}?text=${textoWhatsapp(c)}`}
                target="_blank"
                rel="noreferrer"
              >
                WhatsApp
              </a>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
