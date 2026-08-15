import { useEffect, useState } from 'react';
import { api } from '../../lib/api';
import Avatar from '../../components/Avatar';
import s from './PlataformaPage.module.css';

interface RolPersona {
  tipo: string;
  club: string;
}

interface Persona {
  id: string;
  nombre: string;
  apellido: string;
  email: string | null;
  telefono: string | null;
  esAdminPlataforma: boolean;
  roles: RolPersona[];
}

const CHIP_POR_TIPO: Record<string, string> = {
  Dueño: 'chipVerde',
  Staff: 'chipAmarillo',
  Alumno: 'chipRojo',
};

/**
 * El padrón de PERSONAS de la plataforma (Bloque 6, pedido 11): una proyección de
 * AspNetUsers con sus roles, distinta de "Alumnos" (esa es un padrón de FICHAS, deja
 * afuera al director/staff sin ficha y duplica a quien tiene fichas en varios clubes).
 */
export default function PersonasPage() {
  const [personas, setPersonas] = useState<Persona[]>([]);
  const [cargando, setCargando] = useState(true);
  const [busqueda, setBusqueda] = useState('');

  useEffect(() => {
    api.get<Persona[]>('/admin/personas')
      .then(setPersonas)
      .finally(() => setCargando(false));
  }, []);

  const termino = busqueda.trim().toLowerCase();
  const visibles = personas.filter((p) =>
    termino === ''
    || `${p.nombre} ${p.apellido}`.toLowerCase().includes(termino)
    || (p.telefono ?? '').toLowerCase().includes(termino)
    || (p.email ?? '').toLowerCase().includes(termino),
  );

  if (cargando) return <div className={s.vacio}>Cargando…</div>;

  return (
    <div>
      <div className={s.toolbarPersonas}>
        <input
          className={s.buscador}
          type="search"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          placeholder="Buscar por nombre, celular o email…"
        />
        <div className={s.contador}>{visibles.length} personas</div>
      </div>

      <div className={s.tarjeta}>
        <div className={s.tablaWrap}>
          <table className={s.tabla}>
            <thead>
              <tr>
                <th>Persona</th>
                <th>Roles</th>
                <th>Contacto</th>
              </tr>
            </thead>
            <tbody>
              {visibles.map((p) => (
                <tr key={p.id}>
                  <td>
                    <div className={s.celdaPersona}>
                      <Avatar nombre={p.nombre} apellido={p.apellido} size={36} radius={10} />
                      <div className={s.nombre}>{p.nombre} {p.apellido}</div>
                    </div>
                  </td>
                  <td>
                    <div className={s.roles}>
                      {p.esAdminPlataforma && <span className={`${s.chip} ${s.chipAdmin}`}>Admin plataforma</span>}
                      {p.roles.map((r, i) => (
                        <span key={i} className={`${s.chip} ${s[CHIP_POR_TIPO[r.tipo] ?? 'chipAmarillo']}`}>
                          {r.tipo} de {r.club}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td>
                    <div className={s.sub}>{p.telefono ?? '—'}</div>
                    <div className={s.sub}>{p.email ?? '—'}</div>
                  </td>
                </tr>
              ))}
              {visibles.length === 0 && (
                <tr><td colSpan={3} className={s.vacio}>No hay personas todavía.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
