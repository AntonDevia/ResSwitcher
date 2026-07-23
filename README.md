# ResSwitcher

**Русский** · [English](#english)

Авто-смена разрешения экрана для stalzone. Фоновый сервис
(`ResSwitcher.exe`) автоматически переключает разрешение при запуске игры и
возвращает системное при сворачивании (alt-tab) или закрытии игры. Настройки —
через отдельную прогу `Settings.exe`.


## Возможности

- Автоматическое переключение на целевое разрешение, когда игра в фокусе.
- Возврат к системному разрешению при alt-tab и при выходе из игры (без
  «залипаний» — при закрытии сервиса разрешение принудительно восстанавливается).
- Пресеты соотношения сторон (4:3 / 16:9 / 16:10) с показом только тех режимов,
  которые реально поддерживает монитор, либо своё произвольное разрешение и частота.
- Автозапуск вместе с Windows (ярлык в текущем профиле, без прав администратора).

## Быстрый старт

1. Скачать архив из раздела [**Releases**](../../releases/latest).
2. Распаковать в любую папку (например, на рабочий стол или в папку игры. Файлы `ResSwitcher.exe` и `Settings.exe` надо держать в одной папке.
3. Запустить `Settings.exe`, задать разрешение и нажать «Сохранить».
4. Запустить `ResSwitcher.exe` — появится иконка в трее.
5. Запустить игру. Разрешение переключится автоматически.

## Настройка

Всё через `Settings.exe`:

1. Выбери плитку соотношения сторон (4:3 / 16:9 / 16:10) **или** включи «Своё
   разрешение» и впиши ширину/высоту вручную.
2. При желании включи «Свою частоту обновления» (по умолчанию берётся текущая
   частота монитора).
3. Включи «Запускать ResSwitcher при входе в Windows», если нужен автозапуск.
4. Нажми «Сохранить».

Настройки хранятся в `config.json` рядом с `.exe`. Сервис подхватывает изменения
на лету, перезапускать его не нужно.

Иконка в трее по правой кнопке даёт меню: перезагрузить конфиг, открыть Settings,
выход.

## Структура репозитория

```
ResSwitcher.sln
ResSwitcher.Core/       — общая библиотека
ResSwitcher.Service/    — ResSwitcher.exe
ResSwitcher.Settings/   — Settings.exe
```

---

Ник разработчика — **МС_БЕТОН**

---

## English

[Русский](#resswitcher) · **English**

Automatic screen-resolution switcher for stalzone. A background service
(`ResSwitcher.exe`) automatically switches the resolution when the game starts
and restores the system resolution when the game is minimized (alt-tab) or
closed. Configuration is done through a separate `Settings.exe` app.


## Features

- Automatic switch to the target resolution while the game is focused.
- Restores the system resolution on alt-tab and on game exit (no "stuck"
  resolution — the service force-restores it when it shuts down).
- Aspect-ratio presets (4:3 / 16:9 / 16:10) showing only the modes your monitor
  actually supports, or your own custom resolution and refresh rate.
- Start with Windows (a per-user shortcut, no administrator rights).

## Quick start

1. Download the archive from [**Releases**](../../releases/latest).
2. Extract it into any folder (e.g. your desktop or the game folder). The files
   `ResSwitcher.exe` and `Settings.exe` must be kept in the same folder.
3. Run `Settings.exe`, set the resolution, and click Save.
4. Run `ResSwitcher.exe` — a tray icon appears.
5. Launch the game. The resolution switches automatically.

## Configuration

Everything is in `Settings.exe`:

1. Pick an aspect-ratio tile (4:3 / 16:9 / 16:10) **or** enable "Custom
   resolution" and enter width/height manually.
2. Optionally enable a custom refresh rate (defaults to the monitor's current one).
3. Enable "Start ResSwitcher with Windows" if you want autostart.
4. Click Save.

Settings are stored in `config.json` next to the `.exe`. The service picks up
changes live — no need to restart it.

Right-clicking the tray icon opens a menu: reload config, open Settings, exit.

## Repository layout

```
ResSwitcher.sln
ResSwitcher.Core/       — shared library
ResSwitcher.Service/    — ResSwitcher.exe
ResSwitcher.Settings/   — Settings.exe
```

---

Developer nickname — **МС_БЕТОН**
