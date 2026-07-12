# ResSwitcher

**Русский** · [English](#english)

Авто-смена разрешения экрана для STALCRAFT / SlatZone. Фоновый сервис в трее
(`ResSwitcher.exe`) автоматически переключает разрешение при запуске игры и
возвращает системное при сворачивании (alt-tab) или закрытии игры. Настройки —
через отдельное окно `Settings.exe`.

Написано на C# / .NET 8 (WinForms-трей + WPF-настройки), работает без прав
администратора.

## Возможности

- Автоматическое переключение на целевое разрешение, когда игра в фокусе.
- Возврат к системному разрешению при alt-tab и при выходе из игры (без
  «залипаний» — при закрытии сервиса разрешение принудительно восстанавливается).
- Пресеты соотношения сторон (4:3 / 16:9 / 16:10) с показом только тех режимов,
  которые реально поддерживает монитор, либо своё произвольное разрешение и частота.
- Список отслеживаемых процессов игры настраивается.
- Опциональный «двойной F11» — чинит обрезанный кадр у некоторых движков после
  возврата в игру.
- Автозапуск вместе с Windows (ярлык в текущем профиле, без прав администратора).

## Быстрый старт (готовые .exe)

1. Скачать архив из раздела [**Releases**](../../releases/latest).
2. Распаковать в любую папку (например, на рабочий стол или в
   `C:\Program Files\ResSwitcher\`). Держи `ResSwitcher.exe` и `Settings.exe`
   рядом — они находят друг друга по соседству.
3. Запустить `Settings.exe`, задать разрешение и нажать «Сохранить».
4. Запустить `ResSwitcher.exe` — появится иконка в трее.
5. Запустить игру. Разрешение переключится автоматически.

> Готовые сборки self-contained: .NET на целевой машине **не нужен**, всё зашито
> в `.exe`. При первом запуске Windows SmartScreen может предупредить о
> неподписанном файле — «Подробнее» → «Выполнить в любом случае».

## Настройка

Всё через `Settings.exe`:

1. Выбери плитку соотношения сторон (4:3 / 16:9 / 16:10) **или** включи «Своё
   разрешение» и впиши ширину/высоту вручную.
2. При желании включи «Свою частоту обновления» (по умолчанию берётся текущая
   частота монитора).
3. Проверь список процессов игры (по умолчанию:
   `stalzone.exe, stalzonew.exe, stalcraft.exe, stalcraftw.exe`). Если у тебя
   игра называется иначе — впиши точное имя exe из Диспетчера задач.
4. Включи «Запускать ResSwitcher при входе в Windows», если нужен автозапуск.
5. Нажми «Сохранить».

Настройки хранятся в `config.json` рядом с `.exe`. Сервис подхватывает изменения
на лету, перезапускать его не нужно.

Иконка в трее по правой кнопке даёт меню: перезагрузить конфиг, открыть Settings,
выход.

## Сборка из исходников

Нужен **.NET 8 SDK** (именно SDK, не Runtime):

```powershell
winget install --id Microsoft.DotNet.SDK.8 -e
```

Открой **новое** окно терминала и проверь: `dotnet --version` → `8.0.x`.

Сборка и запуск для проверки (Debug):

```powershell
dotnet build ResSwitcher.sln -c Debug
```

Финальная self-contained сборка (не требует .NET на целевой машине):

```powershell
dotnet publish ResSwitcher.Service\ResSwitcher.Service.csproj  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

dotnet publish ResSwitcher.Settings\ResSwitcher.Settings.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Обе команды кладут результат в общую папку `publish\` — это важно, `.exe` ищут
друг друга по соседству. Размер каждого exe ~60–170 МБ (внутри весь рантайм) —
это нормально для self-contained .NET.

## Структура репозитория

```
ResSwitcher.sln
ResSwitcher.Core/       — общая библиотека (WinAPI, конфиг, state machine)
ResSwitcher.Service/    — ResSwitcher.exe, фоновый трей-сервис
ResSwitcher.Settings/   — Settings.exe, GUI-конфигуратор (WPF)
```

## Известные ограничения

| Ситуация | Поведение |
|---|---|
| Первый запуск с чужого ПК | SmartScreen может ругаться на неподписанный exe — это ожидаемо. |
| Антивирус | Изредка ложная тревога на self-contained .NET exe — вредоносного кода нет. |
| Права администратора | Не нужны: `ChangeDisplaySettingsEx` работает от обычного пользователя. |
| Несколько мониторов | Разрешение меняется на основном (primary) мониторе. |

## Лицензия

MIT — см. [LICENSE](LICENSE).

---

## English

[Русский](#resswitcher) · **English**

Automatic screen-resolution switcher for STALCRAFT / SlatZone. A background tray
service (`ResSwitcher.exe`) switches to your target resolution when the game is
focused and restores the system resolution on alt-tab or when the game exits.
Configuration is done through a separate `Settings.exe` window.

Written in C# / .NET 8 (WinForms tray + WPF settings). Runs without administrator
rights.

## Features

- Switches to the target resolution while the game is focused.
- Restores the system resolution on alt-tab and on game exit (no "stuck"
  resolution — the service force-reverts when it shuts down).
- Aspect-ratio presets (4:3 / 16:9 / 16:10) showing only modes your monitor
  actually supports, or a fully custom resolution and refresh rate.
- Configurable list of watched game process names.
- Optional "double F11" fix for engines that render a cropped frame after
  regaining focus.
- Optional start with Windows (per-user shortcut, no admin rights).

## Quick start (prebuilt .exe)

1. Download the archive from [**Releases**](../../releases/latest).
2. Extract anywhere (e.g. your desktop or `C:\Program Files\ResSwitcher\`). Keep
   `ResSwitcher.exe` and `Settings.exe` next to each other — they locate each
   other by proximity.
3. Run `Settings.exe`, set your resolution, and click Save.
4. Run `ResSwitcher.exe` — a tray icon appears.
5. Launch the game. The resolution switches automatically.

> Prebuilt binaries are self-contained: **no .NET install required** on the
> target machine. On first run Windows SmartScreen may warn about an unsigned
> file — click "More info" → "Run anyway".

## Configuration

Everything is in `Settings.exe`:

1. Pick an aspect-ratio tile (4:3 / 16:9 / 16:10) **or** enable "Custom
   resolution" and enter width/height manually.
2. Optionally enable a custom refresh rate (defaults to the monitor's current one).
3. Check the game process list (default:
   `stalzone.exe, stalzonew.exe, stalcraft.exe, stalcraftw.exe`). If your game's
   executable differs, enter its exact name from Task Manager.
4. Enable "Start ResSwitcher with Windows" if you want autostart.
5. Click Save.

Settings live in `config.json` next to the `.exe`. The service picks up changes
live — no restart needed. Right-click the tray icon for reload / open settings /
exit.

## Build from source

Requires the **.NET 8 SDK** (SDK, not just the Runtime):

```powershell
winget install --id Microsoft.DotNet.SDK.8 -e
```

Open a **new** terminal and verify: `dotnet --version` → `8.0.x`.

Debug build for testing:

```powershell
dotnet build ResSwitcher.sln -c Debug
```

Final self-contained build (no .NET needed on the target machine):

```powershell
dotnet publish ResSwitcher.Service\ResSwitcher.Service.csproj  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

dotnet publish ResSwitcher.Settings\ResSwitcher.Settings.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Both commands output to the same `publish\` folder — important, since the `.exe`
files locate each other by proximity. Each exe is ~60–170 MB (bundled runtime),
which is expected for self-contained .NET.

## Repository layout

```
ResSwitcher.sln
ResSwitcher.Core/       — shared library (WinAPI, config, state machine)
ResSwitcher.Service/    — ResSwitcher.exe, background tray service
ResSwitcher.Settings/   — Settings.exe, GUI configurator (WPF)
```

## Known limitations

| Situation | Behaviour |
|---|---|
| First run from another PC | SmartScreen may warn about an unsigned exe — expected. |
| Antivirus | Occasional false positive on self-contained .NET exe — no malicious code. |
| Administrator rights | Not required: `ChangeDisplaySettingsEx` works as a normal user. |
| Multiple monitors | Resolution is changed on the primary monitor. |

## License

MIT — see [LICENSE](LICENSE).
