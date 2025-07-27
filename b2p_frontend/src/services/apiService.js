import axios from "../utils/axiosCustomize";
axios.defaults.timeout = 5000;

// Court Category Management APIs
const getAllCourtCategories = (search, pageNumber, pageSize) => {
  return axios.get(
    `CourtCategory/get-all-court-categories?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
};

// Add court category - Updated to match new API
const addCourtCategory = async (categoryName) => {
  try {
    // Send as URL parameter instead of request body
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

// ===============================
// 📝 BLOG MANAGEMENT APIs
// ===============================

// Get all blogs with query parameters
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

// Get blog by ID
const getBlogById = (blogId) => {
  return axios.get(`Blog/${blogId}`);
};

// Create new blog
const createBlog = (blogData) => {
  return axios.post("Blog", blogData);
};

// Update blog
const updateBlog = (blogId, blogData) => {
  return axios.put(`Blog/${blogId}`, blogData);
};

// Delete blog
const deleteBlog = (blogId, userId) => {
  return axios.delete(`Blog/${blogId}?userId=${userId}`);
};

// Get blogs by user ID
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
// 💬 COMMENT MANAGEMENT APIs
// ===============================

// Get all comments with query parameters
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

// Create new comment
const createComment = (commentData) => {
  return axios.post("Comment", commentData);
};

// Update comment
const updateComment = (commentId, commentData) => {
  return axios.put(`Comment/${commentId}`, commentData);
};

// Delete comment
const deleteComment = (commentId, userId, roleId) => {
  return axios.delete(`Comment/${commentId}?userId=${userId}&roleId=${roleId}`);
};

// Get comments by user ID
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
// ===============================
// 🖼️ IMAGE MANAGEMENT APIs
// ===============================

// Upload image for blog
const uploadBlogImage = (file, blogId, caption = null) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("entityId", blogId.toString());

  if (caption) {
    formData.append("caption", caption);
  }

  return axios.post("Image/upload-blog", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
    timeout: 30000,
  });
};

// Get blog images
const getBlogImages = (blogId) => {
  return axios.get(`Image/blog/${blogId}`);
};

// Delete image
const deleteImage = (imageId) => {
  return axios.delete(`Image/${imageId}`);
};

// Update image
const updateImage = (imageId, file = null, order = null, caption = null) => {
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

  // 📝 Blog Management
  getAllBlogs,
  getBlogById,
  createBlog,
  updateBlog,
  deleteBlog,
  getBlogsByUserId,

  // 💬 Comment Management
  getAllComments,
  createComment,
  updateComment,
  deleteComment,
  getCommentsByUserId,

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
  // 🖼️ Image Management
  uploadBlogImage,
  getBlogImages,
  deleteImage,
  updateImage,
};
