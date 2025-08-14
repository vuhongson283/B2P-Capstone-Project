import axios from "axios";
import NProgress from "nprogress";

NProgress.configure({
  showSpinner: false,
  trickleSpeed: 100,
});
const instance = axios.create({
  baseURL: "https://localhost:7227/api/",
  timeout: 20000, // Tăng từ 1000 lên 10000ms
});
// Add a request interceptor
instance.interceptors.request.use(
  function (config) {
    NProgress.start();
    return config;
  },
  function (error) {
    // Do something with request error
    return Promise.reject(error);
  }
);

// Add a response interceptor
instance.interceptors.response.use(
  function (response) {
    NProgress.done();

    return response.data
      ? response.data // If the response has data, return it directly
      : response; // Otherwise, return the full response
  },
  function (error) {
    NProgress.done();

    // Any status codes that falls outside the range of 2xx cause this function to trigger
    // Do something with response error
    return error && error.response && error.response.data
      ? error.response.data
      : Promise.reject(error);
  }
);
export default instance;
