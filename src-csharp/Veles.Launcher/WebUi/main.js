"use strict";
var state = {
    buildName: "Серверная сборка",
    version: "—",
    address: "—",
    minecraft: "—",
    loader: "—",
    loaderVersion: "",
    status: "Нет сборки",
    installed: false,
    needsUpdate: true
};
function byId(id) {
    var element = document.getElementById(id);
    if (!element) {
        throw new Error("Missing element: " + id);
    }
    return element;
}
function bridge(action) {
    try {
        var nativeWindow = window;
        if (nativeWindow.velesBridge && nativeWindow.velesBridge.invoke) {
            nativeWindow.velesBridge.invoke(action);
            return;
        }
        if (nativeWindow.external && nativeWindow.external.notify) {
            nativeWindow.external.notify(action);
            return;
        }
    }
    catch (_) { /* Native bridge is optional in browser preview. */ }
}
function setText(id, value) { byId(id).textContent = value; }
function render() {
    setText("build-title", state.buildName);
    setText("build-version", state.version === "—" ? "v—" : "v" + state.version);
    setText("server-address", state.address);
    setText("build-state", state.buildName === "Серверная сборка" && state.version === "—" ? "Сборка сервера пока не опубликована" : state.buildName);
    setText("build-meta", state.version === "—" ? "—" : "Minecraft " + state.minecraft + " · " + state.loader + " " + state.loaderVersion);
    setText("loader-value", state.version === "—" ? "—" : state.loader + " " + state.loaderVersion);
    setText("minecraft-value", state.minecraft);
    setText("status-value", state.status);
    var launch = byId("launch-button");
    launch.disabled = !state.installed || state.needsUpdate;
    launch.className = launch.disabled ? "secondary-button disabled" : "secondary-button";
    var update = byId("update-button");
    update.textContent = state.needsUpdate ? "Обновить сборку сервера" : "Проверить обновления";
}
function updateBuild() {
    var button = byId("update-button");
    button.disabled = true;
    button.textContent = "Проверяю сборку…";
    setText("status-value", "Проверка…");
    setText("notice", "Проверяю последний релиз сборки…");
    bridge("build.update");
}
function launchGame() { bridge("game.launch"); }
function openSettings() { bridge("settings.open"); }
function openSite() { bridge("site.open"); }
function copyAddress() {
    if (state.address === "—") {
        setText("notice", "Адрес появится после публикации сборки.");
        return;
    }
    try {
        if (navigator.clipboard) {
            navigator.clipboard.writeText(state.address);
        }
    }
    catch (_) { /* IE fallback handled by native bridge. */ }
    bridge("server.copy");
    setText("notice", "IP сервера скопирован.");
}
function applyNativeState(json) {
    try {
        state = JSON.parse(json);
        render();
    }
    catch (_) {
        setText("notice", "Не удалось прочитать состояние сборки.");
    }
}
window.velesSetState = applyNativeState;
byId("update-button").onclick = updateBuild;
byId("launch-button").onclick = launchGame;
byId("settings-button").onclick = openSettings;
byId("site-button").onclick = openSite;
byId("copy-button").onclick = copyAddress;
render();
