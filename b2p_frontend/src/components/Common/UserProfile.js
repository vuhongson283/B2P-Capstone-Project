import React, { useState, useEffect } from "react";
import "./UserProfile.scss";
import {
  getUserById,
  updateUserProfile,
  changePassword,
  getAllBankType,
  updateUserImage,
} from "../../services/apiService";

const UserProfile = (props) => {
  const [activeTab, setActiveTab] = useState("profile");
  const [loading, setLoading] = useState(false);
  const [pageLoading, setPageLoading] = useState(true);
  const [message, setMessage] = useState("");
  const [avatarPreview, setAvatarPreview] = useState("");
  const [bankTypes, setBankTypes] = useState([]);
  const [originalEmail, setOriginalEmail] = useState("");
  const [imageLoadError, setImageLoadError] = useState(false);

  // Temporary userId - sẽ thay thế bằng userId từ authentication sau
  const userId = 1;

  // State cho thông tin cơ bản
  const [profileData, setProfileData] = useState({
    fullName: "",
    email: "",
    phone: "",
    isMale: true,
    address: "",
    dob: "",
    accountNumber: "",
    accountHolder: "",
    bankTypeId: 0,
    bankName: "",
    imageUrl: "",
    imageId: null, // Add imageId to profileData
  });

  // State cho đổi mật khẩu
  const [passwordData, setPasswordData] = useState({
    oldPassword: "",
    newPassword: "",
    confirmPassword: "",
    userId: userId,
  });

  const [errors, setErrors] = useState({});

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

  // Helper function để tìm bank type ID từ bank name
  const findBankTypeIdByName = (bankName, bankTypesList) => {
    if (!bankName || !bankTypesList.length) return 0;

    const foundBank = bankTypesList.find(
      (bank) =>
        bank.bankName &&
        bank.bankName.toLowerCase().trim() === bankName.toLowerCase().trim()
    );

    console.log("Finding bank:", bankName, "Found:", foundBank);
    return foundBank ? foundBank.bankTypeId : 0;
  };

  // Load danh sách ngân hàng
  const fetchBankTypes = async () => {
    try {
      const response = await getAllBankType("", 1, 100);

      // Check both possible response structures
      if (response && (response.success || response.Success)) {
        const bankTypesList =
          response.data?.items ||
          response.data ||
          response.Data?.items ||
          response.Data ||
          [];
        setBankTypes(bankTypesList);
        console.log("Bank types loaded:", bankTypesList);
        return bankTypesList;
      } else {
        console.warn("Could not load bank types:", response);
        setBankTypes([]);
        return [];
      }
    } catch (error) {
      console.error("Error fetching bank types:", error);
      setBankTypes([]);
      return [];
    }
  };

  // Load dữ liệu người dùng từ API - SIMPLIFIED
  useEffect(() => {
    const fetchUserData = async () => {
      try {
        setPageLoading(true);

        // Load bank types trước
        const bankTypesList = await fetchBankTypes();

        // Load user data
        const userDataResult = await getUserById(userId);

        console.log("=== GET USER RESPONSE ===");
        console.log(JSON.stringify(userDataResult, null, 2));

        // Check both possible response structures for getUserById
        const isSuccess =
          userDataResult?.success === true || userDataResult?.Success === true;
        const userData = userDataResult?.data || userDataResult?.Data;

        if (isSuccess && userData) {
          // Store original email for comparison
          setOriginalEmail(userData.email || "");

          // Tự động khớp bankTypeId từ bankName nếu có
          let matchedBankTypeId = userData.bankTypeId || 0;

          if (userData.bankName && bankTypesList.length > 0) {
            const foundBankTypeId = findBankTypeIdByName(
              userData.bankName,
              bankTypesList
            );
            if (foundBankTypeId > 0) {
              matchedBankTypeId = foundBankTypeId;
              console.log(
                `Matched bank "${userData.bankName}" with ID: ${foundBankTypeId}`
              );
            } else {
              const existsInList = bankTypesList.find(
                (bank) => bank.bankTypeId === userData.bankTypeId
              );
              if (!existsInList) {
                matchedBankTypeId = 0;
                console.log(
                  `Bank "${userData.bankName}" not found in bank types list, reset to default`
                );
              }
            }
          } else if (userData.bankTypeId && bankTypesList.length > 0) {
            const existsInList = bankTypesList.find(
              (bank) => bank.bankTypeId === userData.bankTypeId
            );
            if (!existsInList) {
              matchedBankTypeId = 0;
              console.log(
                `BankTypeId ${userData.bankTypeId} not found in bank types list, reset to default`
              );
            }
          }

          setProfileData({
            fullName: userData.fullName || "",
            email: userData.email || "",
            phone: userData.phone || "",
            isMale: userData.isMale !== undefined ? userData.isMale : true,
            address: userData.address || "",
            dob: userData.dob ? userData.dob.split("T")[0] : "",
            accountNumber: userData.accountNumber || "",
            accountHolder: userData.accountHolder || "",
            bankTypeId: matchedBankTypeId,
            bankName: userData.bankName || "",
            imageUrl: userData.imageUrl || "",
            imageId: userData.imageId || null, // Get imageId from user data directly
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
  }, [userId]);

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
              imageId: imageId || prev.imageId, // Keep existing imageId if not provided
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

  // Lightweight validation - only basic checks
  const validateProfile = () => {
    const newErrors = {};

    // Basic checks only - let backend do the heavy lifting
    if (!profileData.fullName?.trim()) {
      newErrors.fullName = "Vui lòng nhập tên";
    }

    if (!profileData.email?.trim()) {
      newErrors.email = "Vui lòng nhập email";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(profileData.email)) {
      newErrors.email = "Định dạng email không đúng";
    }

    if (!profileData.address?.trim()) {
      newErrors.address = "Vui lòng nhập địa chỉ";
    }

    if (!profileData.dob) {
      newErrors.dob = "Vui lòng chọn ngày sinh";
    }

    if (!profileData.accountNumber?.trim()) {
      newErrors.accountNumber = "Vui lòng nhập số tài khoản";
    }

    if (!profileData.accountHolder?.trim()) {
      newErrors.accountHolder = "Vui lòng nhập tên chủ tài khoản";
    }

    if (!profileData.bankTypeId || profileData.bankTypeId <= 0) {
      newErrors.bankTypeId = "Vui lòng chọn ngân hàng";
    }

    return newErrors;
  };

  // Lightweight password validation
  const validatePassword = () => {
    const newErrors = {};

    if (!passwordData.oldPassword?.trim()) {
      newErrors.oldPassword = "Vui lòng nhập mật khẩu cũ";
    }

    if (!passwordData.newPassword?.trim()) {
      newErrors.newPassword = "Vui lòng nhập mật khẩu mới";
    }

    if (!passwordData.confirmPassword?.trim()) {
      newErrors.confirmPassword = "Vui lòng nhập xác nhận mật khẩu";
    } else if (passwordData.newPassword !== passwordData.confirmPassword) {
      newErrors.confirmPassword = "Mật khẩu xác nhận không trùng khớp";
    }

    return newErrors;
  };

  // Xử lý cập nhật profile
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
      const updateData = {
        fullName: profileData.fullName.trim(),
        email: profileData.email.trim(),
        address: profileData.address.trim(),
        dob: profileData.dob,
        accountNumber: profileData.accountNumber.trim(),
        accountHolder: profileData.accountHolder.trim(),
        bankTypeId: parseInt(profileData.bankTypeId),
        isMale: profileData.isMale,
      };

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
        setOriginalEmail(updateData.email);

        // Refresh user data
        try {
          const userResponse = await getUserById(userId);
          const isRefreshSuccess =
            userResponse?.success === true || userResponse?.Success === true;
          if (isRefreshSuccess) {
            const userData = userResponse?.data || userResponse?.Data;
            setProfileData((prev) => ({
              ...prev,
              bankName: userData?.bankName || "",
            }));
          }
        } catch (refreshError) {
          console.warn("Could not refresh user data:", refreshError);
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
          } else if (errorMessage.includes("Email không được để trống")) {
            setErrors({ email: errorMessage });
            console.log("✅ Set email empty error:", errorMessage);

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
          } else if (errorMessage.includes("Ngày sinh không được để trống")) {
            setErrors({ dob: errorMessage });
            console.log("✅ Set dob empty error:", errorMessage);

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
          } else if (errorMessage.includes("Địa chỉ không được để trống")) {
            setErrors({ address: errorMessage });
            console.log("✅ Set address empty error:", errorMessage);
          } else if (
            errorMessage.includes("Địa chỉ không được vượt quá 255 ký tự") ||
            errorMessage.includes("255 ký tự")
          ) {
            setErrors({ address: errorMessage });
            console.log("✅ Set address length error:", errorMessage);

            // ACCOUNT NUMBER ERRORS
          } else if (
            errorMessage.includes("Số tài khoản không hợp lệ") ||
            errorMessage.includes("9-16 ký tự") ||
            errorMessage.includes("tài khoản")
          ) {
            setErrors({ accountNumber: errorMessage });
            console.log("✅ Set accountNumber error:", errorMessage);

            // ACCOUNT HOLDER ERRORS
          } else if (
            errorMessage.includes("Tên chủ tài khoản không được để trống")
          ) {
            setErrors({ accountHolder: errorMessage });
            console.log("✅ Set accountHolder empty error:", errorMessage);
          } else if (
            errorMessage.includes(
              "Tên chủ tài khoản không được vượt quá 50 ký tự"
            )
          ) {
            setErrors({ accountHolder: errorMessage });
            console.log("✅ Set accountHolder length error:", errorMessage);

            // BANK TYPE ERRORS
          } else if (
            errorMessage.includes("Loại ngân hàng không hợp lệ") ||
            errorMessage.includes("ngân hàng không hợp lệ")
          ) {
            setErrors({ bankTypeId: errorMessage });
            console.log("✅ Set bankTypeId invalid error:", errorMessage);
          } else if (
            errorMessage.includes("Không tìm thấy kiểu ngân hàng đã chọn") ||
            errorMessage.includes("không tìm thấy") ||
            errorMessage.includes("ngân hàng")
          ) {
            setErrors({ bankTypeId: errorMessage });
            console.log("✅ Set bankTypeId not found error:", errorMessage);

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

  // Xử lý đổi mật khẩu
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
        oldPassword: passwordData.oldPassword,
        newPassword: passwordData.newPassword,
        confirmPassword: passwordData.confirmPassword,
        userId: userId,
      };

      console.log("Changing password for userId:", userId);

      const response = await changePassword(changePasswordRequest);

      console.log("=== PASSWORD RESPONSE ===");
      console.log(JSON.stringify(response, null, 2));

      // Handle both uppercase and lowercase properties
      const isSuccess =
        response?.Success === true || response?.success === true;
      const errorMessage = response?.Message || response?.message;

      console.log("Password isSuccess:", isSuccess);
      console.log("Password errorMessage:", errorMessage);

      if (isSuccess) {
        // Success case
        setMessage(errorMessage || "Đổi mật khẩu thành công");
        setPasswordData({
          oldPassword: "",
          newPassword: "",
          confirmPassword: "",
          userId: userId,
        });
        setTimeout(() => setMessage(""), 5000);
      } else {
        // Error case
        console.log("Processing password error message:", errorMessage);

        if (errorMessage) {
          // OLD PASSWORD ERRORS
          if (errorMessage.includes("Mật khẩu cũ không được để trống")) {
            setErrors({ oldPassword: errorMessage });
            console.log("✅ Set oldPassword empty error:", errorMessage);
          } else if (
            errorMessage.includes("Mật khẩu hiện tại không đúng") ||
            errorMessage.includes("Mật khẩu cũ sai") ||
            errorMessage.includes("Mật khẩu cũ không đúng") ||
            errorMessage.includes("sai mật khẩu") ||
            errorMessage.includes("không đúng")
          ) {
            setErrors({ oldPassword: errorMessage });
            console.log("✅ Set oldPassword wrong error:", errorMessage);

            // NEW PASSWORD ERRORS
          } else if (
            errorMessage.includes("Mật khẩu mới không được để trống")
          ) {
            setErrors({ newPassword: errorMessage });
            console.log("✅ Set newPassword empty error:", errorMessage);
          } else if (
            errorMessage.includes("Mật khẩu có ít nhất 8 ký tự") ||
            errorMessage.includes("ít nhất 8") ||
            errorMessage.includes("8 ký tự")
          ) {
            setErrors({ newPassword: errorMessage });
            console.log("✅ Set newPassword length error:", errorMessage);

            // CONFIRM PASSWORD ERRORS
          } else if (
            errorMessage.includes("Xác nhận mật khẩu không được để trống")
          ) {
            setErrors({ confirmPassword: errorMessage });
            console.log("✅ Set confirmPassword empty error:", errorMessage);
          } else if (
            errorMessage.includes("Mật khẩu xác nhận không trùng khớp") ||
            errorMessage.includes("không trùng khớp") ||
            errorMessage.includes("xác nhận") ||
            errorMessage.includes("trùng khớp")
          ) {
            setErrors({ confirmPassword: errorMessage });
            console.log("✅ Set confirmPassword mismatch error:", errorMessage);

            // GENERAL ERRORS - show in message area
          } else {
            setMessage(errorMessage);
            console.log("✅ Set general password message:", errorMessage);
          }
        } else {
          setMessage("Có lỗi xảy ra khi đổi mật khẩu");
          console.log("❌ No password error message available");
        }
      }
    } catch (error) {
      console.error("=== PASSWORD CATCH ERROR ===");
      console.error("Error:", error);
      setMessage("Có lỗi kết nối. Vui lòng thử lại.");
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

  // Get bank name by ID
  const getBankNameById = (bankTypeId) => {
    const bank = bankTypes.find(
      (bank) => bank.bankTypeId === parseInt(bankTypeId)
    );
    return bank ? bank.bankName : "";
  };

  // Check if current bank selection is valid
  const isCurrentBankSelectionValid = () => {
    return (
      profileData.bankTypeId > 0 && getBankNameById(profileData.bankTypeId)
    );
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
                  Email *
                </label>
                <input
                  type="email"
                  name="email"
                  value={profileData.email}
                  onChange={handleProfileChange}
                  className={errors.email ? "error" : ""}
                  placeholder="Nhập địa chỉ email"
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
                  placeholder="Số điện thoại không thể thay đổi"
                  disabled
                  title="Số điện thoại không thể thay đổi"
                />
              </div>

              <div className="user-profile__form-group">
                <label>
                  <i className="fas fa-calendar"></i>
                  Ngày sinh *
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
                Địa chỉ *
              </label>
              <input
                type="text"
                name="address"
                value={profileData.address}
                onChange={handleProfileChange}
                className={errors.address ? "error" : ""}
                placeholder="Nhập địa chỉ"
              />
              {errors.address && (
                <span className="error-text">{errors.address}</span>
              )}
            </div>

            <div className="user-profile__form-row">
              <div className="user-profile__form-group">
                <label>
                  <i className="fas fa-credit-card"></i>
                  Số tài khoản *
                </label>
                <input
                  type="text"
                  name="accountNumber"
                  value={profileData.accountNumber}
                  onChange={handleProfileChange}
                  className={errors.accountNumber ? "error" : ""}
                  placeholder="Nhập số tài khoản"
                />
                {errors.accountNumber && (
                  <span className="error-text">{errors.accountNumber}</span>
                )}
              </div>

              <div className="user-profile__form-group">
                <label>
                  <i className="fas fa-user-tie"></i>
                  Tên chủ tài khoản *
                </label>
                <input
                  type="text"
                  name="accountHolder"
                  value={profileData.accountHolder}
                  onChange={handleProfileChange}
                  className={errors.accountHolder ? "error" : ""}
                  placeholder="Nhập tên chủ tài khoản"
                />
                {errors.accountHolder && (
                  <span className="error-text">{errors.accountHolder}</span>
                )}
              </div>
            </div>

            <div className="user-profile__form-group">
              <label>
                <i className="fas fa-university"></i>
                Ngân hàng *
              </label>
              <select
                name="bankTypeId"
                value={profileData.bankTypeId}
                onChange={handleProfileChange}
                className={errors.bankTypeId ? "error" : ""}
              >
                <option value={0}>Chọn ngân hàng</option>
                {bankTypes.map((bank) => (
                  <option key={bank.bankTypeId} value={bank.bankTypeId}>
                    {bank.bankName}
                    {bank.description && ` - ${bank.description}`}
                  </option>
                ))}
              </select>
              {errors.bankTypeId && (
                <span className="error-text">{errors.bankTypeId}</span>
              )}

              {/* Hiển thị cảnh báo nếu bankName từ server không khớp với danh sách */}
              {profileData.bankName &&
                !isCurrentBankSelectionValid() &&
                profileData.bankTypeId === 0 && (
                  <span className="bank-name-display bank-not-matched">
                    <i className="fas fa-exclamation-triangle"></i>
                    Ngân hàng từ hệ thống: "{profileData.bankName}" - Không tìm
                    thấy trong danh sách
                  </span>
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

        {/* Password Tab */}
        {activeTab === "password" && (
          <form className="user-profile__form" onSubmit={handleChangePassword}>
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
                placeholder="Nhập mật khẩu cũ"
              />
              {errors.oldPassword && (
                <span className="error-text">{errors.oldPassword}</span>
              )}
            </div>

            <div className="user-profile__form-group">
              <label>
                <i className="fas fa-key"></i>
                Mật khẩu mới *
              </label>
              <input
                type="password"
                name="newPassword"
                value={passwordData.newPassword}
                onChange={handlePasswordChange}
                className={errors.newPassword ? "error" : ""}
                placeholder="Nhập mật khẩu mới"
              />
              {errors.newPassword && (
                <span className="error-text">{errors.newPassword}</span>
              )}
            </div>

            <div className="user-profile__form-group">
              <label>
                <i className="fas fa-shield-alt"></i>
                Xác nhận mật khẩu mới *
              </label>
              <input
                type="password"
                name="confirmPassword"
                value={passwordData.confirmPassword}
                onChange={handlePasswordChange}
                className={errors.confirmPassword ? "error" : ""}
                placeholder="Xác nhận mật khẩu mới"
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
              {loading ? "Đang đổi mật khẩu..." : "Đổi mật khẩu"}
            </button>
          </form>
        )}
      </div>
    </div>
  );
};

export default UserProfile;
