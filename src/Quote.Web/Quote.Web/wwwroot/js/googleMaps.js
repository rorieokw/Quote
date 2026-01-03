// Google Maps integration for Quote platform
// This module provides map rendering, markers, clustering, and autocomplete functionality

window.GoogleMaps = (function () {
    let map = null;
    let markers = [];
    let markerCluster = null;
    let infoWindow = null;
    let autocomplete = null;
    let directionsService = null;
    let directionsRenderer = null;

    // Initialize the map
    function initMap(elementId, options) {
        const defaultOptions = {
            center: { lat: -33.8688, lng: 151.2093 }, // Sydney
            zoom: 10,
            mapTypeControl: false,
            streetViewControl: false,
            fullscreenControl: true
        };

        const mapOptions = { ...defaultOptions, ...options };
        const element = document.getElementById(elementId);

        if (!element) {
            console.error('Map element not found:', elementId);
            return null;
        }

        map = new google.maps.Map(element, mapOptions);
        infoWindow = new google.maps.InfoWindow();
        directionsService = new google.maps.DirectionsService();
        directionsRenderer = new google.maps.DirectionsRenderer({
            suppressMarkers: false,
            polylineOptions: {
                strokeColor: '#2563eb',
                strokeWeight: 4
            }
        });

        return map;
    }

    // Add a single marker
    function addMarker(options) {
        const marker = new google.maps.Marker({
            position: options.position,
            map: map,
            title: options.title,
            icon: options.icon || null,
            animation: options.animate ? google.maps.Animation.DROP : null
        });

        if (options.infoContent) {
            marker.addListener('click', () => {
                infoWindow.setContent(options.infoContent);
                infoWindow.open(map, marker);
            });
        }

        if (options.onClick) {
            marker.addListener('click', options.onClick);
        }

        markers.push(marker);
        return marker;
    }

    // Add job markers with clustering
    function addJobMarkers(jobs) {
        clearMarkers();

        const bounds = new google.maps.LatLngBounds();

        jobs.forEach((job, index) => {
            const position = { lat: job.latitude, lng: job.longitude };
            bounds.extend(position);

            const marker = new google.maps.Marker({
                position: position,
                map: map,
                title: job.title,
                icon: getJobIcon(job),
                animation: google.maps.Animation.DROP
            });

            marker.addListener('click', () => {
                const content = createJobInfoWindow(job);
                infoWindow.setContent(content);
                infoWindow.open(map, marker);
            });

            markers.push(marker);
        });

        // Use MarkerClusterer if available
        if (typeof MarkerClusterer !== 'undefined' && markers.length > 10) {
            markerCluster = new MarkerClusterer(map, markers, {
                imagePath: 'https://developers.google.com/maps/documentation/javascript/examples/markerclusterer/m',
                maxZoom: 14
            });
        }

        if (jobs.length > 0) {
            map.fitBounds(bounds);
            if (map.getZoom() > 15) {
                map.setZoom(15);
            }
        }
    }

    // Get appropriate icon for job
    function getJobIcon(job) {
        const baseUrl = 'https://maps.google.com/mapfiles/ms/icons/';

        if (job.isUrgent) {
            return baseUrl + 'red-dot.png';
        }

        const budgetMax = job.budgetMax || 0;
        if (budgetMax > 5000) {
            return baseUrl + 'green-dot.png';
        } else if (budgetMax > 1000) {
            return baseUrl + 'blue-dot.png';
        }

        return baseUrl + 'yellow-dot.png';
    }

    // Create info window content for a job
    function createJobInfoWindow(job) {
        const budget = job.budgetMax
            ? `$${job.budgetMin?.toLocaleString() || 0} - $${job.budgetMax.toLocaleString()}`
            : 'Budget not specified';

        return `
            <div style="max-width: 280px; font-family: system-ui, sans-serif;">
                <h6 style="margin: 0 0 8px 0; font-size: 14px; font-weight: 600;">
                    ${escapeHtml(job.title)}
                </h6>
                <p style="margin: 0 0 8px 0; color: #666; font-size: 12px;">
                    <strong>${escapeHtml(job.tradeCategory)}</strong><br>
                    ${escapeHtml(job.suburbName)}
                </p>
                <p style="margin: 0 0 8px 0; font-size: 13px; color: #2563eb; font-weight: 500;">
                    ${budget}
                </p>
                ${job.distanceKm ? `<p style="margin: 0 0 8px 0; font-size: 12px; color: #666;">
                    📍 ${job.distanceKm.toFixed(1)} km away
                    ${job.travelTimeMinutes ? ` • 🚗 ${job.travelTimeMinutes} mins` : ''}
                </p>` : ''}
                <div style="margin-top: 10px;">
                    <a href="/jobs/${job.jobId}"
                       style="display: inline-block; padding: 6px 12px; background: #2563eb; color: white; text-decoration: none; border-radius: 4px; font-size: 12px;">
                        View Details
                    </a>
                    ${job.quoteUrl ? `
                    <a href="${job.quoteUrl}"
                       style="display: inline-block; padding: 6px 12px; margin-left: 8px; background: #10b981; color: white; text-decoration: none; border-radius: 4px; font-size: 12px;">
                        Quote Now
                    </a>` : ''}
                </div>
            </div>
        `;
    }

    // Display route on map
    function displayRoute(stops) {
        if (!directionsService || !directionsRenderer || stops.length < 2) {
            return;
        }

        directionsRenderer.setMap(map);

        const origin = { lat: stops[0].latitude, lng: stops[0].longitude };
        const destination = { lat: stops[stops.length - 1].latitude, lng: stops[stops.length - 1].longitude };

        const waypoints = stops.slice(1, -1).map(stop => ({
            location: { lat: stop.latitude, lng: stop.longitude },
            stopover: true
        }));

        directionsService.route({
            origin: origin,
            destination: destination,
            waypoints: waypoints,
            travelMode: google.maps.TravelMode.DRIVING,
            optimizeWaypoints: false
        }, (response, status) => {
            if (status === 'OK') {
                directionsRenderer.setDirections(response);
            } else {
                console.error('Directions request failed:', status);
            }
        });
    }

    // Clear route from map
    function clearRoute() {
        if (directionsRenderer) {
            directionsRenderer.setMap(null);
        }
    }

    // Initialize address autocomplete
    function initAutocomplete(inputId, options, callback) {
        const input = document.getElementById(inputId);
        if (!input) {
            console.error('Autocomplete input not found:', inputId);
            return null;
        }

        const defaultOptions = {
            componentRestrictions: { country: 'au' },
            fields: ['address_components', 'geometry', 'place_id', 'formatted_address']
        };

        autocomplete = new google.maps.places.Autocomplete(input, { ...defaultOptions, ...options });

        autocomplete.addListener('place_changed', () => {
            const place = autocomplete.getPlace();

            if (!place.geometry || !place.geometry.location) {
                console.warn('No geometry for selected place');
                return;
            }

            const result = {
                placeId: place.place_id,
                formattedAddress: place.formatted_address,
                latitude: place.geometry.location.lat(),
                longitude: place.geometry.location.lng(),
                suburb: getAddressComponent(place, 'locality') || getAddressComponent(place, 'sublocality'),
                state: getAddressComponent(place, 'administrative_area_level_1'),
                postcode: getAddressComponent(place, 'postal_code'),
                streetNumber: getAddressComponent(place, 'street_number'),
                street: getAddressComponent(place, 'route')
            };

            if (callback) {
                callback(result);
            }
        });

        return autocomplete;
    }

    // Get specific address component from place result
    function getAddressComponent(place, type) {
        const component = place.address_components?.find(c => c.types.includes(type));
        return component?.long_name || null;
    }

    // Clear all markers
    function clearMarkers() {
        markers.forEach(marker => marker.setMap(null));
        markers = [];

        if (markerCluster) {
            markerCluster.clearMarkers();
            markerCluster = null;
        }
    }

    // Center map on location
    function centerOn(lat, lng, zoom) {
        if (map) {
            map.setCenter({ lat, lng });
            if (zoom) {
                map.setZoom(zoom);
            }
        }
    }

    // Fit map bounds to markers
    function fitToMarkers() {
        if (markers.length === 0) return;

        const bounds = new google.maps.LatLngBounds();
        markers.forEach(marker => bounds.extend(marker.getPosition()));
        map.fitBounds(bounds);
    }

    // Get user's current location
    function getCurrentLocation() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error('Geolocation not supported'));
                return;
            }

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                },
                (error) => {
                    reject(error);
                },
                { enableHighAccuracy: true, timeout: 10000 }
            );
        });
    }

    // Utility: escape HTML
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text || '';
        return div.innerHTML;
    }

    // Public API
    return {
        initMap,
        addMarker,
        addJobMarkers,
        clearMarkers,
        centerOn,
        fitToMarkers,
        displayRoute,
        clearRoute,
        initAutocomplete,
        getCurrentLocation
    };
})();

// Blazor interop functions
window.initGoogleMap = function (elementId, lat, lng, zoom) {
    return window.GoogleMaps.initMap(elementId, {
        center: { lat: lat, lng: lng },
        zoom: zoom || 12
    });
};

window.addJobMarkersToMap = function (jobsJson) {
    const jobs = JSON.parse(jobsJson);
    window.GoogleMaps.addJobMarkers(jobs);
};

window.clearMapMarkers = function () {
    window.GoogleMaps.clearMarkers();
};

window.centerMapOn = function (lat, lng, zoom) {
    window.GoogleMaps.centerOn(lat, lng, zoom);
};

window.displayRouteOnMap = function (stopsJson) {
    const stops = JSON.parse(stopsJson);
    window.GoogleMaps.displayRoute(stops);
};

window.clearMapRoute = function () {
    window.GoogleMaps.clearRoute();
};

window.initAddressAutocomplete = function (inputId, dotNetHelper) {
    window.GoogleMaps.initAutocomplete(inputId, {}, (result) => {
        dotNetHelper.invokeMethodAsync('OnAddressSelected', JSON.stringify(result));
    });
};

window.getUserLocation = async function () {
    try {
        const location = await window.GoogleMaps.getCurrentLocation();
        return JSON.stringify(location);
    } catch (error) {
        console.error('Error getting location:', error);
        return null;
    }
};
