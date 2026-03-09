// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const apiCacheName = 'privestio-api-cache-v1';
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// API endpoints that use network-first strategy (GET only)
const apiNetworkFirstPatterns = [
    /\/api\/v1\/accounts(\/|$|\?)/,
    /\/api\/v1\/analytics(\/|$|\?)/,
    /\/api\/v1\/transactions(\/|$|\?)/
];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

function isApiReadRequest(request) {
    if (request.method !== 'GET') return false;
    const url = new URL(request.url);
    return apiNetworkFirstPatterns.some(pattern => pattern.test(url.pathname));
}

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused static asset caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));

    // Register for background sync if supported
    if (self.registration && 'sync' in self.registration) {
        console.info('Service worker: Background sync is supported');
    }
}

// Network-first strategy for API requests: try network, fall back to cache
async function fetchApiNetworkFirst(request) {
    const apiCache = await caches.open(apiCacheName);
    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            // Cache the successful response for offline fallback
            apiCache.put(request, networkResponse.clone());
        }
        return networkResponse;
    } catch (error) {
        // Network failed -- try serving from cache
        const cachedResponse = await apiCache.match(request);
        if (cachedResponse) {
            console.info('Service worker: Serving API response from cache', request.url);
            return cachedResponse;
        }
        // No cache available -- return a minimal offline response
        return new Response(
            JSON.stringify({ error: 'offline', message: 'You are offline and no cached data is available.' }),
            { status: 503, headers: { 'Content-Type': 'application/json' } }
        );
    }
}

async function onFetch(event) {
    // Network-first strategy for API read endpoints
    if (isApiReadRequest(event.request)) {
        return fetchApiNetworkFirst(event.request);
    }

    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache,
        // unless that request is for an offline resource.
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request);
}

// Background sync handler
self.addEventListener('sync', event => {
    if (event.tag === 'privestio-sync-queue') {
        console.info('Service worker: Background sync triggered for privestio-sync-queue');
        // The actual sync logic is handled by the Blazor app via IndexedDB
        // This event signals the app to flush its sync queue
    }
});
