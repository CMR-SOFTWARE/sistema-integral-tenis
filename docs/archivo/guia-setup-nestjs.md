# Guía de Setup — Sistema Integral Tenis desde cero

> Guía para la primera sesión de trabajo. Seguila en orden, sin saltear pasos.
> Si algo falla, anotá el error exacto y el paso donde estabas, y lo resolvemos juntos.
> Tiempo estimado: 30-45 minutos.

---

## PASO 0 — Prerrequisitos (una sola vez en tu máquina)

### 0.1 Node.js

Abrí una terminal (en VS Code: `Ctrl+ñ`, o PowerShell directo) y verificá:

```bash
node -v
```

✅ **Esperado**: `v20.x.x` o superior (ideal `v22.x.x`, que es el LTS actual).
❌ **Si dice "no se reconoce el comando"** o la versión es vieja: bajá el instalador LTS de https://nodejs.org, instalalo con todo por defecto, **cerrá y reabrí la terminal** y probá de nuevo.

### 0.2 Git

```bash
git -v
```

✅ **Esperado**: `git version 2.x`.
❌ Si no está: https://git-scm.com/downloads, instalación por defecto.

**Si es la primera vez que usás git en esta máquina**, configurá tu identidad (queda grabada en cada commit):

```bash
git config --global user.name "Lucas"
git config --global user.email "tu-email-de-github@ejemplo.com"
```

⚠️ Usá el mismo email con el que estás registrado en GitHub, así los commits quedan vinculados a tu cuenta.

### 0.3 pnpm

```bash
npm install -g pnpm
pnpm -v
```

✅ **Esperado**: `9.x` o `10.x`.
❌ **Error típico en Windows**: "la ejecución de scripts está deshabilitada en este sistema". Solución: abrí PowerShell **como administrador** y ejecutá:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Después cerrá y reabrí la terminal normal y probá `pnpm -v` de nuevo.

### 0.4 Extensiones de VS Code

Panel de extensiones (`Ctrl+Shift+X`), instalá estas tres:

| Extensión | Para qué |
|---|---|
| **ESLint** (Microsoft) | Marca errores de código en vivo |
| **Prettier** (Prettier) | Formatea el código automáticamente |
| **Prisma** (Prisma) | Colores y autocompletado en `schema.prisma` |

---

## PASO 1 — Carpeta del proyecto y git local

Elegí dónde van a vivir tus proyectos (ej: `C:\dev\` o `Documentos\proyectos\`) y desde ahí:

```bash
mkdir sistema-integral-tenis
cd sistema-integral-tenis
git init
code .
```

- `git init` crea el repositorio **local** (la carpeta oculta `.git` donde vive toda la historia).
- `code .` abre VS Code en la carpeta. Si no funciona, abrí VS Code a mano → File → Open Folder → `sistema-integral-tenis`.

✅ **Esperado**: VS Code abierto en una carpeta vacía. En la barra de abajo a la izquierda puede decir `main` o `master` — no importa todavía, lo normalizamos en el paso 8.

**De acá en adelante, todos los comandos van en la terminal integrada de VS Code (`Ctrl+ñ`), parado en la carpeta `sistema-integral-tenis` salvo que se indique lo contrario.**

---

## PASO 2 — Configurar el monorepo

Un monorepo = un solo repositorio que contiene varios proyectos (nuestra API y nuestro frontend). pnpm los maneja como "workspaces".

### 2.1 Creá el archivo `package.json` en la raíz

En VS Code: click derecho en el explorador → New File → `package.json`. Contenido:

```json
{
  "name": "sistema-integral-tenis",
  "private": true
}
```

(`private: true` evita publicar esto por accidente en el registro público de npm.)

### 2.2 Creá el archivo `pnpm-workspace.yaml` en la raíz

```yaml
packages:
  - "apps/*"
```

Esto le dice a pnpm: "cada carpeta dentro de `apps/` es un proyecto independiente".

⚠️ **Cuidado con YAML**: la indentación es con 2 espacios y el guion va exactamente así. Un espacio de más rompe el archivo.

---

## PASO 3 — Backend NestJS

```bash
mkdir apps
cd apps
pnpm dlx @nestjs/cli new api --package-manager pnpm --skip-git --strict
```

Desglose del comando:
- `pnpm dlx` = ejecutar una herramienta sin instalarla globalmente (como `dotnet new` pero descargando la plantilla al vuelo)
- `--skip-git` = que Nest NO cree su propio repo (ya tenemos el de la raíz)
- `--strict` = TypeScript en modo estricto. Viniendo de C# es lo natural: null-safety y tipado fuerte

Tarda unos minutos (descarga dependencias).

✅ **Esperado**: carpeta `apps/api/` con `src/`, `test/`, `package.json`, etc. Sin errores rojos al final.

---

## PASO 4 — Frontend React + Vite

Todavía parado en `apps/`:

```bash
pnpm create vite web --template react-ts
```

Si te hace preguntas interactivas, las respuestas son: framework **React**, variante **TypeScript**.

Después volvé a la raíz e instalá todo:

```bash
cd ..
pnpm install
```

`pnpm install` desde la raíz lee el workspace y resuelve las dependencias de **los dos** proyectos de una.

✅ **Esperado**: carpeta `apps/web/` creada, y el `pnpm install` termina sin errores (warnings amarillos pueden aparecer, no asustan).

---

## PASO 5 — `.gitignore` en la raíz

Nuevo archivo `.gitignore` en la raíz (ojo: el nombre empieza con punto):

```
node_modules/
dist/
build/
.env
.env.*
!.env.example
coverage/
```

Qué hace cada línea:
- `node_modules/` → dependencias descargadas. Pesan cientos de MB y se regeneran con `pnpm install`. JAMÁS van al repo
- `dist/`, `build/` → código compilado, se regenera
- `.env`, `.env.*` → **SEGURIDAD**: acá van a vivir las credenciales (DB, Mercado Pago, Twilio). Si esto pisa GitHub, cualquiera con acceso al repo tiene las llaves de todo
- `!.env.example` → la excepción: un archivo de ejemplo SIN valores reales que sí se comparte, para que el equipo sepa qué variables hacen falta
- `coverage/` → reportes de tests, se regeneran

✅ **Verificación**: en el panel de Source Control de VS Code (`Ctrl+Shift+G`), NO tiene que aparecer ningún archivo dentro de `node_modules`.

---

## PASO 6 — Verificar que todo levanta

Necesitás **dos terminales** a la vez. En VS Code: botón `+` del panel de terminal, o el ícono de dividir.

**Terminal 1 — API:**

```bash
cd apps/api
pnpm start:dev
```

✅ **Esperado**: logs verdes de Nest terminando en algo como `Nest application successfully started`. Abrí http://localhost:3000 en el navegador → tiene que decir **"Hello World!"**.

**Terminal 2 — Frontend:**

```bash
cd apps/web
pnpm dev
```

✅ **Esperado**: `VITE ready in ...ms` y una URL. Abrí http://localhost:5173 → página de bienvenida de Vite + React con un contador clickeable.

❌ **Error típico**: "port 3000 already in use" → tenés otra cosa usando ese puerto; cerrala, o avisame y lo cambiamos.

**Para frenar cualquiera de los dos**: `Ctrl+C` en su terminal. Frenalos antes de seguir.

---

## PASO 7 — Primer commit

Desde la raíz (`cd ../..` si quedaste en apps/web):

```bash
git add .
git status
```

`git status` te muestra qué se va a commitear. **Revisá**: tienen que aparecer archivos de `apps/api` y `apps/web`, y NO tiene que haber nada de `node_modules` ni archivos `.env`.

```bash
git commit -m "chore: setup inicial monorepo (api nestjs + web react)"
```

El prefijo `chore:` es la convención de commits que definimos en el plan: `feat:` para funcionalidad nueva, `fix:` para arreglos, `chore:` para tareas de mantenimiento/setup, `docs:` para documentación.

---

## PASO 8 — Repo en GitHub y push

### 8.1 Crear el repo (en el navegador)

1. https://github.com → botón verde **New** (o el `+` arriba a la derecha → New repository)
2. Repository name: `sistema-integral-tenis`
3. Visibilidad: **Private**
4. ⚠️ **NO marques** "Add a README", ni ".gitignore", ni "license". El repo tiene que nacer **completamente vacío** porque nuestra historia ya existe localmente; si GitHub le mete archivos, las historias divergen y el push falla
5. **Create repository**

### 8.2 Conectar y subir

GitHub te muestra una pantalla con comandos. Usá los del bloque **"…or push an existing repository from the command line"**:

```bash
git remote add origin https://github.com/TU-USUARIO/sistema-integral-tenis.git
git branch -M main
git push -u origin main
```

- `remote add origin` = "este repo local tiene un espejo remoto que se llama origin y vive en esta URL"
- `branch -M main` = renombra la rama a `main` (por si quedó como `master`)
- `push -u origin main` = sube todo y deja vinculadas las ramas (los próximos push son solo `git push`)

**Autenticación**: si es tu primer push desde esta máquina, te va a saltar una ventana para loguearte en GitHub. Elegí la opción del navegador (Sign in with your browser) — es el camino sin fricción.

✅ **Verificación final**: refrescá la página del repo en GitHub. Tenés que ver `apps/api`, `apps/web`, `pnpm-workspace.yaml` y tu commit `chore: setup inicial...`.

---

## Checklist final de la sesión

- [ ] `node -v` ≥ 20, `pnpm -v` funciona
- [ ] Carpeta `sistema-integral-tenis` con git inicializado
- [ ] `package.json` + `pnpm-workspace.yaml` en la raíz
- [ ] `apps/api` (Nest) creado y levanta en :3000 con "Hello World!"
- [ ] `apps/web` (Vite/React) creado y levanta en :5173
- [ ] `.gitignore` en la raíz, sin `node_modules` en el source control
- [ ] Primer commit hecho
- [ ] Repo `sistema-integral-tenis` privado en GitHub con el código subido

---

## Qué viene después (próxima sesión)

**Clase 1 del proyecto nuevo**: desarmamos el Hello World de Nest pieza por pieza — `main.ts`, `AppModule`, `AppController`, `AppService` — y mapeamos cada concepto a su equivalente de ASP.NET Core (módulos ≈ DI container + Startup, controllers ≈ controllers, services ≈ services inyectados). Después conectamos Prisma con el `modelo-alumnos.md` que ya tenemos diseñado.
