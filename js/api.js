// API Service for CEO Admin Dashboard

const API_BASE_URL = 'http://localhost:5115'; // Change this to your actual API URL
const IS_DEVELOPMENT = true; // Set to false in production

// Helper function for making API calls
async function apiCall(endpoint, method = 'GET', data = null) {
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
    };
    
    if (data && (method === 'POST' || method === 'PUT')) {
        options.body = JSON.stringify(data);
    }
    
    try {
        // Use fetch with appropriate options based on environment
        let response;
        const url = `${API_BASE_URL}${endpoint}`;
        
        if (IS_DEVELOPMENT) {
            // In development, you might need to bypass certificate validation
            // This is done differently depending on the environment:
            // 1. For browser, user needs to accept the certificate manually
            console.log(`Making ${method} request to: ${url}`);
            response = await fetch(url, options);
        } else {
            // In production with valid SSL certificates
            response = await fetch(url, options);
        }
        
        // Handle unauthorized response
        if (response.status === 401) {
            localStorage.removeItem('token');
            window.location.href = 'login.html';
            return null;
        }
        
        // Handle other error statuses
        if (!response.ok) {
            const errorText = await response.text();
            console.error(`API error (${response.status}): ${errorText}`);
            return { error: `Server error: ${response.status} ${response.statusText}` };
        }
        
        return await response.json();
    } catch (error) {
        console.error('API call error:', error);
        
        // More specific error messaging based on error type
        if (error.name === 'TypeError' && error.message.includes('Failed to fetch')) {
            return { 
                error: 'Connection error. The server may be unavailable or there might be SSL certificate issues. ' +
                       'Please make sure the API server is running and that you have accepted any self-signed certificates.'
            };
        }
        
        return { error: 'Network error, please try again later' };
    }
}

// Authentication
const AuthService = {
    login: async (username, password) => {
        return await apiCall('/api/Auth/login', 'POST', { email: username, password });
    },
    logout: async () => {
        return await apiCall('/api/Auth/logout', 'POST');
    }
};

// Employee Management
const EmployeeService = {
    getAll: async () => {
        return await apiCall('/Employee/employees');
    },
    getPayroll: async () => {
        return await apiCall('/Employee/payroll');
    }
};

// HR Management
const HrService = {
    getEmployees: async () => {
        return await apiCall('/HrManager/employees');
    },
    getPayroll: async () => {
        return await apiCall('/HrManager/payroll');
    },
    getAttendance: async () => {
        return await apiCall('/HrManager/attendance');
    },
    addEmployee: async (employeeData) => {
        return await apiCall('/HrManager/add-employee', 'POST', employeeData);
    },
    updateEmployee: async (employeeData) => {
        return await apiCall('/HrManager/update-employee', 'PUT', employeeData);
    },
    deleteEmployee: async (id) => {
        return await apiCall(`/HrManager/delete-employee/${id}`, 'DELETE');
    }
};

// Payroll Management
const PayrollService = {
    getEmployees: async () => {
        return await apiCall('/PayrollManager');
    }
};

// Integration Service
const IntegrationService = {
    getEmployees: async () => {
        return await apiCall('/Integration/employees');
    },
    getPayroll: async () => {
        return await apiCall('/Integration/payroll');
    },
    getAttendance: async () => {
        return await apiCall('/Integration/attendance');
    },
    getReports: async () => {
        return await apiCall('/Integration/reports');
    },
    addEmployee: async (employeeData) => {
        return await apiCall('/Integration/add-employee', 'POST', employeeData);
    },
    updateEmployee: async (employeeData) => {
        return await apiCall('/Integration/update-employee', 'PUT', employeeData);
    },
    deleteEmployee: async (id) => {
        return await apiCall(`/Integration/delete-employee/${id}`, 'DELETE');
    },
    postAlert: async (alertData) => {
        return await apiCall('/Integration/alerts', 'POST', alertData);
    }
};
