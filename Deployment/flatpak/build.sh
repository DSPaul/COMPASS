#!/bin/bash
# Build script for the Flatpak package of COMPASS.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
cd "${REPO_ROOT}"

INSTALL_PREFIX="${FLATPAK_DEST:-${REPO_ROOT}/artifacts/flatpak}"
DOTNET_RUNTIME_VERSION="$(< "${SCRIPT_DIR}/dotnet-runtime-version.txt")"

# flatpak-builder sets FLATPAK_ARCH; Flathub builds each arch natively.
case "${FLATPAK_ARCH:-x86_64}" in
    x86_64)  DOTNET_RID=linux-x64 ;;
    aarch64) DOTNET_RID=linux-arm64 ;;
    *) echo "Unsupported architecture: ${FLATPAK_ARCH}" >&2; exit 1 ;;
esac

dotnet publish Source/COMPASS.Linux/COMPASS.Linux.csproj \
    --configuration Release \
    --runtime "${DOTNET_RID}" \
    --self-contained true \
    --property:PublishReadyToRun=true \
    --property:RuntimeFrameworkVersion="${DOTNET_RUNTIME_VERSION}" \
    --source ./nuget-sources \
    --output "${INSTALL_PREFIX}/bin/"
