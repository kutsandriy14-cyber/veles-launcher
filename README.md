# Veles Launcher

Нативный лаунчер для одного сервера Veles PlayGame. Название, версия Minecraft, модлоадер, адрес сервера и описание сборки не зашиты в клиент: они читаются из последнего публичного GitHub Release репозитория сборок. Проект рассчитан на Windows 7–11 и написан на C# WinForms под .NET Framework 4.8.

## Состав

Решение `VelesLauncher.sln` содержит четыре проекта. `Veles.Core` — общая библиотека, `Veles.Launcher` — пользовательский лаунчер со встроенным обновлением Minecraft-сборки, `Veles.BuildPublisher` — отдельная админ-панель публикации, а `Veles.Updater` — служебная программа обновления самого лаунчера, устанавливаемая рядом с ним внутри главного setup.

| Компонент | Репозиторий и назначение |
|---|---|
| Veles Launcher | `kutsandriy14-cyber/veles-launcher`; пользовательское приложение и его обновления |
| Veles Build Publisher | Отдельный проект и setup; публикует `build.zip` и `build-info.txt` в `kutsandriy14-cyber/veles-modpack-releases` |
| Veles Launcher Updater | Отдельный EXE внутри `VelesLauncherSetup.exe`; ищет `VelesLauncherSetup.exe` в последнем релизе лаунчера |
| Minecraft-сборки | `kutsandriy14-cyber/veles-modpack-releases`; публичные GitHub Releases |

## Сценарий игрока

При запуске лаунчер получает последний релиз сборки и проверяет `build-info.txt`. Если сборка отсутствует или устарела, запуск Minecraft заблокирован до обновления. Кнопка обновления скачивает архив, проверяет SHA-256, распаковывает его встроенными средствами .NET и атомарно заменяет старый экземпляр. 7-Zip, `start.bat`, `cmd.exe` и ручные команды не требуются.

Релиз должен содержать `launch.json` и встроенный Java runtime в `runtime/java/bin/javaw.exe` либо `java.exe`. После установки лаунчер использует эту Java и запускает валидированный профиль напрямую. В меню «Настройки» игрок может выбрать папку экземпляра и минимальный/максимальный объём RAM; настройки хранятся в `%AppData%\Veles Launcher\settings.json`.

Если GitHub пока не содержит корректной сборки, игрок видит нейтральное сообщение «Сборка сервера пока не опубликована», версию `—` и отключённую кнопку запуска. Технические детали GitHub API в пользовательский интерфейс не выводятся.

## Сборка и релизы

Сборка выполняется в Visual Studio на Windows с workload «.NET desktop development». Откройте `VelesLauncher.sln`, выберите `Release` и соберите нужный проект. Windows CI создаёт только два распространяемых setup-файла: `VelesLauncherSetup.exe` с Launcher, Updater и Core в одной папке и `VelesBuildPublisherSetup.exe` для отдельной админ-панели.

Для автоматической сборки Windows-артефактов предусмотрен `.github/workflows/windows-build.yml`. После публикации релиза лаунчера asset должен называться `VelesLauncherSetup.exe`, чтобы служебный Updater, установленный рядом с Launcher, мог его найти.

## Токен админ-панели

Для публикации требуется GitHub Fine-grained Personal Access Token с правом `Contents: Read and write` только для репозитория `veles-modpack-releases`. Токен нельзя вставлять в исходный код, TXT-файлы сборок или клиентский лаунчер. В пользовательском Launcher GitHub-токен не нужен.
