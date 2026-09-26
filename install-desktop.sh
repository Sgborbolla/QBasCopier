#!/usr/bin/env bash
# QBasWing Shuttle · QBasCopier y Transfer - instalacion para Linux (escritorio + Nautilus), integra el
# boton "Copiar" / "Mover" en el menu contextual de Nautilus.
set -euo pipefail
cd "$(dirname "$0")"

VER="1.0"
BIN="$HOME/.local/bin/QBasWing-Shuttle"
[ -x "$BIN" ] || { echo "ERROR: primero haz ./build.sh <rid> install"; exit 1; }

mkdir -p "$HOME/.local/share/applications"
cat > "$HOME/.local/share/applications/QBasWing-Shuttle.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=QBasWing Shuttle · QBasCopier y Transfer
Comment=Copiar/Mover rapido (QBasWinG)
Exec=$BIN
Icon=$HOME/.local/share/icons/QBasWing Shuttle · QBasCopier y Transfer.png
Terminal=false
Categories=Utility;System;FileTools;
EOF

mkdir -p "$HOME/.local/share/icons"
cp -f QBasCopier/Assets/logo.png "$HOME/.local/share/icons/QBasWing Shuttle · QBasCopier y Transfer.png" 2>/dev/null || true

# Nautilus: copiar / mover a una carpeta
NAUTDIR="$HOME/.local/share/nautilus/scripts"
mkdir -p "$NAUTDIR"
cat > "$NAUTDIR/QBasWing Shuttle · QBasCopier y Transfer Copiar" <<EOF
#!/usr/bin/env bash
sleep 0.2
"$BIN" --copy --dest-last \$(printf '%s\n' "\$NAUTILUS_SCRIPT_SELECTED_FILE_PATHS") &
EOF
cat > "$NAUTDIR/QBasWing Shuttle · QBasCopier y Transfer Mover" <<EOF
#!/usr/bin/env bash
sleep 0.2
"$BIN" --move --dest-last \$(printf '%s\n' "\$NAUTILUS_SCRIPT_SELECTED_FILE_PATHS") &
EOF
chmod +x "$NAUTDIR/QBasWing Shuttle · QBasCopier y Transfer Copiar" "$NAUTDIR/QBasWing Shuttle · QBasCopier y Transfer Mover"

update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true
echo "Integracion Linux lista: menu + icono + Nautilus (menus: Scripts > QBasWing Shuttle · QBasCopier y Transfer Copiar/Mover)."
