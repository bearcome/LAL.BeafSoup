// 主题桥接：localStorage 缓存 + matchMedia 订阅；data-theme 预置由宿主 index.html 内联脚本完成（无闪烁）。
export function getStoredPreference(key) {
    return window.localStorage.getItem(key);
}

export function storePreference(key, value) {
    window.localStorage.setItem(key, value);
}

export function getSystemTheme() {
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function applyTheme(themeName) {
    document.documentElement.setAttribute("data-theme", themeName);
}

export function subscribeSystemTheme(dotNetHelper) {
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
        dotNetHelper.invokeMethodAsync("OnSystemThemeChanged");
    });
}
