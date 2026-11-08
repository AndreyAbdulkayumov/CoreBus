# Публикация на Flathub

https://flathub.org/

App ID: `io.github.AndreyAbdulkayumov.CoreBus`

## Описание файлов

| Файл | Назначение |
|------|------------|
| `io.github.AndreyAbdulkayumov.CoreBus.yml` | Манифест Flatpak (уходит в PR Flathub) |
| `io.github.AndreyAbdulkayumov.CoreBus.desktop` | Desktop Entry |
| `io.github.AndreyAbdulkayumov.CoreBus.metainfo.xml` | AppStream / страница на Flathub |
| `io.github.AndreyAbdulkayumov.CoreBus.png` | Иконка 256×256 |
| `generate-nuget-sources.sh` | Генерация офлайн NuGet-зависимостей |
| `nuget-sources.json` | Результат генерации (в git не коммитится) |
| `build-local.sh` | Локальная сборка и установка для проверки (не для PR) |

Desktop, metainfo и иконка ставятся в `/app` при сборке из исходников репозитория CoreBus. Поэтому в теге/commit, на который ссылается манифест, эта папка уже должна существовать.

## Подготовка релиза

1. Закоммитить изменения в `Deploy/Flathub` в репозиторий CoreBus.
2. Создать git-тег релиза (или использовать commit), в котором есть эти файлы.
3. В манифесте обновить `tag` и `commit` в секции `sources`.
4. В `metainfo.xml` при необходимости добавить блок `<release>`.

Тег `3.5.3` в манифесте — заглушка: в нём ещё нет `Deploy/Flathub`. Перед первой публикацией замените на новый тег/commit.

## Генерация nuget-sources.json

Только в Linux:

```bash
chmod +x Deploy/Flathub/generate-nuget-sources.sh
./Deploy/Flathub/generate-nuget-sources.sh
```

Потребуются Flatpak, Python 3.10+ и сеть. Скрипт поставит SDK `26.08` и extension `dotnet10` (версия задаётся переменной `FREEDESKTOP_VERSION` и должна совпадать с `runtime-version` в манифесте).

Генератор запускается отдельно для `linux-x64` и `linux-arm64`, результаты объединяются в один файл. Runtime-паки и crossgen2 в файл не попадают: их даёт extension из `/usr/lib/sdk/dotnet10/nuget/packages`.

## Локальная сборка и проверка

Самый простой путь — скрипт (Linux, нужны `flatpak`, `python3`, `python3-yaml`, `curl`):

```bash
chmod +x Deploy/Flathub/*.sh
./Deploy/Flathub/build-local.sh          # генерация nuget-sources.json + сборка + установка
./Deploy/Flathub/build-local.sh --run    # то же и сразу запуск
./Deploy/Flathub/build-local.sh --skip-nuget   # без перегенерации nuget-sources.json
```

Скрипт собирает не из git-тега, а из текущих файлов репозитория (источник `type: dir`), поэтому незакоммиченные изменения тоже попадают в сборку. Рабочие папки (`build-dir`, `repo`, `.flatpak-builder`) лежат в `~/.cache/corebus-flatpak`.

Ручной вариант с манифестом из PR (нужен тег/commit с папкой `Deploy/Flathub`):

```bash
cd Deploy/Flathub

flatpak remote-add --if-not-exists --user flathub \
  https://dl.flathub.org/repo/flathub.flatpakrepo
flatpak install -y --user flathub org.flatpak.Builder

flatpak run org.flatpak.Builder \
  --force-clean --sandbox --user --install --install-deps-from=flathub \
  --mirror-screenshots-url=https://dl.flathub.org/media/ \
  --repo=repo build-dir \
  io.github.AndreyAbdulkayumov.CoreBus.yml

flatpak run io.github.AndreyAbdulkayumov.CoreBus
```

Lint (для `repo` после `build-local.sh` путь — `~/.cache/corebus-flatpak/repo`):

```bash
flatpak run --command=flatpak-builder-lint org.flatpak.Builder \
  manifest io.github.AndreyAbdulkayumov.CoreBus.yml
flatpak run --command=flatpak-builder-lint org.flatpak.Builder repo repo
flatpak run --command=flatpak-builder-lint org.flatpak.Builder \
  appstream io.github.AndreyAbdulkayumov.CoreBus.metainfo.xml
```

Что проверить внутри sandbox:

* Окно открывается и на X11, и на Wayland-сессии (Avalonia работает через X11/XWayland, в манифесте `--socket=x11`).
* COM-порты видны и открываются (`--device=all`; пользователь должен состоять в группе `dialout`, как и без Flatpak).
* Modbus TCP (`--share=network`).
* Настройки, логи и макросы создаются в `~/Documents/CoreBus` (`--filesystem=xdg-documents`).
* Диалоги открытия файлов работают через портал.

## Сабмит в Flathub

В PR на https://github.com/flathub/flathub (base branch: **`new-pr`**) нужны только:

* `io.github.AndreyAbdulkayumov.CoreBus.yml`
* `nuget-sources.json`

Инструкция: https://docs.flathub.org/docs/for-app-authors/submission

Заголовок PR: `Add io.github.AndreyAbdulkayumov.CoreBus`

После замечаний рецензентов: комментарий `bot, build`.

## Обновление версии после принятия

В репозитории `flathub/io.github.AndreyAbdulkayumov.CoreBus`:

1. Обновить `tag` / `commit` в манифесте.
2. Перегенерировать `nuget-sources.json`, если изменились зависимости.
3. Обновить `<releases>` в metainfo в upstream (или в том же PR, если metainfo правите только там — у CoreBus metainfo живёт в upstream).
