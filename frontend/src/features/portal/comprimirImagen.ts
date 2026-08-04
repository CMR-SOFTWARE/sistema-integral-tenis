/**
 * Achica una imagen a un cuadrado de máx `lado` px y la devuelve como data URL
 * (JPEG). Así la foto de perfil viaja liviana y se guarda como texto en la
 * base, sin storage externo. Recorta al centro para que quede cuadrada.
 */
export function comprimirImagen(file: File, lado = 256, calidad = 0.8): Promise<string> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    const url = URL.createObjectURL(file);

    img.onload = () => {
      URL.revokeObjectURL(url);
      const canvas = document.createElement('canvas');
      canvas.width = lado;
      canvas.height = lado;
      const ctx = canvas.getContext('2d');
      if (!ctx) { reject(new Error('No se pudo procesar la imagen.')); return; }

      // Recorte cuadrado centrado (cover)
      const min = Math.min(img.width, img.height);
      const sx = (img.width - min) / 2;
      const sy = (img.height - min) / 2;
      ctx.drawImage(img, sx, sy, min, min, 0, 0, lado, lado);

      resolve(canvas.toDataURL('image/jpeg', calidad));
    };
    img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('No se pudo leer la imagen.')); };
    img.src = url;
  });
}

/**
 * Comprime una imagen a un lado máximo (conservando su proporción) y la devuelve
 * como Blob, lista para subir por multipart. A diferencia de las dos de acá abajo,
 * no genera un data URL: las fotos del perfil del profe van a un storage real, y
 * mandar un archivo binario pesa un 33% menos que mandarlo en base64.
 *
 * Se limita el lado MÁS LARGO y no el ancho: en el perfil las fotos se muestran
 * enteras, así que una vertical de 3000 px de alto tiene que achicarse igual que
 * una horizontal de 3000 px de ancho.
 */
export function comprimirABlob(file: File, maxLado = 1400, calidad = 0.82): Promise<Blob> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    const url = URL.createObjectURL(file);

    img.onload = () => {
      URL.revokeObjectURL(url);
      const escala = Math.min(1, maxLado / Math.max(img.width, img.height));
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(img.width * escala);
      canvas.height = Math.round(img.height * escala);
      const ctx = canvas.getContext('2d');
      if (!ctx) { reject(new Error('No se pudo procesar la imagen.')); return; }
      ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

      canvas.toBlob(
        (blob) => blob ? resolve(blob) : reject(new Error('No se pudo procesar la imagen.')),
        'image/jpeg',
        calidad,
      );
    };
    img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('No se pudo leer la imagen.')); };
    img.src = url;
  });
}

/**
 * Comprime una imagen para un BANNER: la lleva a un ancho máximo conservando
 * su proporción (no recorta como la foto de perfil), y la devuelve como data
 * URL JPEG. Para banners de publicidad livianos que se guardan en la base.
 */
export function comprimirBanner(file: File, maxAncho = 1000, calidad = 0.82): Promise<string> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    const url = URL.createObjectURL(file);

    img.onload = () => {
      URL.revokeObjectURL(url);
      const escala = Math.min(1, maxAncho / img.width);
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(img.width * escala);
      canvas.height = Math.round(img.height * escala);
      const ctx = canvas.getContext('2d');
      if (!ctx) { reject(new Error('No se pudo procesar la imagen.')); return; }
      ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      resolve(canvas.toDataURL('image/jpeg', calidad));
    };
    img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('No se pudo leer la imagen.')); };
    img.src = url;
  });
}
