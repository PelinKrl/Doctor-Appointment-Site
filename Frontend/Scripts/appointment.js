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

    // Add click handlers to city items
    dropdownContainer.querySelectorAll('.city-item').forEach(item => {
        item.addEventListener('click', function() {
            const cityName = this.dataset.cityName;
            document.getElementById('locationInput').value = cityName;
            activeFilters.city = cityName;
            hideDropdown(dropdownContainer);
            fetchDoctors(activeFilters);
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

    // Handle input changes
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

    // Add click handlers to specialty items
    dropdownContainer.querySelectorAll('.specialty-item').forEach(item => {
        item.addEventListener('click', function() {
            const specialty = this.dataset.specialty;
            document.getElementById('searchInput').value = specialty;
            hideSpecialtyDropdown(dropdownContainer);
            searchDoctors();
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
            if (filters.searchQuery) queryParams.append('specialty', filters.searchQuery);
            if (filters.city) queryParams.append('city', filters.city);
            url = `https://localhost:7285/api/patients/search-doctors?${queryParams.toString()}`;
        } else {
            // Use original API for initial load and other cases
            if (filters.searchQuery) queryParams.append('search', filters.searchQuery);
            if (filters.city) queryParams.append('city', filters.city);
            if (filters.availability) queryParams.append('available', 'true');
            if (filters.online) queryParams.append('online', 'true');
            url = `https://localhost:7285/api/doctors/approved?${queryParams.toString()}`;
        }

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
            displayDoctors([]); // Show no doctors message
        }
    } catch (error) {
        console.error('Error fetching doctors:', error);
        displayDoctors([]); // Show no doctors message
    }
}

function displayDoctors(doctors) {
    const doctorsList = document.getElementById('doctorsList');
    if (!doctorsList) return;
    
    doctorsList.innerHTML = '';

    if (!doctors || doctors.length === 0) {
        doctorsList.innerHTML = `
            <div style="text-align: center; padding: 40px; background: white; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                <i class="fas fa-user-md" style="font-size: 48px; color: #007bff; margin-bottom: 20px;"></i>
                <h2 style="color: #333; margin-bottom: 10px;">No Doctors Available</h2>
                <p style="color: #666;">There are currently no doctors available. Please try again later or modify your search criteria.</p>
            </div>
        `;
        return;
    }

    doctors.forEach(doctor => {
        const doctorCard = document.createElement('div');
        doctorCard.className = 'doctor-card';
        doctorCard.innerHTML = `
            <div class="doctor-info">
                <img src="../assets/default-doctor.png" alt="Doctor" class="doctor-image">
                <div class="doctor-details">
                    <h3>Dr. ${doctor.fullName}</h3>
                    <p class="specialty">${doctor.specialty}</p>
                    <div class="rating">
                        <span class="stars">★★★★★</span>
                        <span class="review-count">38 reviews</span>
                    </div>
                    <p class="location">${doctor.city}, ${doctor.town}</p>
                </div>
            </div>
            <div class="availability">
                <div class="date-selector">
                    <button class="date-btn active">Today</button>
                    <button class="date-btn">Tomorrow</button>
                    <button class="date-btn">Wed</button>
                    <button class="date-btn">Thu</button>
                </div>
                <div class="time-slots">
                    <button class="time-btn" data-time="08:30" data-doctor="${doctor.fullName}">08:30</button>
                    <button class="time-btn" data-time="09:00" data-doctor="${doctor.fullName}">09:00</button>
                    <button class="time-btn" data-time="09:30" data-doctor="${doctor.fullName}">09:30</button>
                    <button class="time-btn" data-time="10:00" data-doctor="${doctor.fullName}">10:00</button>
                </div>
            </div>
        `;

        // Add click listeners for time slots
        const timeSlots = doctorCard.querySelectorAll('.time-btn');
        timeSlots.forEach(slot => {
            slot.addEventListener('click', function() {
                const time = this.dataset.time;
                const doctorName = this.dataset.doctor;
                selectTimeSlot(time, doctorName);
            });
        });

        doctorsList.appendChild(doctorCard);
    });
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
    fetchDoctors(activeFilters);
}

function searchDoctors() {
    activeFilters.searchQuery = document.getElementById('searchInput')?.value || '';
    activeFilters.city = document.getElementById('locationInput')?.value || '';
    // Pass true to indicate this is a search operation
    fetchDoctors(activeFilters, true);
}

function selectSpecialty(specialty) {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.value = specialty;
        searchDoctors();
    }
}

function selectTimeSlot(time, doctorName) {
    const token = localStorage.getItem('token');
    if (!token) {
        // If user is not signed in, redirect to Google sign in
        if (confirm('You need to sign in to book an appointment. Would you like to sign in now?')) {
            // Store appointment details for after sign-in
            localStorage.setItem('pendingAppointment', JSON.stringify({
                time: time,
                doctorName: doctorName
            }));
            window.location.href = 'index.html';
        }
        return;
    }
    
    // If user is signed in, proceed with booking
    alert(`Selected appointment time: ${time} with Dr. ${doctorName}`);
    // Here you would typically open a confirmation modal or proceed with booking
}

// Initialize everything when the DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Fetch specialties first
    fetchSpecialties();
    
    // Fetch cities
    fetchCities();

    // Set up specialty item click listeners
    document.querySelectorAll('.specialty-item').forEach(item => {
        item.addEventListener('click', function() {
            const specialty = this.dataset.specialty;
            if (specialty) {
                selectSpecialty(specialty);
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
                activeFilters.availability = false;
                activeFilters.online = false;
                document.querySelectorAll('.filter-tab').forEach(t => t.classList.remove('active'));
                this.classList.add('active');
            } else {
                document.querySelector('[data-filter="all"]').classList.remove('active');
                this.classList.toggle('active');
                
                if (filterType === 'available') {
                    activeFilters.availability = this.classList.contains('active');
                } else if (filterType === 'online') {
                    activeFilters.online = this.classList.contains('active');
                }
            }
            
            fetchDoctors(activeFilters);
        });
    });

    // Set up search input listeners for real-time search
    const searchInput = document.getElementById('searchInput');
    const locationInput = document.getElementById('locationInput');
    
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchDoctors();
            }
        });
    }

    // Initial load of all doctors using the original API
    fetchDoctors();
}); 