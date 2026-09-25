// pwa.js — Registra el service worker y maneja el botón "Instalar app".
(function () {
    'use strict';

    // ---------- Registro del service worker ----------
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js').catch(function (err) {
                console.warn('[PWA] No se pudo registrar el service worker:', err);
            });
        });
    }

    // ---------- Botón "Instalar app" ----------
    // El navegador dispara "beforeinstallprompt" cuando decide que el sitio
    // cumple los requisitos para instalarse (manifest + service worker +
    // HTTPS). Se guarda el evento para poder mostrar el diálogo nativo de
    // instalación recién cuando el usuario hace clic en nuestro botón.
    var eventoInstalacion = null;
    var btnInstalar = document.getElementById('pwaInstallBtn');

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        eventoInstalacion = e;
        if (btnInstalar) btnInstalar.hidden = false;
    });

    if (btnInstalar) {
        btnInstalar.addEventListener('click', function () {
            if (!eventoInstalacion) return;
            btnInstalar.hidden = true;
            eventoInstalacion.prompt();
            eventoInstalacion.userChoice.finally(function () {
                eventoInstalacion = null;
            });
        });
    }

    // Si ya está instalada (o el usuario la acaba de instalar), no tiene
    // sentido seguir mostrando el botón.
    window.addEventListener('appinstalled', function () {
        if (btnInstalar) btnInstalar.hidden = true;
        eventoInstalacion = null;
    });
})();
