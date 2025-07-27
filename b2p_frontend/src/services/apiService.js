import axios from "../utils/axiosCustomize";
axios.defaults.timeout = 5000;

const getAllCourtCategories = (search, pageNumber, pageSize) => {
  return axios.get(
    `CourtCategory/get-all-court-categories?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
};

const getAllActiveSliders = (pageNumber, pageSize) => {
  return axios.get(
    `SliderManagement/get-all-active-sliders/${pageNumber}/${pageSize}`
  );
};

const getAllFacilitiesByPlayer = (pageNumber, pageSize, body) => {
  return axios.post(
    `Facilities/get-all-facility-by-player?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    body
  );
};

const getUserById = (userId) => {
  return axios.get(`User/get-user-by-id?userId=${userId}`);
};

const updateUserProfile = (userId, body) => {
  return axios.put(`User/update-user?userId=${userId}`, body);
};

const changePassword = (body) => {
  return axios.put(`User/change-password`, body);
};

const getAllBankType = (search, pageNumber, pageSize) => {
  return axios.get(
    `BankType/get-all-bank-type?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
};


// Account Management APIs
const getAccountList = (data) => {
  return axios.post("AccountManagement/account-list", data);
};

const getAccountById = (userId) => {
  return axios.get(`AccountManagement/get-user/${userId}`);
};

const banUser = (userId) => {
  return axios.put(`AccountManagement/${userId}/ban`);
};

const unbanUser = (userId) => {
  return axios.put(`AccountManagement/${userId}/unban`);
};

const deleteUser = (userId) => {
  return axios.delete(`AccountManagement/${userId}`);
};

//SliderManagement APIS 
// Slider Management APIs
const getSliderList = (data) => {
  return axios.post("SliderManagement/slider-list", data);
};

const getSliderById = (slideId) => {
  return axios.get(`SliderManagement/get-slider/${slideId}`);
};

const createSlider = (sliderData) => {
  return axios.post("SliderManagement/create-slider", sliderData);
};

const updateSlider = (slideId, sliderData) => {
  return axios.put(`SliderManagement/${slideId}`, sliderData);
};

const deleteSlider = (slideId) => {
  return axios.delete(`SliderManagement/${slideId}`);
};

const activateSlider = (slideId) => {
  return axios.put(`SliderManagement/${slideId}/activate`);
};

const deactivateSlider = (slideId) => {
  return axios.put(`SliderManagement/${slideId}/deactivate`);
};

// Image Management APIs

const getUserImage = (userId) => {
  return axios.get(`Image/user/${userId}`);
};

// Upload new image (POST)
const uploadUserImage = (file, userId, caption = null) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("entityId", userId.toString());

  if (caption) {
    formData.append("caption", caption);
  }

  return axios.post("Image/upload-user", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
    timeout: 30000,
  });
};


const uploadslideImage = (file, slideId, caption = null) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("entityId", slideId.toString());

  if (caption) {
    formData.append("caption", caption);
  }

  return axios.post("Image/upload-slide", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
    timeout: 30000,
  });
};


const updateUserImage = (imageId, file, order = null, caption = null) => {
  const formData = new FormData();

  if (file) {
    formData.append("file", file);
  }

  if (order !== null) {
    formData.append("order", order.toString());
  }

  if (caption) {
    formData.append("caption", caption);
  }

  return axios.put(`Image/update-image/${imageId}`, formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
    timeout: 30000,
  });
};



const updateSlideImage = (imageId, file, order = null, caption = null) => {
  const formData = new FormData();

  if (file) {
    formData.append("file", file);
  }

  if (order !== null) {
    formData.append("order", order.toString());
  }

  if (caption) {
    formData.append("caption", caption);
  }

  return axios.put(`Image/update-image/${imageId}`, formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
    timeout: 30000,
  });
};

// Password Reset APIs

const forgotPasswordByEmail = (email) => {
  return axios.post("User/forgot-password-by-email", { email });
};

const resetPasswordByEmail = (email, otpCode, newPassword, confirmPassword) => {
  return axios.post("User/reset-password-by-email", {
    email,
    otpCode,
    newPassword,
    confirmPassword,
  });
};

const resendOtpByEmail = (email) => {
  return axios.post("User/resend-otp-by-email", { email });
};

const forgotPasswordBySms = (phoneNumber) => {
  return axios.post("User/forgot-password-by-sms", { phoneNumber });
};

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

const resendOtpBySms = (phoneNumber) => {
  return axios.post("User/resend-otp-by-sms", { phoneNumber });
};
// Trong apiService.js, đảm bảo function này return đúng
const getFacilitiesByCourtOwnerId = (
  courtOwnerId,
  facilityName = "",
  statusId = null,
  currentPage = 1,
  itemsPerPage = 3
) => {
  const params = new URLSearchParams();

  if (facilityName && facilityName.trim()) {
    params.append("facilityName", facilityName);
  }

  if (statusId !== null && statusId !== undefined) {
    params.append("statusId", statusId);
  }

  params.append("currentPage", currentPage);
  params.append("itemsPerPage", itemsPerPage);

  const url = `FacilitiesManage/listCourt/${courtOwnerId}?${params.toString()}`;
  console.log("🌐 API URL:", url);

  // Đảm bảo return axios.get, không phải gì khác
  return axios.get(url);
};
// Thêm vào apiService.js
const createFacility = (facilityData) => {
  console.log("🏗️ Creating facility with data:", facilityData);

  const url = `FacilitiesManage/createFacility`;
  console.log("🌐 Create facility URL:", url);

  return axios.post(url, facilityData);
};
const uploadFacilityImages = (formData) => {
  console.log("📤 Uploading facility images...");

  const url = `Image/upload-facility`;
  console.log("🌐 Upload facility images URL:", url);

  return axios.post(url, formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
    timeout: 60000, // 60 giây cho upload ảnh
  });
};
// Thêm vào file apiService.js (theo format của bạn)

const getFacilityById = (facilityId) => {
  console.log("🔍 Getting facility by ID:", facilityId);

  const url = `FacilitiesManage/getFacilityById/${facilityId}`;
  console.log("🌐 Get facility by ID URL:", url);

  return axios.get(url);
};

const updateFacility = (facilityId, updateData) => {
  console.log("📝 Updating facility:", facilityId, updateData);

  const url = `FacilitiesManage/updateFacility/${facilityId}`;
  console.log("🌐 Update facility URL:", url);

  return axios.put(url, updateData);
};
const deleteFacility = (facilityId) => {
  console.log("🗑️ Deleting facility:", facilityId);
  
  const url = `FacilitiesManage/${facilityId}`;
  console.log("🌐 Delete facility URL:", url);
  
  return axios.delete(url);
};
const deleteFacilityImage = (imageId) => {
  return axios.delete(`Image/${imageId}`);
};

const getReport = (
  userId=6, 
  startDate, 
  endDate, 
  facilityId, 
  pageNumber=1, 
  pageSize=10
) => {
  return axios.get(`Report/ReportList?userId=${userId}&startDate=${startDate}&endDate=${endDate}&facilityId=${facilityId}&pageNumber=${pageNumber}&pageSize=${pageSize}`);
};

const getTotalReport = (
  userId=6, 
  startDate, 
  endDate
) => {
  return axios.get(`Report/TotalReport?userId=${userId}&startDate=${startDate}&endDate=${endDate}`);
};

const exportReportToExcel = (
  userId = 6,
  startDate,
  endDate,
  facilityId,
  pageNumber = 1
) => {
  return axios.get(`Report/Export-Report-CourtOwner`, {
    params: {
      userId,
      startDate,
      endDate,
      facilityId,
      pageNumber,
    },
    responseType: 'arraybuffer', // Quan trọng: yêu cầu dữ liệu dạng binary
  });
};

const getAllCourts = (params) => {
  return axios.get('CourtManagement/CourtList', {
    params: {
      PageNumber: params.pageNumber || 1,
      PageSize: params.pageSize || 10,
      FacilityId: params.facilityId,
      Search: params.search || undefined,
      Status: params.status || undefined,
      CategoryId: params.categoryId || undefined
    }
  });
};

const addNewCourt = (courtData) => {
  return axios.post('CourtManagement/CreateCourt', courtData);
};

const updateCourt = (courtData, userId = 6) => {
  return axios.put(`CourtManagement/UpdateCourt?userId=${userId}`, {
    courtId: courtData.courtId,
    statusId: courtData.status,
    courtName: courtData.courtName,
    categoryId: courtData.categoryId,
    pricePerHour: courtData.pricePerHour,
    facilityId: courtData.facilityId
  });
};

const deleteCourt = (courtId, userId = 6) => {
  return axios.delete(`CourtManagement/DeleteCourt?userId=${userId}&courtId=${courtId}`);
};

const getCourtDetail = (courtId) => {
  return axios.get(`CourtManagement/CourtDetail?courtId=${courtId}`);
};

export {
  getAllCourtCategories,
  getAllActiveSliders,
  getAllFacilitiesByPlayer,
  getUserById,
  updateUserProfile,
  changePassword,
  getAllBankType,
  getUserImage,
  uploadUserImage,
  updateUserImage,
  // Account Management APIs
  unbanUser,
  banUser,
  getAccountList,
  getAccountById,
  deleteUser,
  uploadslideImage,
  updateSlideImage,

  // Password Reset

  forgotPasswordByEmail,
  resetPasswordByEmail,
  resendOtpByEmail,
  forgotPasswordBySms,
  resetPasswordBySms,
  resendOtpBySms,

  getReport,
  getTotalReport,
  exportReportToExcel,
  getAllCourts,
  addNewCourt,
  updateCourt,
  deleteCourt,
  getCourtDetail,


  // Facilities Management for Court Owner
  getFacilitiesByCourtOwnerId,
  createFacility,
  uploadFacilityImages,
  updateFacility,
  getFacilityById,
  deleteFacility,
  deleteFacilityImage,

  //SliderManagement
  getSliderList,
  getSliderById,
  createSlider,
  updateSlider,
  deleteSlider,
  activateSlider,
  deactivateSlider,
};