import axios from "../utils/axiosCustomize";
axios.defaults.timeout = 5000;

// Court Category Management APIs
const getAllCourtCategories = (search, pageNumber, pageSize) => {
  return axios.get(
    `CourtCategory/get-all-court-categories?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
};

const addCourtCategory = (categoryData) => {
  return axios.post("CourtCategory/add-court-category", categoryData);
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

// Slider Management APIs
const getAllActiveSliders = (pageNumber, pageSize) => {
  return axios.get(
    `SliderManagement/get-all-active-sliders/${pageNumber}/${pageSize}`
  );
};

// Facilities Management APIs
const getAllFacilitiesByPlayer = (pageNumber, pageSize, body) => {
  return axios.post(
    `Facilities/get-all-facility-by-player?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    body
  );
};

// User Management APIs
const getUserById = (userId) => {
  return axios.get(`User/get-user-by-id?userId=${userId}`);
};

const updateUserProfile = (userId, body) => {
  return axios.put(`User/update-user?userId=${userId}`, body);
};

const changePassword = (body) => {
  return axios.put(`User/change-password`, body);
};

const checkPasswordStatus = (userId) => {
  return axios.get(`User/check-password-status/${userId}`);
};

// Bank Type APIs
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

// Image Management APIs
const getUserImage = (userId) => {
  return axios.get(`Image/user/${userId}`);
};

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

// Export all functions
export {
  // Account Management
  getAccountList,
  getAccountById,
  banUser,
  deleteUser,
  unbanUser,

  // Court Category Management
  getAllCourtCategories,
  addCourtCategory,
  updateCourtCategory,
  getCourtCategoryById,
  deleteCourtCategory,

  // Slider Management
  getAllActiveSliders,

  // Facilities Management
  getAllFacilitiesByPlayer,

  // User Management
  getUserById,
  updateUserProfile,
  changePassword,
  checkPasswordStatus,

  // Bank Type
  getAllBankType,

  // Image Management
  getUserImage,
  uploadUserImage,
  updateUserImage,

  // Password Reset
  forgotPasswordByEmail,
  resetPasswordByEmail,
  resendOtpByEmail,
  forgotPasswordBySms,
  resetPasswordBySms,
  resendOtpBySms,
};
