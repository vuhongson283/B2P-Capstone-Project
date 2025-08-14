import axios from "axios";
import NProgress from "nprogress";

NProgress.configure({
  showSpinner: false,
  trickleSpeed: 100,
});

const instance = axios.create({
  baseURL: "https://localhost:7227/api/",
  timeout: 10000,
});

// Add a request interceptor
instance.interceptors.request.use(
  function (config) {
    NProgress.start();
    
    // ✅ FIX: Dùng 'accessToken' thay vì 'token'
    const token = localStorage.getItem('accessToken'); // ✅ Changed from 'token' to 'accessToken'
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
      console.log('🔐 Added accessToken to request:', token.substring(0, 20) + '...');
    } else {
      console.warn('⚠️ No accessToken found in localStorage');
    }
    
    return config;
  },
  function (error) {
    NProgress.done();
    return Promise.reject(error);
  }
);

// Add a response interceptor
instance.interceptors.response.use(
  function (response) {
    NProgress.done();
    return response.data ? response.data : response;
  },
  function (error) {
    NProgress.done();
    
    // ✅ FIX: Handle 401 with correct token names
    if (error.response?.status === 401) {
      console.error('🚨 401 Unauthorized - Token expired or invalid');
      localStorage.removeItem('accessToken'); // ✅ Changed
      localStorage.removeItem('refreshToken'); // ✅ Added
      localStorage.removeItem('user');
      // Redirect to login if not already there
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }
    
    return error && error.response && error.response.data
      ? error.response.data
      : Promise.reject(error);
  }
);

export default instance;