// pwa-register.js
// Registra el service worker y agrega un botón flotante de "Instalar app"
// que aparece solo cuando el navegador confirma que la página se puede
// instalar (evento beforeinstallprompt). Es un solo archivo autocontenido
// (inyecta su propio CSS) para poder incluirlo en cualquier página con una
// sola línea, sin tocar cada hoja de estilos del sitio.
(function () {
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/service-worker.js').catch(function () {
                // Si falla el registro (por ejemplo en un navegador viejo),
                // el sitio sigue funcionando normal, solo sin modo offline.
            });
        });
    }

    var deferredPrompt = null;
    var boton = null;

    function inyectarEstilos() {
        if (document.getElementById('pwa-install-styles')) return;
        var style = document.createElement('style');
        style.id = 'pwa-install-styles';
        style.textContent =
            '.pwa-install-btn{position:fixed;left:22px;bottom:22px;z-index:45;' +
            'display:inline-flex;align-items:center;gap:10px;background:#0b0b0d;' +
            'color:#f4f5f7;border:1px solid rgba(255,255,255,0.18);border-radius:999px;' +
            'padding:12px 18px;font-family:Inter,system-ui,sans-serif;font-size:0.85rem;' +
            'font-weight:600;cursor:pointer;box-shadow:0 10px 24px rgba(0,0,0,0.35);' +
            'transition:transform .15s ease;}' +
            '.pwa-install-btn:hover{transform:translateY(-2px);}' +
            '.pwa-install-btn svg{width:18px;height:18px;flex-shrink:0;color:#d81324;}' +
            '@media (max-width:600px){.pwa-install-btn{left:16px;bottom:16px;padding:10px 14px;font-size:0.8rem;}}';
        document.head.appendChild(style);
    }

    function crearBoton() {
        if (boton) return boton;
        inyectarEstilos();

        boton = document.createElement('button');
        boton.type = 'button';
        boton.className = 'pwa-install-btn';
        boton.innerHTML =
            '<svg viewBox="0 0 24 24" fill="none"><path d="M12 3v13m0 0l-4.5-4.5M12 16l4.5-4.5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M4 19h16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>' +
            '<span>Instalar app</span>';

        boton.addEventListener('click', function () {
            if (!deferredPrompt) return;
            boton.disabled = true;
            deferredPrompt.prompt();
            deferredPrompt.userChoice.finally(function () {
                deferredPrompt = null;
                ocultarBoton();
            });
        });

        document.body.appendChild(boton);
        return boton;
    }

    function mostrarBoton() {
        crearBoton().style.display = 'inline-flex';
    }

    function ocultarBoton() {
        if (boton) boton.style.display = 'none';
    }

    // Chrome/Edge/Android disparan este evento cuando la app cumple los
    // requisitos de instalación (manifest válido, service worker, https).
    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        mostrarBoton();
    });

    // Ya instalada (o el usuario la instaló desde el menú del navegador):
    // no tiene sentido seguir mostrando el botón.
    window.addEventListener('appinstalled', function () {
        deferredPrompt = null;
        ocultarBoton();
    });
})();
