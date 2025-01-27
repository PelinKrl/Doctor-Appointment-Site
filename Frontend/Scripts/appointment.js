// Functions
function logout() {
    localStorage.removeItem('token');
    window.location.href = 'index.html';
}

// State management for filters and cities
let activeFilters = {
    searchQuery: '',
    city: '',
    availability: false,
    online: false
};

let cities = [];
let cityDropdownVisible = false;
let specialties = [];
let map;
let markers = [];

// Fetch cities on load
async function fetchCities() {
    try {
        const response = await fetch('https://turkiyeapi.dev/api/v1/provinces');
        const data = await response.json();
        cities = data.data.sort((a, b) => a.name.localeCompare(b.name));
        setupCitySearch();
    } catch (error) {
        console.error('Error fetching cities:', error);
    }
}

function setupCitySearch() {
    const locationInput = document.getElementById('locationInput');
    const searchBox = locationInput.parentElement;
    
    // Create dropdown container
    const dropdownContainer = document.createElement('div');
    dropdownContainer.className = 'city-dropdown';
    searchBox.appendChild(dropdownContainer);

    // Handle input changes
    locationInput.addEventListener('input', function(e) {
        const searchTerm = e.target.value.toLowerCase();
        const filteredCities = cities.filter(city => 
            city.name.toLowerCase().includes(searchTerm)
        );
        
        updateCityDropdown(filteredCities, dropdownContainer);
    });

    // Show dropdown on focus
    locationInput.addEventListener('focus', function() {
        const searchTerm = this.value.toLowerCase();
        const filteredCities = cities.filter(city => 
            city.name.toLowerCase().includes(searchTerm)
        );
        updateCityDropdown(filteredCities, dropdownContainer);
    });

    // Handle click outside
    document.addEventListener('click', function(e) {
        if (!searchBox.contains(e.target)) {
            hideDropdown(dropdownContainer);
        }
    });

    // Prevent dropdown from closing when clicking inside it
    dropdownContainer.addEventListener('click', function(e) {
        e.stopPropagation();
    });
}

function updateCityDropdown(filteredCities, dropdownContainer) {
    // Add visible class for animation
    dropdownContainer.classList.add('visible');
    
    if (filteredCities.length === 0) {
        dropdownContainer.innerHTML = '<div class="city-item no-results">Sonuç bulunamadı</div>';
        return;
    }

    const cityItems = filteredCities.slice(0, 8).map(city => `
        <div class="city-item" data-city-id="${city.id}" data-city-name="${city.name}">
            <i class="fas fa-map-marker-alt"></i>
            ${city.name}
        </div>
    `).join('');

    dropdownContainer.innerHTML = cityItems;

    // Add click handlers to city items - only update input, don't search
    dropdownContainer.querySelectorAll('.city-item').forEach(item => {
        item.addEventListener('click', function() {
            const cityName = this.dataset.cityName;
            document.getElementById('locationInput').value = cityName;
            activeFilters.city = cityName;
            hideDropdown(dropdownContainer);
        });
    });
}

function hideDropdown(dropdownContainer) {
    dropdownContainer.classList.remove('visible');
}

async function fetchSpecialties() {
    try {
        const response = await fetch('https://localhost:7285/api/patients/specialties', {
            headers: {
                'Accept': 'application/json'
            }
        });
        if (response.ok) {
            specialties = await response.json();
            setupSpecialtySearch();
        } else {
            console.error('Failed to fetch specialties:', response.status);
        }
    } catch (error) {
        console.error('Error fetching specialties:', error);
    }
}

function setupSpecialtySearch() {
    const searchInput = document.getElementById('searchInput');
    const searchBox = searchInput.parentElement;
    
    // Create dropdown container
    const dropdownContainer = document.createElement('div');
    dropdownContainer.className = 'specialty-dropdown';
    searchBox.appendChild(dropdownContainer);

    // Handle input changes - only update dropdown, don't search
    searchInput.addEventListener('input', function(e) {
        const searchTerm = e.target.value.toLowerCase();
        const filteredSpecialties = specialties.filter(specialty => 
            specialty.toLowerCase().includes(searchTerm)
        );
        
        updateSpecialtyDropdown(filteredSpecialties, dropdownContainer);
    });

    // Show dropdown on focus
    searchInput.addEventListener('focus', function() {
        const searchTerm = this.value.toLowerCase();
        const filteredSpecialties = specialties.filter(specialty => 
            specialty.toLowerCase().includes(searchTerm)
        );
        updateSpecialtyDropdown(filteredSpecialties, dropdownContainer);
    });

    // Handle click outside
    document.addEventListener('click', function(e) {
        if (!searchBox.contains(e.target)) {
            hideSpecialtyDropdown(dropdownContainer);
        }
    });

    // Prevent dropdown from closing when clicking inside it
    dropdownContainer.addEventListener('click', function(e) {
        e.stopPropagation();
    });

    // Add search button click handler
    const searchButton = document.getElementById('searchButton');
    if (searchButton) {
        searchButton.addEventListener('click', function() {
            const searchQuery = searchInput.value;
            const locationInput = document.getElementById('locationInput');
            const city = locationInput ? locationInput.value : '';
            
            activeFilters.searchQuery = searchQuery;
            activeFilters.city = city;
            
            fetchDoctors(activeFilters, true);
        });
    }
}

function updateSpecialtyDropdown(filteredSpecialties, dropdownContainer) {
    dropdownContainer.classList.add('visible');
    
    if (filteredSpecialties.length === 0) {
        dropdownContainer.innerHTML = '<div class="specialty-item no-results">Sonuç bulunamadı</div>';
        return;
    }

    const specialtyItems = filteredSpecialties.slice(0, 8).map(specialty => `
        <div class="specialty-item" data-specialty="${specialty}">
            <i class="fas fa-stethoscope"></i>
            ${specialty}
        </div>
    `).join('');

    dropdownContainer.innerHTML = specialtyItems;

    // Add click handlers to specialty items - only update input, don't search
    dropdownContainer.querySelectorAll('.specialty-item').forEach(item => {
        item.addEventListener('click', function() {
            const specialty = this.dataset.specialty;
            document.getElementById('searchInput').value = specialty;
            hideSpecialtyDropdown(dropdownContainer);
        });
    });
}

function hideSpecialtyDropdown(dropdownContainer) {
    dropdownContainer.classList.remove('visible');
}

async function fetchDoctors(filters = {}, isSearch = false) {
    try {
        let url;
        const queryParams = new URLSearchParams();

        if (isSearch) {
            // Use search API when user clicks search
            // Only add parameters if they have values to avoid empty parameters
            if (filters.searchQuery && filters.searchQuery.trim() !== '') {
                queryParams.append('specialty', filters.searchQuery.trim());
            }
            if (filters.city && filters.city.trim() !== '') {
                queryParams.append('location', filters.city.trim());
            }
            url = `https://localhost:7285/api/patients/search-doctors`;
            // Only append query string if we have parameters
            if (queryParams.toString()) {
                url += `?${queryParams.toString()}`;
            }
        } else {
            // Use original API for initial load and other cases
            if (filters.searchQuery) queryParams.append('search', filters.searchQuery);
            if (filters.city) queryParams.append('city', filters.city);
            if (filters.availability) queryParams.append('available', 'true');
            if (filters.online) queryParams.append('online', 'true');
            url = `https://localhost:7285/api/doctors/approved?${queryParams.toString()}`;
        }

        console.log('Fetching doctors with URL:', url); // Debug log

        const response = await fetch(url, {
            headers: {
                'Accept': 'application/json'
            }
        });

        if (response.ok) {
            const doctors = await response.json();
            displayDoctors(doctors);
            updateActiveFilters();
        } else {
            console.error('Failed to fetch doctors:', response.status);
            const errorText = await response.text();
            console.error('Error details:', errorText); // Debug log
            displayDoctors([]); // Show no doctors message
        }
    } catch (error) {
        console.error('Error fetching doctors:', error);
        displayDoctors([]); // Show no doctors message
    }
}

// Rate limiting helper for Nominatim API
async function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// Display doctors and update map
function displayDoctors(doctors) {
    const doctorsList = document.getElementById('doctorsList');
    doctorsList.innerHTML = '';

    if (!doctors || doctors.length === 0) {
        doctorsList.innerHTML = '<div class="no-results">Sonuç bulunamadı</div>';
        return;
    }

    // Create doctor cards and collect doctor data for map
    const doctorsForMap = doctors.map(doctor => {
        // Create doctor card
        const doctorCard = document.createElement('div');
        doctorCard.className = 'doctor-card';
        doctorCard.dataset.doctorId = doctor.id.toString();
        doctorCard.innerHTML = `
            <div class="doctor-info">
                <img src="${doctor.imageUrl || 'default-doctor-image.jpg'}" alt="${doctor.fullName}" class="doctor-image">
                <div class="doctor-details">
                    <h3>${doctor.fullName}</h3>
                    <p class="specialty">${doctor.specialty}</p>
                    <div class="rating">
                        ${'<i class="fas fa-star"></i>'.repeat(doctor.rating || 5)}
                        <span>${doctor.reviewCount || 0} görüş</span>
                    </div>
                    <p class="location">
                        <i class="fas fa-map-marker-alt"></i>
                        ${doctor.address || `${doctor.city}, ${doctor.town}`}
                    </p>
                </div>
            </div>
            <div class="availability">
                <div class="date-selector"></div>
                <div class="time-slots"></div>
            </div>
        `;

        // Add click handler for the doctor info section only
        const doctorInfo = doctorCard.querySelector('.doctor-info');
        doctorInfo.addEventListener('click', function(e) {
            // Find all expanded cards except this one
            const otherExpandedCards = document.querySelectorAll('.doctor-card.expanded:not([data-doctor-id="' + doctor.id + '"])');
            otherExpandedCards.forEach(card => card.classList.remove('expanded'));
            
            // Toggle this card
            doctorCard.classList.toggle('expanded');
            
            // Load availability if not already loaded
            if (doctorCard.classList.contains('expanded') && !doctorCard.dataset.loaded) {
                console.log('Loading availability for doctor:', doctor.id);
                loadAvailability(doctorCard);
                doctorCard.dataset.loaded = 'true';
            }
        });

        // Prevent availability section clicks from closing the card
        const availabilitySection = doctorCard.querySelector('.availability');
        availabilitySection.addEventListener('click', function(e) {
            e.stopPropagation();
        });

        doctorsList.appendChild(doctorCard);

        // Return doctor data for map
        return {
            id: doctor.id,
            name: doctor.fullName,
            specialty: doctor.specialty,
            address: doctor.address || `${doctor.city}, ${doctor.town}`,
            rating: doctor.rating || 5,
            reviewCount: doctor.reviewCount || 0
        };
    });

    // Update map with all doctors
    updateMapWithDoctors(doctorsForMap);
}

// Update map with rate limiting
async function updateMapWithDoctors(doctors) {
    // Clear existing markers
    markers.forEach(marker => marker.remove());
    markers = [];

    // Add new markers with rate limiting
    for (const doctor of doctors) {
        await addDoctorToMap(doctor);
        await delay(1000); // 1 second delay between geocoding requests
    }

    // If we have markers, fit the map to show all markers
    if (markers.length > 0) {
        const group = L.featureGroup(markers);
        map.fitBounds(group.getBounds().pad(0.1));
    }
}

function updateActiveFilters() {
    const activeFiltersContainer = document.querySelector('.active-filters');
    activeFiltersContainer.innerHTML = '';

    // Add active filters
    if (activeFilters.searchQuery) {
        addActiveFilter('search', activeFilters.searchQuery);
    }
    if (activeFilters.city) {
        addActiveFilter('city', activeFilters.city);
    }
    if (activeFilters.availability) {
        addActiveFilter('availability', 'Uygun Tarihler');
    }
    if (activeFilters.online) {
        addActiveFilter('online', 'Online Görüşme');
    }
}

function addActiveFilter(type, value) {
    const activeFiltersContainer = document.querySelector('.active-filters');
    const filterElement = document.createElement('div');
    filterElement.className = 'active-filter';
    filterElement.innerHTML = `
        <span>${value}</span>
        <i class="fas fa-times remove-filter" data-filter-type="${type}"></i>
    `;

    filterElement.querySelector('.remove-filter').addEventListener('click', () => {
        removeFilter(type);
    });

    activeFiltersContainer.appendChild(filterElement);
}

function removeFilter(type) {
    switch (type) {
        case 'search':
            activeFilters.searchQuery = '';
            document.getElementById('searchInput').value = '';
            break;
        case 'city':
            activeFilters.city = '';
            document.getElementById('locationInput').value = '';
            break;
        case 'availability':
            activeFilters.availability = false;
            document.querySelector('[data-filter="available"]').classList.remove('active');
            break;
        case 'online':
            activeFilters.online = false;
            document.querySelector('[data-filter="online"]').classList.remove('active');
            break;
    }
    // Use the default endpoint to show all doctors
    fetchDoctors(activeFilters, false);
}

function searchDoctors() {
    const searchInput = document.getElementById('searchInput');
    const locationInput = document.getElementById('locationInput');
    
    activeFilters.searchQuery = searchInput?.value || '';
    activeFilters.city = locationInput?.value || '';
    
    // Always use search API when using the search function
    fetchDoctors(activeFilters, true);
}

function selectSpecialty(specialty) {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.value = specialty;
        searchDoctors();
    }
}

// Initialize map when the page loads
function initMap() {
    map = L.map('map').setView([38.4192, 27.1287], 12); // İzmir coordinates
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);
}

// Function to geocode address and add marker
async function addDoctorToMap(doctor) {
    try {
        const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(doctor.address)}`);
        const data = await response.json();
        
        if (data.length > 0) {
            const { lat, lon } = data[0];
            const marker = L.marker([lat, lon]).addTo(map);
            
            const popupContent = `
                <div class="map-popup">
                    <h4>${doctor.name}</h4>
                    <p>${doctor.specialty}</p>
                    <p><i class="fas fa-map-marker-alt"></i> ${doctor.address}</p>
                    ${doctor.rating ? `
                        <div class="rating">
                            ${'★'.repeat(doctor.rating)}${'☆'.repeat(5-doctor.rating)}
                            <span>(${doctor.reviewCount} görüş)</span>
                        </div>
                    ` : ''}
                </div>
            `;
            
            marker.bindPopup(popupContent);
            markers.push(marker);
            
            if (markers.length === 1) {
                map.setView([lat, lon], 12);
            }
        }
    } catch (error) {
        console.error('Error geocoding address:', error);
    }
}

// Update the loadAvailability function with correct API endpoint
async function loadAvailability(card) {
    const doctorId = parseInt(card.dataset.doctorId);
    const dateSelector = card.querySelector('.date-selector');
    const timeSlots = card.querySelector('.time-slots');
    
    try {
        // Fetch doctor's availability schedule
        const availabilityResponse = await fetch(`https://localhost:7285/api/doctors/${doctorId}/availability`);
        if (!availabilityResponse.ok) {
            throw new Error('Failed to fetch doctor availability');
        }
        const availability = await availabilityResponse.json();

        // Fetch doctor's appointments
        const appointmentsResponse = await fetch(`https://localhost:7285/api/patients/get-appointment/${doctorId}`);
        let appointments = [];
        if (appointmentsResponse.ok) {
            appointments = await appointmentsResponse.json();
        } else if (appointmentsResponse.status !== 404) { // 404 means no appointments, which is fine
            console.warn('Failed to fetch appointments:', await appointmentsResponse.text());
        }

        // Get next 7 days and filter only available days based on doctor's schedule
        const next7Days = getNext7Days();
        const availableDays = next7Days.filter(date => {
            const dayOfWeek = new Date(date.value).getDay() || 7; // Convert Sunday (0) to 7
            return availability.some(a => a.day === dayOfWeek);
        });

        if (availableDays.length === 0) {
            dateSelector.innerHTML = '<div class="no-slots">Bu hafta için müsait gün bulunmamaktadır</div>';
            timeSlots.innerHTML = '';
            return;
        }
        
        // Add date buttons only for available days
        dateSelector.innerHTML = availableDays.map((date, index) => `
            <button class="date-btn ${index === 0 ? 'active' : ''}" data-date="${date.value}">
                ${date.label}
            </button>
        `).join('');

        // Add click handlers for date buttons
        dateSelector.querySelectorAll('.date-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                dateSelector.querySelectorAll('.date-btn').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
                updateTimeSlots(timeSlots, availability, appointments, e.target.dataset.date, doctorId);
            });
        });

        // Initially load time slots for first available date
        if (availableDays.length > 0) {
            updateTimeSlots(timeSlots, availability, appointments, availableDays[0].value, doctorId);
        }

    } catch (error) {
        console.error('Error loading availability:', error);
        timeSlots.innerHTML = '<div class="error-message">Müsaitlik bilgisi yüklenirken hata oluştu</div>';
    }
}

function updateTimeSlots(timeSlotsElement, availability, appointments, selectedDate, doctorId) {
    const date = new Date(selectedDate);
    const dayOfWeek = date.getDay() || 7; // Convert Sunday (0) to 7

    // Find doctor's availability for this day
    const dayAvailability = availability.find(a => a.day === dayOfWeek);
    
    if (!dayAvailability) {
        timeSlotsElement.innerHTML = '<div class="no-slots">Bu gün için müsaitlik bulunmamaktadır</div>';
        return;
    }

    // Parse start and end times from availability
    const [startHour, startMinute] = dayAvailability.startTime.split(':').map(Number);
    const [endHour, endMinute] = dayAvailability.endTime.split(':').map(Number);

    // Generate time slots only for the available hours
    const slots = [];
    let currentHour = startHour;
    let currentMinute = startMinute;
    
    while (currentHour < endHour || (currentHour === endHour && currentMinute <= endMinute)) {
        slots.push(`${currentHour.toString().padStart(2, '0')}:${currentMinute.toString().padStart(2, '0')}`);
        currentMinute += 30;
        if (currentMinute >= 60) {
            currentHour += 1;
            currentMinute = 0;
        }
    }
    
    // Filter out already booked appointments for the selected date
    const bookedSlots = appointments
        .filter(app => {
            const appDate = new Date(app.appointmentDate);
            return appDate.toDateString() === date.toDateString();
        })
        .map(app => {
            const appDate = new Date(app.appointmentDate);
            return `${appDate.getHours().toString().padStart(2, '0')}:${appDate.getMinutes().toString().padStart(2, '0')}`;
        });

    // Only show time slots within doctor's availability
    timeSlotsElement.innerHTML = slots.map(slot => {
        const isBooked = bookedSlots.includes(slot);
        return `
            <div class="time-slot ${isBooked ? 'unavailable' : ''}" 
                 data-time="${slot}" 
                 ${!isBooked ? `onclick="selectTimeSlot('${selectedDate} ${slot}', ${doctorId})"` : ''}>
                ${slot}
            </div>
        `;
    }).join('');
}

function getNext7Days() {
    const days = [];
    const today = new Date();
    
    for (let i = 0; i < 7; i++) {
        const date = new Date(today);
        date.setDate(today.getDate() + i);
        days.push({
            value: date.toISOString().split('T')[0],
            label: i === 0 ? 'Bugün' : 
                   i === 1 ? 'Yarın' : 
                   date.toLocaleDateString('tr-TR', { weekday: 'short', day: 'numeric' })
        });
    }
    
    return days;
}

// Update the selectTimeSlot function
async function selectTimeSlot(dateTime, doctorId) {
    const token = localStorage.getItem('token');
    if (!token) {
        if (confirm('You need to sign in to book an appointment. Would you like to sign in now?')) {
            localStorage.setItem('pendingAppointment', JSON.stringify({
                dateTime: dateTime,
                doctorId: doctorId
            }));
            window.location.href = 'index.html';
        }
        return;
    }
    
    try {
        const response = await fetch(`https://localhost:7285/api/patients/make-appointment/${doctorId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                appointmentDate: dateTime
            })
        });

        if (response.ok) {
            alert('Appointment booked successfully!');
            // Reload availability to update the UI
            const doctorCard = document.querySelector(`[data-doctor-id="${doctorId}"]`);
            if (doctorCard) {
                loadAvailability(doctorCard);
            }
        } else {
            const error = await response.text();
            alert(`Failed to book appointment: ${error}`);
        }
    } catch (error) {
        console.error('Error booking appointment:', error);
        alert('Failed to book appointment. Please try again.');
    }
}

// Initialize everything when the DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize map
    initMap();
    
    // Fetch specialties
    fetchSpecialties();
    
    // Fetch cities
    fetchCities();

    // Set up specialty item click listeners
    document.querySelectorAll('.specialty-item').forEach(item => {
        item.addEventListener('click', function() {
            const specialty = this.dataset.specialty;
            if (specialty) {
                document.getElementById('searchInput').value = specialty;
            }
        });
    });

    // Set up search button click listener
    const searchButton = document.getElementById('searchButton');
    if (searchButton) {
        searchButton.addEventListener('click', searchDoctors);
    }

    // Set up filter tab listeners
    document.querySelectorAll('.filter-tab').forEach(tab => {
        tab.addEventListener('click', function() {
            const filterType = this.dataset.filter;
            
            if (filterType === 'all') {
                // Reset all filters
                activeFilters.availability = false;
                activeFilters.online = false;
                activeFilters.searchQuery = '';
                activeFilters.city = '';
                document.getElementById('searchInput').value = '';
                document.getElementById('locationInput').value = '';
                document.querySelectorAll('.filter-tab').forEach(t => t.classList.remove('active'));
                this.classList.add('active');
                // Show all doctors using default endpoint
                fetchDoctors({}, false);
            } else {
                document.querySelector('[data-filter="all"]').classList.remove('active');
                this.classList.toggle('active');
                
                if (filterType === 'available') {
                    activeFilters.availability = this.classList.contains('active');
                } else if (filterType === 'online') {
                    activeFilters.online = this.classList.contains('active');
                }
                // Apply filters using default endpoint
                fetchDoctors(activeFilters, false);
            }
        });
    });

    // Set up search input listeners for Enter key
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchDoctors();
            }
        });
    }
    
    // Initial load - show all doctors using default endpoint
    fetchDoctors({}, false);
}); 