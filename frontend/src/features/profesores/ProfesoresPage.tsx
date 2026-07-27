import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import AccesoCreadoModal from '../alumnos/AccesoCreadoModal';
import { formatoPlata } from '../alumnos/types';
import s from './ProfesoresPage.module.css';

/** Espejo de StaffDto. */
interface Staff {
  id: string;
  userId: string;
  nombre: string;
  apellido: string;
  email: string;
  activo: boolean;
  /** Valor hora base (para el sueldo); null = sin definir. */
  valorHora: number | null;
}

/** Espejo de StaffCreadoDto. */
interface StaffCreado {
  staff: Staff;
  usuario: string | null;
  passwordTemporal: string | null;
}

const FORM_VACIO = { nombre: '', apellido: '', email: '', telefono: '', valorHora: '' };

/**
 * Profes empleados (Staff) del club. El dueño suma a un profe por su email (tiene
 * que tener cuenta en la app), lo ve en la lista y lo activa/desactiva. Los profes
 * empleados ven una versión reducida del panel (su agenda y sus alumnos).
 */
export default function ProfesoresPage() {
  const [staff, setStaff] = useState<Staff[]>([]);
  const [cargando, setCargando] = useState(true);
  const [form, setForm] = useState(FORM_VACIO);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [credenciales, setCredenciales] = useState<{ nombre: string; usuario: string; passwordTemporal: string } | null>(null);
  const [editVh, setEditVh] = useState<{ id: string; valor: string } | null>(null);
  const confirmar = useConfirmar();

  const setCampo = (campo: keyof typeof FORM_VACIO, valor: string) =>
    setForm((f) => ({ ...f, [campo]: valor }));

  const cargar = useCallback(() => {
    setCargando(true);
    api.get<Staff[]>('/staff')
      .then(setStaff)
      .catch(() => setStaff([]))
      .finally(() => setCargando(false));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3000);
  };

  const completo = form.nombre.trim() && form.apellido.trim() && form.telefono.trim();

  const agregar = async () => {
    if (!completo) return;
    setGuardando(true);
    setError(null);
    try {
      const creado = await api.post<StaffCreado>('/staff', {
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        telefono: form.telefono.trim(),
        email: form.email.trim() || undefined,
        valorHora: form.valorHora ? Number(form.valorHora) : undefined,
      });
      setForm(FORM_VACIO);
      cargar();
      if (creado.passwordTemporal) {
        // Cuenta nueva: mostramos las credenciales una sola vez
        setCredenciales({
          nombre: `${creado.staff.nombre} ${creado.staff.apellido}`,
          usuario: creado.usuario ?? creado.staff.email,
          passwordTemporal: creado.passwordTemporal,
        });
      } else {
        avisar(`${creado.staff.nombre} volvió a tu equipo.`);
      }
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo agregar el profe.');
    } finally {
      setGuardando(false);
    }
  };

  const cambiarActivo = async (p: Staff) => {
    if (p.activo && !(await confirmar({
      titulo: 'Sacar del equipo',
      mensaje: `¿Sacar a ${p.nombre} ${p.apellido} de tu equipo? Deja de ver la academia; lo podés volver a activar cuando quieras.`,
      confirmar: 'Sacar',
      peligro: true,
    }))) return;
    await api.patch(`/staff/${p.id}/activo`, { activo: !p.activo });
    cargar();
  };

  /** Guarda el valor hora base del profe (vacío = lo borra). */
  const guardarValorHora = async () => {
    if (!editVh) return;
    try {
      await api.patch(`/staff/${editVh.id}/valor-hora`, {
        valorHora: editVh.valor ? Number(editVh.valor) : null,
      });
      setEditVh(null);
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar el valor hora.');
    }
  };

  /** Borrado REAL: saca al profe de verdad (para los que se cargaron mal). */
  const eliminar = async (p: Staff) => {
    if (!(await confirmar({
      titulo: `Eliminar a ${p.nombre} ${p.apellido}`,
      mensaje: (
        <>
          Se borra el profe de tu equipo y su acceso a la app. Los grupos, horarios y
          alumnos que lo tenían asignado quedan <b>sin profe</b> (no se borran).{' '}
          <b>Esto no se puede deshacer.</b>
        </>
      ),
      confirmar: 'Eliminar definitivamente',
      cancelar: 'No, cancelar',
      peligro: true,
    }))) return;
    try {
      await api.delete(`/staff/${p.id}/definitivo`);
      avisar(`${p.nombre} ${p.apellido} eliminado`);
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo eliminar el profe.');
    }
  };

  return (
    <div>
      <div className={s.toolbar}>
        <div className={s.titulo}>
          Sumá a los profes que trabajan con vos. Le creás la cuenta y su usuario y
          contraseña inicial es su celular. Cada uno entra con su cuenta y ve solo su agenda y sus alumnos.
        </div>
      </div>

      <div className={s.altaCard}>
        <div className={s.altaCampos}>
          <input
            className={s.input}
            value={form.nombre}
            onChange={(e) => setCampo('nombre', e.target.value)}
            placeholder="Nombre"
            maxLength={80}
          />
          <input
            className={s.input}
            value={form.apellido}
            onChange={(e) => setCampo('apellido', e.target.value)}
            placeholder="Apellido"
            maxLength={80}
          />
          <input
            className={s.input}
            value={form.telefono}
            onChange={(e) => setCampo('telefono', e.target.value)}
            placeholder="Celular (su usuario y contraseña)"
          />
          <input
            className={s.input}
            type="email"
            value={form.email}
            onChange={(e) => setCampo('email', e.target.value)}
            placeholder="Email (opcional)"
          />
          <input
            className={s.input}
            type="number"
            min={0}
            value={form.valorHora}
            onChange={(e) => setCampo('valorHora', e.target.value)}
            onWheel={(e) => e.currentTarget.blur()}
            placeholder="Valor hora (opcional)"
          />
        </div>
        <button
          className={s.btnPrimario}
          disabled={guardando || !completo}
          onClick={() => void agregar()}
        >
          {guardando ? 'Creando…' : 'Crear profe'}
        </button>
      </div>

      {error && <div className={s.error}>{error}</div>}

      {cargando && <div className={s.vacio}>Cargando…</div>}

      {!cargando && staff.length === 0 && (
        <div className={s.vacioCard}>
          Todavía no sumaste ningún profe. Agregá uno con su celular para que te ayude con
          las clases.
        </div>
      )}

      {!cargando && staff.length > 0 && (
        <div className={s.lista}>
          {staff.map((p) => (
            <div key={p.id} className={p.activo ? s.fila : s.filaInactiva}>
              <div className={s.avatar}>
                {`${p.nombre.charAt(0)}${p.apellido.charAt(0)}`.toUpperCase()}
              </div>
              <div className={s.cuerpo}>
                <div className={s.nombre}>
                  {p.nombre} {p.apellido}
                  {!p.activo && <span className={s.badgeInactivo}>Inactivo</span>}
                </div>
                <div className={s.email}>{p.email}</div>
              </div>
              <div className={s.valorHoraCell}>
                {editVh?.id === p.id ? (
                  <>
                    <input
                      className={s.vhInput}
                      type="number"
                      min={0}
                      autoFocus
                      value={editVh.valor}
                      onChange={(e) => setEditVh({ id: p.id, valor: e.target.value })}
                      onWheel={(e) => e.currentTarget.blur()}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') void guardarValorHora();
                        if (e.key === 'Escape') setEditVh(null);
                      }}
                      placeholder="Valor hora"
                    />
                    <button className={s.btnMini} onClick={() => void guardarValorHora()}>Guardar</button>
                  </>
                ) : (
                  <button
                    className={`${s.vhChip} ${p.valorHora == null ? s.vhChipVacio : ''}`}
                    title="Valor hora base (para calcular el sueldo)"
                    onClick={() => setEditVh({ id: p.id, valor: p.valorHora?.toString() ?? '' })}
                  >
                    {p.valorHora != null ? `${formatoPlata(p.valorHora)}/h` : '+ valor hora'}
                  </button>
                )}
              </div>
              <button className={s.btnMini} onClick={() => void cambiarActivo(p)}>
                {p.activo ? 'Sacar' : 'Reactivar'}
              </button>
              <button className={s.btnMiniBorrar} onClick={() => void eliminar(p)}>
                Eliminar
              </button>
            </div>
          ))}
        </div>
      )}

      {toast && <div className={s.toast}>{toast}</div>}

      {credenciales && (
        <AccesoCreadoModal
          nombre={credenciales.nombre}
          usuario={credenciales.usuario}
          passwordTemporal={credenciales.passwordTemporal}
          onClose={() => setCredenciales(null)}
        />
      )}
    </div>
  );
}
