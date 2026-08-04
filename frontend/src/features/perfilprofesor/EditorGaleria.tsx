import { useState } from 'react';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { ApiError } from '../../lib/api';
import SubirImagen from './SubirImagen';
import { TOPES, type FotoPerfil } from './types';
import { useEditarPerfil } from './usePerfilProfesor';
import s from './MiPerfil.module.css';

interface Props {
  fotos: FotoPerfil[];
}

/**
 * La galería: fotos con su pie. El pie se guarda al salir del campo (blur), así el
 * profe escribe y sigue sin tener que apretar un botón por cada foto.
 */
export default function EditorGaleria({ fotos }: Props) {
  const { agregarFoto, cambiarPie, eliminarFoto, reordenarFotos } = useEditarPerfil();
  const confirmar = useConfirmar();

  // Lo tipeado antes de guardarse: la clave es el id de la foto
  const [pies, setPies] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  const lleno = fotos.length >= TOPES.fotos;
  const pieDe = (f: FotoPerfil) => pies[f.id] ?? f.pieDeFoto ?? '';

  const guardarPie = async (f: FotoPerfil) => {
    const nuevo = pieDe(f).trim();
    if (nuevo === (f.pieDeFoto ?? '')) return; // no cambió: no molestamos al servidor
    try {
      await cambiarPie(f.id, nuevo || null);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar el pie de foto.');
    }
  };

  const borrar = async (f: FotoPerfil) => {
    const ok = await confirmar({
      titulo: 'Borrar esta foto',
      mensaje: 'La foto se borra de tu perfil y no se puede recuperar.',
      confirmar: 'Borrar',
      peligro: true,
    });
    if (!ok) return;
    try {
      await eliminarFoto(f.id);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo borrar la foto.');
    }
  };

  const mover = async (desde: number, hacia: number) => {
    if (hacia < 0 || hacia >= fotos.length) return;
    const ids = fotos.map((f) => f.id);
    [ids[desde], ids[hacia]] = [ids[hacia], ids[desde]];
    try {
      await reordenarFotos(ids);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo reordenar.');
    }
  };

  return (
    <section className={s.tarjeta}>
      <h3 className={s.tarjetaTitulo}>Galería</h3>
      <p className={s.ayuda}>
        Hasta {TOPES.fotos} fotos. Contá algo en cada pie de foto: eso es lo que la
        hace interesante para el que la mira ({fotos.length}/{TOPES.fotos} cargadas).
      </p>

      {error && <div className={s.error}>{error}</div>}

      <div className={s.grilla}>
        {fotos.map((f, i) => (
          <div key={f.id} className={s.celda}>
            <div className={s.celdaFoto}>
              <img src={f.url} alt={f.pieDeFoto ?? ''} />
            </div>
            <div className={s.celdaCuerpo}>
              <input
                type="text"
                value={pieDe(f)}
                maxLength={TOPES.pieDeFoto}
                placeholder="Contá algo de esta foto…"
                onChange={(e) => setPies({ ...pies, [f.id]: e.target.value })}
                onBlur={() => void guardarPie(f)}
              />
              <div className={s.celdaBotones}>
                <div className={s.filaBotones}>
                  <button className={s.btnIcono} onClick={() => void mover(i, i - 1)} disabled={i === 0} aria-label="Mover antes">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M15 18l-6-6 6-6" />
                    </svg>
                  </button>
                  <button className={s.btnIcono} onClick={() => void mover(i, i + 1)} disabled={i === fotos.length - 1} aria-label="Mover después">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M9 18l6-6-6-6" />
                    </svg>
                  </button>
                </div>
                <button className={s.btnIcono} onClick={() => void borrar(f)} aria-label="Borrar foto">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        ))}

        {!lleno && (
          <SubirImagen
            etiqueta="Sumar foto"
            onError={setError}
            onElegir={(file) => agregarFoto(file, '')}
          >
            {(abrir, subiendo) => (
              <button className={s.sumar} onClick={abrir} disabled={subiendo}>
                {subiendo ? 'Subiendo…' : (
                  <>
                    <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M3 5h18v14H3zM3 16l5-5 4 4 3-3 6 6" />
                      <circle cx="8.5" cy="9.5" r="1.5" />
                    </svg>
                    Sumar foto
                  </>
                )}
              </button>
            )}
          </SubirImagen>
        )}
      </div>
    </section>
  );
}
