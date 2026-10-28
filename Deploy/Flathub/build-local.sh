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
BUILD_DIR="${SCRIPT_DIR}/build-dir"
REPO_DIR="${SCRIPT_DIR}/repo"
LOCAL_REMOTE="corebus-local"

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
python3 << EOF
import json
import sys
from pathlib import Path

try:
    import yaml
except ImportError:
    print("Нужен python3-yaml", file=sys.stderr)
    sys.exit(1)

manifest_path = Path(r"""${MANIFEST_YML}""")
nuget_path = Path(r"""${NUGET_SOURCES}""")
out_path = Path(r"""${MANIFEST_RESOLVED}""")
repo_root = Path(r"""${REPO_ROOT}""")

with manifest_path.open(encoding="utf-8") as f:
    manifest = yaml.safe_load(f)

if not isinstance(manifest, dict) or not manifest.get("modules"):
    print(f"Некорректный манифест: {manifest_path}", file=sys.stderr)
    sys.exit(1)

with nuget_path.open(encoding="utf-8") as f:
    nuget = json.load(f)

if not isinstance(nuget, list):
    print(
        f"{nuget_path} должен быть JSON-массивом, получено: {type(nuget).__name__}",
        file=sys.stderr,
    )
    sys.exit(1)

# Для локальной сборки не читаем sources из YAML (хрупкий include) —
# всегда: исходники с диска + содержимое nuget-sources.json.
module = manifest["modules"][0]
module["sources"] = [
    {"type": "dir", "path": str(repo_root)},
    *nuget,
]

with out_path.open("w", encoding="utf-8") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")

print(f"Готово: {out_path} (nuget packages: {len(nuget)})")
EOF

echo "==> Сборка и установка Flatpak"
# Относительные пути: абсолютные с пробелом («Рабочий стол») ломают
# разбор аргументов у flatpak-builder внутри org.flatpak.Builder.
# Явно --filesystem=host, чтобы был доступ к каталогу проекта.
flatpak run --filesystem=host --command=flatpak-builder org.flatpak.Builder \
  --force-clean \
  --user \
  --install-deps-from=flathub \
  --repo=repo \
  build-dir \
  manifest.resolved.json

# Для remote нужен абсолютный путь к repo
flatpak --user remote-add --no-gpg-verify --if-not-exists "${LOCAL_REMOTE}" "${REPO_DIR}"

if flatpak --user info "${APP_ID}" >/dev/null 2>&1; then
  flatpak --user uninstall -y "${APP_ID}" || true
fi

flatpak --user install -y "${LOCAL_REMOTE}" "${APP_ID}"

echo "==> Установлено: ${APP_ID}"
echo "Запуск: flatpak run ${APP_ID}"

if [[ "${RUN_APP}" -eq 1 ]]; then
  echo "==> Запуск приложения"
  flatpak run "${APP_ID}"
fi
