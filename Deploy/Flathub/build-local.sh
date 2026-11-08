#!/usr/bin/env bash
# Локальная сборка и установка Flatpak-пакета CoreBus (не для PR в Flathub).
# Запуск из любой директории:
#   ./Deploy/Flathub/build-local.sh
#   ./Deploy/Flathub/build-local.sh --run
#   ./Deploy/Flathub/build-local.sh --skip-nuget   # не перегенерировать nuget-sources.json
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
MANIFEST_YML="${SCRIPT_DIR}/io.github.AndreyAbdulkayumov.CoreBus.yml"
MANIFEST_RESOLVED="${SCRIPT_DIR}/manifest.resolved.json"
NUGET_SOURCES="${SCRIPT_DIR}/nuget-sources.json"
APP_ID="io.github.AndreyAbdulkayumov.CoreBus"
# Рабочие папки вне репозитория: иначе они копируются в сборку как часть
# источника type: dir при каждом следующем запуске.
WORK_DIR="${XDG_CACHE_HOME:-${HOME}/.cache}/corebus-flatpak"
BUILD_DIR="${WORK_DIR}/build-dir"
REPO_DIR="${WORK_DIR}/repo"
STATE_DIR="${WORK_DIR}/.flatpak-builder"

RUN_APP=0
SKIP_NUGET=0
for arg in "$@"; do
  case "${arg}" in
    --run) RUN_APP=1 ;;
    --skip-nuget) SKIP_NUGET=1 ;;
    -h|--help)
      echo "Usage: $0 [--run] [--skip-nuget]"
      exit 0
      ;;
    *)
      echo "Неизвестный аргумент: ${arg}" >&2
      exit 1
      ;;
  esac
done

cd "${SCRIPT_DIR}"

need_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Не найдена команда: $1" >&2
    echo "Установите: sudo apt install -y $2" >&2
    exit 1
  fi
}

need_cmd flatpak flatpak
need_cmd python3 python3
need_cmd curl curl

if ! python3 -c "import yaml" 2>/dev/null; then
  echo "Не найден модуль PyYAML." >&2
  echo "Установите: sudo apt install -y python3-yaml" >&2
  exit 1
fi

if [[ ! -f "${MANIFEST_YML}" ]]; then
  echo "Не найден манифест: ${MANIFEST_YML}" >&2
  exit 1
fi

echo "==> Flathub remote и org.flatpak.Builder"
flatpak remote-add --if-not-exists --user flathub \
  https://dl.flathub.org/repo/flathub.flatpakrepo
flatpak install -y --user flathub org.flatpak.Builder

if [[ "${SKIP_NUGET}" -eq 0 ]]; then
  echo "==> Генерация nuget-sources.json"
  chmod +x "${SCRIPT_DIR}/generate-nuget-sources.sh"
  "${SCRIPT_DIR}/generate-nuget-sources.sh"
elif [[ ! -f "${NUGET_SOURCES}" ]]; then
  echo "Нет ${NUGET_SOURCES}. Запустите без --skip-nuget." >&2
  exit 1
else
  echo "==> Пропуск генерации nuget-sources.json (--skip-nuget)"
fi

echo "==> Сборка манифеста для локальной проверки (type: dir + inline nuget)"
python3 - "${MANIFEST_YML}" "${NUGET_SOURCES}" "${MANIFEST_RESOLVED}" "${REPO_ROOT}" << 'EOF'
import json
import sys
from pathlib import Path

try:
    import yaml
except ImportError:
    sys.exit("Нужен python3-yaml")

manifest_path, nuget_path, out_path, repo_root = (Path(p) for p in sys.argv[1:5])

with manifest_path.open(encoding="utf-8") as f:
    manifest = yaml.safe_load(f)

if not isinstance(manifest, dict) or not manifest.get("modules"):
    sys.exit(f"Некорректный манифест: {manifest_path}")

with nuget_path.open(encoding="utf-8") as f:
    nuget = json.load(f)

if not isinstance(nuget, list) or not nuget:
    sys.exit(f"{nuget_path} должен быть непустым JSON-массивом")

# В сборку не копируем git-метаданные, артефакты релизов и bin/obj проектов:
# skip принимает только точные пути относительно path, без масок.
skip = [".git", ".idea", "Deploy/Artifacts"]
for csproj in repo_root.glob("*/*.csproj"):
    for folder in ("bin", "obj"):
        if (csproj.parent / folder).is_dir():
            skip.append(f"{csproj.parent.name}/{folder}")

# Для локальной сборки не читаем sources из YAML (хрупкий include) —
# всегда: исходники с диска + содержимое nuget-sources.json.
# path относительно манифеста, который лежит в Deploy/Flathub.
module = manifest["modules"][0]
module["sources"] = [
    {"type": "dir", "path": "../..", "skip": skip},
    *nuget,
]

with out_path.open("w", encoding="utf-8") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")

print(f"Готово: {out_path} (nuget packages: {len(nuget)}, skip: {len(skip)})")
EOF

echo "==> Сборка и установка Flatpak"
mkdir -p "${WORK_DIR}"
# Явно --filesystem=host, чтобы был доступ к каталогу проекта и к WORK_DIR.
# --install ставит приложение в user-инсталляцию сразу из собранного repo.
# --mirror-screenshots-url повторяет сборку Flathub, иначе lint repo ругается
# на незеркалированные скриншоты.
flatpak run --filesystem=host --command=flatpak-builder org.flatpak.Builder \
  --force-clean \
  --user \
  --install \
  --install-deps-from=flathub \
  --mirror-screenshots-url=https://dl.flathub.org/media/ \
  --state-dir="${STATE_DIR}" \
  --repo="${REPO_DIR}" \
  "${BUILD_DIR}" \
  manifest.resolved.json

echo "==> Установлено: ${APP_ID}"
echo "Запуск: flatpak run ${APP_ID}"
echo "Lint repo: flatpak run --command=flatpak-builder-lint org.flatpak.Builder repo ${REPO_DIR}"

if [[ "${RUN_APP}" -eq 1 ]]; then
  echo "==> Запуск приложения"
  flatpak run "${APP_ID}"
fi
