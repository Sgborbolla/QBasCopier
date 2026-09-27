# AGENTS.md — QBasWing Shuttle

Instrucciones permanentes para trabajar en este repositorio. Leer antes de tocar nada.

## Qué es este proyecto

- **Nombre: QBasWing Shuttle.** Ese es el nombre del programa, de la carpeta que crea al
  recibir y de la marca. El paquete es `com.qbaswing.qbascopier`.
- Copia y mueve archivos, y transfiere entre equipos por la red local sin internet.
- Avalonia 11.3.5 / C#. Windows, Linux, macOS y Android. **20 idiomas**.
- `QBasCopier/` es el codigo compartido (se compila en los dos proyectos);
  `QBasCopier.Android/` es solo Android.

## Identidad (lo mas importante)

- Todo lo visible es nuestro: nombre, textos, colores, iconos, disposicion y nombres de
  carpeta. **Ninguna marca ajena en el codigo, en los comentarios ni en la interfaz.**
- **No se nombra ninguna otra aplicacion en ninguna parte**: ni en la interfaz, ni en
  los textos, ni en los comentarios del codigo, ni en la documentacion. Otras
  aplicaciones de este tipo solo sirven como referencia mental de que funciones
  existen (que se puede elegir, donde y como se ordena).
- Ante la duda, se decide por lo que seria QBasWing Shuttle, no por lo que hace otro.

## Reglas del producto

- **Copiar, mover y transferir: cualquier tipo de archivo y cualquier tamaño**, de unos KB
  a varios GB. Sin topes, sin listas de extensiones permitidas, sin conversiones.
- **Calidad = integridad**: copia byte a byte. Nada de recomprimir, convertir ni degradar.
  Lo que llega es identico a lo que salio (checksum cuando se pueda).
- **Velocidad**: nada de esperas ni de doble escritura. Buffers grandes, sin compresion
  inútil al empaquetar carpetas, y todo en streaming (nunca un archivo entero en memoria).
- **Recepcion**: se crea sola la carpeta de la marca con subcarpetas por tipo
  (Fotos, Videos, Musica, Documentos, Instaladores, Otros). Los nombres de archivo se
  respetan y los repetidos no se pisan.
- **Destino a elegir**: en el PC cualquier unidad (D:, USB, carpeta de red); en Android
  interno, externo o USB, con permiso SAF. El usuario lo elige en Opciones > Transferir.
- **Movil y PC no son lo mismo**: en PC dos columnas, pegar el codigo y buscador de
  archivos dentro de la app; en movil camara, selector del sistema y panel a pantalla
  completa. Lo que no exista en la plataforma se oculta, no se deja roto.
- **Opciones**: lo del copiador se queda como estaba; el transferidor tiene su propio
  bloque, separado.

## Como trabajar aqui

- **Textos**: todo literal visible va por `L.Get("clave")`. Las claves viven en
  `QBasCopier/Localization.cs` y **cada clave nueva necesita los 20 idiomas**, en este
  orden: en, es, pt, fr, de, it, nl, pl, ru, tr, ar, zh-Hans, ja, ko, hi, id, sv, fi, cs, th.
  Los textos largos se parten en lineas de 8 + 8 + 4 valores.
  Al cambiar el idioma, lo que dependa de un texto se registra en `_texts`.
- **Antes de dar algo por terminado**: `python3 scripts/check-localizacion.py` (valida
  cantidad de idiomas, claves repetidas, vacios, marcadores `{0}` y alfabetos).
- **Plataforma**: lo que sea solo de escritorio va dentro de `#if !ANDROID`; lo que sea
  solo de movil, dentro de `#if ANDROID`. Sin `catch { }` en lo critico: se registra con
  `CrashLog`.
- **Fallos que se tragan**: 114 `catch { }` vacios. Los que esconden algo importante se
  arreglan; no se deja: se registra.
- **No compilar sin motivo**: el entorno tiene poca conexion y los restores tardan mucho.
  La CI de GitHub es la referencia real de si algo compila.
- **Nada de APK ni ZIP hasta que todo este programado y sin errores, y el usuario lo
  pida expresamente.** Tampoco hacer push ni publicar sin que lo pida.
- No reescribir un archivo entero para tocar cuatro lineas: se edita lo justo.

## Comandos

```bash
python3 scripts/check-localizacion.py      # traducciones (obligatorio antes de terminar)
dotnet build QBasCopier/QBasCopier.csproj # escritorio
dotnet build QBasCopier.Android/QBasCopier.Android.csproj   # android
```
