# QBasWing Shuttle

Copiador/movedor ultrarrápido de **QBasWinG** (identidad propia de su creador).
Programa multi-archivo ultrarrápido, con cola, barras en vivo (archivo + global,
velocidad, tiempo transcurrido/restante), verificación SHA-256 opcional, aviso de
espacio en disco y **20 idiomas**.

Al copiar o al mover se abre una **ventanita de progreso** en la esquina: general y
por archivo, velocidad, tiempo restante, Pausar, Cancelar, Más (lista completa),
Abrir destino y Reintentar. Cuando termina se va sola. Desde el menú del Explorador
o de otro gestor de archivos, esa ventanita es lo único que aparece: la ventana
principal no se abre.

Motor, interfaz y textos 100% propios: paleta, logotipo y marca de
agua "QBasWing Shuttle © 2026" de QBasWinG. El comportamiento se ha diseñado
desde cero, sin copiar código, textos ni aspecto de ninguna otra aplicación.

## Carpetas del proyecto

| Ruta | Contenido |
|---|---|
| `QBasCopier\` | App principal (Avalonia, multiplataforma: Windows/Linux/macOS/Android) |
| `QBasCopierSetup\` | Instalador de Windows (WPF): idioma + música + acceso directo |
| `QBasCopier.Android\` | Proyecto Android (APK) |
| `build.bat` | **Windows**: doble clic -> `dist\Windows\` |
| `build.sh` | **Linux/macOS**: portátil -> `dist\Linux\` o `dist\macOS\` |
| `make-app.sh` | **macOS**: bundle `dist/macOS/QBasWing-Shuttle.app` |
| `install-desktop.sh` | Linux: menú, icono y Scripts de Nautilus (Copiar/Mover) |
| `build-android.sh` | Android: APK (requiere workload `android`) |

## Cómo construir cada versión (en una PC con .NET SDK 10)

### Windows (la que quieres "winwin")
```
build.bat        → doble clic (o desde cmd)
```
Resultado en `dist\Windows\`:
- `QBasWing-Shuttle.exe` — portátil (se copia a cualquier Windows x64 y funciona)
- `QBasWing-Shuttle Setup.exe` — instalador con selector de idioma y música

> ¿Lo quieres en la PC sin compilar? En cada release de GitHub hay dos ZIP:
> `QBasWing-Shuttle-v1.2-win-x64.zip` (la app ya compilada) y
> `QBasWing-Shuttle-v1.2-fuentes.zip` (**el proyecto entero**, sin `bin\` ni
> `obj\`). Descomprime el segundo en el PC y ya puedes abrirlo, tocarlo y
> compilarlo con `build.bat`.

### Linux
```
./build.sh linux-x64            # o simplemente ./build.sh
./build.sh linux-x64 install    # además lo instala (menú + icono + Nautilus)
```

### macOS
```
./make-app.sh osx-arm64   # Apple Silicon  (o osx-x64 en Intel)
# → dist/macOS/QBasWing-Shuttle.app
```

### Android (celular)
```
./build-android.sh        # instala workload android (1 vez), genera el .apk
```
El proyecto Android apunta a `net8.0-android` (LTS estable; .NET 10 aún no
publica el pack host Mono de Linux necesario para compilar en la nube).
> La copia en Android usa SAF (`content://`) en interno, externo y USB, con el
> selector del sistema. En el destino se crean solas las carpetas y los archivos
> repetidos no se pisan.

#### Obtener el APK SIN PC (github.com, gratis)
1. Crea un repositorio en GitHub y sube el contenido de este ZIP.
2. Entra en la pestaña **Actions** → workflow **"Compilar APK QBasWing Shuttle"**.
3. Pulss **Run workflow** (o deja que corra solo al subir).
4. Al terminar abre el artefacto **QBasWing-Shuttle-v1.2-apk**, descarga el `.apk`.
5. Compártelo con cualquiera (Android pedirá "permitir fuentes desconocidas").
El APK sale firmado con la clave de depuración automática: vale para instalarlo
y compartirlo; no es válido para publicarlo en Google Play (eso requiere tu
llave de firma propia, se documenta en una v1.1).

### iOS
No incluido en v1.0 (necesita Mac con Xcode, cuenta de desarrollador y firma).
Se puede añadir como proyecto esqueleto en una próxima versión.

## Integración con el sistema
- **Windows**: menú contextual "Copiar con QBasWing Shuttle" / "Mover con…",
  "Copiar aquí…" en el fondo de una carpeta, opción propia de Ctrl+C / Ctrl+V
  (con vuelta atrás para restaurar el del sistema), Enviar a, inicio con Windows,
  bandeja e instancia única. Los verbos leen la **selección marcada entera** del
  Explorador, no solo el primer archivo.
- **Linux**: entrada de menú + Scripts de Nautilus.

### Otros gestores de archivos
Cualquier gestor que deje configurar un comando externo lleva la misma línea:

```
"<ruta>\QBasWing-Shuttle.exe" --copy -- "<origen>" --dest "<destino>"
"<ruta>\QBasWing-Shuttle.exe" --move -- "<origen>" --dest "<destino>"
```

Pasando varias rutas de origen, la copia sale de una. Si la app ya estaba corriendo,
el comando se le pasa a ella y no se abre una segunda copia.

## Ajustes (espejo profesional, adaptados)
Idioma, inicio con Windows / activar al inicio, bandeja/minimizar, unidad de
tamaño, fin de copia, límite de velocidad (MB/s y KB), colisiones (8 modos),
errores + reintentos, añadir listas mientras copia (nunca/siempre/misma fuente/
mismo destino/ambos/alguno + confirmar), atributos/seguridad, borrado de
incompletos, renombrado con patrón `%NAME% (%COPY%)%EXT%`, log de errores,
y Avanzado: búfer/caché (presets) + KB, subprocesos, intervalos (ventana/
velocidad/throttle), prioridad, verificación SHA-256, aviso de espacio libre.

## Créditos y licencias
Ver `LICENSES.txt`.

## Requisitos de compilación
- .NET SDK 10 (escritorio): https://dotnet.microsoft.com/download/dotnet/10.0
- Android: .NET SDK 8 (LTS) + `dotnet workload install android`