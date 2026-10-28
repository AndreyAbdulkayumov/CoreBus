#!/usr/bin/env bash
# Генерация nuget-sources.json для офлайн-сборки Flatpak.
# Запускать в Linux при установленных Flatpak SDK и extension dotnet10.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
GENERATOR_URL="https://raw.githubusercontent.com/flatpak/flatpak-builder-tools/master/dotnet/flatpak-dotnet-generator.py"
GENERATOR_PATH="${SCRIPT_DIR}/.flatpak-dotnet-generator.py"
OUTPUT="${SCRIPT_DIR}/nuget-sources.json"
CSPROJ="${REPO_ROOT}/CoreBus.Desktop/CoreBus.Desktop.csproj"
FREEDESKTOP_VERSION="25.08"
DOTNET_VERSION="10"

if [[ ! -f "${CSPROJ}" ]]; then
  echo "Не найден проект: ${CSPROJ}" >&2
  exit 1
fi

echo "Установка SDK и extension (если ещё нет)..."
flatpak remote-add --if-not-exists --user flathub https://dl.flathub.org/repo/flathub.flatpakrepo
flatpak install -y --user \
  "org.freedesktop.Sdk//${FREEDESKTOP_VERSION}" \
  "org.freedesktop.Sdk.Extension.dotnet10//${FREEDESKTOP_VERSION}"

echo "Скачивание flatpak-dotnet-generator..."
curl -fsSL "${GENERATOR_URL}" -o "${GENERATOR_PATH}"

echo "Генерация ${OUTPUT} (linux-x64 и linux-arm64)..."
# Позиционные output/project до --runtime: иначе nargs="+" съедает их как runtime.
# printf отвечает на вопрос flatpak «user или system», если extension установлен дважды.
printf '1\n1\n' | python3 "${GENERATOR_PATH}" \
  --dotnet "${DOTNET_VERSION}" \
  --freedesktop "${FREEDESKTOP_VERSION}" \
  "${OUTPUT}" \
  "${CSPROJ}" \
  --runtime linux-x64 linux-arm64

echo "Готово: ${OUTPUT}"
echo "Скопируйте этот файл вместе с манифестом в PR Flathub."
