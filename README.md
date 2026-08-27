# Veles Launcher

Нативный лаунчер для сервера Veles PlayGame и сборки TerraFirmaGreg: Modern. Проект ориентирован на Windows 7–11 и написан на C# WinForms под .NET Framework 4.8.

## Состав

Решение `VelesLauncher.sln` содержит четыре проекта. `Veles.Core` — общая библиотека без интерфейса. `Veles.Launcher` — серверный пользовательский лаунчер со встроенным обновлением Minecraft-сборки. `Veles.BuildPublisher` — полностью отдельная админ-панель, которая не входит в клиентский лаунчер и хранит GitHub-токен только в памяти. `Veles.Updater` — отдельная служебная программа, которая устанавливается рядом с Launcher внутри основного setup и проверяет обновления самого лаунчера.

| Компонент | Репозиторий и назначение |
|---|---|
| Veles Launcher | `kutsandriy14-cyber/veles-launcher`; пользовательское приложение и его обновления |
| Veles Build Publisher | Собирается из отдельного проекта; публикует `build.zip` и `build-info.txt` в `kutsandriy14-cyber/veles-modpack-releases` |
| Veles Launcher Updater | Отдельный проект; ищет `VelesLauncherSetup.exe` в последнем релизе репозитория лаунчера |
| Minecraft-сборки | `kutsandriy14-cyber/veles-modpack-releases`; публичные GitHub Releases |

## Сценарий клиента

При запуске лаунчер получает последний GitHub Release из репозитория сборок, скачивает `build-info.txt`, читает версию сборки, Minecraft, модлоадера и IP:порт. Если локальная сборка отсутствует или устарела, кнопка запуска заблокирована до установки актуального `build.zip`. После установки лаунчер записывает сервер в `servers.dat`. Игровой архив должен содержать `start.bat` либо другой стартовый файл, указанный через `LAUNCH_COMMAND`.

## Сборка

Сборка выполняется в Visual Studio на Windows с workload «.NET desktop development». Откройте `VelesLauncher.sln`, выберите `Release` и соберите нужный проект отдельно. Windows CI создаёт два распространяемых setup-файла: `VelesLauncherSetup.exe` с Launcher и Updater рядом в одной папке и `VelesBuildPublisherSetup.exe` для отдельной админ-панели.

Для автоматической сборки Windows-артефактов предусмотрен `.github/workflows/windows-build.yml`. После публикации релиза лаунчера asset должен называться `VelesLauncherSetup.exe`, чтобы Updater, установленный рядом с Launcher, мог его найти.

## Токен админ-панели

Для публикации требуется GitHub Fine-grained Personal Access Token с правом `Contents: Read and write` только для репозитория `veles-modpack-releases`. Токен нельзя вставлять в исходный код, TXT-файлы сборок или клиентский лаунчер.
