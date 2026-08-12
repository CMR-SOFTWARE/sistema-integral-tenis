import type { ReactNode } from 'react';
import PelotaTrayectoria from '../../components/tenis/PelotaTrayectoria';
import RaquetaLineArt from '../../components/tenis/RaquetaLineArt';
import { BotonTema } from '../../theme/Tema';
import s from './LoginPage.module.css';

/**
 * Shell compartido de las pantallas de entrada (login, registro, checkout):
 * panel izquierdo de marca + caja de contenido a la derecha. Reusa el CSS
 * de LoginPage para que todas se vean como un solo producto.
 */
export default function AuthShell({ children }: { children: ReactNode }) {
  return (
    <div className={s.pantalla}>
      {/* ── Panel izquierdo: marca ── */}
      <div className={s.panelMarca}>
        <div className={s.marca}>
          <div className={s.marcaLogo}>CMR</div>
          <div className={s.marcaNombre}>CMR Tennis</div>
        </div>

        <div className={s.heroTexto}>
          <div className={s.eyebrow}>Ecosistema de tenis</div>
          <h1 className={s.titulo}>Tu cancha,<br />bajo control.</h1>
          <p className={s.bajada}>
            Alumnos, turnos, clases por categoría, cuotas y disponibilidad.
            Todo en un solo lugar, rápido y claro.
          </p>
          <PelotaTrayectoria className={s.trayectoria} variante="saque" />
          <RaquetaLineArt oscilar className={s.raquetaMarca} />
        </div>

        <div className={s.stats}>
          <div><div className={s.statNum}>12</div><div className={s.statLabel}>Alumnos</div></div>
          <div className={s.statSep} />
          <div><div className={s.statNum}>7</div><div className={s.statLabel}>Categorías</div></div>
          <div className={s.statSep} />
          <div><div className={s.statNum}>$228k</div><div className={s.statLabel}>Este mes</div></div>
        </div>
      </div>

      {/* ── Panel derecho: el contenido de cada pantalla ── */}
      <div className={s.panelLogin}>
        <div className={s.temaLogin}><BotonTema /></div>
        <div className={s.caja}>{children}</div>
      </div>
    </div>
  );
}
