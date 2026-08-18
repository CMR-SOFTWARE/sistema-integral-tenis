import { useEffect, useRef, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { comprimirABlob } from '../portal/comprimirImagen';
import { formatoPlata } from '../alumnos/types';
import type { Servicio } from '../cuotas/types';
import s from './ProductosPage.module.css';

/** Igual que el tope del backend: se avisa acá para no hacer viajar una subida que va a rebotar. */
const MAXIMO_FOTOS = 5;

/**
 * El catálogo del profe: lo que vende además de las clases. Antes era una tarjeta
 * adentro de Configuración con nombre y precio nada más; ahora tiene descripción y
 * fotos, que es lo que hace falta para ofrecer una raqueta o merch (con un encordado
 * alcanzaba el nombre).
 *
 * Las fotos van al storage y en la base queda su URL: por eso solo se pueden subir a
 * un producto YA creado (antes no hay id al que colgarlas).
 */
export default function ProductosPage() {
  const [servicios, setServicios] = useState<Servicio[]>([]);
  const [cargando, setCargando] = useState(true);
  const [nombre, setNombre] = useState('');
  const [precio, setPrecio] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [editId, setEditId] = useState<string | null>(null);
  const [editNombre, setEditNombre] = useState('');
  const [editPrecio, setEditPrecio] = useState('');
  const [editDescripcion, setEditDescripcion] = useState('');
  const [subiendoEn, setSubiendoEn] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const confirmar = useConfirmar();

  // Un solo input de archivo para toda la lista; este ref dice a qué producto va la
  // foto que se elija. Va en ref y no en estado: lo lee el onChange, no el render.
  const destino = useRef<string | null>(null);
  const fileInput = useRef<HTMLInputElement>(null);

  const cargar = () => {
    void api.get<Servicio[]>('/configuracion/servicios')
      .then(setServicios)
      .catch(() => {})
      .finally(() => setCargando(false));
  };
  useEffect(cargar, []);

  const agregar = async () => {
    if (nombre.trim() === '' || precio === '') return;
    setError(null);
    try {
      await api.post('/configuracion/servicios', {
        nombre: nombre.trim(),
        descripcion: descripcion.trim() || null,
        precio: Number(precio),
      });
      setNombre(''); setPrecio(''); setDescripcion('');
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo agregar el producto.');
    }
  };

  const empezarEdicion = (sv: Servicio) => {
    setEditId(sv.id);
    setEditNombre(sv.nombre);
    setEditPrecio(sv.precio.toString());
    setEditDescripcion(sv.descripcion ?? '');
  };

  const guardarEdicion = async (id: string) => {
    setError(null);
    try {
      await api.put(`/configuracion/servicios/${id}`, {
        nombre: editNombre.trim(),
        descripcion: editDescripcion.trim() || null,
        precio: Number(editPrecio),
      });
      setEditId(null);
      cargar();
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

  const pedirFoto = (servicioId: string) => {
    destino.current = servicioId;
    fileInput.current?.click();
  };

  const subirFoto = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = ''; // permite volver a elegir el mismo archivo
    const servicioId = destino.current;
    destino.current = null;
    if (!file || !servicioId) return;

    setError(null);
    setSubiendoEn(servicioId);
    try {
      // Se comprime ACÁ: del celular sale una foto de 8 MB y al storage tiene que
      // llegar una de ~200 KB, o el catálogo se arrastra en la pantalla del alumno.
      const blob = await comprimirABlob(file, 1200);
      const form = new FormData();
      form.append('archivo', blob, 'foto.jpg');
      await api.postForm(`/configuracion/servicios/${servicioId}/fotos`, form);
      cargar();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo subir la foto.');
    } finally {
      setSubiendoEn(null);
    }
  };

  const borrarFoto = async (sv: Servicio, fotoId: string) => {
    if (!(await confirmar({
      titulo: 'Borrar la foto',
      mensaje: `Se borra la imagen de "${sv.nombre}". No se puede deshacer.`,
      confirmar: 'Borrar',
      peligro: true,
    }))) return;
    setError(null);
    try {
      await api.delete(`/configuracion/servicios/${sv.id}/fotos/${fotoId}`);
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo borrar la foto.');
    }
  };

  return (
    <div className={s.contenedor}>
      <div className={s.tarjeta}>
        <h3 className={s.titulo}>Productos que vendo</h3>
        <p className={s.bajada}>
          Lo que ofrecés además de las clases: encordados, tubos de pelotas, raquetas, remeras.
          Tus alumnos los piden desde su <b>Shop</b> y vos confirmás. Cambiar un precio no toca
          los pedidos ya hechos. Cargalo primero y después sumale las fotos.
        </p>
        {error && <div className={s.error}>{error}</div>}

        <div className={s.alta}>
          <input
            value={nombre}
            onChange={(e) => setNombre(e.target.value)}
            placeholder="Nombre, ej: Raqueta Wilson Pro"
            maxLength={80}
          />
          <input
            className={s.altaPrecio}
            type="number"
            min={0}
            value={precio}
            onChange={(e) => setPrecio(e.target.value)}
            placeholder="Precio"
          />
          <button
            className={s.btnPrimario}
            onClick={() => void agregar()}
            disabled={nombre.trim() === '' || precio === ''}
          >
            + Agregar
          </button>
        </div>
        <textarea
          className={s.altaDescripcion}
          value={descripcion}
          onChange={(e) => setDescripcion(e.target.value)}
          placeholder="Descripción (opcional): de qué se trata, medidas, colores…"
          maxLength={500}
          rows={2}
        />
      </div>

      {/* Un solo input para toda la lista (ver el ref `destino`) */}
      <input ref={fileInput} type="file" accept="image/*" hidden onChange={(e) => void subirFoto(e)} />

      {cargando && <div className={s.vacio}>Cargando…</div>}
      {!cargando && servicios.length === 0 && (
        <div className={s.vacio}>Todavía no cargaste productos. Agregá el primero arriba.</div>
      )}

      <div className={s.lista}>
        {servicios.map((sv) => (
          <div key={sv.id} className={sv.activo ? s.producto : s.productoInactivo}>
            {editId === sv.id ? (
              <div className={s.edicion}>
                <input
                  className={s.editInput}
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
                <textarea
                  className={s.editInput}
                  value={editDescripcion}
                  onChange={(e) => setEditDescripcion(e.target.value)}
                  placeholder="Descripción (opcional)"
                  maxLength={500}
                  rows={3}
                />
                <div className={s.acciones}>
                  <button className={s.btnMiniGris} onClick={() => setEditId(null)}>Cancelar</button>
                  <button className={s.btnMini} onClick={() => void guardarEdicion(sv.id)}>Guardar</button>
                </div>
              </div>
            ) : (
              <>
                <div className={s.cabecera}>
                  <span className={s.nombre}>{sv.nombre}</span>
                  {!sv.activo && <span className={s.chipInactivo}>Inactivo</span>}
                  <span className={s.precio}>{formatoPlata(sv.precio)}</span>
                </div>

                {sv.descripcion && <p className={s.descripcion}>{sv.descripcion}</p>}

                <div className={s.galeria}>
                  {sv.fotos.map((f, i) => (
                    <div key={f.id} className={s.foto}>
                      <img src={f.url} alt={sv.nombre} />
                      {/* La primera es la que ve el alumno en el listado del Shop:
                          conviene que se note cuál es antes de borrar la de al lado. */}
                      {i === 0 && <span className={s.fotoPrincipal}>Principal</span>}
                      <button
                        className={s.fotoBorrar}
                        title="Borrar la foto"
                        onClick={() => void borrarFoto(sv, f.id)}
                      >
                        ✕
                      </button>
                    </div>
                  ))}
                  {sv.fotos.length < MAXIMO_FOTOS && (
                    <button
                      className={s.fotoAgregar}
                      disabled={subiendoEn === sv.id}
                      onClick={() => pedirFoto(sv.id)}
                    >
                      {subiendoEn === sv.id ? '…' : '+ Foto'}
                    </button>
                  )}
                </div>

                <div className={s.acciones}>
                  <button className={s.btnMiniGris} onClick={() => empezarEdicion(sv)}>Editar</button>
                  {sv.activo ? (
                    <button className={s.btnMiniGris} onClick={() => void cambiarActivo(sv)}>Desactivar</button>
                  ) : (
                    <button className={s.btnMini} onClick={() => void cambiarActivo(sv)}>Activar</button>
                  )}
                </div>
              </>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
