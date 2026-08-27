# Approved Launcher UI reference v2

Источник: повторно присланные пользователем screenshots 2560x1440 от 27 августа 2026 года. Эти screenshots являются единственным визуальным эталоном до следующей проверки.

## Main Launcher

Window is a dark, nearly full-width 16:9 desktop window with a thin dark title bar. The top content header has the Veles orange V mark and the text `VELES PLAYGAME / SERVER LAUNCHER` on the left. On the right are exactly two outlined orange controls: a gear icon with `Настройки` and a globe icon with `Сайт сервера`. There is no visible `Обновить Launcher` control in the header.

The hero area begins below the header. In the empty state it has an orange server/rack illustration on the left, the dynamic title `Серверная сборка`, and the version line `v—` below the title. In the installed state, the hero uses a large Minecraft-style build illustration, dynamic build name, the label `СЕРВЕРНАЯ СБОРКА`, and a separate outlined version badge such as `v0.12.7`.

The main content has two side-by-side cards with small rounded corners and a graphite-blue background. The left card is the server connection card. Its header has a large orange Wi-Fi icon inside a circular orange outline and the title `ПОДКЛЮЧЕНИЕ К СЕРВЕРУ`. In empty state it shows only `—` and `Адрес появится после публикации сборки`. In installed state it shows the dynamic `IP:PORT` in large white type and an outlined orange `Копировать` button with a copy icon.

The right card is the build card. Its header has a large orange cube/package icon inside a circular orange outline and the title `СБОРКА И ОБНОВЛЕНИЕ`. In empty state its center contains an orange outlined smiling box illustration with small accent rays, then `Сборка сервера пока не опубликована`, a `—` line, and two horizontally aligned buttons: outlined orange `Проверить и обновить сборку` with a refresh icon, and disabled grey `Запустить Minecraft` with a play icon. In installed state the center instead shows the build illustration, dynamic build name, Minecraft/loader metadata, outlined orange `Проверить обновления`, and filled orange `Запустить Minecraft`.

The three technical parameter blocks are a full-width horizontal strip below the two cards, not embedded inside the right card. The strip has three equal sections separated by thin vertical lines. Each section has a large grey/orange icon and a two-line label/value pair: `МОДЛОАДЕР`, `ВЕРСИЯ MINECRAFT`, and `СТАТУС`. Empty state values are `—`, `—`, and `Нет сборки`; installed state values are dynamic, such as `Forge 47.2.0`, `1.20.1`, and `Готово`.

## Settings window

The Settings window is a separate dark graphite native window titled `Настройки Veles Launcher`, with the orange V icon in the title bar. Its content title is orange uppercase `НАСТРОЙКИ ИГРЫ`. It has a full-width instance-folder row with label `Папка экземпляра`, a dark textbox and button `Выбрать папку`. Below are full-width numeric rows `Минимальная память (МБ)` and `Максимальная память (МБ)`. The Java area is read-only explanatory text: `Java: встроенный runtime` with orange runtime text and `Java устанавливается автоматически вместе с корректным релизом сборки`. The bottom bar contains `Отмена` and filled orange `Сохранить` aligned to the right.

## Publisher window

The separate Publisher window is dark graphite and titled `Veles Build Publisher`, with orange V icon. The heading is orange `Veles Build Publisher`. The visible form rows in the reference are `Название сборки`, `Версия сборки`, `Версия Minecraft`, `Модлоадер`, `Версия модлоадера`, `Адрес сервера (IP:PORT)`, `Название сервера`, `Сайт сервера`, `Версия Java`, and `Папка Java в ZIP`. Bottom controls are `Выбрать ZIP`, `Настроить доступ`, and filled orange `Опубликовать сборку`. The new product requirement supersedes the technical manual fields: values that can be read from ZIP metadata must become read-only and auto-derived; the user should not be forced to enter technical Java/loader/RAM values manually.

## Non-negotiable visual rules

The next implementation must use the exact three-card geometry shown above: two upper cards and one separate full-width lower strip. It must not use the earlier version where the parameter strip was inside the right card. It must not claim pixel-perfect parity until a new real screenshot has been produced and compared.
