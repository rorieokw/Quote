// Leaflet Map wrapper for Blazor Mobile
window.jobMap = {
    map: null,
    markers: [],
    markerLayer: null,
    userMarker: null,

    initialize: function(elementId, lat, lng, zoom) {
        if (typeof L === 'undefined') {
            console.error('Leaflet not loaded');
            return false;
        }

        // Initialize map
        this.map = L.map(elementId, {
            zoomControl: true,
            scrollWheelZoom: true
        }).setView([lat, lng], zoom || 11);

        // Use CartoDB Voyager tiles (clean, modern look)
        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; OpenStreetMap &copy; CARTO',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(this.map);

        // Create marker layer group
        this.markerLayer = L.layerGroup().addTo(this.map);

        return true;
    },

    setCenter: function(lat, lng, zoom) {
        if (this.map) {
            this.map.flyTo([lat, lng], zoom || 14, { duration: 1 });
        }
    },

    setMarkers: function(markers) {
        if (!this.map || !this.markerLayer) return;

        // Clear existing markers
        this.markerLayer.clearLayers();
        this.markers = [];

        markers.forEach((m) => {
            const markerColor = '#0f6cfb';

            const icon = L.divIcon({
                html: `<div style="
                    width: 30px;
                    height: 30px;
                    background: ${markerColor};
                    border-radius: 50% 50% 50% 0;
                    transform: rotate(-45deg);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    box-shadow: 0 3px 10px rgba(0,0,0,0.3);
                    border: 2px solid white;
                "></div>`,
                className: '',
                iconSize: [30, 30],
                iconAnchor: [15, 30],
                popupAnchor: [0, -30]
            });

            const budgetText = m.budgetMax
                ? `<div style="font-size: 16px; font-weight: bold; color: #059669; margin: 8px 0;">$${m.budgetMax.toLocaleString()}</div>`
                : '';

            const marker = L.marker([m.latitude, m.longitude], { icon: icon })
                .bindPopup(`
                    <div style="min-width: 200px;">
                        <div style="font-weight: 600; font-size: 14px; margin-bottom: 8px;">${m.title}</div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 4px;">${m.suburbName}</div>
                        <div style="font-size: 12px; color: #666;">${m.tradeCategoryName}</div>
                        ${budgetText}
                    </div>
                `, {
                    maxWidth: 250
                });

            marker.jobId = m.jobId;
            this.markerLayer.addLayer(marker);
            this.markers.push(marker);
        });

        // Fit map to markers if any exist
        if (this.markers.length > 0) {
            const group = L.featureGroup(this.markers);
            this.map.fitBounds(group.getBounds().pad(0.1), { maxZoom: 13 });
        }
    },

    addUserMarker: function(lat, lng) {
        if (!this.map) return;

        if (this.userMarker) {
            this.map.removeLayer(this.userMarker);
        }

        const icon = L.divIcon({
            html: `<div style="
                width: 16px;
                height: 16px;
                background: #10b981;
                border: 3px solid white;
                border-radius: 50%;
                box-shadow: 0 2px 8px rgba(0,0,0,0.3);
            "></div>`,
            className: '',
            iconSize: [16, 16],
            iconAnchor: [8, 8]
        });

        this.userMarker = L.marker([lat, lng], { icon: icon })
            .bindPopup('You are here')
            .addTo(this.map);
    }
};

// Geolocation helper
window.geolocation = {
    getCurrentPosition: function() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject('Geolocation not supported');
                return;
            }
            navigator.geolocation.getCurrentPosition(
                pos => resolve({
                    latitude: pos.coords.latitude,
                    longitude: pos.coords.longitude
                }),
                err => reject(err.message),
                { enableHighAccuracy: true, timeout: 10000 }
            );
        });
    }
};
