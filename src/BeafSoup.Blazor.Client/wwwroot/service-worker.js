// BeafSoup PWA service worker：缓存外壳资产，API 请求一律直连网络（不回退伪造响应）。
const cacheName = "beafsoup-shell-v7";
const shellAssets = ["./", "./index.html", "./manifest.webmanifest", "./icon.svg", "./appsettings.json"];

self.addEventListener("install", event => {
    event.waitUntil(caches.open(cacheName).then(cache => cache.addAll(shellAssets)));
    self.skipWaiting();
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys.filter(key => key.startsWith("beafsoup-shell-") && key !== cacheName).map(key => caches.delete(key))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener("fetch", event => {
    const url = new URL(event.request.url);
    if (event.request.method !== "GET" || url.pathname.startsWith("/api/")) {
        return; // API：浏览器默认网络行为；失败由客户端明确报告
    }
    event.respondWith(
        caches.match(event.request).then(cached => cached ?? fetch(event.request))
    );
});
