# Web UI и Windows 7–11

Проверено 27 августа 2026 года.

WebView2 исключён из архитектуры, потому что он не подходит как общий runtime для Windows 7.

Официальное обсуждение CefSharp #4336 сообщает: Chrome/CEF версии 109 — последняя ветка с поддержкой Windows 7, Windows 8 и Windows 8.1; версии 110+ требуют Windows 10 и выше.
Источник: https://github.com/cefsharp/CefSharp/discussions/4336

Официальная страница NuGet CefSharp.WinForms 109.1.110 подтверждает пакет для WinForms и target .NET Framework 4.5.2 или выше. Страница также показывает предупреждение NuGet о vulnerability с высокой severity, поэтому перед релизом нужно осознанно зафиксировать эту старую ветку и ограничить локальную поверхность UI, не использовать удалённый произвольный контент и проверить актуальность безопасности.
Источник: https://www.nuget.org/packages/CefSharp.WinForms/109.1.110

Выбранная схема: TypeScript + React + CSS собираются локально в статические файлы ES5/совместимого JavaScript; C# .NET Framework 4.8 остаётся native shell и bridge для BuildService, Java/Minecraft launch, settings и auto-update; CefSharp/CEF 109 отображает только локальный UI.
