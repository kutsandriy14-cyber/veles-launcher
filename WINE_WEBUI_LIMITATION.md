# Wine Web UI check

Последний Windows CI успешно собрал Launcher с TypeScript WebUi внутри custom Setup.

Custom Setup под Wine устанавливается и запускает `VelesLauncher.exe`. Окно Launcher создаётся, но область встроенного WinForms WebBrowser остаётся белой даже после установки Wine Mono и Wine Gecko 2.47.4. В логах Wine ранее фиксировалось отсутствие/ограничение HTML rendering; этот результат относится к Wine и не доказывает, что WebBrowser не работает на настоящей Windows 7 с установленным IE11.

Реальный интерфейс нельзя считать подтверждённым по этому белому Wine-скриншоту. Для Windows 7 надёжный следующий вариант — нативный CEF 109/CefSharp 109 с локальным TypeScript UI, но это увеличивает payload и имеет известное ограничение безопасности старой Chromium-ветки. WebView2 намеренно не используется из-за несовместимости с Windows 7.
