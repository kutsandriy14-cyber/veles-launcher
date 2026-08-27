# Veles Launcher — отчёт проверки

Дата: 2026-08-27.

## Текущее состояние

| Область | Результат |
|---|---|
| Формат релиза | Переведён на `SERVER_ADDRESS=IP:PORT`, `launch.json`, `JAVA_VERSION`, `JAVA_RUNTIME_PATH`, RAM-параметры и SHA-256. Старые `SERVER_IP`, `SERVER_PORT`, `LAUNCH_COMMAND` удалены из исходного контракта. |
| Распаковка | Используется встроенный `System.IO.Compression.ZipFile`; 7-Zip не требуется. Установка идёт через staging/backup и атомарную замену экземпляра. |
| Java | Launcher ищет встроенный `runtime\\java\\bin\\javaw.exe` либо `java.exe`; Publisher не принимает ZIP без этого runtime. Для Windows 7 SP1+ используется portable BellSoft Liberica JDK 17 и требуется отдельная проверка на реальной Windows. |
| Прямой запуск | Launcher читает `launch.json` и создаёт процесс Java напрямую. `start.bat`, `cmd.exe` и произвольные команды не используются. |
| Настройки | Добавлен диалог RAM и папки экземпляра; значения сохраняются в `%AppData%\\Veles Launcher\\settings.json`. |
| Пользовательский интерфейс | Главный Launcher и Publisher используют тёмные плоские блоки с оранжевым акцентом; состояние отсутствия релиза показывается нейтральным текстом. Сборка Publisher после исправления layout прошла Windows CI. |

## Что было подтверждено ранее

Предыдущий Windows CI успешно собирал Veles.Core, Launcher, Build Publisher и Updater; core-тесты подтверждали разбор TXT с BOM и комментариями, сравнение версий, проверку порта и запись `servers.dat`. Новый pipeline должен создавать два собственных Veles Setup с payload-манифестами: `VelesLauncherSetup.exe` с Launcher/Updater/Core и отдельный `VelesBuildPublisherSetup.exe`.

## Подтверждение v0.1.6

Для предыдущей Inno-версии run `33068742727` завершился успешно. После перехода на custom Setup требуется новый CI: он должен собрать `Veles.Setup`, упаковать два payload-setup, проверить иконки, состав архива, замену существующей версии и отсутствие `VelesLauncherUpdaterSetup.exe`.

Локальная среда Linux не содержит MSBuild для .NET Framework 4.8, поэтому локальная проверка ограничена статическим валидатором и `git diff --check`. Windows-сборка подтверждена GitHub Actions.

## Неподтверждённые пункты

Настоящий запуск Forge 1.20.1 нельзя честно подтвердить без реального `build.zip`, содержащего рабочий `launch.json`, клиентские библиотеки, встроенный Java runtime и подходящую схему авторизации. До получения такой сборки Publisher должен принимать только полный архив, а Launcher при неполном релизе обязан отказать с понятным сообщением.

## Критерий следующего релиза

Новый тег и публичный релиз создаются только после успешного Windows CI, проверки скачанных setup-файлов, проверки состава основного setup (`Veles.Launcher.exe`, `Veles.Updater.exe`, `Veles.Core.dll`) и подтверждения, что в списке public assets находятся ровно два setup-файла без отдельного updater setup. Эти условия для v0.1.6 выполнены на уровне CI и артефактов; реальный Forge launch остаётся отдельным E2E-тестом на Windows с настоящей сборкой.
