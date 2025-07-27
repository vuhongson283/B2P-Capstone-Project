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

// Update existing image (PUT) - NEW FUNCTION
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

// Forgot Password APIs
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
  userId, 
  startDate, 
  endDate, 
  facilityId, 
  pageNumber, 
  pageSize
) => {
  return axios.get(`Report/Export-Report-CourtOwner?userId=${userId}&startDate=${startDate}&endDate=${endDate}&facilityId=${facilityId}&pageNumber=${pageNumber}`);
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
};
