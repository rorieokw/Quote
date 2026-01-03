// Enhanced Leaflet Map wrapper for Blazor interop
window.jobMap = {
    map: null,
    markers: [],
    markerLayer: null,
    userMarker: null,
    dotNetHelper: null,

    initialize: function(elementId, lat, lng, zoom = 11) {
        if (typeof L === 'undefined') {
            console.error('Leaflet not loaded');
            return;
        }

        // Initialize map with smooth options
        this.map = L.map(elementId, {
            zoomControl: false,
            scrollWheelZoom: true
        }).setView([lat, lng], zoom);

        // Add zoom control to bottom right
        L.control.zoom({ position: 'bottomright' }).addTo(this.map);

        // Use CartoDB Voyager tiles (clean, modern look - free)
        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(this.map);

        // Create marker layer group
        this.markerLayer = L.layerGroup().addTo(this.map);
    },

    setCenter: function(lat, lng, zoom = 14) {
        if (this.map) {
            this.map.flyTo([lat, lng], zoom, { duration: 1 });
        }
    },

    setMarkers: function(markers) {
        if (!this.map || !this.markerLayer) return;

        // Clear existing markers
        this.markerLayer.clearLayers();
        this.markers = [];

        markers.forEach((m, index) => {
            // Create custom styled marker
            const isUrgent = m.isUrgent;
            const markerColor = isUrgent ? '#ef4444' : '#3b82f6';
            const pulseClass = isUrgent ? 'pulse-urgent' : '';

            const iconHtml = `
                <div class="custom-marker ${pulseClass}" style="--marker-color: ${markerColor}">
                    <div class="marker-pin">
                        <i class="bi ${isUrgent ? 'bi-lightning-fill' : 'bi-briefcase-fill'}"></i>
                    </div>
                    <div class="marker-shadow"></div>
                </div>
            `;

            const icon = L.divIcon({
                html: iconHtml,
                className: 'custom-marker-container',
                iconSize: [40, 50],
                iconAnchor: [20, 50],
                popupAnchor: [0, -45]
            });

            const budgetText = m.budgetMax
                ? `<div class="popup-budget">$${m.budgetMax.toLocaleString()}</div>`
                : '<div class="popup-budget text-muted">Quote requested</div>';

            const urgentBadge = isUrgent
                ? '<span class="popup-badge urgent"><i class="bi bi-lightning-fill"></i> Urgent</span>'
                : '';

            const marker = L.marker([m.latitude, m.longitude], { icon: icon })
                .bindPopup(`
                    <div class="job-popup">
                        <div class="popup-header">
                            <h6>${m.title}</h6>
                            ${urgentBadge}
                        </div>
                        <div class="popup-details">
                            <div class="popup-row">
                                <i class="bi bi-geo-alt"></i>
                                <span>${m.suburbName}</span>
                            </div>
                            <div class="popup-row">
                                <i class="bi bi-tools"></i>
                                <span>${m.tradeCategoryName}</span>
                            </div>
                        </div>
                        ${budgetText}
                        <a href="/tradie/quote/${m.jobId}" class="popup-cta">
                            <i class="bi bi-send-fill"></i> Quick Quote
                        </a>
                    </div>
                `, {
                    className: 'custom-popup',
                    maxWidth: 280,
                    minWidth: 240
                });

            marker.jobId = m.jobId;
            marker.jobData = m;

            // Add with slight delay for animation effect
            setTimeout(() => {
                this.markerLayer.addLayer(marker);
            }, index * 50);

            this.markers.push(marker);
        });

        // Fit map to markers if any exist
        if (this.markers.length > 0) {
            setTimeout(() => {
                const group = L.featureGroup(this.markers);
                this.map.fitBounds(group.getBounds().pad(0.15), { maxZoom: 13 });
            }, markers.length * 50 + 100);
        }
    },

    addUserMarker: function(lat, lng) {
        if (!this.map) return;

        // Remove existing user marker
        if (this.userMarker) {
            this.map.removeLayer(this.userMarker);
        }

        const icon = L.divIcon({
            html: `
                <div class="user-location-marker">
                    <div class="user-dot"></div>
                    <div class="user-pulse"></div>
                </div>
            `,
            className: 'user-marker-container',
            iconSize: [24, 24],
            iconAnchor: [12, 12]
        });

        this.userMarker = L.marker([lat, lng], { icon: icon })
            .bindPopup('<div class="user-popup"><i class="bi bi-person-fill"></i> You are here</div>', {
                className: 'custom-popup user'
            })
            .addTo(this.map);
    },

    highlightMarker: function(jobId) {
        this.markers.forEach(marker => {
            if (marker.jobId === jobId) {
                marker.openPopup();
            }
        });
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

// Inject enhanced styles
const mapStyles = document.createElement('style');
mapStyles.textContent = `
    /* Custom Marker Styles */
    .custom-marker-container {
        background: transparent !important;
        border: none !important;
    }

    .custom-marker {
        position: relative;
        display: flex;
        flex-direction: column;
        align-items: center;
    }

    .marker-pin {
        width: 36px;
        height: 36px;
        background: var(--marker-color, #3b82f6);
        border-radius: 50% 50% 50% 0;
        transform: rotate(-45deg);
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 3px 10px rgba(0,0,0,0.25);
        transition: transform 0.2s ease;
    }

    .marker-pin i {
        transform: rotate(45deg);
        color: white;
        font-size: 16px;
    }

    .marker-shadow {
        width: 14px;
        height: 4px;
        background: rgba(0,0,0,0.2);
        border-radius: 50%;
        margin-top: 2px;
    }

    .custom-marker:hover .marker-pin {
        transform: rotate(-45deg) scale(1.1);
    }

    /* Urgent pulse animation */
    .pulse-urgent .marker-pin::after {
        content: '';
        position: absolute;
        width: 100%;
        height: 100%;
        border-radius: 50% 50% 50% 0;
        background: var(--marker-color);
        animation: pulse 1.5s ease-out infinite;
        z-index: -1;
    }

    @keyframes pulse {
        0% { transform: scale(1); opacity: 0.5; }
        100% { transform: scale(1.8); opacity: 0; }
    }

    /* User location marker */
    .user-marker-container {
        background: transparent !important;
        border: none !important;
    }

    .user-location-marker {
        position: relative;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .user-dot {
        width: 14px;
        height: 14px;
        background: #10b981;
        border: 3px solid white;
        border-radius: 50%;
        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
        z-index: 2;
    }

    .user-pulse {
        position: absolute;
        width: 40px;
        height: 40px;
        background: rgba(16, 185, 129, 0.3);
        border-radius: 50%;
        animation: userPulse 2s ease-out infinite;
    }

    @keyframes userPulse {
        0% { transform: scale(0.5); opacity: 1; }
        100% { transform: scale(1.5); opacity: 0; }
    }

    /* Custom Popup Styles */
    .custom-popup .leaflet-popup-content-wrapper {
        border-radius: 12px;
        padding: 0;
        overflow: hidden;
        box-shadow: 0 4px 20px rgba(0,0,0,0.15);
    }

    .custom-popup .leaflet-popup-content {
        margin: 0;
        width: 100% !important;
    }

    .custom-popup .leaflet-popup-tip {
        background: white;
    }

    .custom-popup.user .leaflet-popup-content-wrapper {
        background: #10b981;
    }

    .custom-popup.user .leaflet-popup-tip {
        background: #10b981;
    }

    .user-popup {
        padding: 8px 12px;
        color: white;
        font-weight: 500;
        display: flex;
        align-items: center;
        gap: 6px;
    }

    /* Job Popup Content */
    .job-popup {
        padding: 0;
    }

    .popup-header {
        padding: 12px 14px;
        background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
        color: white;
    }

    .popup-header h6 {
        margin: 0;
        font-size: 14px;
        font-weight: 600;
        line-height: 1.3;
    }

    .popup-badge {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 3px 8px;
        border-radius: 4px;
        font-size: 11px;
        font-weight: 600;
        margin-top: 6px;
    }

    .popup-badge.urgent {
        background: rgba(255,255,255,0.2);
        color: white;
    }

    .popup-details {
        padding: 10px 14px;
        background: #f8fafc;
        border-bottom: 1px solid #e2e8f0;
    }

    .popup-row {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 12px;
        color: #64748b;
        padding: 3px 0;
    }

    .popup-row i {
        width: 14px;
        color: #94a3b8;
    }

    .popup-budget {
        padding: 10px 14px;
        font-size: 18px;
        font-weight: 700;
        color: #059669;
        text-align: center;
        border-bottom: 1px solid #e2e8f0;
    }

    .popup-budget.text-muted {
        font-size: 13px;
        font-weight: 500;
        color: #94a3b8;
    }

    .popup-cta {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 6px;
        padding: 12px 14px;
        background: #3b82f6;
        color: white;
        text-decoration: none;
        font-weight: 600;
        font-size: 13px;
        transition: background 0.2s;
    }

    .popup-cta:hover {
        background: #2563eb;
        color: white;
    }

    /* Leaflet control styling */
    .leaflet-control-zoom {
        border: none !important;
        box-shadow: 0 2px 10px rgba(0,0,0,0.1) !important;
    }

    .leaflet-control-zoom a {
        border-radius: 8px !important;
        width: 36px !important;
        height: 36px !important;
        line-height: 36px !important;
        font-size: 18px !important;
    }

    .leaflet-control-zoom-in {
        border-radius: 8px 8px 0 0 !important;
    }

    .leaflet-control-zoom-out {
        border-radius: 0 0 8px 8px !important;
    }
`;
document.head.appendChild(mapStyles);
