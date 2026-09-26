#!/usr/bin/env bash
# QBasWing Shuttle · QBasCopier y Transfer - make-app.sh
# Crea el bundle .app para macOS (solo en un Mac con .NET SDK 10).
#   ./make-app.sh osx-arm64   (Apple Silicon)
#   ./make-app.sh osx-x64     (Intel)
set -euo pipefail
cd "$(dirname "$0")"

command -v dotnet >/dev/null 2>&1 || { echo "ERROR: .NET SDK 10 no encontrado."; exit 1; }
RID="${1:-osx-arm64}"

echo "[1/3] Publicando ($RID)..."
dotnet publish "QBasCopier/QBasCopier.csproj" -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o "dist/app"

APP="dist/QBasWing-Shuttle.app"
echo "[2/3] Creando bundle..."
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cat > "$APP/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>QBasWing Shuttle · QBasCopier y Transfer</string>
  <key>CFBundleDisplayName</key><string>QBasWing Shuttle · QBasCopier y Transfer</string>
  <key>CFBundleIdentifier</key><string>com.qbaswing.qbascopier</string>
  <key>CFBundleVersion</key><string>1.0</string>
  <key>CFBundleShortVersionString</key><string>1.0</string>
  <key>CFBundleExecutable</key><string>QBasWing Shuttle · QBasCopier y Transfer</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
EOF
cp -f "dist/app/QBasCopier" "$APP/Contents/MacOS/QBasWing-Shuttle"
cp -f "QBasCopier/Assets/logo.png" "$APP/Contents/Resources/logo.png" 2>/dev/null || true
chmod +x "$APP/Contents/MacOS/QBasWing-Shuttle"

echo "[3/3] Firmando (ad-hoc)..."
codesign --force --deep --sign - "$APP" 2>/dev/null || echo "  (codesign no disponible, se omite)"
echo "LISTO: dist/QBasWing-Shuttle.app  (arrastralo a /Applications)"