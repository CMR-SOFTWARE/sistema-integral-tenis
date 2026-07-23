import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import AccesoCreadoModal from '../alumnos/AccesoCreadoModal';
import s from './ProfesoresPage.module.css';

/** Espejo de StaffDto. */
interface Staff {
  id: string;
  userId: string;
  nombre: string;
  apellido: string;
  email: string;
  activo: boolean;
}

/** Espejo de StaffCreadoDto. */
interface StaffCreado {
  staff: Staff;
  usuario: string | null;
  passwordTemporal: string | null;
}

const FORM_VACIO = { nombre: '', apellido: '', email: '', telefono: '' };

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
              <button className={s.btnMini} onClick={() => void cambiarActivo(p)}>
                {p.activo ? 'Sacar' : 'Reactivar'}
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
