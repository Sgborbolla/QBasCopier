#!/usr/bin/env python3
"""Quita de Localization.cs las claves que ningun archivo del proyecto pide.

Localization.cs guarda 20 traducciones por clave. Las claves que se quedaron sin
usar al quitar el panel de dos columnas son un peso muerto que alguien tiene que
releer cada vez que toca el archivo, asi que se borran.

Se apoya en las mismas funciones de analisis que check-localizacion.py, que ya
saben donde acaba cada llamada A(...), y borra la llamada entera de una vez.

Uso:  python3 scripts/quitar-claves-sin-usar.py
Salida: 0 si todo esta bien, 1 si hay errores.
"""

import importlib.util
import os
import re
import sys

_AQUI = os.path.dirname(os.path.abspath(__file__))
_espec = importlib.util.spec_from_file_location(
    "check_localizacion", os.path.join(_AQUI, "check-localizacion.py"))
_cl = importlib.util.module_from_spec(_espec)
_espec.loader.exec_module(_cl)
RAIZ = _cl.RAIZ
argumentos = _cl.argumentos
fin_de_llamada = _cl.fin_de_llamada
revisa_uso = _cl.revisa_uso

RUTA = os.path.join(RAIZ, "QBasCopier", "Localization.cs")


def main():
    with open(RUTA, encoding="utf-8") as f:
        src = f.read()

    claves = {}
    for m in re.finditer(r'(?<![\w.])A\s*\(\s*"', src):
        abre = src.index("(", m.start())
        vals = argumentos(src, abre)
        if vals:
            claves[vals[0]] = m.start()

    sin_usar, errores = revisa_uso(RUTA, set(claves))
    if errores:
        for e in errores:
            print(e)
        return 1
    if not sin_usar:
        print("No hay claves sin usar.")
        return 0

    # De derecha a izquierda: asi los indices de las que quedan siguen valiendo.
    for clave in sorted(sin_usar, key=lambda k: -claves[k]):
        ini = claves[clave]
        fin = fin_de_llamada(src, src.index("(", ini))
        if fin is None:
            print(f"No se pudo localizar el final de la llamada de '{clave}'")
            return 1
        # Se come tambien la sangria y el salto de linea de delante, para no dejar
        # lineas en blanco metidas en medio del archivo.
        ini = src.rfind("\n", 0, ini) + 1
        if src[ini:src.index("(", claves[clave])].strip() == "":
            pass
        else:
            ini = claves[clave]
        fin = src.find("\n", fin)
        src = src[:ini] + (src[fin + 1:] if fin >= 0 else "")

    with open(RUTA, "w", encoding="utf-8") as f:
        f.write(src)
    print(f"Borradas {len(sin_usar)} claves: " + ", ".join(sin_usar))
    return 0


if __name__ == "__main__":
    sys.exit(main())
