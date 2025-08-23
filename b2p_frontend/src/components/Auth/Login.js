import React, { useState, useEffect } from 'react';
import { GoogleLogin } from '@react-oauth/google';
import { jwtDecode } from 'jwt-decode';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import { useAuth } from '../../contexts/AuthContext';
import {
  googleLoginAxios,
  sendOtpAxios,
  verifyOtpAxios,
  loginAxios,
  checkUserExistAxios
} from '../../services/apiService';
import './Login.scss';

const Login = () => {
  const navigate = useNavigate();
  const { login } = useAuth();

  // 🎯 State management
  const [currentStep, setCurrentStep] = useState('email-input'); // 'email-input' | 'password-input' | 'otp' | 'google-otp'
  const [isLoading, setIsLoading] = useState(false);
  const [countdown, setCountdown] = useState(0);
  const [userCheckResult, setUserCheckResult] = useState(null); // Store check result

  // ✅ State riêng cho email input và password
  const [emailInput, setEmailInput] = useState('');
  const [passwordInput, setPasswordInput] = useState('');

  // 🎯 Regular OTP state
  const [otpData, setOtpData] = useState({
    phoneOrEmail: '',
    password: '',
    otpCode: '',
    sessionToken: '',
    maskedContact: '',
    isOtpSent: false,
    isVerifying: false,
    isEmail: false
  });

  // ⏰ Countdown timer
  useEffect(() => {
    let timer;
    if (countdown > 0) {
      timer = setTimeout(() => setCountdown(countdown - 1), 1000);
    }
    return () => clearTimeout(timer);
  }, [countdown]);

  // ✅ HÀM CHECK USER EXIST (Bước 1)
  const handleCheckUser = async () => {
    try {
      setIsLoading(true);

      const response = await checkUserExistAxios({
        PhoneOrEmail: emailInput.trim()
      });

      const backendResponse = response;
      console.log('📥 Backend response:', backendResponse);

      // ✅ CASE 1: SUCCESS - User exists và có password → Yêu cầu nhập password
      if (backendResponse.success && backendResponse.data?.isExist && backendResponse.data?.hasPassword) {
        console.log('🎉 User can login with password');
        setUserCheckResult(backendResponse.data);
        setCurrentStep('password-input');
        message.success('Vui lòng nhập mật khẩu để đăng nhập');
        return;
      }

      // ✅ CASE 2: SUCCESS - OTP đã được gửi (user không tồn tại hoặc không có password)
      if (backendResponse.success && backendResponse.data?.sessionToken) {
        console.log('📱 OTP sent successfully');

        // ✅ FIX: Set đúng state cho OTP step
        setOtpData({
          phoneOrEmail: emailInput.trim(),
          password: '', // Empty vì đây là login flow
          otpCode: '',
          sessionToken: backendResponse.data.sessionToken,
          maskedContact: backendResponse.data.maskedContact,
          expiresAt: backendResponse.data.expiresAt,
          isOtpSent: true,
          isVerifying: false,
          isEmail: backendResponse.data.maskedContact?.includes('@') || false
        });

        // ✅ FIX: Đổi step thành 'otp' để match với render condition
        setCurrentStep('otp');
        setCountdown(60); // 5 phút countdown
        message.success(backendResponse.message || 'OTP đã được gửi');
        return;
      }

      // ✅ CASE 3: ERROR responses
      if (backendResponse.success === false) {
        console.log('❌ Backend returned error:', backendResponse);

        switch (backendResponse.status) {
          case 403:
            message.error(`🔒 ${backendResponse.message || 'Tài khoản đã bị khóa hoặc vô hiệu hóa'}`);
            break;
          case 400:
            message.error(`⚠️ ${backendResponse.message || 'Định dạng email/số điện thoại không hợp lệ'}`);
            break;
          case 429:
            message.error(`⏰ ${backendResponse.message || 'Vui lòng đợi trước khi yêu cầu OTP khác'}`);
            break;
          case 500:
            message.error('🔧 Lỗi hệ thống, vui lòng thử lại sau');
            break;
          default:
            message.error(`❌ ${backendResponse.message || 'Có lỗi xảy ra'}`);
        }
        return;
      }

      // ✅ CASE 4: Fallback
      console.log('❌ Unexpected response structure:', backendResponse);
      message.error('Phản hồi từ server không đúng định dạng');

    } catch (error) {
      console.error('❌ Check user error:', error);

      if (error.response?.data) {
        const errorData = error.response.data;

        switch (error.response.status) {
          case 403:
            message.error(`🔒 ${errorData.message || 'Tài khoản đã bị khóa hoặc vô hiệu hóa'}`);
            break;
          case 400:
            message.error(`⚠️ ${errorData.message || 'Thông tin không hợp lệ'}`);
            break;
          case 429:
            message.error(`⏰ ${errorData.message || 'Vui lòng đợi trước khi thử lại'}`);
            break;
          case 500:
            message.error('🔧 Lỗi server, vui lòng thử lại');
            break;
          default:
            message.error(`❌ ${errorData.message || 'Lỗi kết nối server'}`);
        }
      } else {
        message.error('🌐 Không thể kết nối đến server');
      }
    } finally {
      setIsLoading(false);
    }
  };
  // ✅ HÀM LOGIN VỚI PASSWORD (Bước 2)
  const handleLogin = async () => {
    if (!emailInput.trim() || !passwordInput.trim()) {
      message.error('Vui lòng nhập đầy đủ thông tin');
      return;
    }

    try {
      setIsLoading(true);

      const response = await loginAxios({
        phoneOrEmail: emailInput.trim(),
        password: passwordInput.trim()
      });

      console.log('📥 Login response:', response);

      // ✅ CASE 1: SUCCESS - Login thành công
      if (response.success && response.data?.accessToken && response.data?.user) {
        login(response.data.user, response.data.accessToken, response.data.refreshToken);
        message.success(`🎉 Chào mừng ${response.data.user.fullName || response.data.user.email}!`);
        handleNavigateAfterLogin(response.data.user);
        return;
      }

      // ✅ CASE 2: ERROR response với success = false
      if (response.success === false) {
        console.log('❌ Login failed:', response);

        switch (response.status) {
          case 403:
            // 🚨 TÀI KHOẢN BỊ KHÓA
            message.error(`🔒 ${response.message || 'Tài khoản đã bị khóa hoặc vô hiệu hóa'}`);
            break;
          case 401:
            message.error('🔐 Email/số điện thoại hoặc mật khẩu không đúng');
            break;
          case 400:
            message.error(`⚠️ ${response.message || 'Thông tin đăng nhập không hợp lệ'}`);
            break;
          case 500:
            message.error('🔧 Lỗi hệ thống, vui lòng thử lại');
            break;
          default:
            message.error(`❌ ${response.message || 'Đăng nhập thất bại'}`);
        }
        return;
      }

      // ✅ CASE 3: Success = true nhưng thiếu data
      message.error('🔴 Phản hồi từ server không đầy đủ');

    } catch (error) {
      console.error('❌ Login error:', error);

      // ✅ XỬ LÝ ERROR RESPONSE TỪ SERVER
      if (error.response?.data) {
        const errorData = error.response.data;
        console.log('📥 Error response data:', errorData);

        // Xử lý theo HTTP status code
        switch (error.response.status) {
          case 403:
            // 🚨 TÀI KHOẢN BỊ KHÓA - QUAN TRỌNG!
            message.error(`🔒 ${errorData.message || 'Tài khoản đã bị khóa hoặc vô hiệu hóa'}`);
            break;
          case 401:
            message.error('🔐 Email/số điện thoại hoặc mật khẩu không đúng');
            break;
          case 400:
            message.error(`⚠️ ${errorData.message || 'Thông tin đăng nhập không hợp lệ'}`);
            break;
          case 500:
            message.error('🔧 Lỗi server, vui lòng thử lại');
            break;
          default:
            message.error(`❌ Lỗi: ${errorData.message || error.response.status}`);
        }
      }
      // ✅ XỬ LÝ LỖI NETWORK/KHÁC
      else {
        message.error(`🌐 ${error.message || 'Không thể kết nối đến server'}`);
      }
    } finally {
      setIsLoading(false);
    }
  };

  // 📱 Handle regular OTP Send - FIXED
  const handleSendOtp = async () => {
    if (!otpData.phoneOrEmail.trim()) {
      message.error('Vui lòng nhập số điện thoại hoặc email');
      return;
    }

    try {
      setIsLoading(true);

      const response = await sendOtpAxios({
        phoneOrEmail: otpData.phoneOrEmail.trim()
      });

      console.log('📥 SendOTP response:', response.data);

      // ✅ Handle multiple response formats
      let sessionToken = null;
      let maskedContact = null;
      let otpMessage = null;

      // Case 1: Wrapped format
      if (response.data && response.data.success && response.data.data) {
        sessionToken = response.data.data.sessionToken;
        maskedContact = response.data.data.maskedContact;
        otpMessage = response.data.data.message;
      }
      // Case 2: Direct format (like Google login)
      else if (response.data && response.data.sessionToken) {
        sessionToken = response.data.sessionToken;
        maskedContact = response.data.maskedContact;
        otpMessage = response.data.message;
      }

      if (sessionToken && maskedContact) {
        const isEmail = maskedContact.includes('@');

        setOtpData(prev => ({
          ...prev,
          isOtpSent: true,
          sessionToken: sessionToken,
          maskedContact: maskedContact,
          isEmail: isEmail
        }));

        setCurrentStep('otp');
        setCountdown(60);

        message.success(`📱 ${otpMessage || 'OTP đã được gửi!'}`);
      } else {
        message.error(response.data?.message || 'Gửi OTP thất bại');
      }
    } catch (error) {
      console.error('Send OTP error:', error);
      message.error(`Lỗi gửi OTP: ${error.response?.data?.message || error.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  // ✅ UNIFIED OTP Verification - Cho cả Regular và Google
  const handleVerifyOtp = async () => {
    if (!otpData.otpCode.trim()) {
      message.error('Vui lòng nhập mã OTP');
      return;
    }

    try {
      setOtpData(prev => ({ ...prev, isVerifying: true }));

      const requestData = {
        phoneOrEmail: otpData.phoneOrEmail,
        otp: otpData.otpCode,
        sessionToken: otpData.sessionToken
      };

      console.log("📡 Verifying OTP:", requestData);

      const response = await verifyOtpAxios(requestData);
      console.log("📥 OTP verification response:", response);

      // ✅ BE định nghĩa rõ: Success, Status, Message, Data
      if (response.success && response.data?.accessToken) {
        const { accessToken, refreshToken, user, isNewUser } = response.data;

        login(user, accessToken, refreshToken);

        const welcomeMsg = isNewUser
          ? `🎉 Tài khoản mới tạo thành công! Chào mừng ${user.fullName || user.email}!`
          : `🎉 Chào mừng ${user.fullName || user.email} quay lại!`;

        message.success(welcomeMsg);

        console.log("✅ Login successful:", {
          userId: user.userId,
          email: user.email,
          roleId: user.roleId,
          roleName: user.roleName
        });

        // Reset state
        setOtpData({
          phoneOrEmail: "",
          password: "",
          otpCode: "",
          sessionToken: "",
          maskedContact: "",
          isOtpSent: false,
          isVerifying: false,
          isEmail: false
        });

        handleNavigateAfterLogin(user);
        return;
      }

      // ❌ Trường hợp thất bại (BE trả về success=false)
      if (response.success === false) {
        console.log("❌ OTP verification failed:", response);

        switch (response.status) {
          case 403:
            message.error(`🔒 ${response.message || "Tài khoản đã bị khóa hoặc vô hiệu hóa"}`);
            break;
          case 400:
            if (response.message?.includes("OTP")) {
              message.error(`🔢 ${response.message}`);
            } else {
              message.error(`⚠️ ${response.message || "Thông tin không hợp lệ"}`);
            }
            break;
          case 500:
            message.error("🔧 Lỗi hệ thống trong quá trình xác thực");
            break;
          default:
            message.error(`❌ ${response.message || "Xác thực OTP thất bại"}`);
        }
        return;
      }

      // Fallback
      throw new Error(response.message || "Missing accessToken or user data in response");

    } catch (error) {
      console.error("❌ OTP verification error:", error);

      if (error.response?.data) {
        const errorData = error.response.data;
        switch (error.response.status) {
          case 403:
            message.error(`🔒 ${errorData.message || "Tài khoản đã bị khóa hoặc vô hiệu hóa"}`);
            break;
          case 400:
            if (errorData.message?.includes("OTP")) {
              message.error(`🔢 ${errorData.message}`);
            } else {
              message.error(`⚠️ ${errorData.message || "Thông tin không hợp lệ"}`);
            }
            break;
          case 401:
            message.error(`🔐 ${errorData.message || "Phiên đăng nhập đã hết hạn"}`);
            break;
          case 500:
            message.error("🔧 Lỗi hệ thống, vui lòng thử lại");
            break;
          default:
            message.error(`❌ ${errorData.message || "Xác thực thất bại"}`);
        }
      } else {
        message.error(`🌐 ${error.message || "Lỗi kết nối server"}`);
      }
    } finally {
      setOtpData(prev => ({ ...prev, isVerifying: false }));
    }
  };


  const handleGoogleSuccess = async (credentialResponse) => {
    if (!credentialResponse?.credential) {
      message.error('Không nhận được token từ Google');
      return;
    }

    try {
      setIsLoading(true);

      const userInfo = jwtDecode(credentialResponse.credential);
      console.log('👤 Google User info:', {
        email: userInfo.email,
        name: userInfo.name,
        picture: userInfo.picture
      });

      const response = await googleLoginAxios(credentialResponse.credential);
      console.log('🎯 Google login full response:', response);

      // ✅ CASE 1: SUCCESS - Login thành công với access token
      if (response.success && response.data?.accessToken && response.data?.user) {
        console.log('✅ Google login successful');

        // Login user
        login(response.data.user, response.data.accessToken, response.data.refreshToken);

        // Show welcome message
        const welcomeMsg = response.data.isNewUser
          ? `🎉 Tài khoản Google mới tạo thành công! Chào mừng ${response.data.user.fullName || response.data.user.email}!`
          : `🎉 Chào mừng ${response.data.user.fullName || response.data.user.email} quay lại!`;

        message.success(welcomeMsg);
        handleNavigateAfterLogin(response.data.user);
        return;
      }

      // ✅ CASE 2: ERROR response
      if (response.success === false) {
        console.error('❌ Backend returned error:', response);

        switch (response.status) {
          case 403:
            // 🚨 TÀI KHOẢN BỊ KHÓA
            message.error(`🔒 ${response.message || 'Tài khoản Google đã bị khóa hoặc vô hiệu hóa'}`);
            break;
          case 401:
            message.error('🔐 Token Google không hợp lệ hoặc đã hết hạn');
            break;
          case 500:
            message.error('🔧 Lỗi hệ thống Google login');
            break;
          default:
            message.error(`❌ ${response.message || 'Google login thất bại'}`);
        }
        return;
      }

      // ✅ CASE 3: UNEXPECTED RESPONSE FORMAT
      console.error('❌ Unexpected response format:', response);
      message.error('Phản hồi từ server không đúng định dạng');

    } catch (error) {
      console.error('❌ Google login error:', error);

      // ✅ XỬ LÝ ERROR RESPONSE
      if (error.response?.data) {
        const errorData = error.response.data;

        switch (error.response.status) {
          case 403:
            // 🚨 TÀI KHOẢN BỊ KHÓA
            message.error(`🔒 ${errorData.message || 'Tài khoản Google đã bị khóa hoặc vô hiệu hóa'}`);
            break;
          case 401:
            message.error('🔐 Token Google không hợp lệ hoặc đã hết hạn');
            break;
          case 500:
            message.error('🔧 Lỗi server Google login');
            break;
          default:
            message.error(`❌ ${errorData.message || 'Lỗi Google Login'}`);
        }
      } else {
        // Handle JWT/Google specific errors
        if (error.message?.includes('Invalid token') || error.message?.includes('JWT') || error.message?.includes('Google')) {
          message.error('🔐 Token Google không hợp lệ hoặc đã hết hạn');
        } else {
          message.error(`🌐 ${error.message || 'Không thể kết nối Google'}`);
        }
      }
    } finally {
      setIsLoading(false);
    }
  };
  const handleGoogleError = (error) => {
    console.error('❌ Google OAuth error:', error);
    message.error('🔴 Không thể kết nối tới Google');
  };

  // 🎯 Navigate based on user role
  const handleNavigateAfterLogin = (user) => {
    console.log('🎯 Navigating user:', {
      userId: user.userId,
      roleId: user.roleId,
      roleName: user.roleName
    });

    setTimeout(() => {
      // ✅ Check roleId và navigate đúng path
      if (user.roleId === 1) { // Admin
        console.log('🔄 Navigating to Admin dashboard');
        navigate('/admin');
      } else if (user.roleId === 3) { // CourtOwner  
        console.log('🔄 Navigating to CourtOwner dashboard');
        navigate('/court-owner');
      } else { // Player or default
        console.log('🔄 Navigating to Player dashboard');
        navigate('/');
      }
    }, 1000);
  };

  // 🔄 Reset functions
  const handleBackToEmailInput = () => {
    setCurrentStep('email-input');
    setUserCheckResult(null);
    setPasswordInput('');
  };

  const handleResendOtp = () => {
    setCurrentStep('email-input');
    setOtpData({
      phoneOrEmail: otpData.phoneOrEmail,
      password: '',
      otpCode: '',
      sessionToken: '',
      maskedContact: '',
      isOtpSent: false,
      isVerifying: false,
      isEmail: false
    });
    setCountdown(0);
  };

 

  const handleBackToLogin = () => {
  setCurrentStep('email-input');
  setUserCheckResult(null);
  setOtpData({
    phoneOrEmail: '',
    password: '',
    otpCode: '',
    sessionToken: '',
    maskedContact: '',
    isOtpSent: false,
    isVerifying: false,
    isEmail: false
  });
  setCountdown(0);
};


  // ✅ RENDER EMAIL INPUT STEP (Bước 1)
  const renderEmailInputStep = () => (
    <div className="login-step">
      {/* Sports Hero Section */}
      <div className="sports-hero">
        <div className="floating-sports">
          <div className="sport-icon basketball">🏀</div>
          <div className="sport-icon football">⚽</div>
          <div className="sport-icon tennis">🎾</div>
          <div className="sport-icon volleyball">🏐</div>
        </div>
        <h1 className="hero-title">
          <span className="gradient-text">B2P SPORT</span>
          <div className="title-underline"></div>
        </h1>
        <p className="hero-subtitle">Kết nối đam mê thể thao của bạn</p>
      </div>

      {/* Email Input Section */}
      <div className="login-methods">
        <div className="input-section">
          {/* ✅ CHỈ HIỆN Ô EMAIL/PHONE */}
          <div className="input-wrapper">
            <div className="input-icon">📧</div>
            <input
              type="text"
              placeholder="Email hoặc số điện thoại"
              value={emailInput}
              onChange={(e) => setEmailInput(e.target.value)}
              className="sport-input"
              disabled={isLoading}
              onKeyPress={(e) => e.key === 'Enter' && handleCheckUser()}
            />
          </div>

          {/* ✅ NÚT TIẾP TỤC */}
          <button
            className="sport-btn primary pulse"
            onClick={handleCheckUser}
            disabled={isLoading || !emailInput.trim()}
          >
            {isLoading ? (
              <><span className="spinner"></span> Đang kiểm tra...</>
            ) : (
              <>🔍 Tiếp tục</>
            )}
          </button>
        </div>

        {/* Divider */}
        <div className="divider-sports">
          <div className="divider-line"></div>
          <span className="divider-text">HOẶC</span>
          <div className="divider-line"></div>

        </div>
        <div className="forgot-password-container">
          <a href="/forgot-password" className="forgot-link">Quên mật khẩu?</a>
        </div>

        {/* Google Login */}
        <div className="google-section">
          <div className="google-wrapper">
            <GoogleLogin
              onSuccess={handleGoogleSuccess}
              onError={handleGoogleError}
              theme="outline"
              size="large"
              text="signin_with"
              shape="rectangular"
              width="100%"
              disabled={isLoading}
            />
          </div>

          {isLoading && (
            <div className="loading-indicator">
              <div className="spinner"></div>
              <span>Đang xử lý đăng nhập Google...</span>
            </div>
          )}
        </div>
      </div>
    </div>
  );

  // ✅ RENDER PASSWORD INPUT STEP (Bước 2)
  // ✅ RENDER PASSWORD INPUT STEP (Bước 2) - FIXED
  const renderPasswordInputStep = () => (
    <div className="login-step">
      {/* Header cho password step */}
      <div className="password-header">
        <div className="user-info">
          <div className="user-icon">👤</div>
          <h2>Nhập mật khẩu</h2>
          <p>Đăng nhập cho tài khoản</p>
          <div className="user-email">{emailInput}</div>
        </div>
      </div>

      <div className="login-methods">
        <div className="input-section">
          {/* ✅ FIXED: Dùng passwordInput thay vì otpData.password */}
          <div className="input-wrapper">
            <div className="input-icon">🔒</div>
            <input
              type="password"
              placeholder="Nhập mật khẩu của bạn"
              value={passwordInput}
              onChange={(e) => setPasswordInput(e.target.value)}
              className="sport-input"
              disabled={isLoading}
              onKeyPress={(e) => e.key === 'Enter' && handleLogin()}
              autoFocus
            />
          </div >

          {/* ✅ FIXED: Kiểm tra passwordInput.trim() */}
          <button
            style={{ marginBottom: '20px' }}
            className="sport-btn success pulse"
            onClick={handleLogin}
            disabled={isLoading || !passwordInput.trim()}
          >
            {isLoading ? (
              <><span className="spinner"></span> Đang đăng nhập...</>
            ) : (
              <>🚀 Đăng nhập</>
            )}
          </button>

          {/* ✅ NÚT QUAY LẠI */}
          <button
            className="sport-btn secondary"
            onClick={handleBackToEmailInput}
            disabled={isLoading}
          >
            ← Quay lại
          </button>
        </div>

        {/* ✅ FIXED: Đặt forgot password trong input-section */}
        <div className="forgot-password-container">
          <a href="/forgot-password" className="forgot-link">Quên mật khẩu?</a>
        </div>
      </div>
    </div>
  );

  // 🎨 Render OTP steps (giữ nguyên)
  const renderOtpStep = () => (
    <div className="otp-step">
      <div className="otp-header">
        <div className="otp-icon">📲</div>
        <h2>Xác thực OTP</h2>
        <p>Mã xác thực đã được gửi đến</p>
        <div className="contact-display">{otpData.maskedContact}</div>
      </div>

      <div className="otp-input-section">
        <div className="otp-input-wrapper">
          <input
            type="text"
            placeholder="000000"
            value={otpData.otpCode}
            onChange={(e) => {
              const value = e.target.value.replace(/\D/g, '').slice(0, 6);
              setOtpData(prev => ({ ...prev, otpCode: value }));
            }}
            className="otp-input"
            maxLength={6}
            disabled={otpData.isVerifying}
          />
        </div>

        <div className="otp-actions">
          <button
            className="sport-btn success"
            onClick={handleVerifyOtp}
            disabled={otpData.isVerifying || otpData.otpCode.length !== 6}
          >
            {otpData.isVerifying ? (
              <><span className="spinner"></span> Đang xác thực...</>
            ) : (
              <>Xác thực</>
            )}
          </button>
        </div>

        <div className="otp-footer">
          {countdown > 0 ? (
            <p className="countdown">Gửi lại sau {countdown}s</p>
          ) : (
            <button className="resend-btn" onClick={handleResendOtp}>
              🔄 Gửi lại mã
            </button>
          )}
          <button className="back-btn" onClick={handleBackToLogin}>
            ← Quay lại
          </button>
        </div>
      </div>
    </div>
  );
  // ✅ MAIN RENDER
  return (
    <div className="sports-login-container">
      <div className="sports-background">
        <div className="bg-shape shape1"></div>
        <div className="bg-shape shape2"></div>
        <div className="bg-shape shape3"></div>
      </div>

      <div className="login-card-sports">
        {/* ✅ RENDER THEO STEP */}
        {currentStep === 'email-input' && renderEmailInputStep()}
        {currentStep === 'password-input' && renderPasswordInputStep()}
        {currentStep === 'otp' && renderOtpStep()}
      </div>

      {/* Sports Footer */}
      <div className="sports-footer">
        <p>🏆 Tham gia cộng đồng thể thao lớn nhất Việt Nam</p>
      </div>
    </div>
  );
};

export default Login;