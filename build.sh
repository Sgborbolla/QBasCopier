#!/usr/bin/env bash
# QBasCopier - build.sh
# Uso:
#   ./build.sh                    -> publica para el SO actual (linux-x64 / osx-arm64)
#   ./build.sh linux-x64          -> fuerza RID
#   ./build.sh <rid> install      -> publica y ademas instala en ~/.local
# Salida: dist/Linux/QBasCopier  o  dist/macOS/QBasCopier (localizacion segun SO)
set -euo pipefail
cd "$(dirname "$0")"

command -v dotnet >/dev/null 2>&1 || { echo "ERROR: .NET SDK 10 no encontrado. https://dotnet.microsoft.com/download/dotnet/10.0"; exit 1; }

case "$(uname -s)" in
  Darwin) SYS=macOS;;
  Linux) SYS=Linux;;
  *) SYS=Linux;;
esac
ARCH=$(uname -m); [ "$ARCH" = "x86_64" ] && ARCH=x64
RID="${1:-${SYS,,}-$ARCH}"
MODE="${2:-build}"

echo "[1/3] Publicando QBasCopier ($RID, self-contained)..."
mkdir -p "dist/$SYS"
dotnet publish "QBasCopier/QBasCopier.csproj" -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o "dist/$SYS"
echo "[2/3] LISTO: dist/$SYS/QBasCopier"

if [ "$MODE" = "install" ]; then
  echo "[3/3] Instalando en ~/.local ..."
  mkdir -p "$HOME/.local/bin"
  cp "dist/$SYS/QBasCopier" "$HOME/.local/bin/QBasCopier"
  chmod +x "$HOME/.local/bin/QBasCopier"
  [ -f install-desktop.sh ] && bash install-desktop.sh || true
  echo "Instalado. Reabre la sesion para el menu/icono."
else
  echo "[3/3] Para instalar: ./build.sh $RID install"
  echo "     En macOS usa ./make-app.sh para el bundle .app"
fi