#!/usr/bin/env python3
"""Revisa Localization.cs antes de compilar.

El texto de la app vive en una funcion local A(clave, 20 idiomas) dentro del
constructor estatico de L. Con 20 parametros, un idioma de mas o de menos es un
error de compilacion (CS1501) que aparece 30 minutos despues, cuando el build
ya descargo los paquetes. Este script lo dice en un segundo, y de paso revisa
lo que el compilador no puede ver: claves repetidas, textos vacios, placeholders
descolocados y claves que el codigo pide pero no existen.

Uso:  python3 scripts/check-localizacion.py [ruta/Localization.cs]
Salida: 0 si todo esta bien, 1 si hay errores.
"""

import os
import re
import sys
import unicodedata

IDIOMAS = ["en", "es", "pt", "fr", "de", "it", "nl", "pl", "ru", "tr", "ar",
           "zh", "ja", "ko", "hi", "id", "sv", "fi", "cs", "th"]

# Alfabetos que delatan un texto colocado en el idioma que no es. Un eslovaco
# en la casilla de tailandes o un arabe en la de indonesio no se ven a simple
# vista en una lista de 20 cadenas, pero el usuario los lee en su idioma.
ALFABETOS = {
    "ru": r"[Ѐ-ӿ]",
    "ar": r"[؀-ۿݐ-ݿ]",
    "zh": r"[一-鿿]",
    "ja": r"[぀-ヿ一-鿿]",
    "ko": r"[가-힯ᄀ-ᇿ]",
    "hi": r"[ऀ-ॿ]",
    "th": r"[฀-๿]",
}

# Palabras que se escriben igual en todos los idiomas y no delatan una
# traduccion.puta en la casilla equivocada.
NEUTRALES = {
    # Palabras universales, tecnicas o de marca propia: se pueden repetir tal cual en
    # cualquier idioma y el validador no las mira.
    "ok", "ms", "mb", "gb", "tb", "kb", "b", "s", "%", "n/a", "sha-256", "sha256",
    "vpn", "wifi", "wi-fi", "wi fi", "ssid", "url", "tcp", "usb", "sd", "dns", "ip",
    "apl", "vpn", "q", "d", "c", "f", "kbps", "mbps", "fps", "id", "uuid", "api",
    "http", "https", "ftp", "html", "xml", "json", "md", "txt", "exe", "apk", "zip",
    "drive", "app", "web", "server", "backup", "preview", "av", "iso", "pdf",
    "copiar", "cancelar", "destino", "total", "interfaz", "version", "archivo",
    "pasta", "status", "motor", "performance", "help", "about", "settings", "demo",
}

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def argumentos(texto, abre):
    """Cadenas de la llamada que empieza en el parentesis 'abre'."""
    prof = 0
    args, actual, j, en_cadena = [], "", abre, False
    while j < len(texto):
        c = texto[j]
        if en_cadena:
            if c == "\\":
                actual += texto[j:j + 2]
                j += 2
                continue
            if c == '"':
                en_cadena = False
            else:
                actual += c
        else:
            if c == '"':
                en_cadena = True
            elif c == "(":
                prof += 1
            elif c == ")":
                prof -= 1
                if prof == 0:
                    args.append(actual.strip())
                    return args
            elif c == "," and prof == 1:
                args.append(actual.strip())
                actual = ""
            else:
                actual += c
        j += 1
    return None


def fin_de_llamada(texto, abre):
    """Posicion justo despues del parentesis de cierre."""
    prof, j, en_cadena = 0, abre, False
    while j < len(texto):
        c = texto[j]
        if en_cadena:
            if c == "\\":
                j += 2
                continue
            if c == '"':
                en_cadena = False
        else:
            if c == '"':
                en_cadena = True
            elif c == "(":
                prof += 1
            elif c == ")":
                prof -= 1
                if prof == 0:
                    return j + 1
        j += 1
    return None


def normaliza(s):
    s = unicodedata.normalize("NFKD", s.lower())
    s = "".join(c for c in s if not unicodedata.combining(c))
    return re.sub(r"[^a-z0-9]+", "", s)


def revisa_traducciones(ruta):
    with open(ruta, encoding="utf-8") as f:
        src = f.read()

    claves = {}
    orden = []
    errores = []

    for m in re.finditer(r'(?<![\w.])A\s*\(\s*"', src):
        abre = src.index("(", m.start())
        vals = argumentos(src, abre)
        linea = src[:m.start()].count("\n") + 1
        if vals is None:
            errores.append(f"linea {linea}: llamada a A() sin cerrar")
            continue
        clave, textos = vals[0], vals[1:]
        if clave in claves:
            errores.append(f"linea {linea}: la clave '{clave}' ya estaba en la linea {claves[clave]}")
            continue
        claves[clave] = linea
        orden.append((linea, clave, textos))

        if len(textos) != len(IDIOMAS):
            errores.append(
                f"linea {linea}: '{clave}' tiene {len(textos)} textos y se esperan "
                f"{len(IDIOMAS)} (uno por idioma). Sobran {len(textos) - len(IDIOMAS)}, "
                f"faltan {len(IDIOMAS) - len(textos)}."
                if len(textos) != len(IDIOMAS) else "")
            continue

        for i, (idioma, texto) in enumerate(zip(IDIOMAS, textos)):
            if not texto:
                errores.append(f"linea {linea}: '{clave}' tiene el texto de {idioma} vacio")

        ref = set(re.findall(r"\{\d+\}", textos[0]))
        for idioma, texto in zip(IDIOMAS[1:], textos[1:]):
            if set(re.findall(r"\{\d+\}", texto)) != ref:
                errores.append(
                    f"linea {linea}: '{clave}' en {idioma} usa {sorted(set(re.findall(r'{{\\d+}}', texto)))} "
                    f"y en ingles son {sorted(ref)}")

        for idioma, texto in zip(IDIOMAS, textos):
            alfabeto = ALFABETOS.get(idioma)
            # Se mira palabra a palabra: un texto puede ser corto y universal
            # ("SD / USB") y seguir siendo valido sin翻译.
            solo_neutral = all(
                p in NEUTRALES or p in {"/", "-", "·", "|", ":", "..", "..."}
                for p in re.split(r"[\s/|·:,-]+", texto.lower()) if p)
            if alfabeto and not re.search(alfabeto, texto) and not solo_neutral:
                errores.append(
                    f"linea {linea}: el texto de {idioma} en '{clave}' no parece {idioma}: "
                    f"'{texto[:60]}'")

    return claves, orden, errores


def revisa_uso(ruta, claves):
    """Claves que el codigo pide y no existen, y claves que ya no se usan.

    El uso se busca como literal en cualquier .cs y .axaml, no solo en
    L.Get("..."), porque la UI arma las tablas con L.Get(key) sobre una variable.
    """
    errores = []
    todo = []
    for raiz, _, nombres in os.walk(RAIZ):
        if any(p in raiz for p in ("/obj", "/bin", "/.git")):
            continue
        for nombre in nombres:
            if not (nombre.endswith(".cs") or nombre.endswith(".axaml")):
                continue
            # Localization.cs es donde viven todas las claves: si se contara, ninguna
            # pareceria sin usar nunca.
            if nombre == "Localization.cs":
                continue
            ruta_cs = os.path.join(raiz, nombre)
            with open(ruta_cs, encoding="utf-8") as f:
                texto = f.read()
            # Se guarda el texto entero y se buscara cada clave como "clave" tal cual.
            # Con una expresion regular los literales se emparejaban mal en cuanto
            # habia una cadena con escape y algunas claves parecia no usarse nunca.
            todo.append(texto)
            for k in re.findall(r'L\.Get\(\s*"([^"]+)"', texto):
                if k not in claves:
                    linea = texto[:texto.index(f'L.Get("{k}"')].count("\n") + 1
                    errores.append(
                        f"{os.path.relpath(ruta_cs, RAIZ)}:{linea}: "
                        f'L.Get("{k}") no existe en Localization.cs')
    codigo = "\n".join(todo)
    sin_usar = sorted(k for k in claves if ('"' + k + '"') not in codigo)
    return sin_usar, errores


def main():
    ruta = sys.argv[1] if len(sys.argv) > 1 else os.path.join(RAIZ, "QBasCopier", "Localization.cs")
    if not os.path.exists(ruta):
        print(f"No existe {ruta}")
        return 1

    claves, orden, errores = revisa_traducciones(ruta)
    sin_usar, err_uso = revisa_uso(ruta, claves)
    errores += err_uso

    print(f"Localization.cs: {len(claves)} claves x {len(IDIOMAS)} idiomas, "
          f"{len(sin_usar)} sin usar en el codigo")
    if sin_usar:
        print("  aviso: sin usar: " + ", ".join(sin_usar))

    if errores:
        print(f"\n{len(errores)} error(es):")
        for e in errores:
            if e:
                print(f"  {e}")
        return 1

    print("Traducciones OK.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
