import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import { useHorarios } from '../agenda/hooks';
import { DIAS, horaCorta, horaFin } from '../agenda/types';
import type { Horario } from '../agenda/types';
import { CAT_COLOR, CAT_LABEL } from './types';
import type { Alumno } from './types';
import s from './AsignarHorarioModal.module.css';

interface Props {
  alumno: Alumno;
  onClose: () => void;
  /** "Crear una clase nueva": lo resuelve la ficha con el modal de horario de siempre. */
  onCrearNueva: () => void;
  /** Se sumó a una clase: la ficha refresca lo suyo. */
  onAsignado: () => void;
}

/**
 * Sumar al alumno a una clase QUE YA EXISTE, desde su ficha. Antes el único camino
 * era crear una clase nueva; para meterlo en una existente había que salir a la
 * Agenda, buscarla en la grilla y abrir su roster: el mismo gesto, cuatro pantallas
 * más lejos.
 *
 * Se ofrecen solo las clases CON LUGAR y DEL CLUB del alumno. Las llenas no se
 * muestran: no se puede hacer nada con ellas y ensucian la lista. La categoría se
 * muestra como dato pero no filtra — la restricción por categoría es para el alumno
 * que se anota solo desde el portal, no para el profe armando su agenda.
 */
export default function AsignarHorarioModal({ alumno, onClose, onCrearNueva, onAsignado }: Props) {
  const { horarios, cargando, agregarAlumnos } = useHorarios();
  const [sumando, setSumando] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // El club se compara por NOMBRE: es único por tenant (índice único en el DbContext),
  // así que alcanza y evita tener que resolver la sede desde la cancha.
  const sinClub = alumno.sedeNombre === null;
  const disponibles = horarios
    .filter((h) => h.activo)
    .filter((h) => sinClub || h.sede === alumno.sedeNombre)
    .filter((h) => h.cupoMaximo === null || h.miembrosActivos < h.cupoMaximo)
    .filter((h) => !h.miembros.some((m) => m.alumnoId === alumno.id))
    .sort((a, b) => {
      const dia = ordenDia(a) - ordenDia(b);
      return dia !== 0 ? dia : a.horaInicio.localeCompare(b.horaInicio);
    });

  const sumar = async (h: Horario) => {
    setError(null);
    setSumando(h.id);
    try {
      await agregarAlumnos(h.id, [alumno.id]);
      onAsignado();
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo sumar a la clase.');
      setSumando(null);
    }
  };

  return (
    <Modal
      titulo={`Asignar horario a ${alumno.nombre}`}
      subtitulo={sinClub ? 'Sin club asignado' : (alumno.sedeNombre ?? undefined)}
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnCrear} onClick={onCrearNueva}>+ Crear una clase nueva</button>
        </>
      }
    >
      {error && <div className={s.error}>{error}</div>}

      {sinClub && (
        <div className={s.aviso}>
          {alumno.nombre} no tiene club asignado, así que se muestran las clases de todos.
          Podés cargarle uno editando su ficha.
        </div>
      )}

      {cargando && <div className={s.vacio}>Cargando clases…</div>}

      {!cargando && disponibles.length === 0 && (
        <div className={s.vacio}>
          No hay clases con lugar{sinClub ? '' : ` en ${alumno.sedeNombre}`}. Podés crear una
          nueva con el botón de abajo.
        </div>
      )}

      <div className={s.lista}>
        {disponibles.map((h) => {
          const cat = h.categoria && h.categoria !== 'SinCategoria' ? h.categoria : null;
          return (
            <div key={h.id} className={s.fila}>
              <div className={s.cuando}>
                <span className={s.dia}>{DIAS.find((d) => d.valor === h.dia)?.corto}</span>
                <span className={s.hora}>{horaCorta(h.horaInicio)}</span>
              </div>
              <div className={s.datos}>
                <div className={s.titulo}>
                  {h.titulo}
                  {cat && (
                    <span className={s.chip} style={{ background: `${CAT_COLOR[cat]}1a`, color: CAT_COLOR[cat] }}>
                      {CAT_LABEL[cat]}
                    </span>
                  )}
                </div>
                <div className={s.detalle}>
                  {horaCorta(h.horaInicio)}–{horaFin(h.horaInicio, h.duracionMinutos)} · {h.cancha}
                  {' · '}
                  {h.miembrosActivos}{h.cupoMaximo !== null ? `/${h.cupoMaximo}` : ''}
                </div>
              </div>
              <button
                className={s.btnSumar}
                disabled={sumando !== null}
                onClick={() => void sumar(h)}
              >
                {sumando === h.id ? 'Sumando…' : 'Sumar'}
              </button>
            </div>
          );
        })}
      </div>
    </Modal>
  );
}

/** Lunes primero, como el resto de la agenda. */
function ordenDia(h: Horario): number {
  return DIAS.findIndex((d) => d.valor === h.dia);
}
