import { authenticateWithGoogle, registerDoctor, getCities, getTowns } from './api.js';

// Store the authentication token
let authToken = null;
let googleAuth = null;

// Initialize Google Auth
function initGoogleAuth() {
    gapi.load('auth2', function() {
        gapi.auth2.init({
            client_id: 'YOUR_GOOGLE_CLIENT_ID',
            scope: 'profile email'
        }).then(function(auth) {
            googleAuth = auth;
        }).catch(function(error) {
            showError('Failed to initialize Google authentication');
            console.error('Auth init error:', error);
        });
    });
}

// Initialize the form
document.addEventListener('DOMContentLoaded', async () => {
    initGoogleAuth();

    // Initialize city dropdown
    const citySelect = document.getElementById('city');
    const townSelect = document.getElementById('town');
    
    try {
        const { cities } = await getCities();
        cities.forEach(city => {
            const option = document.createElement('option');
            option.value = city;
            option.textContent = city;
            citySelect.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading cities:', error);
        showError('Failed to load cities. Please refresh the page.');
    }

    // Handle city selection
    citySelect.addEventListener('change', async (e) => {
        const selectedCity = e.target.value;
        if (selectedCity) {
            try {
                const { towns } = await getTowns(selectedCity);
                townSelect.innerHTML = '<option value="">Select Town</option>';
                towns.forEach(town => {
                    const option = document.createElement('option');
                    option.value = town;
                    option.textContent = town;
                    townSelect.appendChild(option);
                });
                townSelect.disabled = false;
            } catch (error) {
                console.error('Error loading towns:', error);
                showError('Failed to load towns. Please try again.');
            }
        } else {
            townSelect.innerHTML = '<option value="">Select Town</option>';
            townSelect.disabled = true;
        }
    });

    // Set default times
    document.getElementById('startTime').value = '09:00';
    document.getElementById('endTime').value = '17:00';
});

// Handle Google Authentication
window.handleGoogleAuth = async () => {
    const emailInput = document.getElementById('email');
    const email = emailInput.value.trim();

    if (!email) {
        showError('Please enter your email address');
        return;
    }

    if (!googleAuth) {
        showError('Google authentication is not initialized. Please refresh the page.');
        return;
    }

    try {
        const googleUser = await googleAuth.signIn();
        const googleEmail = googleUser.getBasicProfile().getEmail();

        if (googleEmail.toLowerCase() !== email.toLowerCase()) {
            showError('The email entered does not match your Google account email');
            return;
        }

        const id_token = googleUser.getAuthResponse().id_token;
        const authResponse = await authenticateWithGoogle(id_token);
        authToken = authResponse.token;
        
        // Show the doctor details form
        document.getElementById('doctor-details').style.display = 'block';
        
        // Pre-fill name if available
        const googleName = googleUser.getBasicProfile().getName();
        if (googleName) {
            document.getElementById('fullName').value = googleName;
        }

        // Disable email input and auth button
        emailInput.disabled = true;
        document.querySelector('.google-auth').disabled = true;
        
        showSuccess('Successfully authenticated with Google');
    } catch (error) {
        console.error('Google sign-in error:', error);
        showError('Failed to authenticate with Google. Please try again.');
    }
};

// Helper function to show errors
function showError(message) {
    const errorDiv = document.getElementById('error-message');
    errorDiv.textContent = message;
    errorDiv.style.display = 'block';
    
    // Hide error after 5 seconds
    setTimeout(() => {
        errorDiv.style.display = 'none';
    }, 5000);
}

// Helper function to show success messages
function showSuccess(message) {
    const successDiv = document.createElement('div');
    successDiv.className = 'success';
    successDiv.textContent = message;
    
    const formGroup = document.querySelector('.form-group');
    formGroup.appendChild(successDiv);
    
    setTimeout(() => {
        successDiv.remove();
    }, 3000);
}

// Handle form submission
document.getElementById('registration-form').addEventListener('submit', async (e) => {
    e.preventDefault();

    if (!authToken) {
        showError('Please authenticate with Google first');
        return;
    }

    // Get selected days from checkboxes
    const selectedDays = Array.from(document.querySelectorAll('input[name="availableDays"]:checked'))
        .map(checkbox => checkbox.value);

    if (selectedDays.length === 0) {
        showError('Please select at least one available day');
        return;
    }

    const formData = {
        email: document.getElementById('email').value,
        fullName: document.getElementById('fullName').value,
        specialty: document.getElementById('specialty').value,
        areaOfInterest: document.getElementById('areaOfInterest').value.split(',').map(item => item.trim()),
        availableDays: selectedDays,
        availableHours: {
            start: document.getElementById('startTime').value,
            end: document.getElementById('endTime').value
        },
        address: {
            city: document.getElementById('city').value,
            town: document.getElementById('town').value,
            fullAddress: document.getElementById('fullAddress').value
        }
    };

    try {
        const response = await registerDoctor(formData, authToken);
        if (response.doctorId) {
            // Show success message
            const successDiv = document.createElement('div');
            successDiv.className = 'success';
            successDiv.innerHTML = '<i class="fas fa-check-circle"></i> Registration successful! Your application is pending approval.';
            document.getElementById('registration-form').style.display = 'none';
            document.querySelector('.container').appendChild(successDiv);
            
            // Redirect to dashboard after 3 seconds
            setTimeout(() => {
                window.location.href = '/doctor-dashboard.html';
            }, 3000);
        }
    } catch (error) {
        console.error('Registration error:', error);
        showError('Failed to register. Please check your information and try again.');
    }
}); 