// speech.js
// Envuelve el Web Speech API nativo del navegador (sin librerías externas)
// para poder usarlo desde cualquier página del sitio:
//   - window.Speech.escuchar(onResult, onError)  -> dictado por voz (Speech to Text)
//   - window.Speech.leer(texto, onEnd)            -> lectura en voz alta (Text to Speech)
//
// Soporte de navegadores: Chrome/Edge soportan ambas APIs. Firefox y Safari
// soportan síntesis (leer) pero no siempre reconocimiento (escuchar), por eso
// cada función expone una bandera "*Soportado" para que la UI pueda ocultar
// el botón correspondiente si el navegador no lo soporta.
(function (window) {
    'use strict';

    var SpeechRecognitionImpl = window.SpeechRecognition || window.webkitSpeechRecognition;
    var synth = window.speechSynthesis;
    var IDIOMA = 'es-ES';

    var Speech = {
        reconocimientoSoportado: !!SpeechRecognitionImpl,
        sintesisSoportada: !!synth,

        // Escucha una sola frase desde el micrófono y llama a onResult(texto)
        // cuando el navegador termina de reconocerla. Devuelve la instancia de
        // reconocimiento (por si se quiere cancelar manualmente con .stop()).
        escuchar: function (onResult, onError) {
            if (!this.reconocimientoSoportado) {
                if (onError) onError('Tu navegador no soporta búsqueda por voz. Prueba con Chrome o Edge.');
                return null;
            }

            var recognition = new SpeechRecognitionImpl();
            recognition.lang = IDIOMA;
            recognition.interimResults = false;
            recognition.maxAlternatives = 1;

            recognition.onresult = function (event) {
                var texto = event.results[0][0].transcript;
                onResult(texto);
            };

            recognition.onerror = function (event) {
                var mensaje = event.error === 'not-allowed' || event.error === 'permission-denied'
                    ? 'Debes permitir el acceso al micrófono para buscar por voz.'
                    : event.error === 'no-speech'
                        ? 'No se detectó ninguna voz. Intenta de nuevo.'
                        : 'No se pudo reconocer el audio. Intenta de nuevo.';
                if (onError) onError(mensaje);
            };

            recognition.start();
            return recognition;
        },

        // Lee un texto en voz alta. Cancela cualquier lectura anterior en curso
        // (para que no se superpongan dos lecturas si el usuario hace doble clic).
        leer: function (texto, onEnd) {
            if (!this.sintesisSoportada || !texto) return;

            synth.cancel();

            var utterance = new SpeechSynthesisUtterance(texto);
            utterance.lang = IDIOMA;
            utterance.rate = 1;
            if (onEnd) utterance.onend = onEnd;

            synth.speak(utterance);
        },

        // Detiene cualquier lectura en curso.
        detenerLectura: function () {
            if (this.sintesisSoportada) synth.cancel();
        },

        // Normaliza texto para comparar comandos de voz: minúsculas y sin
        // tildes (para que "básquet", "basquet" y "BÁSQUET" se traten igual).
        normalizar: function (texto) {
            return (texto || '')
                .toLowerCase()
                .normalize('NFD')
                .replace(/[\u0300-\u036f]/g, '')
                .trim();
        }
    };

    window.Speech = Speech;
})(window);
