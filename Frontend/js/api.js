// API endpoints
const BASE_URL = 'https://api-gateway.yourdomain.com/api/v1';

// Authentication
export async function authenticateWithGoogle(googleToken) {
    try {
        const response = await fetch(`${BASE_URL}/auth/google`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ token: googleToken })
        });
        return await response.json();
    } catch (error) {
        console.error('Authentication error:', error);
        throw error;
    }
}

// Doctor Registration
export async function registerDoctor(doctorData, token) {
    try {
        const response = await fetch(`${BASE_URL}/doctors/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(doctorData)
        });
        return await response.json();
    } catch (error) {
        console.error('Registration error:', error);
        throw error;
    }
}

// Get Cities
export async function getCities() {
    try {
        const response = await fetch(`${BASE_URL}/cities`);
        return await response.json();
    } catch (error) {
        console.error('Error fetching cities:', error);
        throw error;
    }
}

// Get Towns
export async function getTowns(city) {
    try {
        const response = await fetch(`${BASE_URL}/towns?city=${encodeURIComponent(city)}`);
        return await response.json();
    } catch (error) {
        console.error('Error fetching towns:', error);
        throw error;
    }
} 