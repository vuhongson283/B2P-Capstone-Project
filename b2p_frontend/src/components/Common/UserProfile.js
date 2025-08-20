import React, { useState, useEffect } from "react";
import "./UserProfile.scss";
import { useAuth } from "../../contexts/AuthContext";
import {
  getUserById,
  updateUserProfile,
  changePassword,
  checkPasswordStatus,
  updateUserImage,
} from "../../services/apiService";

const UserProfile = (props) => {
  const [activeTab, setActiveTab] = useState("profile");
  const [loading, setLoading] = useState(false);
  const [pageLoading, setPageLoading] = useState(true);
  const [message, setMessage] = useState("");
  const [avatarPreview, setAvatarPreview] = useState("");
  const [originalEmail, setOriginalEmail] = useState("");
  const [imageLoadError, setImageLoadError] = useState(false);
  const [passwordStatus, setPasswordStatus] = useState(null);

  const { userId, isLoggedIn, isLoading: authLoading } = useAuth();

  // State cho thông tin cơ bản
  const [profileData, setProfileData] = useState({
    fullName: "",
    email: "",
    phone: "",
    isMale: true,
    address: "",
    dob: "",
    imageUrl: "",
    imageId: null,
  });

  // State cho đổi mật khẩu
  const [passwordData, setPasswordData] = useState({
    oldPassword: "",
    newPassword: "",
    confirmPassword: "",
  });

  const [errors, setErrors] = useState({});

  // 🎯 useEffect để check password status khi chuyển sang tab password
  useEffect(() => {
    const fetchPasswordStatus = async () => {
      if (activeTab === "password" && userId) {
        try {
          setPasswordStatus(null);

          const response = await checkPasswordStatus(userId);

          console.log("=== PASSWORD STATUS RESPONSE ===");
          console.log(JSON.stringify(response, null, 2));

          const isSuccess = response?.success === true;

          if (isSuccess && response.data) {
            const passwordData = response.data;

            setPasswordStatus({
              UserId: passwordData.userId,
              HasPassword: passwordData.hasPassword,
              RequireOldPassword: passwordData.requireOldPassword,
              PasswordStatus: passwordData.passwordStatus,
              FullName: passwordData.fullName,
              Email: passwordData.email,
              Phone: passwordData.phone,
            });

            console.log("✅ Password status set successfully");
            console.log("HasPassword:", passwordData.hasPassword);
            console.log("RequireOldPassword:", passwordData.requireOldPassword);
          } else {
            console.warn("Could not get password status:", response?.message);
            setPasswordStatus({
              HasPassword: true,
              RequireOldPassword: true,
              PasswordStatus: "Không xác định được trạng thái",
            });
          }
        } catch (error) {
          console.error("Error checking password status:", error);
          setPasswordStatus({
            HasPassword: true,
            RequireOldPassword: true,
            PasswordStatus: "Lỗi khi kiểm tra trạng thái",
          });
        }
      }
    };

    fetchPasswordStatus();
  }, [activeTab, userId]);

  // Updated Google Drive URL converter using your working function
  const convertGoogleDriveUrl = (url) => {
    if (!url) return "";

    if (url.includes("drive.google.com")) {
      const fileIdMatch =
        url.match(/\/d\/([a-zA-Z0-9-_]+)/) ||
        url.match(/[?&]id=([a-zA-Z0-9-_]+)/);
      if (fileIdMatch) {
        const fileId = fileIdMatch[1];
        return `https://drive.google.com/thumbnail?id=${fileId}&sz=w1000`;
      }
    }

    return url;
  };

  // Handle image load error with multiple fallback attempts
  const handleImageError = (e, attempt = 0) => {
    console.warn(
      `Failed to load image (attempt ${attempt + 1}):`,
      e.target.src
    );

    if (profileData.imageUrl && attempt < 2) {
      const fileIdMatch =
        profileData.imageUrl.match(/\/d\/([a-zA-Z0-9-_]+)/) ||
        profileData.imageUrl.match(/[?&]id=([a-zA-Z0-9-_]+)/);

      if (fileIdMatch) {
        const fileId = fileIdMatch[1];
        let fallbackUrl;

        if (attempt === 0) {
          // First fallback: Use thumbnail with different size
          fallbackUrl = `https://drive.google.com/thumbnail?id=${fileId}&sz=w500`;
        } else if (attempt === 1) {
          // Second fallback: Use uc?export=view format
          fallbackUrl = `https://drive.google.com/uc?export=view&id=${fileId}`;
        }

        if (fallbackUrl) {
          console.log(`Trying fallback ${attempt + 1}:`, fallbackUrl);
          e.target.src = fallbackUrl;
          e.target.onerror = (newE) => handleImageError(newE, attempt + 1);
          return;
        }
      }
    }

    // All attempts failed
    setImageLoadError(true);
  };

  // Reset image error state when image URL changes
  useEffect(() => {
    setImageLoadError(false);
  }, [profileData.imageUrl, avatarPreview]);

  // Load dữ liệu người dùng từ API - SIMPLIFIED
  useEffect(() => {
    const fetchUserData = async () => {
      try {
        setPageLoading(true);
        if (authLoading) {
          console.log("⏳ Waiting for auth context to load...");
          return; // ← DỪNG LẠI, CHƯA CALL API
        }

        // ✅ CHECK AUTHENTICATION
        if (!isLoggedIn || !userId) {
          console.error("❌ User not authenticated or missing userId");
          setMessage("Người dùng chưa đăng nhập");
          return;
        }

        console.log("👤 Auth loaded, fetching user data for userId:", userId);
        setPageLoading(true);

        // Load user data
        const userDataResult = await getUserById(userId);

        console.log("=== GET USER RESPONSE ===");
        console.log(JSON.stringify(userDataResult, null, 2));

        const isSuccess =
          userDataResult?.success === true || userDataResult?.Success === true;
        const userData = userDataResult?.data || userDataResult?.Data;

        if (isSuccess && userData) {
          // Store original email for comparison
          setOriginalEmail(userData.email || "");

          setProfileData({
            fullName: userData.fullName || "",
            email: userData.email || "",
            phone: userData.phone || "",
            isMale: userData.isMale !== undefined ? userData.isMale : true,
            address: userData.address || "",
            dob: userData.dob ? userData.dob.split("T")[0] : "",
            imageUrl: userData.imageUrl || "",
            imageId: userData.imageId || null,
          });

          // Set avatar preview nếu có imageUrl
          if (userData.imageUrl) {
            const convertedUrl = convertGoogleDriveUrl(userData.imageUrl);
            setAvatarPreview(convertedUrl);
            console.log("Original image URL:", userData.imageUrl);
            console.log("Converted image URL:", convertedUrl);
          }
        } else {
          const errorMessage =
            userDataResult?.message ||
            userDataResult?.Message ||
            "Không thể tải thông tin người dùng";
          setMessage(errorMessage);
        }
      } catch (error) {
        console.error("Error fetching data:", error);
        setMessage("Có lỗi xảy ra khi tải thông tin");
      } finally {
        setPageLoading(false);
      }
    };

    fetchUserData();
  }, [userId, isLoggedIn, authLoading]);

  // Simplified avatar upload handler
  const handleAvatarChange = async (e) => {
    const file = e.target.files[0];
    if (file) {
      // Validate file size (10MB limit to match backend)
      if (file.size > 10 * 1024 * 1024) {
        setMessage("Kích thước file không được vượt quá 10MB");
        return;
      }

      // Validate file type
      const allowedTypes = [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
      ];
      if (!allowedTypes.includes(file.type)) {
        setMessage("Chỉ chấp nhận file ảnh (JPEG, PNG, GIF)");
        return;
      }

      // Check if we have imageId to update
      if (!profileData.imageId) {
        setMessage(
          "Không tìm thấy ảnh để cập nhật, vui lòng liên hệ quản trị viên"
        );
        return;
      }

      try {
        setLoading(true);
        setMessage("");
        setImageLoadError(false);

        // Show preview immediately (local file preview)
        const reader = new FileReader();
        reader.onloadend = () => {
          setAvatarPreview(reader.result);
        };
        reader.readAsDataURL(file);

        // Update existing image using profileData.imageId
        console.log("Updating image with ID:", profileData.imageId);
        const response = await updateUserImage(
          profileData.imageId,
          file,
          1,
          "Profile avatar"
        );

        console.log("=== UPDATE IMAGE RESPONSE ===");
        console.log(JSON.stringify(response, null, 2));

        if (response && response.data) {
          const responseData =
            response.data.Data || response.data.data || response.data;
          const { imageUrl, imageId, message: updateMessage } = responseData;

          if (imageUrl) {
            // Convert Google Drive link
            const convertedUrl = convertGoogleDriveUrl(imageUrl);

            setProfileData((prev) => ({
              ...prev,
              imageUrl: imageUrl,
              imageId: imageId || prev.imageId,
            }));

            // Use converted URL for preview
            setAvatarPreview(convertedUrl);

            setMessage(updateMessage || "Cập nhật ảnh đại diện thành công");
            setTimeout(() => setMessage(""), 5000);

            console.log("✅ Avatar updated successfully:");
            console.log("Image ID:", imageId || profileData.imageId);
            console.log("Original URL:", imageUrl);
            console.log("Converted URL:", convertedUrl);
          } else {
            setMessage("Cập nhật ảnh đại diện thất bại");
          }
        } else {
          setMessage("Cập nhật ảnh đại diện thất bại");
        }
      } catch (error) {
        console.error("=== UPDATE IMAGE ERROR ===");
        console.error("Error:", error);

        // Handle specific error responses
        if (error.response?.data) {
          const errorMessage =
            error.response.data.message ||
            error.response.data.Message ||
            "Cập nhật ảnh đại diện thất bại";
          setMessage(errorMessage);
        } else {
          setMessage("Có lỗi xảy ra khi cập nhật ảnh đại diện");
        }

        // Reset preview on error - keep the current avatar
        if (profileData.imageUrl) {
          setAvatarPreview(convertGoogleDriveUrl(profileData.imageUrl));
        } else {
          setAvatarPreview("");
        }
      } finally {
        setLoading(false);
      }
    }
  };

  // Determine what to show in avatar
  const getAvatarContent = () => {
    // Priority: local preview > converted server image > fallback initials
    const imageUrl =
      avatarPreview || convertGoogleDriveUrl(profileData.imageUrl);

    if (imageUrl && !imageLoadError) {
      return (
        <img
          src={imageUrl}
          alt="Avatar"
          className="user-profile__avatar-image"
          onError={(e) => handleImageError(e, 0)}
          onLoad={() => {
            console.log("✅ Image loaded successfully:", imageUrl);
            setImageLoadError(false);
          }}
        />
      );
    } else {
      // Show initials as fallback
      return (
        <div className="user-profile__avatar-placeholder">
          {getAvatarInitials(profileData.fullName)}
          {imageLoadError && profileData.imageUrl && (
            <div className="avatar-error-overlay" title="Không thể tải ảnh">
              <i className="fas fa-exclamation-triangle"></i>
            </div>
          )}
        </div>
      );
    }
  };

  // Xử lý thay đổi input cho profile
  const handleProfileChange = (e) => {
    const { name, value, type, checked } = e.target;
    setProfileData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));

    // Clear error khi user nhập lại
    if (errors[name]) {
      setErrors((prev) => ({
        ...prev,
        [name]: "",
      }));
    }

    // Clear message khi user thay đổi input
    if (message) {
      setMessage("");
    }
  };

  // Xử lý thay đổi input cho password
  const handlePasswordChange = (e) => {
    const { name, value } = e.target;
    setPasswordData((prev) => ({
      ...prev,
      [name]: value,
    }));

    // Clear error khi user nhập lại
    if (errors[name]) {
      setErrors((prev) => ({
        ...prev,
        [name]: "",
      }));
    }

    // Clear message khi user thay đổi input
    if (message) {
      setMessage("");
    }
  };

  // Updated validation logic - loại bỏ bank validation
  const validateProfile = () => {
    const newErrors = {};

    // FullName - BẮT BUỘC
    if (!profileData.fullName?.trim()) {
      newErrors.fullName = "Tên người dùng không được để trống";
    } else if (profileData.fullName.length > 50) {
      newErrors.fullName = "Tên người dùng không được vượt quá 50 ký tự";
    }

    // KIỂM TRA PHƯƠNG THỨC ĐĂNG NHẬP - ít nhất 1 trong 2 (email hoặc phone)
    const hasEmail = profileData.email?.trim();
    const hasPhone = profileData.phone?.trim();

    if (!hasEmail && !hasPhone) {
      newErrors.email = "Cần có ít nhất email hoặc số điện thoại";
      newErrors.phone = "Cần có ít nhất email hoặc số điện thoại";
    } else {
      // Email - validate format nếu có giá trị
      if (hasEmail && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(profileData.email)) {
        newErrors.email = "Định dạng email không đúng";
      }
    }

    // Address - validate length nếu có giá trị
    if (profileData.address?.trim() && profileData.address.length > 255) {
      newErrors.address = "Địa chỉ không được vượt quá 255 ký tự";
    }

    // Date of Birth - validate nếu có giá trị
    if (profileData.dob) {
      const dobDate = new Date(profileData.dob);
      const today = new Date();

      if (dobDate > today) {
        newErrors.dob = "Ngày sinh không được là ngày tương lai";
      } else {
        // Check age >= 15
        const age = today.getFullYear() - dobDate.getFullYear();
        const monthDiff = today.getMonth() - dobDate.getMonth();
        const dayDiff = today.getDate() - dobDate.getDate();

        let actualAge = age;
        if (monthDiff < 0 || (monthDiff === 0 && dayDiff < 0)) {
          actualAge--;
        }

        if (actualAge < 15) {
          newErrors.dob = "Người dùng phải từ 15 tuổi trở lên";
        }
      }
    }

    return newErrors;
  };

  const validatePassword = () => {
    const newErrors = {};

    // Chỉ require old password nếu user đã có password
    if (
      passwordStatus?.RequireOldPassword &&
      !passwordData.oldPassword?.trim()
    ) {
      newErrors.oldPassword = "Mật khẩu cũ không được để trống";
    }

    // NEW: Validate new password với regex mới từ backend
    if (!passwordData.newPassword?.trim()) {
      newErrors.newPassword = "Vui lòng nhập mật khẩu mới";
    } else {
      const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;

      if (!passwordRegex.test(passwordData.newPassword)) {
        newErrors.newPassword =
          "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự";
      }
    }

    // Validate confirm password - khớp với backend
    if (!passwordData.confirmPassword?.trim()) {
      newErrors.confirmPassword = "Xác nhận mật khẩu không được để trống";
    } else if (passwordData.newPassword !== passwordData.confirmPassword) {
      newErrors.confirmPassword = "Mật khẩu xác nhận không trùng khớp";
    }

    return newErrors;
  };

  // Updated profile update handler - Send NULL for empty fields
  const handleUpdateProfile = async (e) => {
    e.preventDefault();

    // Basic frontend validation
    const validationErrors = validateProfile();
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setLoading(true);
    setMessage("");
    setErrors({});

    try {
      // Prepare update data
      const updateData = {
        fullName: profileData.fullName.trim(), // Required field - always has value
      };

      // 🎯 FIX: Send NULL for empty optional fields instead of empty string
      updateData.email = profileData.email?.trim() || null;
      updateData.phone = profileData.phone?.trim() || null;
      updateData.address = profileData.address?.trim() || "";

      // DOB: send value if exists, otherwise don't include field
      if (profileData.dob) {
        updateData.dob = profileData.dob;
      }

      // Gender luôn gửi (có default value)
      updateData.isMale = profileData.isMale;

      console.log("Updating profile with data:", updateData);

      const response = await updateUserProfile(userId, updateData);

      console.log("=== UPDATE RESPONSE ===");
      console.log(JSON.stringify(response, null, 2));

      // Handle both uppercase and lowercase properties
      const isSuccess =
        response?.Success === true || response?.success === true;
      const errorMessage = response?.Message || response?.message;
      const status = response?.Status || response?.status;

      console.log("isSuccess:", isSuccess);
      console.log("errorMessage:", errorMessage);
      console.log("status:", status);

      if (isSuccess) {
        // Success case
        setMessage(errorMessage || "Cập nhật thông tin người dùng thành công");
        setTimeout(() => setMessage(""), 5000);

        // Update original email after successful update
        setOriginalEmail(updateData.email || "");

        // REFRESH USER DATA sau khi cập nhật thành công
        try {
          console.log("🔄 Refreshing user data after successful update...");
          const userResponse = await getUserById(userId);

          console.log("=== REFRESH USER DATA RESPONSE ===");
          console.log(JSON.stringify(userResponse, null, 2));

          const isRefreshSuccess =
            userResponse?.success === true || userResponse?.Success === true;
          const refreshedUserData = userResponse?.data || userResponse?.Data;

          if (isRefreshSuccess && refreshedUserData) {
            setProfileData({
              fullName: refreshedUserData.fullName || "",
              email: refreshedUserData.email || "",
              phone: refreshedUserData.phone || "",
              isMale:
                refreshedUserData.isMale !== undefined
                  ? refreshedUserData.isMale
                  : true,
              address: refreshedUserData.address || "",
              dob: refreshedUserData.dob
                ? refreshedUserData.dob.split("T")[0]
                : "",
              imageUrl: refreshedUserData.imageUrl || "",
              imageId: refreshedUserData.imageId || null,
            });

            // Cập nhật lại originalEmail với email mới từ server
            setOriginalEmail(refreshedUserData.email || "");

            console.log("✅ User data refreshed successfully after update");
          } else {
            console.warn(
              "❌ Could not refresh user data:",
              userResponse?.message || userResponse?.Message
            );
          }
        } catch (refreshError) {
          console.error("❌ Error refreshing user data:", refreshError);
        }
      } else {
        // Error case - map to specific fields
        console.log("Processing error message:", errorMessage);

        if (errorMessage) {
          // EMAIL ERRORS
          if (
            errorMessage.includes("Email đã được sử dụng") ||
            errorMessage.includes("email đã tồn tại") ||
            errorMessage.includes("email này đã được sử dụng")
          ) {
            setErrors({ email: errorMessage });
            console.log("✅ Set email duplicate error:", errorMessage);
          } else if (
            errorMessage.includes("Địa chỉ Email không hợp lệ") ||
            errorMessage.includes("Email không hợp lệ") ||
            errorMessage.includes("định dạng email")
          ) {
            setErrors({ email: errorMessage });
            console.log("✅ Set email invalid error:", errorMessage);

            // PHONE ERRORS
          } else if (
            errorMessage.includes("Số điện thoại đã được sử dụng") ||
            errorMessage.includes("số điện thoại này đã được sử dụng")
          ) {
            setErrors({ phone: errorMessage });
            console.log("✅ Set phone duplicate error:", errorMessage);
          } else if (errorMessage.includes("Số điện thoại không hợp lệ")) {
            setErrors({ phone: errorMessage });
            console.log("✅ Set phone invalid error:", errorMessage);

            // AGE/DOB ERRORS
          } else if (
            errorMessage.includes("Người dùng phải từ 15 tuổi trở lên") ||
            errorMessage.includes("15 tuổi") ||
            errorMessage.includes("tuổi")
          ) {
            setErrors({ dob: errorMessage });
            console.log("✅ Set age error:", errorMessage);
          } else if (
            errorMessage.includes("Ngày sinh không được là ngày tương lai") ||
            errorMessage.includes("tương lai")
          ) {
            setErrors({ dob: errorMessage });
            console.log("✅ Set future date error:", errorMessage);

            // FULLNAME ERRORS
          } else if (
            errorMessage.includes("Tên người dùng không được để trống")
          ) {
            setErrors({ fullName: errorMessage });
            console.log("✅ Set fullName empty error:", errorMessage);
          } else if (
            errorMessage.includes(
              "Tên người dùng không được vượt quá 50 ký tự"
            ) ||
            errorMessage.includes("50 ký tự")
          ) {
            setErrors({ fullName: errorMessage });
            console.log("✅ Set fullName length error:", errorMessage);

            // ADDRESS ERRORS
          } else if (
            errorMessage.includes("Địa chỉ không được vượt quá 255 ký tự") ||
            errorMessage.includes("255 ký tự")
          ) {
            setErrors({ address: errorMessage });
            console.log("✅ Set address length error:", errorMessage);

            // GENERAL ERRORS
          } else {
            setMessage(errorMessage);
            console.log("✅ Set general message:", errorMessage);
          }
        } else {
          // No error message available
          setMessage("Có lỗi xảy ra khi cập nhật thông tin");
          console.log("❌ No error message available");
        }
      }
    } catch (error) {
      console.error("=== UPDATE CATCH ERROR ===");
      console.error("Error:", error);
      setMessage("Có lỗi kết nối. Vui lòng thử lại.");
    } finally {
      setLoading(false);
    }
  };

  // Xử lý đổi mật khẩu - FIXED
  const handleChangePassword = async (e) => {
    e.preventDefault();

    const validationErrors = validatePassword();
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setLoading(true);
    setMessage("");
    setErrors({});

    try {
      const changePasswordRequest = {
        userId: userId,
        newPassword: passwordData.newPassword,
        confirmPassword: passwordData.confirmPassword,
      };

      // Chỉ thêm oldPassword nếu RequireOldPassword = true
      if (passwordStatus?.RequireOldPassword) {
        changePasswordRequest.oldPassword = passwordData.oldPassword;
      }

      console.log("Changing password with data:", changePasswordRequest);

      const response = await changePassword(changePasswordRequest);

      console.log("=== CHANGE PASSWORD RESPONSE ===");
      console.log(JSON.stringify(response, null, 2));

      const isSuccess =
        response?.success === true || response?.Success === true;
      const errorMessage = response?.message || response?.Message;

      if (isSuccess) {
        // Success - show appropriate message
        const successMessage = passwordStatus?.HasPassword
          ? "Đổi mật khẩu thành công"
          : "Thiết lập mật khẩu thành công";

        setMessage(errorMessage || successMessage);

        // Reset form
        setPasswordData({
          oldPassword: "",
          newPassword: "",
          confirmPassword: "",
        });

        // Update password status sau khi thành công
        setPasswordStatus((prev) => ({
          ...prev,
          HasPassword: true,
          RequireOldPassword: true,
          PasswordStatus: "Đã thiết lập mật khẩu",
        }));

        setTimeout(() => setMessage(""), 5000);
      } else {
        // Error case
        if (errorMessage) {
          if (
            errorMessage.includes("mật khẩu cũ") &&
            errorMessage.includes("trống")
          ) {
            setErrors({ oldPassword: errorMessage });
          } else if (
            errorMessage.includes("Mật khẩu hiện tại không đúng") ||
            errorMessage.includes("Mật khẩu cũ sai") ||
            errorMessage.includes("Mật khẩu cũ không đúng") ||
            errorMessage.toLowerCase().includes("sai mật khẩu")
          ) {
            setErrors({ oldPassword: errorMessage });
          } else if (
            errorMessage.includes("mật khẩu mới") &&
            errorMessage.includes("trống")
          ) {
            setErrors({ newPassword: errorMessage });
          } else if (
            errorMessage.includes("ít nhất 8") ||
            errorMessage.includes("8 ký tự")
          ) {
            setErrors({ newPassword: errorMessage });
          } else if (
            errorMessage.includes("xác nhận") &&
            errorMessage.includes("trống")
          ) {
            setErrors({ confirmPassword: errorMessage });
          } else if (
            errorMessage.includes("không trùng khớp") ||
            errorMessage.includes("trùng khớp")
          ) {
            setErrors({ confirmPassword: errorMessage });
          } else {
            setMessage(errorMessage);
          }
        } else {
          setMessage("Có lỗi xảy ra khi đổi mật khẩu");
        }
      }
    } catch (error) {
      console.error("=== CHANGE PASSWORD CATCH ERROR ===", error);

      let errorMessage = "Có lỗi kết nối. Vui lòng thử lại.";

      if (error && typeof error === "object") {
        errorMessage = error.message || error.Message || errorMessage;
      }

      setMessage(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  // Generate avatar initials
  const getAvatarInitials = (fullName) => {
    if (!fullName) return "U";
    return fullName
      .split(" ")
      .map((name) => name.charAt(0))
      .join("")
      .substring(0, 2)
      .toUpperCase();
  };

  // Show loading screen while fetching user data
  if (pageLoading) {
    return (
      <div className="user-profile">
        <div className="user-profile__container">
          <div className="user-profile__loading">
            <div className="loading-spinner"></div>
            <p>Đang tải thông tin người dùng...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="user-profile">
      <div className="user-profile__container">
        {/* Avatar Section */}
        <div className="user-profile__avatar-section">
          <div className="user-profile__avatar-wrapper">
            {getAvatarContent()}
            <label
              htmlFor="avatar-upload"
              className="user-profile__avatar-upload-btn"
              title="Thay đổi ảnh đại diện"
            >
              <i className="fas fa-camera"></i>
            </label>
            <input
              id="avatar-upload"
              type="file"
              accept="image/*"
              onChange={handleAvatarChange}
              style={{ display: "none" }}
            />
          </div>
        </div>

        {/* Image load error warning */}
        {imageLoadError && profileData.imageUrl && (
          <div className="user-profile__image-warning">
            <i className="fas fa-exclamation-triangle"></i>
            <span>
              Không thể tải ảnh đại diện. Đang hiển thị chữ cái thay thế.
            </span>
            <button
              className="retry-btn"
              onClick={() => {
                setImageLoadError(false);
                setAvatarPreview(convertGoogleDriveUrl(profileData.imageUrl));
              }}
            >
              <i className="fas fa-redo"></i> Thử lại
            </button>
          </div>
        )}

        {/* Message */}
        {message && (
          <div
            className={`user-profile__message ${
              message.includes("thành công") ? "success" : "error"
            }`}
          >
            <i
              className={`fas ${
                message.includes("thành công")
                  ? "fa-check-circle"
                  : "fa-exclamation-circle"
              }`}
            ></i>
            {message}
          </div>
        )}

        {/* Show warning if no image ID found */}
        {!profileData.imageId && (
          <div className="user-profile__image-warning">
            <i className="fas fa-info-circle"></i>
            <span>Chưa tìm thấy ảnh đại diện. Vui lòng tải lên ảnh mới.</span>
          </div>
        )}

        {/* Tabs */}
        <div className="user-profile__tabs">
          <button
            className={`user-profile__tab ${
              activeTab === "profile" ? "active" : ""
            }`}
            onClick={() => setActiveTab("profile")}
          >
            <i className="fas fa-user"></i>
            Thông tin cá nhân
          </button>
          <button
            className={`user-profile__tab ${
              activeTab === "password" ? "active" : ""
            }`}
            onClick={() => setActiveTab("password")}
          >
            <i className="fas fa-lock"></i>
            Đổi mật khẩu
          </button>
        </div>

        {/* Profile Tab */}
        {activeTab === "profile" && (
          <form className="user-profile__form" onSubmit={handleUpdateProfile}>
            <div className="user-profile__form-row">
              <div className="user-profile__form-group">
                <label>
                  <i className="fas fa-user"></i>
                  Tên người dùng *
                </label>
                <input
                  type="text"
                  name="fullName"
                  value={profileData.fullName}
                  onChange={handleProfileChange}
                  className={errors.fullName ? "error" : ""}
                  placeholder="Nhập tên người dùng"
                />
                {errors.fullName && (
                  <span className="error-text">{errors.fullName}</span>
                )}
              </div>

              <div className="user-profile__form-group">
                <label>
                  <i className="fas fa-envelope"></i>
                  Email
                </label>
                <input
                  type="email"
                  name="email"
                  value={profileData.email}
                  onChange={handleProfileChange}
                  className={errors.email ? "error" : ""}
                  placeholder="Nhập địa chỉ email (tùy chọn)"
                />
                {errors.email && (
                  <span className="error-text">{errors.email}</span>
                )}
              </div>
            </div>

            <div className="user-profile__form-row">
              <div className="user-profile__form-group">
                <label>
                  <i className="fas fa-phone"></i>
                  Số điện thoại
                </label>
                <input
                  type="tel"
                  name="phone"
                  value={profileData.phone}
                  onChange={handleProfileChange}
                  placeholder="Nhập số điện thoại (tùy chọn)"
                  className={errors.phone ? "error" : ""}
                />
                {errors.phone && (
                  <span className="error-text">{errors.phone}</span>
                )}
              </div>

              <div className="user-profile__form-group">
                <label>
                  <i className="fas fa-calendar"></i>
                  Ngày sinh
                </label>
                <input
                  type="date"
                  name="dob"
                  value={profileData.dob}
                  onChange={handleProfileChange}
                  className={errors.dob ? "error" : ""}
                />
                {errors.dob && <span className="error-text">{errors.dob}</span>}
              </div>
            </div>

            <div className="user-profile__form-group">
              <label>
                <i className="fas fa-venus-mars"></i>
                Giới tính
              </label>
              <div className="user-profile__radio-group">
                <label className="user-profile__radio">
                  <input
                    type="radio"
                    name="isMale"
                    checked={profileData.isMale === true}
                    onChange={() =>
                      setProfileData((prev) => ({ ...prev, isMale: true }))
                    }
                  />
                  <span className="radio-custom"></span>
                  <span>Nam</span>
                </label>
                <label className="user-profile__radio">
                  <input
                    type="radio"
                    name="isMale"
                    checked={profileData.isMale === false}
                    onChange={() =>
                      setProfileData((prev) => ({ ...prev, isMale: false }))
                    }
                  />
                  <span className="radio-custom"></span>
                  <span>Nữ</span>
                </label>
              </div>
            </div>

            <div className="user-profile__form-group">
              <label>
                <i className="fas fa-map-marker-alt"></i>
                Địa chỉ
              </label>
              <input
                type="text"
                name="address"
                value={profileData.address}
                onChange={handleProfileChange}
                className={errors.address ? "error" : ""}
                placeholder="Nhập địa chỉ (tùy chọn)"
              />
              {errors.address && (
                <span className="error-text">{errors.address}</span>
              )}
            </div>

            <button
              type="submit"
              className="user-profile__submit-btn"
              disabled={loading}
            >
              {loading && <span className="loading-spinner"></span>}
              {loading ? "Đang cập nhật..." : "Cập nhật thông tin"}
            </button>
          </form>
        )}

        {/* Password Tab - FINAL VERSION */}
        {activeTab === "password" && (
          <div className="user-profile__password-section">
            {/* Loading state */}
            {!passwordStatus && (
              <div className="password-status-loading">
                <div className="loading-spinner"></div>
                <p>Đang kiểm tra trạng thái mật khẩu...</p>
              </div>
            )}

            {/* Password Form */}
            {passwordStatus && (
              <form
                className="user-profile__form"
                onSubmit={handleChangePassword}
              >
                {/* Old Password - CHỈ hiển thị khi RequireOldPassword = true */}
                {passwordStatus.RequireOldPassword && (
                  <div className="user-profile__form-group">
                    <label>
                      <i className="fas fa-lock"></i>
                      Mật khẩu cũ *
                    </label>
                    <input
                      type="password"
                      name="oldPassword"
                      value={passwordData.oldPassword}
                      onChange={handlePasswordChange}
                      className={errors.oldPassword ? "error" : ""}
                      placeholder="Nhập mật khẩu hiện tại"
                      autoComplete="current-password"
                    />
                    {errors.oldPassword && (
                      <span className="error-text">{errors.oldPassword}</span>
                    )}
                  </div>
                )}

                {/* Notice cho first-time setup */}
                {!passwordStatus.RequireOldPassword && (
                  <div className="password-setup-notice">
                    <i className="fas fa-info-circle"></i>
                    <span>Thiết lập mật khẩu lần đầu</span>
                  </div>
                )}

                <div className="user-profile__form-group">
                  <label>
                    <i className="fas fa-key"></i>
                    {passwordStatus.RequireOldPassword
                      ? "Mật khẩu mới *"
                      : "Mật khẩu *"}
                  </label>
                  <input
                    type="password"
                    name="newPassword"
                    value={passwordData.newPassword}
                    onChange={handlePasswordChange}
                    className={errors.newPassword ? "error" : ""}
                    placeholder={
                      passwordStatus.RequireOldPassword
                        ? "Nhập mật khẩu mới (ít nhất 6 ký tự, có chữ hoa, thường, số)"
                        : "Nhập mật khẩu (ít nhất 6 ký tự, có chữ hoa, thường, số)"
                    }
                    autoComplete="new-password"
                  />
                  {errors.newPassword && (
                    <span className="error-text">{errors.newPassword}</span>
                  )}
                </div>

                <div className="user-profile__form-group">
                  <label>
                    <i className="fas fa-shield-alt"></i>
                    Xác nhận mật khẩu *
                  </label>
                  <input
                    type="password"
                    name="confirmPassword"
                    value={passwordData.confirmPassword}
                    onChange={handlePasswordChange}
                    className={errors.confirmPassword ? "error" : ""}
                    placeholder="Nhập lại mật khẩu"
                    autoComplete="new-password"
                  />
                  {errors.confirmPassword && (
                    <span className="error-text">{errors.confirmPassword}</span>
                  )}
                </div>

                <button
                  type="submit"
                  className="user-profile__submit-btn"
                  disabled={loading}
                >
                  {loading && <span className="loading-spinner"></span>}
                  {loading
                    ? "Đang xử lý..."
                    : passwordStatus.RequireOldPassword
                    ? "Đổi mật khẩu"
                    : "Thiết lập mật khẩu"}
                </button>
              </form>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default UserProfile;
