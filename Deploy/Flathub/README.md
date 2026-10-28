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

Потребуются Flatpak, Python 3 и сеть. Скрипт поставит SDK `25.08` и extension `dotnet10`.

## Локальная сборка и проверка

```bash
cd Deploy/Flathub

flatpak remote-add --if-not-exists --user flathub \
  https://dl.flathub.org/repo/flathub.flatpakrepo
flatpak install -y flathub org.flatpak.Builder

flatpak run --command=flatpak-builder org.flatpak.Builder \
  --force-clean --user --install-deps-from=flathub \
  --repo=repo build-dir \
  io.github.AndreyAbdulkayumov.CoreBus.yml

flatpak --user remote-add --no-gpg-verify --if-not-exists corebus-local repo
flatpak --user install -y corebus-local io.github.AndreyAbdulkayumov.CoreBus
flatpak run io.github.AndreyAbdulkayumov.CoreBus
```

Lint:

```bash
flatpak run --command=flatpak-builder-lint org.flatpak.Builder \
  manifest io.github.AndreyAbdulkayumov.CoreBus.yml
flatpak run --command=flatpak-builder-lint org.flatpak.Builder repo repo
flatpak run --command=flatpak-builder-lint org.flatpak.Builder \
  appstream io.github.AndreyAbdulkayumov.CoreBus.metainfo.xml
```

Проверьте COM-порты и Modbus TCP внутри sandbox.

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
