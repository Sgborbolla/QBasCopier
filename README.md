# QBasCopier — v1.0

Copiador/movedor ultrarrápido de **QBasWinG** (identidad propia de su creador).
Programa multi-archivo ultrarrápido, con explorador doble, cola, barras en vivo
(archivo + global, velocidad, tiempo transcurrido/restante), verificación SHA-256
opcional, aviso de espacio en disco y **20 idiomas**.

Basado en inspiración de copiadores conocidos (SuperCopier/TeraCopy),
pero con motor, interfaz y textos 100% propios: paleta, logotipo y marca de
agua "QBasCopier © 2026" de QBasWinG.

## Carpetas del proyecto

| Ruta | Contenido |
|---|---|
| `QBasCopier\` | App principal (Avalonia, multiplataforma: Windows/Linux/macOS/Android) |
| `QBasCopierSetup\` | Instalador de Windows (WPF): idioma + música + acceso directo |
| `QBasCopier.Android\` | Proyecto Android (APK) |
| `build.bat` | **Windows**: doble clic -> `dist\Windows\` |
| `build.sh` | **Linux/macOS**: portátil -> `dist\Linux\` o `dist\macOS\` |
| `make-app.sh` | **macOS**: bundle `dist\macOS\QBasCopier.app` |
| `install-desktop.sh` | Linux: menú, icono y Scripts de Nautilus (Copiar/Mover) |
| `build-android.sh` | Android: APK (requiere workload `android`) |

## Cómo construir cada versión (en una PC con .NET SDK 10)

### Windows (la que quieres "winwin")
```
build.bat        → doble clic (o desde cmd)
```
Resultado en `dist\Windows\`:
- `QBasCopier.exe` — portátil (se copia a cualquier Windows x64 y funciona)
- `QBasCopier Setup.exe` — instalador con selector de idioma y música

### Linux
```
./build.sh linux-x64            # o simplemente ./build.sh
./build.sh linux-x64 install    # además lo instala (menú + icono + Nautilus)
```

### macOS
```
./make-app.sh osx-arm64   # Apple Silicon  (o osx-x64 en Intel)
# → dist/macOS/QBasCopier.app
```

### Android (celular)
```
./build-android.sh        # instala workload android (1 vez), genera el .apk
```
El proyecto Android apunta a `net8.0-android` (LTS estable; .NET 10 aún no
publica el pack host Mono de Linux necesario para compilar en la nube).
> Nota experimental: usa los permisos clásicos de almacenamiento (until API 32).
> Para Android 13+ se adaptaría con SAF (DocumentProvider) en una v2.

#### Obtener el APK SIN PC (github.com, gratis)
1. Crea un repositorio en GitHub y sube el contenido de este ZIP.
2. Entra en la pestaña **Actions** → workflow **"Compilar APK QBasCopier"**.
3. Pulss **Run workflow** (o deja que corra solo al subir).
4. Al terminar abre el artefacto **QBasCopier-apk**, descarga el `.apk`.
5. Compártelo con cualquiera (Android pedirá "permitir fuentes desconocidas").
El APK sale firmado con la clave de depuración automática: vale para instalarlo
y compartirlo; no es válido para publicarlo en Google Play (eso requiere tu
llave de firma propia, se documenta en una v1.1).

### iOS
No incluido en v1.0 (necesita Mac con Xcode, cuenta de desarrollador y firma).
Se puede añadir como proyecto esqueleto en una próxima versión.

## Integración con el sistema
- **Windows**: menú contextual "Copiar"/"Mover" (y "Copiar aquí…"), SendTo, inicio
  con Windows, bandeja, instancia única.
- **Linux**: entrada de menú + Scripts de Nautilus.

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