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
    needsUpdate: true,
    builds: []
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
    var versionElement = byId("build-version");
    versionElement.className = state.version === "—" ? "hero-version" : "hero-version installed-badge";
    setText("server-address", state.address);
    setText("build-state", state.buildName === "Серверная сборка" && state.version === "—" ? "Сборка сервера пока не опубликована" : state.buildName);
    setText("build-meta", state.version === "—" ? "—" : "Minecraft " + state.minecraft + " · " + state.loader + " " + state.loaderVersion);
    var emptyState = byId("empty-state");
    emptyState.className = state.version === "—" ? "empty-state empty" : "empty-state installed";
    setText("loader-value", state.version === "—" ? "—" : state.loader + " " + state.loaderVersion);
    setText("minecraft-value", state.minecraft);
    setText("status-value", state.status);
    renderBuilds();
    var launch = byId("launch-button");
    launch.disabled = !state.installed || state.needsUpdate;
    launch.className = launch.disabled ? "secondary-button disabled" : "secondary-button";
    var update = byId("update-button");
    update.textContent = state.needsUpdate ? "Обновить сборку сервера" : "Проверить обновления";
}
function renderBuilds() {
    var list = byId("builds-list");
    while (list.firstChild) {
        list.removeChild(list.firstChild);
    }
    if (!state.builds || state.builds.length === 0) {
        list.textContent = "Опубликованных сборок пока нет.";
        return;
    }
    for (var i = 0; i < state.builds.length; i++) {
        var item = state.builds[i];
        var row = document.createElement("div");
        row.className = item.active ? "build-row active" : "build-row";
        var title = document.createElement("strong");
        title.textContent = item.name + "  v" + item.version;
        var meta = document.createElement("span");
        meta.textContent = (item.active ? "АКТИВНА" : "НЕАКТИВНА") + "  ·  приоритет " + item.priority;
        row.appendChild(title);
        row.appendChild(meta);
        list.appendChild(row);
    }
}
function openBuilds() { var modal = byId("builds-modal"); modal.className = "builds-modal open"; modal.setAttribute("aria-hidden", "false"); }
function closeBuilds() { var modal = byId("builds-modal"); modal.className = "builds-modal"; modal.setAttribute("aria-hidden", "true"); }
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
byId("build-heading").onclick = openBuilds;
byId("builds-close").onclick = closeBuilds;
render();
