// Speech Recognition API wrapper for Blazor interop
window.speechRecognition = {
    recognition: null,

    isSupported: function() {
        return 'webkitSpeechRecognition' in window || 'SpeechRecognition' in window;
    },

    start: function() {
        return new Promise((resolve, reject) => {
            if (!this.isSupported()) {
                reject('Speech recognition not supported');
                return;
            }

            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            this.recognition = new SpeechRecognition();

            this.recognition.continuous = false;
            this.recognition.interimResults = false;
            this.recognition.lang = 'en-AU';  // Australian English

            this.recognition.onresult = function(event) {
                const transcript = event.results[0][0].transcript;
                resolve(transcript);
            };

            this.recognition.onerror = function(event) {
                reject(event.error);
            };

            this.recognition.onend = function() {
                // Recognition ended without result
            };

            this.recognition.start();
        });
    },

    stop: function() {
        if (this.recognition) {
            this.recognition.stop();
            this.recognition = null;
        }
    }
};

// Geolocation wrapper for Blazor interop
window.geolocation = {
    getCurrentPosition: function() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject('Geolocation not supported');
                return;
            }

            navigator.geolocation.getCurrentPosition(
                function(position) {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    });
                },
                function(error) {
                    reject(error.message);
                },
                {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 300000  // Cache for 5 minutes
                }
            );
        });
    }
};
