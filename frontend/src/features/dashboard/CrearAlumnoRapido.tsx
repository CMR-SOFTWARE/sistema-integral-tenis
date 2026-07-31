import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import AccesoCreadoModal from '../alumnos/AccesoCreadoModal';
import type { AlumnoCreado, Categoria } from '../alumnos/types';
import { api, ApiError } from '../../lib/api';
import s from './CrearAlumnoRapido.module.css';

interface Cred { nombre: string; usuario: string | null; passwordTemporal: string | null; }

/**
 * Alta express desde el inicio: nombre + apellido + celular, sin salir del dashboard.
 * El alumno nace en la lista de espera (es alumno cuando se le asigna una clase).
 * Si el celular ya es de una cuenta, se suma a esa familia; el back lo resuelve.
 */
export default function CrearAlumnoRapido() {
  const qc = useQueryClient();
  const [nombre, setNombre] = useState('');
  const [apellido, setApellido] = useState('');
  const [telefono, setTelefono] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [cred, setCred] = useState<Cred | null>(null);

  const valido = nombre.trim() !== '' && apellido.trim() !== '' && telefono.trim() !== '';

  const avisar = (msg: string) => { setToast(msg); setTimeout(() => setToast(null), 4500); };

  const agregar = async () => {
    if (!valido || enviando) return;
    setError(null);
    setEnviando(true);
    try {
      const creado = await api.post<AlumnoCreado>('/alumnos', {
        nombre: nombre.trim(),
        apellido: apellido.trim(),
        telefono: telefono.trim(),
        esMenor: false,
        categoria: 'SinCategoria' as Categoria,
        consentimientoWhatsapp: false,
        consentimientoDatos: false,
      });
      // Refrescamos lo que depende de la lista (alumnos, cuotas y las métricas del inicio).
      await Promise.all([
        qc.invalidateQueries({ queryKey: ['alumnos'] }),
        qc.invalidateQueries({ queryKey: ['cuotas'] }),
        qc.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
      setNombre(''); setApellido(''); setTelefono('');
      if (creado.sumadoAFamilia) {
        avisar(`${creado.alumno.nombre} se sumó a la cuenta de ${creado.familiaTitular ?? 'la familia'}.`);
      } else if (creado.usuario && creado.passwordTemporal) {
        setCred({
          nombre: `${creado.alumno.nombre} ${creado.alumno.apellido}`,
          usuario: creado.usuario,
          passwordTemporal: creado.passwordTemporal,
        });
      } else {
        avisar(`${creado.alumno.nombre} quedó en la lista de espera.`);
      }
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear el alumno.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div className={s.card}>
      <div className={s.head}>
        <h3 className={s.titulo}>Cargar un alumno rápido</h3>
        <span className={s.hint}>Queda en la lista de espera hasta que le asignes una clase.</span>
      </div>
      {error && <div className={s.error}>{error}</div>}
      <div className={s.form}>
        <input value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Nombre" />
        <input value={apellido} onChange={(e) => setApellido(e.target.value)} placeholder="Apellido" />
        <input
          value={telefono}
          onChange={(e) => setTelefono(e.target.value)}
          inputMode="tel"
          placeholder="Celular"
          onKeyDown={(e) => { if (e.key === 'Enter') void agregar(); }}
        />
        <button className={s.btn} disabled={!valido || enviando} onClick={() => void agregar()}>
          {enviando ? 'Agregando…' : '+ Agregar'}
        </button>
      </div>
      {toast && <div className={s.toast}>{toast}</div>}

      {cred && (
        <AccesoCreadoModal
          nombre={cred.nombre}
          usuario={cred.usuario}
          passwordTemporal={cred.passwordTemporal}
          vinculado={false}
          titular={null}
          onClose={() => setCred(null)}
        />
      )}
    </div>
  );
}
