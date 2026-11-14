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
# Должно совпадать с runtime-version в манифесте.
FREEDESKTOP_VERSION="26.08"
DOTNET_VERSION="10"
# Flathub собирает x86_64 и aarch64.
RUNTIMES=(linux-x64 linux-arm64)

if [[ ! -f "${CSPROJ}" ]]; then
  echo "Не найден проект: ${CSPROJ}" >&2
  exit 1
fi

# Генератор создаёт временную папку в текущей директории, она должна быть видна
# внутри sandbox (--filesystem=host не покрывает /tmp).
cd "${SCRIPT_DIR}"

echo "Установка SDK и extension (если ещё нет)..."
flatpak remote-add --if-not-exists --user flathub https://dl.flathub.org/repo/flathub.flatpakrepo
flatpak install -y --user \
  "org.freedesktop.Sdk//${FREEDESKTOP_VERSION}" \
  "org.freedesktop.Sdk.Extension.dotnet10//${FREEDESKTOP_VERSION}"

echo "Скачивание flatpak-dotnet-generator..."
curl -fsSL "${GENERATOR_URL}" -o "${GENERATOR_PATH}"

# Генератор запускает restore для всех runtime параллельно в одном obj/ и не
# проверяет код возврата, поэтому каждый runtime обрабатывается отдельно.
PART_FILES=()
for runtime in "${RUNTIMES[@]}"; do
  part="${SCRIPT_DIR}/.nuget-sources.${runtime}.json"
  echo "Генерация для ${runtime}..."
  # Позиционные output/project до --runtime: иначе nargs="+" съедает их как runtime.
  # printf отвечает на вопрос flatpak «user или system», если extension установлен дважды.
  printf '1\n' | python3 "${GENERATOR_PATH}" \
    --dotnet "${DOTNET_VERSION}" \
    --freedesktop "${FREEDESKTOP_VERSION}" \
    "${part}" \
    "${CSPROJ}" \
    --runtime "${runtime}"
  PART_FILES+=("${part}")
done

echo "Объединение в ${OUTPUT}..."
python3 - "${OUTPUT}" "${PART_FILES[@]}" << 'EOF'
import json
import sys

output, *parts = sys.argv[1:]
merged = {}

for part in parts:
    with open(part, encoding="utf-8") as f:
        sources = json.load(f)

    if not sources:
        sys.exit(f"Пустой результат restore: {part}. Смотрите вывод dotnet restore выше.")

    for source in sources:
        merged[source["dest-filename"]] = source

with open(output, "w", encoding="utf-8") as f:
    json.dump(sorted(merged.values(), key=lambda s: s["dest-filename"]), f, indent=4)
    f.write("\n")

print(f"Пакетов: {len(merged)}")
EOF

rm -f "${PART_FILES[@]}"

echo "Готово: ${OUTPUT}"
echo "Скопируйте этот файл вместе с манифестом в PR Flathub."
