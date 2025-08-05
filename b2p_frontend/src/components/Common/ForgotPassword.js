import React, { useState, useEffect } from "react";
import "./ForgotPassword.scss";
import {
  forgotPasswordByEmail,
  resetPasswordByEmail,
  resendOtpByEmail,
  forgotPasswordBySms,
  resetPasswordBySms,
  resendOtpBySms,
} from "../../services/apiService";

const ForgotPassword = (props) => {
  const [step, setStep] = useState(1);
  const [method, setMethod] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [errors, setErrors] = useState({});
  const [countdown, setCountdown] = useState(0);

  // Form data
  const [formData, setFormData] = useState({
    email: "",
    phoneNumber: "",
    otpCode: "",
    newPassword: "",
    confirmPassword: "",
  });

  // Countdown timer for resend OTP
  useEffect(() => {
    let timer;
    if (countdown > 0) {
      timer = setTimeout(() => setCountdown(countdown - 1), 1000);
    }
    return () => clearTimeout(timer);
  }, [countdown]);

  // Handle input changes
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));

    // Clear errors when user types
    if (errors[name]) {
      setErrors((prev) => ({
        ...prev,
        [name]: "",
      }));
    }

    // Clear message
    if (message) {
      setMessage("");
    }
  };

  // Validate email
  const validateEmail = (email) => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  };

  // Validate phone number (Vietnamese format)
  const validatePhoneNumber = (phone) => {
    return /^0[3-9]\d{8}$/.test(phone);
  };

  // Extract response data - Backend trả về response trực tiếp, không có .data wrapper
  const getResponseData = (response) => {
    // Response structure: { Data, Message, Success, Status }
    return response.data || response;
  };

  // Check if response is successful
  const isSuccessResponse = (response) => {
    const data = getResponseData(response);
    return data.Success === true || data.success === true;
  };

  // Get response message
  const getResponseMessage = (response) => {
    const data = getResponseData(response);
    return data.Message || data.message || "";
  };

  // Extract error message from error response
  const getErrorMessage = (error) => {
    console.log("=== ERROR OBJECT ===");
    console.log(error);

    if (error.response) {
      // Backend trả về error response
      const errorData = error.response.data;
      console.log("=== ERROR RESPONSE DATA ===");
      console.log(errorData);

      return errorData.Message || errorData.message || "Có lỗi xảy ra";
    }

    return "Có lỗi kết nối. Vui lòng thử lại.";
  };

  // Step 1: Choose method
  const handleMethodSelect = (selectedMethod) => {
    setMethod(selectedMethod);
    setStep(2);
    setMessage("");
    setErrors({});
  };

  // Step 2: Send OTP
  const handleSendOtp = async (e) => {
    e.preventDefault();

    const newErrors = {};

    if (method === "email") {
      if (!formData.email.trim()) {
        newErrors.email = "Vui lòng nhập email";
      } else if (!validateEmail(formData.email)) {
        newErrors.email = "Định dạng email không đúng";
      }
    } else {
      if (!formData.phoneNumber.trim()) {
        newErrors.phoneNumber = "Vui lòng nhập số điện thoại";
      } else if (!validatePhoneNumber(formData.phoneNumber)) {
        newErrors.phoneNumber =
          "Số điện thoại không hợp lệ (10 số, bắt đầu bằng 0)";
      }
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setLoading(true);
    setMessage("");
    setErrors({});

    try {
      let response;

      if (method === "email") {
        response = await forgotPasswordByEmail(formData.email.trim());
      } else {
        response = await forgotPasswordBySms(formData.phoneNumber.trim());
      }

      console.log("=== SEND OTP SUCCESS RESPONSE ===");
      console.log(JSON.stringify(response, null, 2));

      if (isSuccessResponse(response)) {
        const successMessage =
          getResponseMessage(response) || "Mã OTP đã được gửi thành công";
        setMessage(successMessage);
        setStep(3);
        setCountdown(60); // Start 60 second countdown
      } else {
        // Response có Success = false
        const errorMessage = getResponseMessage(response);
        setMessage(errorMessage);
      }
    } catch (error) {
      console.error("=== SEND OTP ERROR ===");
      console.error("Error:", error);
      console.error("Error Response:", error.response);

      const errorMessage = getErrorMessage(error);
      setMessage(errorMessage);

      // Handle specific error cases - map to field errors if needed
      if (error.response) {
        const status = error.response.status;
        const errorData = error.response.data;

        if (status === 404) {
          // User not found
          if (method === "email") {
            setErrors({ email: errorMessage });
          } else {
            setErrors({ phoneNumber: errorMessage });
          }
        } else if (status === 400) {
          // Validation error
          if (
            errorMessage.includes("email") ||
            errorMessage.includes("Email")
          ) {
            setErrors({ email: errorMessage });
          } else if (
            errorMessage.includes("số điện thoại") ||
            errorMessage.includes("PhoneNumber")
          ) {
            setErrors({ phoneNumber: errorMessage });
          }
        }
      }
    } finally {
      setLoading(false);
    }
  };

  // Resend OTP
  const handleResendOtp = async () => {
    if (countdown > 0) return;

    setLoading(true);
    setMessage("");

    try {
      let response;

      if (method === "email") {
        response = await resendOtpByEmail(formData.email);
      } else {
        response = await resendOtpBySms(formData.phoneNumber);
      }

      console.log("=== RESEND OTP RESPONSE ===");
      console.log(JSON.stringify(response, null, 2));

      if (isSuccessResponse(response)) {
        const successMessage =
          getResponseMessage(response) || "OTP đã được gửi lại";
        setMessage(successMessage);
        setCountdown(60);
      } else {
        const errorMessage = getResponseMessage(response);
        setMessage(errorMessage);
      }
    } catch (error) {
      console.error("=== RESEND OTP ERROR ===");
      console.error("Error:", error);

      const errorMessage = getErrorMessage(error);
      setMessage(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  // Step 3: Reset password
  const handleResetPassword = async (e) => {
    e.preventDefault();

    const newErrors = {};

    if (!formData.otpCode.trim()) {
      newErrors.otpCode = "Vui lòng nhập mã OTP";
    } else if (!/^\d{6}$/.test(formData.otpCode)) {
      newErrors.otpCode = "Mã OTP phải gồm 6 chữ số";
    }

    // 🎯 NEW: Validate password với regex mới từ backend
    if (!formData.newPassword.trim()) {
      newErrors.newPassword = "Vui lòng nhập mật khẩu mới";
    } else {
      // 🎯 Backend regex: ^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$
      const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;

      if (!passwordRegex.test(formData.newPassword)) {
        newErrors.newPassword =
          "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự";
      }
    }

    if (!formData.confirmPassword.trim()) {
      newErrors.confirmPassword = "Vui lòng nhập xác nhận mật khẩu";
    } else if (formData.newPassword !== formData.confirmPassword) {
      newErrors.confirmPassword = "Mật khẩu xác nhận không trùng khớp";
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setLoading(true);
    setMessage("");
    setErrors({});

    try {
      let response;

      if (method === "email") {
        response = await resetPasswordByEmail(
          formData.email,
          formData.otpCode,
          formData.newPassword,
          formData.confirmPassword
        );
      } else {
        response = await resetPasswordBySms(
          formData.phoneNumber,
          formData.otpCode,
          formData.newPassword,
          formData.confirmPassword
        );
      }

      console.log("=== RESET PASSWORD SUCCESS RESPONSE ===");
      console.log(JSON.stringify(response, null, 2));

      if (isSuccessResponse(response)) {
        const successMessage =
          getResponseMessage(response) || "Đặt lại mật khẩu thành công";
        setMessage(successMessage);

        // Redirect to login after 3 seconds
        setTimeout(() => {
          window.location.href = "/login"; // or use router navigation
        }, 3000);
      } else {
        const errorMessage = getResponseMessage(response);

        // Map specific errors to fields based on backend validation
        if (errorMessage.includes("OTP") || errorMessage.includes("mã OTP")) {
          setErrors({ otpCode: errorMessage });
        } else if (
          errorMessage.includes("mật khẩu mới") ||
          errorMessage.includes("NewPassword")
        ) {
          setErrors({ newPassword: errorMessage });
        } else if (
          errorMessage.includes("xác nhận") ||
          errorMessage.includes("trùng khớp") ||
          errorMessage.includes("ConfirmPassword")
        ) {
          setErrors({ confirmPassword: errorMessage });
        } else {
          setMessage(errorMessage);
        }
      }
    } catch (error) {
      console.error("=== RESET PASSWORD ERROR ===");
      console.error("Error:", error);

      const errorMessage = getErrorMessage(error);

      // Handle specific HTTP status codes
      if (error.response) {
        const status = error.response.status;

        if (status === 400) {
          // Bad request - likely validation error
          if (errorMessage.includes("OTP") || errorMessage.includes("mã OTP")) {
            setErrors({ otpCode: errorMessage });
          } else if (errorMessage.includes("mật khẩu")) {
            if (
              errorMessage.includes("trùng khớp") ||
              errorMessage.includes("xác nhận")
            ) {
              setErrors({ confirmPassword: errorMessage });
            } else {
              setErrors({ newPassword: errorMessage });
            }
          } else {
            setMessage(errorMessage);
          }
        } else if (status === 404) {
          // Not found - user or OTP not found
          setMessage(errorMessage);
        } else {
          // Other errors
          setMessage(errorMessage);
        }
      } else {
        setMessage(errorMessage);
      }
    } finally {
      setLoading(false);
    }
  };

  // Back to previous step
  const handleBack = () => {
    if (step > 1) {
      setStep(step - 1);
      setMessage("");
      setErrors({});
    }
  };

  // Render method selection
  const renderMethodSelection = () => (
    <div className="forgot-password__method-selection">
      <h2 className="forgot-password__title">
        <i className="fas fa-key"></i>
        Quên mật khẩu
      </h2>
      <p className="forgot-password__subtitle">
        Chọn phương thức để khôi phục mật khẩu của bạn
      </p>

      <div className="forgot-password__methods">
        <div
          className="forgot-password__method-card"
          onClick={() => handleMethodSelect("email")}
        >
          <div className="method-icon">
            <i className="fas fa-envelope"></i>
          </div>
          <h3>Qua Email</h3>
          <p>Nhận mã OTP qua địa chỉ email đã đăng ký</p>
          <div className="method-arrow">
            <i className="fas fa-arrow-right"></i>
          </div>
        </div>

        <div
          className="forgot-password__method-card"
          onClick={() => handleMethodSelect("sms")}
        >
          <div className="method-icon">
            <i className="fas fa-sms"></i>
          </div>
          <h3>Qua SMS</h3>
          <p>Nhận mã OTP qua số điện thoại đã đăng ký</p>
          <div className="method-arrow">
            <i className="fas fa-arrow-right"></i>
          </div>
        </div>
      </div>
    </div>
  );

  // Render email/phone input
  const renderContactInput = () => (
    <div className="forgot-password__contact-input">
      <button className="forgot-password__back-btn" onClick={handleBack}>
        <i className="fas fa-arrow-left"></i>
        Quay lại
      </button>

      <h2 className="forgot-password__title">
        <i className={`fas fa-${method === "email" ? "envelope" : "sms"}`}></i>
        {method === "email" ? "Nhập Email" : "Nhập Số Điện Thoại"}
      </h2>

      <p className="forgot-password__subtitle">
        {method === "email"
          ? "Nhập địa chỉ email để nhận mã OTP"
          : "Nhập số điện thoại để nhận mã OTP"}
      </p>

      <form className="forgot-password__form" onSubmit={handleSendOtp}>
        <div className="forgot-password__form-group">
          <label>
            <i
              className={`fas fa-${method === "email" ? "envelope" : "phone"}`}
            ></i>
            {method === "email" ? "Email *" : "Số điện thoại *"}
          </label>
          <input
            type={method === "email" ? "email" : "tel"}
            name={method === "email" ? "email" : "phoneNumber"}
            value={method === "email" ? formData.email : formData.phoneNumber}
            onChange={handleInputChange}
            className={
              errors[method === "email" ? "email" : "phoneNumber"]
                ? "error"
                : ""
            }
            placeholder={
              method === "email"
                ? "Nhập địa chỉ email"
                : "Nhập số điện thoại (VD: 0987654321)"
            }
          />
          {errors[method === "email" ? "email" : "phoneNumber"] && (
            <span className="error-text">
              {errors[method === "email" ? "email" : "phoneNumber"]}
            </span>
          )}
        </div>

        <button
          type="submit"
          className="forgot-password__submit-btn"
          disabled={loading}
        >
          {loading && <span className="loading-spinner"></span>}
          {loading ? "Đang gửi..." : "Gửi mã OTP"}
        </button>
      </form>
    </div>
  );

  // Render OTP verification and password reset
  const renderPasswordReset = () => (
    <div className="forgot-password__password-reset">
      <button className="forgot-password__back-btn" onClick={handleBack}>
        <i className="fas fa-arrow-left"></i>
        Quay lại
      </button>

      <h2 className="forgot-password__title">
        <i className="fas fa-shield-alt"></i>
        Đặt lại mật khẩu
      </h2>

      <p className="forgot-password__subtitle">
        Nhập mã OTP và mật khẩu mới của bạn
      </p>

      <div className="forgot-password__contact-info">
        <i className={`fas fa-${method === "email" ? "envelope" : "sms"}`}></i>
        <span>
          {method === "email" ? formData.email : formData.phoneNumber}
        </span>
      </div>

      <form className="forgot-password__form" onSubmit={handleResetPassword}>
        <div className="forgot-password__form-group">
          <label>
            <i className="fas fa-key"></i>
            Mã OTP *
          </label>
          <input
            type="text"
            name="otpCode"
            value={formData.otpCode}
            onChange={handleInputChange}
            className={errors.otpCode ? "error" : ""}
            placeholder="Nhập mã OTP 6 số"
            maxLength="6"
          />
          {errors.otpCode && (
            <span className="error-text">{errors.otpCode}</span>
          )}

          <div className="forgot-password__resend">
            {countdown > 0 ? (
              <span className="countdown">Gửi lại sau {countdown}s</span>
            ) : (
              <button
                type="button"
                className="resend-btn"
                onClick={handleResendOtp}
                disabled={loading}
              >
                <i className="fas fa-redo"></i>
                Gửi lại OTP
              </button>
            )}
          </div>
        </div>

        <div className="forgot-password__form-group">
          <label>
            <i className="fas fa-lock"></i>
            Mật khẩu mới *
          </label>
          <input
            type="password"
            name="newPassword"
            value={formData.newPassword}
            onChange={handleInputChange}
            className={errors.newPassword ? "error" : ""}
            placeholder="Nhập mật khẩu mới (ít nhất 8 ký tự)"
          />
          {errors.newPassword && (
            <span className="error-text">{errors.newPassword}</span>
          )}
        </div>

        <div className="forgot-password__form-group">
          <label>
            <i className="fas fa-lock"></i>
            Xác nhận mật khẩu *
          </label>
          <input
            type="password"
            name="confirmPassword"
            value={formData.confirmPassword}
            onChange={handleInputChange}
            className={errors.confirmPassword ? "error" : ""}
            placeholder="Nhập lại mật khẩu mới"
          />
          {errors.confirmPassword && (
            <span className="error-text">{errors.confirmPassword}</span>
          )}
        </div>

        <button
          type="submit"
          className="forgot-password__submit-btn"
          disabled={loading}
        >
          {loading && <span className="loading-spinner"></span>}
          {loading ? "Đang xử lý..." : "Đặt lại mật khẩu"}
        </button>
      </form>
    </div>
  );

  return (
    <div className="forgot-password">
      <div className="forgot-password__container">
        {/* Message display */}
        {message && (
          <div
            className={`forgot-password__message ${
              message.includes("thành công") ||
              message.includes("đã được gửi") ||
              message.includes("OTP đã được gửi lại")
                ? "success"
                : "error"
            }`}
          >
            <i
              className={`fas ${
                message.includes("thành công") ||
                message.includes("đã được gửi") ||
                message.includes("OTP đã được gửi lại")
                  ? "fa-check-circle"
                  : "fa-exclamation-circle"
              }`}
            ></i>
            {message}
          </div>
        )}

        {/* Steps */}
        {step === 1 && renderMethodSelection()}
        {step === 2 && renderContactInput()}
        {step === 3 && renderPasswordReset()}

        {/* Login link */}
        <div className="forgot-password__login-link">
          <p>
            Nhớ mật khẩu rồi?
            <a href="/login"> Đăng nhập ngay</a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default ForgotPassword;
