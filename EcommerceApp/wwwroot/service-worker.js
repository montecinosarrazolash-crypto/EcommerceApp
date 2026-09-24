// service-worker.js
//
// Estrategia pensada para una tienda con precios, stock y sesión que
// cambian todo el tiempo:
//   - Páginas (navegación): siempre red primero. Si no hay conexión,
//     se muestra /offline.html en vez de dejar la pantalla en blanco.
//   - Estáticos (css, js, imágenes, manifest, íconos): cache primero,
//     y de paso se van guardando en caché los que se visiten (runtime
//     caching), para que la segunda visita cargue más rápido.
//   - Todo lo demás (POST, /Cart, /Account, /Admin, /Reports, APIs,
//     etc.): no se intercepta, va directo a la red.
//
// Subir CACHE_VERSION cada vez que cambien los estáticos "core" fuerza
// a los navegadores a descartar la caché vieja.
const CACHE_VERSION = 'v1';
const CACHE_STATIC = `imagen-static-${CACHE_VERSION}`;

const PRECACHE_URLS = [
    '/offline.html',
    '/manifest.json',
    '/favicon.ico',
    '/images/icon-192.png',
    '/images/icon-512.png',
    '/images/logo.png',
    '/css/site.css',
    '/css/home.css',
    '/css/catalog.css',
    '/css/product-detail.css',
    '/css/cart.css',
    '/js/site.js',
    '/js/speech.js'
];

self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(CACHE_STATIC)
            .then(function (cache) { return cache.addAll(PRECACHE_URLS); })
            .then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (nombres) {
            return Promise.all(
                nombres
                    .filter(function (nombre) { return nombre.startsWith('imagen-static-') && nombre !== CACHE_STATIC; })
                    .map(function (nombre) { return caches.delete(nombre); })
            );
        }).then(function () { return self.clients.claim(); })
    );
});

function esEstatico(url) {
    return url.pathname.startsWith('/css/') ||
        url.pathname.startsWith('/js/') ||
        url.pathname.startsWith('/images/') ||
        url.pathname.startsWith('/lib/') ||
        url.pathname === '/manifest.json' ||
        url.pathname === '/favicon.ico';
}

self.addEventListener('fetch', function (event) {
    var request = event.request;

    // Solo GET, y solo del mismo origen (nunca interceptar APIs externas,
    // Supabase, Google, WhatsApp, etc.).
    if (request.method !== 'GET') return;
    var url = new URL(request.url);
    if (url.origin !== self.location.origin) return;

    // Navegación de página completa: red primero, offline.html si falla.
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(function () {
                return caches.match('/offline.html');
            })
        );
        return;
    }

    // Estáticos: cache primero, y se guarda en caché lo que se vaya pidiendo.
    if (esEstatico(url)) {
        event.respondWith(
            caches.match(request).then(function (cacheado) {
                if (cacheado) return cacheado;

                return fetch(request).then(function (respuesta) {
                    if (respuesta && respuesta.ok) {
                        var copia = respuesta.clone();
                        caches.open(CACHE_STATIC).then(function (cache) { cache.put(request, copia); });
                    }
                    return respuesta;
                }).catch(function () {
                    // Sin red y sin copia en caché: no hay nada que devolver.
                    return undefined;
                });
            })
        );
    }

    // Cualquier otra ruta (Cart, Orders, Account, Admin, Reports, etc.):
    // no se intercepta, pasa directo a la red.
});
