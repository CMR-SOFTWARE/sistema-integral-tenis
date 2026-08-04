import { useState } from 'react';
import Avatar from '../../components/Avatar';
import PerfilVista, { PerfilVistaCargando } from './PerfilVista';
import type { ProfesorTarjeta } from './types';
import { usePerfilPublico, useProfesoresDelClub } from './usePerfilProfesor';
import s from './ProfesoresClub.module.css';

interface Props {
  tenantId: string;
  /** Encabezado de la lista ("Los profes de Academia Río Cuarto"). */
  titulo: string;
}

/**
 * Los profes de un club y, al tocar uno, su carta de presentación. Se usa en
 * "Mi club": tanto para el alumno ya vinculado como para el que está mirando un
 * club antes de mandar la solicitud.
 */
export default function ProfesoresClub({ tenantId, titulo }: Props) {
  const [viendo, setViendo] = useState<ProfesorTarjeta | null>(null);
  const { data: profesores, isLoading } = useProfesoresDelClub(tenantId);

  if (viendo) {
    return <PerfilDeProfe tenantId={tenantId} profe={viendo} onVolver={() => setViendo(null)} />;
  }

  if (isLoading) {
    return (
      <div className={s.bloque}>
        <h3 className={s.titulo}>{titulo}</h3>
        <div className={s.esqueleto} />
        <div className={s.esqueleto} />
      </div>
    );
  }

  if (!profesores || profesores.length === 0) {
    return (
      <div className={s.bloque}>
        <h3 className={s.titulo}>{titulo}</h3>
        <p className={s.vacio}>Este club todavía no tiene profes cargados.</p>
      </div>
    );
  }

  return (
    <div className={s.bloque}>
      <h3 className={s.titulo}>{titulo}</h3>
      {profesores.map((p) => {
        const contenido = (
          <>
            <Avatar nombre={p.nombre} apellido={p.apellido} fotoUrl={p.avatarUrl} size={52} radius={999} />
            <div className={s.datos}>
              <div className={s.nombre}>
                {p.nombre} {p.apellido}
                {p.esDueño && <span className={s.insignia}>Director</span>}
              </div>
              {p.titular && <div className={s.titular}>{p.titular}</div>}
              {!p.tienePerfil && <div className={s.sinPerfil}>Todavía no armó su perfil</div>}
              {p.especialidades.length > 0 && (
                <div className={s.chips}>
                  {p.especialidades.map((e) => <span key={e} className={s.chip}>{e}</span>)}
                </div>
              )}
            </div>
            {p.tienePerfil && (
              <svg className={s.flecha} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M9 18l6-6-6-6" />
              </svg>
            )}
          </>
        );

        // Sin perfil publicado no hay nada que abrir: la tarjeta queda informativa
        return p.tienePerfil ? (
          <button key={p.userId} className={`${s.tarjeta} ${s.clickeable}`} onClick={() => setViendo(p)}>
            {contenido}
          </button>
        ) : (
          <div key={p.userId} className={s.tarjeta}>{contenido}</div>
        );
      })}
    </div>
  );
}

function PerfilDeProfe({ tenantId, profe, onVolver }: {
  tenantId: string;
  profe: ProfesorTarjeta;
  onVolver: () => void;
}) {
  const { data: perfil, isLoading, isError } = usePerfilPublico(tenantId, profe.userId);

  const volver = (
    <button className={s.volver} onClick={onVolver}>
      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M15 18l-6-6 6-6" />
      </svg>
      Volver
    </button>
  );

  if (isLoading) return <div className={s.bloque}>{volver}<PerfilVistaCargando /></div>;

  // Se despublicó mientras el alumno miraba la lista: no se rompe la pantalla
  if (isError || !perfil) {
    return (
      <div className={s.bloque}>
        {volver}
        <p className={s.vacio}>Este perfil ya no está disponible.</p>
      </div>
    );
  }

  return (
    <div className={s.bloque}>
      {volver}
      <PerfilVista perfil={perfil} />
    </div>
  );
}
