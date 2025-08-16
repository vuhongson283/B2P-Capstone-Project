import axios from "../utils/axiosCustomize";

// ✅ LOGGER SERVICE INTÉGRÉ
class Logger {
  static levels = {
    ERROR: 0,
    WARN: 1,
    INFO: 2,
    DEBUG: 3
  };

  static currentLevel = process.env.NODE_ENV === 'production' ? 1 : 3;

  static log(level, message, data = {}) {
    if (this.levels[level] > this.currentLevel) return;
    
    const logEntry = {
      timestamp: new Date().toISOString(),
      level,
      message,
      data,
      url: window.location.href,
      userAgent: navigator.userAgent
    };

    // Console logging avec couleurs
    const colors = {
      ERROR: 'color: #ff4757; font-weight: bold;',
      WARN: 'color: #ffa502; font-weight: bold;',
      INFO: 'color: #2ed573; font-weight: bold;',
      DEBUG: 'color: #5352ed; font-weight: bold;'
    };

    console.log(
      `%c[${level}] ${message}`,
      colors[level],
      data
    );

    // Envoyer vers service externe en production
    if (process.env.NODE_ENV === 'production') {
      this.sendToService(logEntry);
    }
  }

  static error(message, data) { this.log('ERROR', message, data); }
  static warn(message, data) { this.log('WARN', message, data); }
  static info(message, data) { this.log('INFO', message, data); }
  static debug(message, data) { this.log('DEBUG', message, data); }

  static sendToService(logEntry) {
    // Intégration avec Sentry, LogRocket, etc.
    try {
      fetch('/api/logs', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(logEntry)
      }).catch(() => {}); // Silent fail
    } catch (error) {
      // Ne pas logger les erreurs de logging pour éviter les boucles
    }
  }
}

// ✅ WRAPPER AXIOS AVEC LOGGING AUTOMATIQUE
const loggedAxios = {
  async request(config) {
    const startTime = Date.now();
    const { method, url, data } = config;
    
    Logger.info(`🚀 API Request: ${method?.toUpperCase()} ${url}`, {
      method,
      url,
      data: data && typeof data === 'object' ? data : undefined
    });

    try {
      const response = await axios.request(config);
      const duration = Date.now() - startTime;
      
      Logger.info(`✅ API Success: ${method?.toUpperCase()} ${url} (${duration}ms)`, {
        status: response.status,
        duration,
        dataSize: response.data ? JSON.stringify(response.data).length : 0
      });
      
      return response;
    } catch (error) {
      const duration = Date.now() - startTime;
      
      Logger.error(`❌ API Error: ${method?.toUpperCase()} ${url} (${duration}ms)`, {
        status: error.response?.status,
        message: error.message,
        duration,
        errorData: error.response?.data
      });
      
      throw error;
    }
  },

  get: (url, config) => loggedAxios.request({ ...config, method: 'GET', url }),
  post: (url, data, config) => loggedAxios.request({ ...config, method: 'POST', url, data }),
  put: (url, data, config) => loggedAxios.request({ ...config, method: 'PUT', url, data }),
  delete: (url, config) => loggedAxios.request({ ...config, method: 'DELETE', url })
};

// Configuration timeout
axios.defaults.timeout = 5000;

/* ===============================
   🏟️ COURT CATEGORY MANAGEMENT
================================ */
const getAllCourtCategories = (search, pageNumber, pageSize) => {
  return loggedAxios.get(
    `CourtCategory/get-all-court-categories?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
};

const addCourtCategory = async (categoryName) => {
  try {
    Logger.debug('Adding court category', { categoryName });
    const response = await loggedAxios.post(
      `CourtCategory/add-court-category?cateName=${encodeURIComponent(categoryName)}`
    );
    Logger.info('Court category added successfully', { categoryName });
    return response;
  } catch (error) {
    Logger.error("Failed to add court category", { categoryName, error: error.message });
    throw error;
  }
};

const updateCourtCategory = (categoryData) => {
  Logger.debug('Updating court category', { categoryData });
  return loggedAxios.put("CourtCategory/update-court-category", categoryData);
};

const getCourtCategoryById = (categoryId) => {
  return loggedAxios.get(`CourtCategory/get-court-category-by-id?categoryId=${categoryId}`);
};

const deleteCourtCategory = (categoryId) => {
  Logger.warn('Deleting court category', { categoryId });
  return loggedAxios.delete(`CourtCategory/delete-court-category?categoryId=${categoryId}`);
};

/* ===============================
   📝 BLOG MANAGEMENT
================================ */
const getAllBlogs = (queryParams = {}) => {
  const {
    search = "",
    page = 1,
    pageSize = 10,
    sortBy = "postAt",
    sortDirection = "desc",
  } = queryParams;

  return loggedAxios.get("Blog", {
    params: {
      Search: search,
      Page: page,
      PageSize: pageSize,
      SortBy: sortBy,
      SortDirection: sortDirection,
    },
  });
};

const getBlogById = (blogId) => loggedAxios.get(`Blog/${blogId}`);
const createBlog = (blogData) => {
  Logger.info('Creating new blog', { title: blogData.title });
  return loggedAxios.post("Blog", blogData);
};
const updateBlog = (blogId, blogData) => loggedAxios.put(`Blog/${blogId}`, blogData);
const deleteBlog = (blogId, userId) => {
  Logger.warn('Deleting blog', { blogId, userId });
  return loggedAxios.delete(`Blog/${blogId}?userId=${userId}`);
};

const getBlogsByUserId = (userId, queryParams = {}) => {
  const {
    search = "",
    page = 1,
    pageSize = 10,
    sortBy = "postAt",
    sortDirection = "desc",
  } = queryParams;

  return loggedAxios.get(`Blog/user/${userId}`, {
    params: {
      Search: search,
      Page: page,
      PageSize: pageSize,
      SortBy: sortBy,
      SortDirection: sortDirection,
    },
  });
};

/* ===============================
   💬 COMMENT MANAGEMENT
================================ */
const getAllComments = (queryParams = {}) => {
  const {
    search = "",
    page = 1,
    pageSize = 10,
    sortBy = "postAt",
    sortDirection = "desc",
  } = queryParams;

  return loggedAxios.get("Comment", {
    params: {
      Search: search,
      Page: page,
      PageSize: pageSize,
      SortBy: sortBy,
      SortDirection: sortDirection,
    },
  });
};

const createComment = (commentData) => loggedAxios.post("Comment", commentData);
const updateComment = (commentId, commentData) => loggedAxios.put(`Comment/${commentId}`, commentData);
const deleteComment = (commentId, userId, roleId) =>
  loggedAxios.delete(`Comment/${commentId}?userId=${userId}&roleId=${roleId}`);

const getCommentsByUserId = (userId, queryParams = {}) => {
  const {
    search = "",
    page = 1,
    pageSize = 10,
    sortBy = "postAt",
    sortDirection = "desc",
  } = queryParams;

  return loggedAxios.get(`Comment/user/${userId}`, {
    params: {
      Search: search,
      Page: page,
      PageSize: pageSize,
      SortBy: sortBy,
      SortDirection: sortDirection,
    },
  });
};

/* ===============================
   🖼️ IMAGE MANAGEMENT
================================ */
const getUserImage = (userId) => loggedAxios.get(`Image/user/${userId}`);

const uploadUserImage = (file, userId, caption = null) => {
  Logger.info('Uploading user image', { userId, fileName: file.name, fileSize: file.size });
  
  const formData = new FormData();
  formData.append("file", file);
  formData.append("entityId", userId.toString());
  if (caption) formData.append("caption", caption);

  return loggedAxios.post("Image/upload-user", formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 30000,
  });
};

const uploadslideImage = (file, slideId, caption = null) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("entityId", slideId.toString());
  if (caption) formData.append("caption", caption);

  return loggedAxios.post("Image/upload-slide", formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 30000,
  });
};

const updateUserImage = (imageId, file, order = null, caption = null) => {
  const formData = new FormData();
  if (file) formData.append("file", file);
  if (order !== null) formData.append("order", order.toString());
  if (caption) formData.append("caption", caption);

  return loggedAxios.put(`Image/update-image/${imageId}`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 30000,
  });
};

const updateSlideImage = updateUserImage;
const updateImage = updateUserImage;

const uploadBlogImage = (file, blogId, caption = null) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("entityId", blogId.toString());
  if (caption) formData.append("caption", caption);

  return loggedAxios.post("Image/upload-blog", formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 30000,
  });
};

const getBlogImages = (blogId) => loggedAxios.get(`Image/blog/${blogId}`);
const deleteImage = (imageId) => {
  Logger.warn('Deleting image', { imageId });
  return loggedAxios.delete(`Image/${imageId}`);
};

/* ===============================
   🔒 PASSWORD RESET
================================ */
const forgotPasswordByEmail = (email) => {
  Logger.info('Password reset requested by email', { email: email.substring(0, 3) + '***' });
  return loggedAxios.post("User/forgot-password-by-email", { email });
};

const resetPasswordByEmail = (email, otpCode, newPassword, confirmPassword) => {
  Logger.info('Resetting password by email', { email: email.substring(0, 3) + '***' });
  return loggedAxios.post("User/reset-password-by-email", {
    email,
    otpCode,
    newPassword,
    confirmPassword,
  });
};

const resendOtpByEmail = (email) => loggedAxios.post("User/resend-otp-by-email", { email });

const forgotPasswordBySms = (phoneNumber) => {
  Logger.info('Password reset requested by SMS', { phone: phoneNumber.substring(0, 3) + '***' });
  return loggedAxios.post("User/forgot-password-by-sms", { phoneNumber });
};

const resetPasswordBySms = (phoneNumber, otpCode, newPassword, confirmPassword) => {
  return loggedAxios.post("User/reset-password-by-sms", {
    phoneNumber,
    otpCode,
    newPassword,
    confirmPassword,
  });
};
const resendOtpBySms = (phoneNumber) => loggedAxios.post("User/resend-otp-by-sms", { phoneNumber });

/* ===============================
   📋 USER MANAGEMENT
================================ */
const getUserById = (userId) => loggedAxios.get(`User/get-user-by-id?userId=${userId}`);
const updateUserProfile = (userId, body) => {
  Logger.info('Updating user profile', { userId });
  return loggedAxios.put(`User/update-user?userId=${userId}`, body);
};
const changePassword = (body) => {
  Logger.info('Changing user password', { userId: body.userId });
  return loggedAxios.put(`User/change-password`, body);
};
const checkPasswordStatus = (userId) => loggedAxios.get(`User/check-password-status/${userId}`);

/* ===============================
   🏦 BANK TYPE
================================ */
const getAllBankType = (search, pageNumber, pageSize) => {
  return loggedAxios.get(`BankType/get-all-bank-type?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`);
};

/* ===============================
   🧑‍💼 ACCOUNT MANAGEMENT
================================ */
const getAccountList = (data) => loggedAxios.post("AccountManagement/account-list", data);
const getAccountById = (userId) => loggedAxios.get(`AccountManagement/get-user/${userId}`);
const banUser = (userId) => {
  Logger.warn('Banning user', { userId });
  return loggedAxios.put(`AccountManagement/${userId}/ban`);
};
const unbanUser = (userId) => {
  Logger.info('Unbanning user', { userId });
  return loggedAxios.put(`AccountManagement/${userId}/unban`);
};
const deleteUser = (userId) => {
  Logger.error('Deleting user account', { userId });
  return loggedAxios.delete(`AccountManagement/${userId}`);
};

const registerCourtOwner = (payload) => {
  Logger.info('Registering new court owner', { email: payload.email });
  return loggedAxios.post("Account/register-court-owner", payload, { validateStatus: () => true });
};

/* ===============================
   🖼️ SLIDER MANAGEMENT
================================ */
const getAllActiveSliders = (pageNumber, pageSize) => {
  return loggedAxios.get(`SliderManagement/get-all-active-sliders/${pageNumber}/${pageSize}`);
};

const getSliderList = (data) => loggedAxios.post("SliderManagement/slider-list", data);
const getSliderById = (slideId) => loggedAxios.get(`SliderManagement/get-slider/${slideId}`);
const createSlider = (sliderData) => {
  Logger.info('Creating new slider', { title: sliderData.title });
  return loggedAxios.post("SliderManagement/create-slider", sliderData);
};
const updateSlider = (slideId, sliderData) => loggedAxios.put(`SliderManagement/${slideId}`, sliderData);
const deleteSlider = (slideId) => {
  Logger.warn('Deleting slider', { slideId });
  return loggedAxios.delete(`SliderManagement/${slideId}`);
};
const activateSlider = (slideId) => loggedAxios.put(`SliderManagement/${slideId}/activate`);
const deactivateSlider = (slideId) => loggedAxios.put(`SliderManagement/${slideId}/deactivate`);

/* ===============================
   🏟️ FACILITY + COURT + REPORT
================================ */
const getAllFacilitiesByPlayer = (pageNumber, pageSize, body) => {
  return loggedAxios.post(
    `Facilities/get-all-facility-by-player?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    body
  );
};

const getAvailableSlots = (facilityId, categoryId, checkInDate) => {
  Logger.debug('Fetching available slots', { facilityId, categoryId, checkInDate });
  return loggedAxios.get(
    `Booking/available-slots?facilityId=${facilityId}&categoryId=${categoryId}&checkInDate=${checkInDate}`
  );
};

const getFacilityDetailsById = (facilityId) => loggedAxios.get(`Facilities/get-facility-by-id?id=${facilityId}`);

const getFacilitiesByCourtOwnerId = (
  courtOwnerId,
  facilityName = "",
  statusId = null,
  currentPage = 1,
  itemsPerPage = 3
) => {
  const params = new URLSearchParams();
  if (facilityName?.trim()) params.append("facilityName", facilityName);
  if (statusId != null) params.append("statusId", statusId);
  params.append("currentPage", currentPage);
  params.append("itemsPerPage", itemsPerPage);

  const url = `FacilitiesManage/listCourt/${courtOwnerId}?${params.toString()}`;
  return loggedAxios.get(url);
};

const createFacility = (facilityData) => {
  Logger.info('Creating new facility', { name: facilityData.name });
  return loggedAxios.post(`FacilitiesManage/createFacility`, facilityData);
};

const uploadFacilityImages = (formData) => {
  Logger.info('Uploading facility images');
  return loggedAxios.post(`Image/upload-facility`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 60000,
  });
};

const getFacilityById = (facilityId) => loggedAxios.get(`FacilitiesManage/getFacilityById/${facilityId}`);
const updateFacility = (facilityId, updateData) => {
  Logger.info('Updating facility', { facilityId });
  return loggedAxios.put(`FacilitiesManage/updateFacility/${facilityId}`, updateData);
};
const deleteFacility = (facilityId) => {
  Logger.warn('Deleting facility', { facilityId });
  return loggedAxios.delete(`FacilitiesManage/${facilityId}`);
};
const deleteFacilityImage = (imageId) => loggedAxios.delete(`Image/${imageId}`);

const getReport = (
  userId = 6,
  startDate,
  endDate,
  facilityId,
  pageNumber = 1,
  pageSize = 10
) => {
  Logger.debug('Generating report', { userId, startDate, endDate, facilityId });
  const formattedStartDate = startDate ? new Date(startDate).toISOString() : null;
  const formattedEndDate = endDate ? new Date(endDate).toISOString() : null;

  return loggedAxios.get(`Report/ReportList`, {
    params: {
      userId,
      startDate: formattedStartDate,
      endDate: formattedEndDate,
      facilityId,
      pageNumber,
      pageSize,
    },
  });
};

const getTotalReport = (userId = 6, startDate, endDate) => {
  const formattedStartDate = startDate ? new Date(startDate).toISOString() : null;
  const formattedEndDate = endDate ? new Date(endDate).toISOString() : null;

  return loggedAxios.get(`Report/TotalReport`, {
    params: { userId, startDate: formattedStartDate, endDate: formattedEndDate },
  });
};

const getAdminReport = async (month, year) => {
  try {
    const response = await axios.get(`Report/AdminReport`, {
      params: {
        year,
        month
      }
    });
    console.log('API Response:', response.data);
    return response;
  } catch (error) {
    console.error('Error fetching admin report:', error);
    throw error;
  }
}

const exportReportToExcel = (
  userId = 6,
  startDate,
  endDate,
  facilityId,
  pageNumber = 1
) => {
  Logger.info('Exporting report to Excel', { userId, facilityId });
  const formattedStartDate = startDate ? new Date(startDate).toISOString() : null;
  const formattedEndDate = endDate ? new Date(endDate).toISOString() : null;

  return loggedAxios.get(`Report/Export-Report-CourtOwner`, {
    params: { userId, startDate: formattedStartDate, endDate: formattedEndDate, facilityId, pageNumber },
    responseType: "arraybuffer",
  });
};

const getAllCourts = (params) => {
  return loggedAxios.get("CourtManagement/CourtList", {
    params: {
      PageNumber: params.pageNumber || 1,
      PageSize: params.pageSize || 10,
      FacilityId: params.facilityId,
      Search: params.search || undefined,
      Status: params.status || undefined,
      CategoryId: params.categoryId || undefined,
    },
  });
};

const addNewCourt = (courtData) => {
  Logger.info('Adding new court', { courtName: courtData.courtName });
  return loggedAxios.post("CourtManagement/CreateCourt", courtData);
};

const updateCourt = (courtData, userId = 6) => {
  Logger.info('Updating court', { courtId: courtData.courtId, courtName: courtData.courtName });
  return loggedAxios.put(`CourtManagement/UpdateCourt?userId=${userId}`, {
    courtId: courtData.courtId,
    statusId: courtData.status,
    courtName: courtData.courtName,
    categoryId: courtData.categoryId,
    pricePerHour: courtData.pricePerHour,
    facilityId: courtData.facilityId,
  });
};

const deleteCourt = (courtId, userId = 6) => {
  Logger.warn('Deleting court', { courtId, userId });
  return loggedAxios.delete(`CourtManagement/DeleteCourt?userId=${userId}&courtId=${courtId}`);
};

const lockCourt = (courtId, statusId, userId) => {
  return loggedAxios.put(`CourtManagement/LockCourt?courtId=${courtId}&statusId=${statusId}&userId=${userId}`);
}

const getCourtDetail = (courtId) => loggedAxios.get(`CourtManagement/CourtDetail?courtId=${courtId}`);

/* ===============================
   📅 TIMESLOT MANAGEMENT
================================ */
const getTimeSlotsByFacilityId = (facilityId) => {
  return loggedAxios.get(`TimeslotManagement/facility/${facilityId}`);
};

const getTimeslotsByFacilityId = (
  facilityId,
  statusId = null,
  pageNumber = 1,
  pageSize = 10
) => {
  const params = new URLSearchParams();
  if (statusId != null) params.append("statusId", statusId);
  params.append("pageNumber", pageNumber);
  params.append("pageSize", pageSize);
  return loggedAxios.get(`TimeslotManagement/facility/${facilityId}?${params.toString()}`);
};

const createTimeslot = (createRequest) => {
  Logger.info('Creating new timeslot', { facilityId: createRequest.facilityId });
  return loggedAxios.post(`TimeslotManagement/create`, createRequest);
};
const deleteTimeslot = (timeSlotId) => {
  Logger.warn('Deleting timeslot', { timeSlotId });
  return loggedAxios.delete(`TimeslotManagement/delete/${timeSlotId}`);
};
const updateTimeslot = (timeSlotId, updateRequest) => {
  Logger.info('Updating timeslot', { timeSlotId });
  return loggedAxios.put(`TimeslotManagement/update/${timeSlotId}`, updateRequest);
};

const createRating = (ratingData) => {
  Logger.info('Creating rating', { facilityId: ratingData.facilityId, rating: ratingData.rating });
  return loggedAxios.post("Ratings", ratingData, { validateStatus: () => true });
};

/* ===============================
   📅 BOOKING MANAGEMENT
================================ */
const getBookingsByFacilityId = (facilityId, pageNumber = 1, pageSize = 10) => {
  return loggedAxios.get(`Booking/court-owner`, { params: { facilityId, pageNumber, pageSize } });
};

const getBookingsByUserId = (userId, page = 1, pageSize = 10) => {
  return loggedAxios.get(`Booking`, { params: { userId, Page: page, PageSize: pageSize } });
};

const getBookingById = (bookingId) => {
  return loggedAxios.get(`Booking/${bookingId}`);
};

const createBookingForCO = (bookingData) => {
  Logger.info('Creating booking for court owner', { facilityId: bookingData.facilityId });
  return loggedAxios.post("Booking", bookingData, { validateStatus: () => true });
};
//Auth
const googleLoginAxios = async (googleToken) => {
  try {
    const response = await axios.post('/auth/google-login', {
      googleToken: googleToken
    }, {
      timeout: 30000, // ✅ 30 seconds cho Google login
      headers: {
        'Content-Type': 'application/json'
      }
    });
    console.log('✅ Google Login API response:', response.data);
    return response;
  } catch (error) {
    console.error('❌ Google login API error:', error);
    throw error;
  }
};
const sendOtpAxios = async (data) => {
  try {
    console.log('📡 Calling sendOtp API with data:', data);
    const response = await axios.post('/auth/send-otp', data);
    console.log('✅ SendOtp API response:', response.data);
    return response; // ✅ Return full response
  } catch (error) {
    console.error('❌ SendOtp API error:', error);
    throw error;
  }
};
// Verify OTP API (CHUNG CHO CẢ REGULAR VÀ GOOGLE)
const verifyOtpAxios = async (data) => {
  try {
    console.log('📡 Calling verifyOtp API with data:', data);
    const response = await axios.post('/auth/verify-otp', data);
    console.log('✅ VerifyOtp API response:', response.data);
    return response; // ✅ Return full response
  } catch (error) {
    console.error('❌ VerifyOtp API error:', error);
    throw error;
  }
};
const loginAxios = async (data) => {
  try {
    console.log('📡 Calling login API with data:', data);
    const response = await axios.post('/auth/login', data);
    console.log('✅ Login API response:', response.data);
    return response;
  } catch (error) {
    console.error('❌ Login API error:', error);
    throw error;
  }
};

const createBookingForPlayer = (bookingData) => {
  Logger.info('Creating booking for player', { facilityId: bookingData.facilityId, userId: bookingData.userId });
  return loggedAxios.post("Booking", bookingData, { validateStatus: () => true });
};

const createPaymentOrder = (paymentData) => {
  Logger.info('Creating payment order', { amount: paymentData.amount, bookingId: paymentData.bookingId });
  return loggedAxios.post("Payment/create-order", paymentData, { validateStatus: () => true });
};

const completeBooking = (bookingId) => {
  Logger.info('Completing booking', { bookingId });
  return loggedAxios.post(`Booking/${bookingId}/complete`);
};

/* ===============================
   ✅ EXPORT ALL
================================ */
export {
  // Logger pour usage externe
  Logger,
  
  // Court Category
  getAllCourtCategories,
  addCourtCategory,
  updateCourtCategory,
  getCourtCategoryById,
  deleteCourtCategory,

  // Blog
  getAllBlogs,
  getBlogById,
  createBlog,
  updateBlog,
  deleteBlog,
  getBlogsByUserId,

  // Comment
  getAllComments,
  createComment,
  updateComment,
  deleteComment,
  getCommentsByUserId,

  // Image
  getUserImage,
  uploadUserImage,
  updateUserImage,
  uploadslideImage,
  updateSlideImage,
  uploadBlogImage,
  getBlogImages,
  deleteImage,
  updateImage,

  // Password Reset

  forgotPasswordByEmail,
  resetPasswordByEmail,
  resendOtpByEmail,
  forgotPasswordBySms,
  resetPasswordBySms,
  resendOtpBySms,

  // User
  getUserById,
  updateUserProfile,
  changePassword,
  checkPasswordStatus,

  // Bank
  getAllBankType,

  // Account Management

  getAccountList,
  getAccountById,
  banUser,
  unbanUser,
  deleteUser,
  registerCourtOwner,

  // Slider
  getAllActiveSliders,
  getSliderList,
  getSliderById,
  createSlider,
  updateSlider,
  deleteSlider,
  activateSlider,
  deactivateSlider,

  // Facility & Reports
  getAllFacilitiesByPlayer,
  getAvailableSlots,
  getFacilityDetailsById,
  getFacilitiesByCourtOwnerId,
  createFacility,
  uploadFacilityImages,
  getFacilityById,
  updateFacility,
  deleteFacility,
  deleteFacilityImage,

  // Report

  getReport,
  getTotalReport,
  getAdminReport,
  exportReportToExcel,

  // Court Management
  getAllCourts,
  addNewCourt,
  updateCourt,
  deleteCourt,
  lockCourt,
  getCourtDetail,

  // Timeslot (✅ Fixed duplicates)
  getTimeSlotsByFacilityId,        // Simple version
  getTimeslotsByFacilityId,        // Advanced version with pagination
  createTimeslot,
  deleteTimeslot,
  updateTimeslot,

  // Booking
  getBookingsByFacilityId,
  getBookingsByUserId,
  getBookingById,
  createBookingForCO,
  createBookingForPlayer,
  createPaymentOrder,
  completeBooking,
  // Auth
  googleLoginAxios,
  verifyOtpAxios,
  sendOtpAxios,
  loginAxios,

  //Rating
  createRating
};