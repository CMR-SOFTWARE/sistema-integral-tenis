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
