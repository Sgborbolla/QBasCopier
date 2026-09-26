#!/usr/bin/env bash
# QBasWing Shuttle · QBasCopier y Transfer - build.sh
# Uso:
#   ./build.sh                    -> publica para el SO actual (linux-x64 / osx-arm64)
#   ./build.sh linux-x64          -> fuerza RID
#   ./build.sh <rid> install      -> publica y ademas instala en ~/.local
# Salida: dist/Linux/QBasWing Shuttle · QBasCopier y Transfer  o  dist/macOS/QBasWing Shuttle · QBasCopier y Transfer
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

echo "[1/3] Publicando QBasWing Shuttle · QBasCopier y Transfer ($RID, self-contained)..."
mkdir -p "dist/$SYS"
dotnet publish "QBasCopier/QBasCopier.csproj" -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o "dist/$SYS"
# el apphost conserva el nombre del ensamblado; aqui se renombra al nombre largo
mv "dist/$SYS/QBasCopier" "dist/$SYS/QBasWing-Shuttle" 2>/dev/null || true
echo "[2/3] LISTO: dist/$SYS/QBasWing-Shuttle"

if [ "$MODE" = "install" ]; then
  echo "[3/3] Instalando en ~/.local ..."
  mkdir -p "$HOME/.local/bin"
  cp "dist/$SYS/QBasWing-Shuttle" "$HOME/.local/bin/QBasWing-Shuttle"
  chmod +x "$HOME/.local/bin/QBasWing-Shuttle"
  [ -f install-desktop.sh ] && bash install-desktop.sh || true
  echo "Instalado. Reabre la sesion para el menu/icono."
else
  echo "[3/3] Para instalar: ./build.sh $RID install"
  echo "     En macOS usa ./make-app.sh para el bundle .app"
fi
