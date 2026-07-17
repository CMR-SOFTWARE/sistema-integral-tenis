import { useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useSedes } from './hooks';
import { formatoPlata } from '../alumnos/types';
import type { Precios, Servicio } from '../cuotas/types';
import s from './ConfiguracionPage.module.css';

/** Card de precios del profe (la base de la fórmula de cuotas). */
function PreciosCard() {
  const [grupal, setGrupal] = useState('');
  const [individual, setIndividual] = useState('');
  const [guardado, setGuardado] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void api.get<Precios>('/configuracion/precios').then((p) => {
      setGrupal(p.valorHoraGrupal?.toString() ?? '');
      setIndividual(p.valorClaseIndividual?.toString() ?? '');
    });
  }, []);

  const guardar = async () => {
    setError(null);
    setGuardado(false);
    try {
      await api.put<Precios>('/configuracion/precios', {
        valorHoraGrupal: grupal === '' ? null : Number(grupal),
        valorClaseIndividual: individual === '' ? null : Number(individual),
      });
      setGuardado(true);
      setTimeout(() => setGuardado(false), 2500);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudieron guardar los precios.');
    }
  };

  return (
    <div className={s.tarjeta}>
      <h3 className={s.titulo}>Precios</h3>
      <p className={s.bajada}>
        La base de la liquidación: ambos valores son <b>por hora</b> y se prorratean
        por la duración del turno (30' = la mitad). La <b>grupal</b> además se divide
        entre los asignados del turno. Los cargos ya generados no cambian si
        actualizás estos valores.
      </p>
      {error && <div className={s.error}>{error}</div>}
      <div className={s.precios}>
        <label className={s.precio}>
          <span>Valor hora grupal</span>
          <input type="number" min={0} value={grupal} onChange={(e) => setGrupal(e.target.value)} placeholder="16000" />
        </label>
        <label className={s.precio}>
          <span>Valor hora individual</span>
          <input type="number" min={0} value={individual} onChange={(e) => setIndividual(e.target.value)} placeholder="16000" />
        </label>
        <button className={s.btnPrimario} onClick={() => void guardar()}>
          {guardado ? '✓ Guardado' : 'Guardar precios'}
        </button>
      </div>
    </div>
  );
}

interface DatosPagoConfig {
  aliasCbu: string | null;
  titularPago: string | null;
}

/** Card de datos de transferencia: lo que ve el alumno al informar un pago. */
function DatosPagoCard() {
  const [alias, setAlias] = useState('');
  const [titular, setTitular] = useState('');
  const [guardado, setGuardado] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void api.get<DatosPagoConfig>('/configuracion/datos-pago').then((d) => {
      setAlias(d.aliasCbu ?? '');
      setTitular(d.titularPago ?? '');
    });
  }, []);

  const guardar = async () => {
    setError(null);
    setGuardado(false);
    try {
      await api.put<DatosPagoConfig>('/configuracion/datos-pago', {
        aliasCbu: alias.trim() === '' ? null : alias.trim(),
        titularPago: titular.trim() === '' ? null : titular.trim(),
      });
      setGuardado(true);
      setTimeout(() => setGuardado(false), 2500);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudieron guardar los datos.');
    }
  };

  return (
    <div className={s.tarjeta}>
      <h3 className={s.titulo}>Datos de transferencia</h3>
      <p className={s.bajada}>
        A dónde te transfieren tus alumnos. El portal se los muestra cuando avisan que
        pagaron, así vos solo confirmás que te llegó.
      </p>
      {error && <div className={s.error}>{error}</div>}
      <div className={s.precios}>
        <label className={s.precio}>
          <span>Alias o CBU</span>
          <input value={alias} onChange={(e) => setAlias(e.target.value)} placeholder="juan.perez.mp" maxLength={120} />
        </label>
        <label className={s.precio}>
          <span>Titular de la cuenta</span>
          <input value={titular} onChange={(e) => setTitular(e.target.value)} placeholder="Juan Pérez" maxLength={120} />
        </label>
        <button className={s.btnPrimario} onClick={() => void guardar()}>
          {guardado ? '✓ Guardado' : 'Guardar datos'}
        </button>
      </div>
    </div>
  );
}

/** Card del catálogo de servicios (M4): lo que el profe ofrece, con precio. */
function ServiciosCard() {
  const [servicios, setServicios] = useState<Servicio[]>([]);
  const [nombre, setNombre] = useState('');
  const [precio, setPrecio] = useState('');
  const [editId, setEditId] = useState<string | null>(null);
  const [editNombre, setEditNombre] = useState('');
  const [editPrecio, setEditPrecio] = useState('');
  const [error, setError] = useState<string | null>(null);

  const cargar = () => {
    void api.get<Servicio[]>('/configuracion/servicios').then(setServicios).catch(() => {});
  };
  useEffect(cargar, []);

  const agregar = async () => {
    if (nombre.trim() === '' || precio === '') return;
    setError(null);
    try {
      await api.post('/configuracion/servicios', { nombre: nombre.trim(), precio: Number(precio) });
      setNombre(''); setPrecio(''); cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo agregar el servicio.');
    }
  };

  const empezarEdicion = (sv: Servicio) => {
    setEditId(sv.id); setEditNombre(sv.nombre); setEditPrecio(sv.precio.toString());
  };

  const guardarEdicion = async (id: string) => {
    setError(null);
    try {
      await api.put(`/configuracion/servicios/${id}`, { nombre: editNombre.trim(), precio: Number(editPrecio) });
      setEditId(null); cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar.');
    }
  };

  const cambiarActivo = async (sv: Servicio) => {
    setError(null);
    try {
      await api.patch(`/configuracion/servicios/${sv.id}/activo`, { activo: !sv.activo });
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo cambiar el estado.');
    }
  };

  return (
    <div className={s.tarjeta}>
      <h3 className={s.titulo}>Servicios que ofrezco</h3>
      <p className={s.bajada}>
        Lo que vendés además de las clases (encordados, tubos de pelotas, etc.), con su precio.
        Tus alumnos los piden desde el portal y vos confirmás. Cambiar un precio no toca los
        pedidos ya hechos.
      </p>
      {error && <div className={s.error}>{error}</div>}

      <div className={s.altaSede}>
        <input
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          placeholder="Nombre, ej: Encordado"
          maxLength={80}
        />
        <input
          type="number"
          min={0}
          value={precio}
          onChange={(e) => setPrecio(e.target.value)}
          placeholder="Precio"
          style={{ maxWidth: 120 }}
        />
        <button className={s.btnPrimario} onClick={() => void agregar()} disabled={nombre.trim() === '' || precio === ''}>
          + Agregar
        </button>
      </div>

      {servicios.length === 0 && <div className={s.vacio}>Todavía no cargaste servicios.</div>}

      <div className={s.sedes}>
        {servicios.map((sv) => (
          <div key={sv.id} className={sv.activo ? s.sede : s.sedeInactiva}>
            {editId === sv.id ? (
              <div className={s.sedeHeader}>
                <input
                  className={`${s.editInput} ${s.editNombre}`}
                  value={editNombre}
                  onChange={(e) => setEditNombre(e.target.value)}
                  maxLength={80}
                />
                <input
                  className={`${s.editInput} ${s.editPrecio}`}
                  type="number"
                  min={0}
                  value={editPrecio}
                  onChange={(e) => setEditPrecio(e.target.value)}
                />
                <div className={s.spacer} />
                <button className={s.btnMini} onClick={() => void guardarEdicion(sv.id)}>Guardar</button>
                <button className={s.btnMiniGris} onClick={() => setEditId(null)}>✕</button>
              </div>
            ) : (
              <div className={s.sedeHeader}>
                <span className={s.sedeNombre}>{sv.nombre}</span>
                {!sv.activo && <span className={s.chipInactiva}>Inactivo</span>}
                <span className={s.sedeCanchas}>{formatoPlata(sv.precio)}</span>
                <div className={s.spacer} />
                <button className={s.btnMiniGris} onClick={() => empezarEdicion(sv)}>Editar</button>
                {sv.activo ? (
                  <button className={s.btnMiniGris} onClick={() => void cambiarActivo(sv)}>Desactivar</button>
                ) : (
                  <button className={s.btnMini} onClick={() => void cambiarActivo(sv)}>Activar</button>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

/** Configuración del tenant. Por ahora: sedes y canchas (donde trabaja el profe). */
export default function ConfiguracionPage() {
  const { sedes, cargando, crearSede, agregarCancha, desactivarSede, reactivarSede } = useSedes();
  const [nombreSede, setNombreSede] = useState('');
  const [canchaEn, setCanchaEn] = useState<string | null>(null); // sedeId con input abierto
  const [nombreCancha, setNombreCancha] = useState('');
  const [error, setError] = useState<string | null>(null);

  const altaSede = async () => {
    if (nombreSede.trim() === '') return;
    setError(null);
    try {
      await crearSede(nombreSede.trim());
      setNombreSede('');
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear la sede.');
    }
  };

  const bajaSede = async (sedeId: string, nombre: string) => {
    if (!window.confirm(
      `¿Deshabilitar "${nombre}"? Deja de ofrecerse para horarios nuevos; los turnos ya generados se conservan.`,
    )) return;
    setError(null);
    try {
      await desactivarSede(sedeId);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo deshabilitar la sede.');
    }
  };

  const altaDeNuevo = async (sedeId: string) => {
    setError(null);
    try {
      await reactivarSede(sedeId);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo habilitar la sede.');
    }
  };

  const altaCancha = async (sedeId: string) => {
    if (nombreCancha.trim() === '') return;
    setError(null);
    try {
      await agregarCancha(sedeId, nombreCancha.trim());
      setNombreCancha('');
      setCanchaEn(null);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear la cancha.');
    }
  };

  return (
    <div className={s.contenedor}>
      <PreciosCard />
      <DatosPagoCard />
      <ServiciosCard />
      <div className={s.tarjeta}>
        <h3 className={s.titulo}>Sedes y canchas</h3>
        <p className={s.bajada}>
          Los clubes donde trabajás y sus canchas. Los horarios se asignan a una cancha
          y no pueden superponerse dentro de la misma.
        </p>

        {error && <div className={s.error}>{error}</div>}

        <div className={s.altaSede}>
          <input
            value={nombreSede}
            onChange={(e) => setNombreSede(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && void altaSede()}
            placeholder="Nombre de la sede, ej: Club Atlético Norte"
            maxLength={80}
          />
          <button className={s.btnPrimario} onClick={() => void altaSede()} disabled={nombreSede.trim() === ''}>
            + Agregar sede
          </button>
        </div>

        {cargando && <div className={s.vacio}>Cargando…</div>}
        {!cargando && sedes.length === 0 && (
          <div className={s.vacio}>Todavía no hay sedes. Agregá la primera arriba.</div>
        )}

        <div className={s.sedes}>
          {sedes.map((sede) => (
            <div key={sede.id} className={sede.activo ? s.sede : s.sedeInactiva}>
              <div className={s.sedeHeader}>
                <span className={s.sedeNombre}>{sede.nombre}</span>
                {!sede.activo && <span className={s.chipInactiva}>Deshabilitada</span>}
                <span className={s.sedeCanchas}>
                  {sede.canchas.length === 0
                    ? 'sin canchas'
                    : `${sede.canchas.length} cancha${sede.canchas.length > 1 ? 's' : ''}`}
                </span>
                <div className={s.spacer} />
                {sede.activo ? (
                  <button
                    className={s.btnMiniGris}
                    title="Deshabilitar: deja de ofrecerse para horarios nuevos"
                    onClick={() => void bajaSede(sede.id, sede.nombre)}
                  >
                    Deshabilitar
                  </button>
                ) : (
                  <button className={s.btnMini} onClick={() => void altaDeNuevo(sede.id)}>
                    Habilitar
                  </button>
                )}
              </div>
              <div className={s.canchas}>
                {sede.canchas.map((c) => (
                  <span key={c.id} className={s.canchaChip}>{c.nombre}</span>
                ))}
                {canchaEn === sede.id ? (
                  <span className={s.altaCancha}>
                    <input
                      autoFocus
                      value={nombreCancha}
                      onChange={(e) => setNombreCancha(e.target.value)}
                      onKeyDown={(e) => e.key === 'Enter' && void altaCancha(sede.id)}
                      placeholder="Cancha 1"
                      maxLength={40}
                    />
                    <button className={s.btnMini} onClick={() => void altaCancha(sede.id)}>OK</button>
                    <button className={s.btnMiniGris} onClick={() => { setCanchaEn(null); setNombreCancha(''); }}>✕</button>
                  </span>
                ) : (
                  <button className={s.btnCancha} onClick={() => { setCanchaEn(sede.id); setNombreCancha(''); }}>
                    + cancha
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
