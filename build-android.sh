#!/usr/bin/env bash
# QBasCopier - build-android.sh
# Genera el APK en una PC con .NET SDK 8 (LTS, Android estable) + workload android.
#   ./build-android.sh
# APK resultante: QBasCopier.Android/bin/Release/net8.0-android/*.apk
set -euo pipefail
cd "$(dirname "$0")"

command -v dotnet >/dev/null 2>&1 || { echo "ERROR: .NET SDK 8 no encontrado."; exit 1; }

if ! dotnet workload list 2>/dev/null | grep -q android; then
  echo "Instalando workload android (una sola vez)..."
  dotnet workload install android
fi

echo "[1/2] Compilando APK (net8.0-android)..."
dotnet build "QBasCopier.Android/QBasCopier.Android.csproj" -c Release -t:SignAndroidPackage

echo "[2/2] LISTO. Busca el .apk en:"
ls -1 QBasCopier.Android/bin/Release/net8.0-android/*.apk 2>/dev/null || true