import axios from "../utils/axiosCustomize";
axios.defaults.timeout = 5000;

// ===============================
// 🏟️ COURT CATEGORY MANAGEMENT
// ===============================
const getAllCourtCategories = (search, pageNumber, pageSize) => {
  return axios.get(
    `CourtCategory/get-all-court-categories?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
};

const addCourtCategory = async (categoryName) => {
  try {
    const response = await axios.post(
      `CourtCategory/add-court-category?cateName=${encodeURIComponent(
        categoryName
      )}`
    );
    return response;
  } catch (error) {
    console.error("Error adding court category:", error);
    throw error;
  }
};

const updateCourtCategory = (categoryData) => {
  return axios.put("CourtCategory/update-court-category", categoryData);
};

const getCourtCategoryById = (categoryId) => {
  return axios.get(
    `CourtCategory/get-court-category-by-id?categoryId=${categoryId}`
  );
};

const deleteCourtCategory = (categoryId) => {
  return axios.delete(
    `CourtCategory/delete-court-category?categoryId=${categoryId}`
  );
};

const completeBooking = (bookingId) => {
  return axios.post(`Booking/${bookingId}/complete`);
};

// ===============================
// 📝 BLOG MANAGEMENT
// ===============================
const getAllBlogs = (queryParams = {}) => {
  const {
    search = "",
    page = 1,
    pageSize = 10,
    sortBy = "postAt",
    sortDirection = "desc",
  } = queryParams;

  return axios.get("Blog", {
    params: {
      Search: search,
      Page: page,
      PageSize: pageSize,
      SortBy: sortBy,
      SortDirection: sortDirection,
    },
  });
};

const getBlogById = (blogId) => axios.get(`Blog/${blogId}`);
const createBlog = (blogData) => axios.post("Blog", blogData);
const updateBlog = (blogId, blogData) => axios.put(`Blog/${blogId}`, blogData);
const deleteBlog = (blogId, userId) =>
  axios.delete(`Blog/${blogId}?userId=${userId}`);

const getBlogsByUserId = (userId, queryParams = {}) => {
  const {
    search = "",
    page = 1,
    pageSize = 10,
    sortBy = "postAt",
    sortDirection = "desc",
  } = queryParams;

  return axios.get(`Blog/user/${userId}`, {
    params: {
      Search: search,
      Page: page,
      PageSize: pageSize,
      SortBy: sortBy,
      SortDirection: sortDirection,
    },
  });
};

// ===============================
// 💬 COMMENT MANAGEMENT
// ===============================
const getAllComments = (queryParams = {}) => {
  const {
    search = "",
    page = 1,
    pageSize = 10,
    sortBy = "postAt",
    sortDirection = "desc",
  } = queryParams;

  return axios.get("Comment", {
    params: {
      Search: search,
      Page: page,
      PageSize: pageSize,
      SortBy: sortBy,
      SortDirection: sortDirection,
    },
  });
};

const createComment = (commentData) => axios.post("Comment", commentData);
const updateComment = (commentId, commentData) =>
  axios.put(`Comment/${commentId}`, commentData);
const deleteComment = (commentId, userId, roleId) =>
  axios.delete(`Comment/${commentId}?userId=${userId}&roleId=${roleId}`);

const getCommentsByUserId = (userId, queryParams = {}) => {
  const {
    search = "",
    page = 1,
    pageSize = 10,
    sortBy = "postAt",
    sortDirection = "desc",
  } = queryParams;

  return axios.get(`Comment/user/${userId}`, {
    params: {
      Search: search,
      Page: page,
      PageSize: pageSize,
      SortBy: sortBy,
      SortDirection: sortDirection,
    },
  });
};

// ===============================
// 🖼️ IMAGE MANAGEMENT
// ===============================
const getUserImage = (userId) => axios.get(`Image/user/${userId}`);

const uploadUserImage = (file, userId, caption = null) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("entityId", userId.toString());
  if (caption) formData.append("caption", caption);

  return axios.post("Image/upload-user", formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 30000,
  });
};

const uploadslideImage = (file, slideId, caption = null) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("entityId", slideId.toString());
  if (caption) formData.append("caption", caption);

  return axios.post("Image/upload-slide", formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 30000,
  });
};

const updateUserImage = (imageId, file, order = null, caption = null) => {
  const formData = new FormData();
  if (file) formData.append("file", file);
  if (order !== null) formData.append("order", order.toString());
  if (caption) formData.append("caption", caption);

  return axios.put(`Image/update-image/${imageId}`, formData, {
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

  return axios.post("Image/upload-blog", formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 30000,
  });
};

const getBlogImages = (blogId) => axios.get(`Image/blog/${blogId}`);
const deleteImage = (imageId) => axios.delete(`Image/${imageId}`);

// ===============================
// 🔒 PASSWORD RESET
// ===============================
const forgotPasswordByEmail = (email) =>
  axios.post("User/forgot-password-by-email", { email });
const resetPasswordByEmail = (email, otpCode, newPassword, confirmPassword) => {
  return axios.post("User/reset-password-by-email", {
    email,
    otpCode,
    newPassword,
    confirmPassword,
  });
};
const resendOtpByEmail = (email) =>
  axios.post("User/resend-otp-by-email", { email });

const forgotPasswordBySms = (phoneNumber) =>
  axios.post("User/forgot-password-by-sms", { phoneNumber });
const resetPasswordBySms = (
  phoneNumber,
  otpCode,
  newPassword,
  confirmPassword
) => {
  return axios.post("User/reset-password-by-sms", {
    phoneNumber,
    otpCode,
    newPassword,
    confirmPassword,
  });
};
const resendOtpBySms = (phoneNumber) =>
  axios.post("User/resend-otp-by-sms", { phoneNumber });

// ===============================
// 📋 USER MANAGEMENT
// ===============================
const getUserById = (userId) =>
  axios.get(`User/get-user-by-id?userId=${userId}`);
const updateUserProfile = (userId, body) =>
  axios.put(`User/update-user?userId=${userId}`, body);
const changePassword = (body) => axios.put(`User/change-password`, body);
const checkPasswordStatus = (userId) =>
  axios.get(`User/check-password-status/${userId}`);

// ===============================
// 🏦 BANK TYPE
// ===============================
const getAllBankType = (search, pageNumber, pageSize) => {
  return axios.get(
    `BankType/get-all-bank-type?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
};

// ===============================
// 🧑‍💼 ACCOUNT MANAGEMENT
// ===============================
const getAccountList = (data) =>
  axios.post("AccountManagement/account-list", data);
const getAccountById = (userId) =>
  axios.get(`AccountManagement/get-user/${userId}`);
const banUser = (userId) => axios.put(`AccountManagement/${userId}/ban`);
const unbanUser = (userId) => axios.put(`AccountManagement/${userId}/unban`);
const deleteUser = (userId) => axios.delete(`AccountManagement/${userId}`);

const registerCourtOwner = (payload) => {
  return axios.post("Account/register-court-owner", payload, {
    validateStatus: () => true,
  });
};

// ===============================
// 🖼️ SLIDER MANAGEMENT
// ===============================
const getAllActiveSliders = (pageNumber, pageSize) => {
  return axios.get(
    `SliderManagement/get-all-active-sliders/${pageNumber}/${pageSize}`
  );
};

const getSliderList = (data) =>
  axios.post("SliderManagement/slider-list", data);
const getSliderById = (slideId) =>
  axios.get(`SliderManagement/get-slider/${slideId}`);
const createSlider = (sliderData) =>
  axios.post("SliderManagement/create-slider", sliderData);
const updateSlider = (slideId, sliderData) =>
  axios.put(`SliderManagement/${slideId}`, sliderData);
const deleteSlider = (slideId) => axios.delete(`SliderManagement/${slideId}`);
const activateSlider = (slideId) =>
  axios.put(`SliderManagement/${slideId}/activate`);
const deactivateSlider = (slideId) =>
  axios.put(`SliderManagement/${slideId}/deactivate`);

// ===============================
// 🏟️ FACILITY + COURT + REPORT
// ===============================
const getAllFacilitiesByPlayer = (pageNumber, pageSize, body) => {
  return axios.post(
    `Facilities/get-all-facility-by-player?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    body
  );
};

const getAvailableSlots = (facilityId, categoryId, checkInDate) => {
  return axios.get(
    `Booking/available-slots?facilityId=${facilityId}&categoryId=${categoryId}&checkInDate=${checkInDate}`
  );
};

const getFacilityDetailsById = (facilityId) =>
  axios.get(`Facilities/get-facility-by-id?id=${facilityId}`);

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
  return axios.get(url);
};

const createFacility = (facilityData) =>
  axios.post(`FacilitiesManage/createFacility`, facilityData);
const uploadFacilityImages = (formData) =>
  axios.post(`Image/upload-facility`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
    timeout: 60000,
  });
const getFacilityById = (facilityId) =>
  axios.get(`FacilitiesManage/getFacilityById/${facilityId}`);
const updateFacility = (facilityId, updateData) =>
  axios.put(`FacilitiesManage/updateFacility/${facilityId}`, updateData);
const deleteFacility = (facilityId) =>
  axios.delete(`FacilitiesManage/${facilityId}`);
const deleteFacilityImage = (imageId) => axios.delete(`Image/${imageId}`);

const getReport = (
  userId = 6,
  startDate,
  endDate,
  facilityId,
  pageNumber = 1,
  pageSize = 10
) => {
  const formattedStartDate = startDate ? new Date(startDate).toISOString() : null;
  const formattedEndDate = endDate ? new Date(endDate).toISOString() : null;

  return axios.get(`Report/ReportList`, {
    params: {
      userId,
      startDate: formattedStartDate,
      endDate: formattedEndDate,
      facilityId,
      pageNumber,
      pageSize
    }
  });
};

const getTotalReport = (userId = 6, startDate, endDate) => {
  const formattedStartDate = startDate ? new Date(startDate).toISOString() : null;
  const formattedEndDate = endDate ? new Date(endDate).toISOString() : null;

  return axios.get(`Report/TotalReport`, {
    params: {
      userId,
      startDate: formattedStartDate,
      endDate: formattedEndDate
    }
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
  const formattedStartDate = startDate ? new Date(startDate).toISOString() : null;
  const formattedEndDate = endDate ? new Date(endDate).toISOString() : null;

  return axios.get(`Report/Export-Report-CourtOwner`, {
    params: {
      userId,
      startDate: formattedStartDate,
      endDate: formattedEndDate,
      facilityId,
      pageNumber,
    },
    responseType: "arraybuffer",
  });
};

const getAllCourts = (params) => {
  return axios.get("CourtManagement/CourtList", {
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
  return axios.post("CourtManagement/CreateCourt", courtData);
};

const updateCourt = (courtData, userId = 6) => {
  return axios.put(`CourtManagement/UpdateCourt?userId=${userId}`, {
    courtId: courtData.courtId,
    statusId: courtData.status,
    courtName: courtData.courtName,
    categoryId: courtData.categoryId,
    pricePerHour: courtData.pricePerHour,
    facilityId: courtData.facilityId,
  });
};

const deleteCourt = (courtId, userId = 6) => {
  return axios.delete(
    `CourtManagement/DeleteCourt?userId=${userId}&courtId=${courtId}`
  );
};

const getCourtDetail = (courtId) => {
  return axios.get(`CourtManagement/CourtDetail?courtId=${courtId}`);
};

// ===============================
// 📅 TIMESLOT MANAGEMENT
// ===============================
const getTimeSlotsByFacilityId = (facilityId) => {
  return axios.get(`TimeslotManagement/facility/${facilityId}`);
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

  const url = `TimeslotManagement/facility/${facilityId}?${params.toString()}`;
  return axios.get(url);
};

const createTimeslot = (createRequest) => {
  const url = `TimeslotManagement/create`;
  return axios.post(url, createRequest);
};

const deleteTimeslot = (timeSlotId) => {
  const url = `TimeslotManagement/delete/${timeSlotId}`;
  return axios.delete(url);
};

const updateTimeslot = (timeSlotId, updateRequest) => {
  const url = `TimeslotManagement/update/${timeSlotId}`;
  return axios.put(url, updateRequest);
};

// ===============================
// 📅 BOOKING MANAGEMENT
// ===============================
const getBookingsByFacilityId = (facilityId, pageNumber = 1, pageSize = 10) => {
  return axios.get(`Booking/court-owner`, {
    params: {
      facilityId,
      pageNumber,
      pageSize,
    },
  });
};

const getBookingsByUserId = (userId, page = 1, pageSize = 10) => {
  return axios.get(`Booking`, {
    params: {
      userId,
      Page: page,
      PageSize: pageSize,
    },
  });
};

const createBookingForCO = (bookingData) => {
  return axios.post("Booking", bookingData, {
    validateStatus: () => true,
  });
};

// ===============================
// ✅ EXPORT ALL (CLEANED UP)
// ===============================
export {
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
  
  // Facility
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
  createBookingForCO,
  completeBooking,
};