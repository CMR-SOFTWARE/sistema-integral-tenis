import { Link } from 'react-router-dom';
import { obtenerSesion } from '../auth/sesion';
import { formatoPlata } from '../alumnos/types';
import { ESTADO_LIQ_UI, MESES } from '../cuotas/types';
import { fechaCorta, horaCorta, DIAS } from '../agenda/types';
import { useMisTurnos, useMiCuota, useNoticias, useNotas } from './hooks';
import type { MiTurno } from './types';
import EscenaRally from '../../components/tenis/EscenaRally';
import FranjaTenis from '../../components/tenis/FranjaTenis';
import PelotaTrayectoria from '../../components/tenis/PelotaTrayectoria';
import Seccion from '../../components/tenis/Seccion';
import { TennisIcon } from '../../components/iconos';
import s from './PortalPages.module.css';

/** "2026-07-14" → "MAR" (para la columna de horarios asignados). */
function diaCorto(iso: string): string {
  const d = new Date(`${iso}T00:00:00`);
  const idx = (d.getDay() + 6) % 7; // lunes = 0
  return DIAS[idx].corto.toUpperCase();
}

/**
 * Inicio del portal (mockup): hero con la próxima clase, estado de la cuota,
 * horarios asignados y noticias del club.
 */
export default function InicioPage() {
  const hoy = new Date();
  const ficha = obtenerSesion()?.alumno ?? null;
  const conClub = ficha != null;
  const turnosQuery = useMisTurnos();
  const cuotaQuery = useMiCuota(hoy.getFullYear(), hoy.getMonth() + 1);
  const { data: noticias = [] } = useNoticias();
  const { data: notas = [] } = useNotas();
  // Las importantes suben arriba de todo; las demás quedan en la tarjeta de abajo (y
  // completas en la sección Noticias).
  const destacadas = noticias.filter((n) => n.importante);
  const comunes = noticias.filter((n) => !n.importante);

  if (turnosQuery.error) {
    return <div className={s.error}>{turnosQuery.error.message || 'Error cargando tus clases'}</div>;
  }
  // Sin club las consultas ni se disparan (`enabled: tieneFicha()`), así que nunca hay
  // data: se espera solo cuando de verdad hay algo cargando.
  if (conClub && !turnosQuery.data) return <div className={s.vacio}>Cargando…</div>;

  const proximos = turnosQuery.data?.proximos ?? [];
  // La cuota es secundaria en el inicio: si aún no llegó (o falló), mostramos
  // "sin movimientos" en vez de bloquear la pantalla.
  const cuota = cuotaQuery.data ?? null;
  const proxima: MiTurno | undefined = proximos.find((t) => t.estado !== 'Cancelado');
  const estadoCuota = cuota ? ESTADO_LIQ_UI[cuota.estado] : null;

  return (
    <div className={s.inicioGrilla}>
      {/* ── Estado de la cuenta: una BANDA, nunca una pantalla que tape el Inicio ──
          Ni el que no tiene club ni el que está en la espera son un caso aparte: son
          usuarios como cualquier otro y ven el portal completo, con sus estados vacíos.
          Cada banda dice en qué situación está y ofrece la única salida que tiene. */}
      {!conClub && (
        <div className={s.bandaEspera}>
          <div>
            <div className={s.bandaTitulo}>
              <TennisIcon size={16} className={s.bandaIcono} />
              Todavía no estás en ningún club
            </div>
            <div className={s.bandaTexto}>
              Cuando te unas a una academia vas a ver acá tus clases y tu cuota. Mientras
              tanto podés usar el resto del portal.
            </div>
          </div>
          <Link to="/portal/club" className={s.bandaBoton}>Buscar mi club</Link>
        </div>
      )}

      {conClub && ficha?.enEspera && (
        <div className={s.bandaEspera}>
          <div>
            <div className={s.bandaTitulo}>
              <TennisIcon size={16} className={s.bandaIcono} />
              Estás en la lista de espera
            </div>
            <div className={s.bandaTexto}>
              Ya sos parte de <b>{ficha.club}</b>. Tu profe te va a sumar a una clase, o
              podés pedir lugar en la que te sirva.
            </div>
          </div>
          <Link to="/portal/reservar" className={s.bandaBoton}>Ver clases</Link>
        </div>
      )}

      {/* ── Noticias importantes: lo primero que ve al entrar, y en rojo ── */}
      {destacadas.length > 0 && (
        <div className={s.destacadas}>
          {destacadas.map((n) => (
            <div key={n.id} className={s.destacada}>
              <span className={s.destacadaEtiqueta}>
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round">
                  <path d="M12 8v5" /><path d="M12 17h.01" />
                </svg>
                IMPORTANTE
              </span>
              <div className={s.destacadaTitulo}>{n.titulo}</div>
              <div className={s.destacadaMensaje}>{n.mensaje}</div>
            </div>
          ))}
        </div>
      )}

      {/* ── Hero: tu próxima clase ── */}
      <div className={s.hero}>
        <div className={s.heroEyebrow}>Tu próxima clase</div>
        {proxima ? (
          <>
            <div className={s.heroFecha}>{fechaCorta(proxima.fecha)}</div>
            <div className={s.heroDetalle}>
              {horaCorta(proxima.horaInicio)} hs · {proxima.titulo}
            </div>
            {proxima.companeros.length > 0 && (
              <div className={s.heroCompaneros}>
                Con {proxima.companeros.map((c) => c.split(' ')[0]).join(', ')}
              </div>
            )}
          </>
        ) : (
          <div className={s.heroDetalle}>No tenés clases programadas por ahora.</div>
        )}
        <PelotaTrayectoria variante="rally" className={s.heroLinea} />
      </div>

      {/* ── Estado de la cuota ── */}
      <Seccion className={s.tarjeta}>
        <FranjaTenis modo="globo" />
        <h3 className={s.tarjetaTitulo}>Estado de tu cuota</h3>
        <div className={s.cuotaMonto}>{formatoPlata(cuota ? cuota.saldo : 0)}</div>
        {estadoCuota ? (
          <span className={s.chip} style={{ background: estadoCuota.bg, color: estadoCuota.fg }}>
            {cuota!.estado === 'Pagada' ? 'Pagado' : cuota!.estado}
          </span>
        ) : (
          <span className={`${s.chip} ${s.chipVerde}`}>Sin movimientos</span>
        )}
        <div className={s.cuotaVence}>Vence el 10 de {MESES[hoy.getMonth()].toLowerCase()}</div>
        <Link to="/portal/cuota" className={s.btnPrimario}>Ver mi cuota</Link>
      </Seccion>

      {/* ── Tus horarios asignados ── */}
      <Seccion className={s.tarjeta}>
        <FranjaTenis modo="lateral" />
        <h3 className={s.tarjetaTitulo}>Tus horarios asignados</h3>
        {proximos.length === 0 && <div className={s.vacio}>Sin clases próximas.</div>}
        <div className={s.horariosLista}>
          {proximos.slice(0, 4).map((t) => (
            <div key={t.id} className={s.horarioFila}>
              <div className={s.horarioDia}>
                <div className={s.horarioDiaNombre}>{diaCorto(t.fecha)}</div>
                <div className={s.horarioHora}>{horaCorta(t.horaInicio)}</div>
              </div>
              <div className={s.horarioInfo}>
                <div className={s.horarioTitulo}>{t.titulo}</div>
                <div className={s.horarioDetalle}>
                  {fechaCorta(t.fecha)}
                  {t.companeros.length > 0 && <> · {t.companeros.map((c) => c.split(' ')[0]).join(', ')}</>}
                </div>
              </div>
              {t.estado === 'Cancelado'
                ? <span className={`${s.chip} ${s.chipRojo}`}>Cancelada</span>
                : <span className={`${s.chip} ${s.chipVerde}`}>Confirmada</span>}
            </div>
          ))}
        </div>
      </Seccion>

      {/* ── Noticias del club: las últimas, con el link a la sección ──
          Las importantes ya subieron arriba de todo, así que acá no se repiten. */}
      <Seccion className={s.tarjeta}>
        <FranjaTenis modo="red" />
        <div className={s.tarjetaHeader}>
          <h3 className={s.tarjetaTitulo}>Noticias del club</h3>
          <Link to="/portal/noticias" className={s.tarjetaLink}>Ver todas →</Link>
        </div>
        {comunes.length === 0 ? (
          <div className={s.vacio}>No hay noticias por ahora.</div>
        ) : (
          <div className={s.avisosLista}>
            {comunes.slice(0, 3).map((n) => (
              <div key={n.id} className={s.avisoItem}>
                <div className={s.avisoTitulo}>{n.titulo}</div>
                <div className={s.avisoMensaje}>{n.mensaje}</div>
              </div>
            ))}
          </div>
        )}
      </Seccion>

      {/* ── Notas de tu profe (seguimiento compartido) ── */}
      {notas.length > 0 && (
        <Seccion className={s.tarjeta}>
          <h3 className={s.tarjetaTitulo}>Notas de tu profe</h3>
          <div className={s.avisosLista}>
            {notas.map((n) => (
              <div key={n.id} className={s.notaItem}>
                <div className={s.avisoMensaje}>{n.texto}</div>
                <div className={s.notaFecha}>
                  {new Date(n.creadoEl).toLocaleDateString('es-AR', { day: 'numeric', month: 'long' })}
                </div>
              </div>
            ))}
          </div>
        </Seccion>
      )}

      <EscenaRally />
    </div>
  );
}
