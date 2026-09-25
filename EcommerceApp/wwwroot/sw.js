JS
// sw.js — Service Worker de la PWA "IMAGEN".
//
// Objetivo: que el sitio sea instalable y que el catálogo (home + listado
// de productos + páginas de producto ya visitadas) siga viéndose sin
// conexión. Las páginas de Admin, Cuenta y Reportes NUNCA se cachean: son
// privadas, cambian seguido y no tiene sentido verlas offline.
//
// IMPORTANTE: cada vez que cambies CSS/JS del sitio, sube este número de
// versión para que los navegadores descarten la caché vieja.
var VERSION = 'v2';
var CACHE_ESTATICA = 'imagen-estatica-' + VERSION;
var CACHE_PAGINAS = 'imagen-paginas-' + VERSION;
var CACHE_IMAGENES = 'imagen-imagenes-' + VERSION;
var CACHE_FUENTES = 'imagen-fuentes-' + VERSION;
var TODAS_LAS_CACHES = [CACHE_ESTATICA, CACHE_PAGINAS, CACHE_IMAGENES, CACHE_FUENTES];

var OFFLINE_URL = '/offline.html';

// "App shell": lo mínimo para que el sitio abra y se vea decente offline,
// aunque el usuario nunca haya visitado esas páginas antes.
var APP_SHELL = [
    '/',
    '/Products',
    '/Products?category=Voley',
    '/Products?category=Futbol',
    '/Products?category=Basquetbol',
    OFFLINE_URL,
    '/manifest.webmanifest',
    '/css/home.css',
    '/css/catalog.css',
    '/css/product-detail.css',
    '/css/site.css',
    '/js/site.js',
    '/js/speech.js',
    '/images/logo.png',
    '/images/icons/icon-192.png',
    '/images/icons/icon-512.png',
    '/favicon.ico'
];

// Rutas que nunca se cachean (privadas / sensibles / cambian todo el tiempo).
var RUTAS_EXCLUIDAS = ['/Admin', '/Account', '/Reports'];

// Máximo de imágenes de producto que se guardan en caché, para que el
// almacenamiento del navegador no crezca sin límite con el tiempo.
var MAX_IMAGENES_EN_CACHE = 80;

self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(CACHE_ESTATICA).then(function (cache) {
            // "addAll" falla entero si UN solo recurso falla (por ejemplo si
            // todavía no hay productos en alguna categoría), así que se
            // agregan uno por uno para no perder toda la app shell por un
            // solo recurso opcional.
            return Promise.all(
                APP_SHELL.map(function (url) {
                    return cache.add(url).catch(function (err) {
                        console.warn('[SW] No se pudo precachear', url, err);
                    });
                })
            );
        }).then(function () {
            return self.skipWaiting();
        })
    );
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (nombres) {
            return Promise.all(
                nombres
                    .filter(function (nombre) { return TODAS_LAS_CACHES.indexOf(nombre) === -1; })
                    .map(function (nombre) { return caches.delete(nombre); })
            );
        }).then(function () {
            return self.clients.claim();
        })
    );
});

self.addEventListener('fetch', function (event) {
    var request = event.request;

    // Solo interceptamos lecturas. Los POST (agregar al carrito, checkout,
    // login, formularios) siempre van directo a la red: no tiene sentido
    // cachear una mutación, y si no hay internet es correcto que falle.
    if (request.method !== 'GET') return;

    var url = new URL(request.url);

    if (url.origin === self.location.origin &&
        RUTAS_EXCLUIDAS.some(function (ruta) { return url.pathname.startsWith(ruta); })) {
        return;
    }

    // Navegaciones (el usuario abre o recarga una página completa).
    if (request.mode === 'navigate') {
        event.respondWith(manejarNavegacion(request));
        return;
    }

    // Imágenes: incluye las propias (/images/...) y las de Supabase
    // Storage (otro dominio), para que las fotos de producto ya vistas
    // se sigan viendo sin conexión.
    if (request.destination === 'image') {
        event.respondWith(manejarImagen(request));
        return;
    }

    // CSS/JS propios del sitio.
    if (url.origin === self.location.origin &&
        (url.pathname.startsWith('/css/') || url.pathname.startsWith('/js/') || url.pathname.startsWith('/lib/'))) {
        event.respondWith(manejarEstatico(request));
        return;
    }

    // Google Fonts: se sirve lo cacheado al instante y se actualiza en
    // segundo plano (no vale la pena esperar a la red para una tipografía).
    if (url.hostname === 'fonts.googleapis.com' || url.hostname === 'fonts.gstatic.com') {
        event.respondWith(staleWhileRevalidate(request, CACHE_FUENTES));
    }
});

// Network-first para páginas HTML: siempre se prefiere la versión más
// nueva del servidor (precios, stock, estado de pedidos). Si no hay
// conexión, se sirve la última versión cacheada de ESA página, y si nunca
// se visitó, se muestra la pantalla de "sin conexión".
function manejarNavegacion(request) {
    return fetch(request).then(function (respuesta) {
        return guardarEnCache(CACHE_PAGINAS, request, respuesta);
    }).catch(function () {
        return caches.match(request).then(function (cacheada) {
            return cacheada || caches.match(OFFLINE_URL);
        });
    });
}

// Cache-first: ideal para archivos que casi no cambian (CSS/JS con
// asp-append-version).
function manejarEstatico(request) {
    return caches.match(request).then(function (cacheada) {
        if (cacheada) return cacheada;
        return fetch(request).then(function (respuesta) {
            return guardarEnCache(CACHE_ESTATICA, request, respuesta);
        });
    });
}

// Cache-first para imágenes de producto, incluidas las de Supabase
// Storage. Se piden con mode "no-cors" para poder guardarlas en caché sin
// que el navegador las bloquee por CORS (la respuesta queda "opaca": no se
// puede inspeccionar, pero sí mostrarse en un <img>, que es todo lo que
// se necesita).
function manejarImagen(request) {
    return caches.open(CACHE_IMAGENES).then(function (cache) {
        return cache.match(request).then(function (cacheada) {
            if (cacheada) return cacheada;

            return fetch(request, { mode: 'no-cors' }).then(function (respuesta) {
                cache.put(request, respuesta.clone());
                limitarTamanoCache(CACHE_IMAGENES, MAX_IMAGENES_EN_CACHE);
                return respuesta;
            }).catch(function () {
                return cacheada;
            });
        });
    });
}

// Stale-while-revalidate: responde con lo cacheado al toque (si existe) y
// de paso pide la versión nueva para la próxima vez.
function staleWhileRevalidate(request, nombreCache) {
    return caches.open(nombreCache).then(function (cache) {
        return cache.match(request).then(function (cacheada) {
            var actualizando = fetch(request).then(function (respuesta) {
                cache.put(request, respuesta.clone());
                return respuesta;
            }).catch(function () {
                return cacheada;
            });
            return cacheada || actualizando;
        });
    });
}

function guardarEnCache(nombreCache, request, respuesta) {
    // No cachear respuestas de error (404, 500) ni redirecciones.
    if (!respuesta || respuesta.status !== 200) return respuesta;

    var copia = respuesta.clone();
    caches.open(nombreCache).then(function (cache) {
        cache.put(request, copia);
    });
    return respuesta;
}

// Quita las entradas más viejas de una caché cuando supera el límite dado.
function limitarTamanoCache(nombreCache, maxItems) {
    caches.open(nombreCache).then(function (cache) {
        cache.keys().then(function (keys) {
            if (keys.length > maxItems) {
                cache.delete(keys[0]).then(function () {
                    limitarTamanoCache(nombreCache, maxItems);
                });
            }
        });
    });
}