import React, { useState, useEffect } from 'react';
import { GoogleLogin } from '@react-oauth/google';
import { jwtDecode } from 'jwt-decode';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import { useAuth } from '../../context/AuthContext';
import {
  googleLoginAxios,
  sendOtpAxios,
  verifyOtpAxios,
  loginAxios
} from '../../services/apiService';
import './Login.scss';

const Login = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  // 🎯 State management
  const [currentStep, setCurrentStep] = useState('login'); // 'login' | 'otp' | 'google-otp'
  const [isLoading, setIsLoading] = useState(false);
  const [countdown, setCountdown] = useState(0);

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

  // 🎯 Google OTP state  
  const [googleOtpData, setGoogleOtpData] = useState({
    email: '',
    userName: '',
    sessionToken: '',
    otpCode: '',
    isNewUser: false,
    isVerifying: false
  });

  // ⏰ Countdown timer
  useEffect(() => {
    let timer;
    if (countdown > 0) {
      timer = setTimeout(() => setCountdown(countdown - 1), 1000);
    }
    return () => clearTimeout(timer);
  }, [countdown]);
  const handleLogin = async () => {
    if (!otpData.phoneOrEmail.trim() || !otpData.password?.trim()) {
      message.error('Vui lòng nhập đầy đủ email/số điện thoại và mật khẩu');
      return;
    }

    try {
      setIsLoading(true);

      // ✅ Call login API với username/password
      const response = await loginAxios({ // ← Cần tạo function này
        phoneOrEmail: otpData.phoneOrEmail.trim(),
        password: otpData.password.trim()
      });

      console.log('📥 Login response:', response.data);

      // ✅ Handle response (tương tự Google login)
      if (response.data?.accessToken && response.data?.user) {
        login(response.data.user, response.data.accessToken, response.data.refreshToken);
        message.success(`🎉 Chào mừng ${response.data.user.fullName || response.data.user.email}!`);
        handleNavigateAfterLogin(response.data.user);
      } else {
        throw new Error('Response không hợp lệ');
      }

    } catch (error) {
      console.error('❌ Login error:', error);
      if (error.response?.status === 401) {
        message.error('🔴 Sai email/số điện thoại hoặc mật khẩu');
      } else {
        message.error(`🔴 ${error.response?.data?.message || error.message}`);
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
    // 🎯 Determine current step data
    const isGoogleFlow = currentStep === 'google-otp';
    const currentData = isGoogleFlow ? googleOtpData : otpData;
    const setCurrentData = isGoogleFlow ? setGoogleOtpData : setOtpData;

    if (!currentData.otpCode.trim()) {
      message.error('Vui lòng nhập mã OTP');
      return;
    }

    try {
      setCurrentData(prev => ({ ...prev, isVerifying: true }));

      // ✅ UNIFIED request - Backend handles all cases
      const requestData = {
        phoneOrEmail: isGoogleFlow ? currentData.email : currentData.phoneOrEmail,
        otp: currentData.otpCode,
        sessionToken: currentData.sessionToken
      };

      console.log(`📡 Verifying OTP (${isGoogleFlow ? 'Google' : 'Regular'}):`, requestData);

      const response = await verifyOtpAxios(requestData);
      console.log('📥 OTP verification response:', response.data);

      // ✅ Handle multiple response formats
      let accessToken = null;
      let refreshToken = null;
      let user = null;
      let isNewUser = false;

      // Case 1: Wrapped format
      if (response.data && response.data.success && response.data.data?.accessToken) {
        accessToken = response.data.data.accessToken;
        refreshToken = response.data.data.refreshToken;
        user = response.data.data.user;
        isNewUser = response.data.data.isNewUser || false;
      }
      // Case 2: Direct format (current backend format)
      else if (response.data && response.data.accessToken) {
        accessToken = response.data.accessToken;
        refreshToken = response.data.refreshToken;
        user = response.data.user;
        isNewUser = response.data.isNewUser || false;
      }
      // Case 3: Error response
      else if (response.data && response.data.success === false) {
        throw new Error(response.data.message || 'OTP verification failed');
      }

      console.log('🔍 Extracted verification data:', {
        hasAccessToken: !!accessToken,
        hasUser: !!user,
        userId: user?.userId,
        userEmail: user?.email,
        roleId: user?.roleId,
        isNewUser,
        flowType: isGoogleFlow ? 'Google' : 'Regular'
      });

      // ✅ SUCCESS: Process login
      if (accessToken && user) {
        login(user, accessToken, refreshToken);
        const flowText = isGoogleFlow ? 'Google ' : '';
        const welcomeMsg = isNewUser
          ? `🎉 Tài khoản ${flowText}mới tạo thành công! Chào mừng ${user.fullName || user.email}!`
          : `🎉 Chào mừng ${user.fullName || user.email} quay lại!`;

        message.success(welcomeMsg);

        console.log(`✅ ${isGoogleFlow ? 'Google' : 'Regular'} login successful:`, {
          userId: user.userId,
          email: user.email,
          roleId: user.roleId,
          roleName: user.roleName
        });

        // Reset all states
        setOtpData({
          phoneOrEmail: '', otpCode: '', sessionToken: '', maskedContact: '',
          isOtpSent: false, isVerifying: false, isEmail: false
        });
        setGoogleOtpData({
          email: '', userName: '', sessionToken: '', otpCode: '',
          isNewUser: false, isVerifying: false
        });

        handleNavigateAfterLogin(user);

      } else {
        const errorMsg = response.data?.message || 'Missing accessToken or user data in response';
        throw new Error(errorMsg);
      }

    } catch (error) {
      console.error(`❌ ${isGoogleFlow ? 'Google' : 'Regular'} OTP verification error:`, error);
      message.error(`🔴 ${error.response?.data?.message || error.message}`);
    } finally {
      setCurrentData(prev => ({ ...prev, isVerifying: false }));
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

      // ✅ CALL GOOGLE LOGIN API
      const response = await googleLoginAxios(credentialResponse.credential);
      console.log('🎯 Google login full response:', response.data);

      // 🔍 LOG RESPONSE STRUCTURE
      console.log('🔍 Response structure:', {
        hasSuccess: 'success' in response.data,
        hasData: 'data' in response.data,
        hasAccessToken: 'accessToken' in response.data,
        hasSessionToken: 'sessionToken' in response.data,
        hasUser: 'user' in response.data,
        topLevelKeys: Object.keys(response.data)
      });

      // ✅ CASE 1: DIRECT FORMAT WITH ACCESS TOKEN (EXISTING USER)
      if (response.data?.accessToken && response.data?.user) {
        console.log('✅ Case 1: Existing user - Direct login with access token');
        console.log('User data:', {
          userId: response.data.user.userId,
          email: response.data.user.email,
          fullName: response.data.user.fullName,
          roleId: response.data.user.roleId
        });

        // Login existing user directly
        login(response.data.user, response.data.accessToken, response.data.refreshToken);

        const welcomeMsg = response.data.isNewUser
          ? `🎉 Tài khoản Google mới tạo thành công! Chào mừng ${response.data.user.fullName || response.data.user.email}!`
          : `🎉 Chào mừng ${response.data.user.fullName || response.data.user.email} quay lại!`;

        message.success(welcomeMsg);
        handleNavigateAfterLogin(response.data.user);
        return;
      }

      // ✅ CASE 2: WRAPPED FORMAT (success/data structure)
      if (response.data?.success === true && response.data?.data) {
        const responseData = response.data.data;

        // Case 2a: Existing user in wrapped format
        if (responseData.accessToken && responseData.user) {
          console.log('✅ Case 2a: Existing user - Wrapped format login');

          login(responseData.user, responseData.accessToken, responseData.refreshToken);
          message.success(`🎉 Chào mừng ${responseData.user.fullName || responseData.user.email} quay lại!`);
          handleNavigateAfterLogin(responseData.user);
          return;
        }

        // Case 2b: New user needs OTP
        if (responseData.sessionToken) {
          console.log('🆕 Case 2b: New user - Need OTP verification');

          const otpMessage = responseData.message || response.data.message || `Mã OTP đã được gửi đến ${userInfo.email}!`;
          message.info(`📧 ${otpMessage}`);

          setGoogleOtpData({
            email: userInfo.email,
            userName: userInfo.name || userInfo.email,
            sessionToken: responseData.sessionToken,
            otpCode: '',
            isNewUser: true,
            isVerifying: false
          });

          setCurrentStep('google-otp');
          setCountdown(60);
          return;
        }
      }

      // ✅ CASE 3: DIRECT FORMAT WITH SESSION TOKEN (NEW USER)
      if (response.data?.sessionToken) {
        console.log('🆕 Case 3: New user - Direct format need OTP');

        const otpMessage = response.data.message || `Mã OTP đã được gửi đến ${userInfo.email}!`;
        message.info(`📧 ${otpMessage}`);

        setGoogleOtpData({
          email: userInfo.email,
          userName: userInfo.name || userInfo.email,
          sessionToken: response.data.sessionToken,
          otpCode: '',
          isNewUser: true,
          isVerifying: false
        });

        setCurrentStep('google-otp');
        setCountdown(60);
        return;
      }

      // ✅ HANDLE ERROR RESPONSE
      if (response.data?.success === false) {
        console.error('❌ Backend returned error:', response.data.message);
        throw new Error(response.data.message || 'Google login failed');
      }

      // ✅ NO VALID CASE MATCHED
      console.error('❌ No valid response format matched:', response.data);
      console.error('Available keys:', Object.keys(response.data));
      throw new Error('Response không chứa accessToken hoặc sessionToken hợp lệ');

    } catch (error) {
      console.error('❌ Google login error:', error);
      console.error('❌ Error details:', {
        message: error.message,
        responseData: error.response?.data,
        status: error.response?.status
      });

      if (error.response?.status === 429) {
        message.error('🔴 Vui lòng đợi trước khi thử lại Google login');
      } else if (error.response?.data?.message) {
        message.error(`🔴 ${error.response.data.message}`);
      } else {
        message.error(`🔴 Lỗi Google Login: ${error.message}`);
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
  // 🎯 Navigate based on user role - FIXED
  const handleNavigateAfterLogin = (user) => {
    console.log('🎯 Navigating user:', {
      userId: user.userId,
      roleId: user.roleId,
      roleName: user.roleName
    });

    setTimeout(() => {
      // ✅ Check roleId và navigate đúng path
      if (user.roleId === 1) { // Admin (theo log trước đó bạn có roleId = 1)
        console.log('🔄 Navigating to Admin dashboard');
        navigate('/admin');
      } else if (user.roleId === 3) { // CourtOwner  
        console.log('🔄 Navigating to CourtOwner dashboard');
        navigate('/court-owner');
      } else { // Player or default (roleId = 3 hoặc khác)
        console.log('🔄 Navigating to Player dashboard');
        navigate('/'); // ✅ Fixed: có path cụ thể
      }
    }, 1000);
  };

  // 🔄 Reset functions
  const handleResendOtp = () => {
    setCurrentStep('login');
    setOtpData({
      phoneOrEmail: otpData.phoneOrEmail,
      otpCode: '',
      sessionToken: '',
      maskedContact: '',
      isOtpSent: false,
      isVerifying: false,
      isEmail: false
    });
    setCountdown(0);
  };

  const handleResendGoogleOtp = async () => {
    try {
      setIsLoading(true);
      message.info('Vui lòng thử đăng nhập Google lại để nhận OTP mới');
      setCurrentStep('login');
      setGoogleOtpData({
        email: '', userName: '', sessionToken: '', otpCode: '', isNewUser: false, isVerifying: false
      });
    } catch (error) {
      message.error('Không thể gửi lại OTP');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBackToLogin = () => {
    setCurrentStep('login');
    setOtpData({
      phoneOrEmail: '', otpCode: '', sessionToken: '', maskedContact: '',
      isOtpSent: false, isVerifying: false, isEmail: false
    });
    setGoogleOtpData({
      email: '', userName: '', sessionToken: '', otpCode: '', isNewUser: false, isVerifying: false
    });
    setCountdown(0);
  };

  // 🎨 Render components
  const renderLoginStep = () => (
    <div className="login-step">
      {/* Sports Hero Section - GIỮ NGUYÊN */}
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

      {/* Login Methods - SỬA ĐÂY */}
      <div className="login-methods">
        {/* ✅ FORM LOGIN TRUYỀN THỐNG */}
        <div className="input-section">
          {/* Email/Phone Input */}
          <div className="input-wrapper">
            <div className="input-icon">📧</div>
            <input
              type="text"
              placeholder="Email hoặc số điện thoại"
              value={otpData.phoneOrEmail}
              onChange={(e) => setOtpData(prev => ({ ...prev, phoneOrEmail: e.target.value }))}
              className="sport-input"
              disabled={isLoading}
            />
          </div>

          {/* ✅ THÊM Ô MẬT KHẨU */}
          <div className="input-wrapper">
            <div className="input-icon">🔒</div>
            <input
              type="password"
              placeholder="Mật khẩu"
              value={otpData.password || ''}
              onChange={(e) => setOtpData(prev => ({ ...prev, password: e.target.value }))}
              className="sport-input"
              disabled={isLoading}
            />
          </div>

          {/* ✅ NUT LOGIN THAY VÌ GỬI OTP */}
          <button
            className="sport-btn primary pulse"
            onClick={handleLogin} // ✅ Thay function mới
            disabled={isLoading || !otpData.phoneOrEmail.trim() || !otpData.password?.trim()}
          >
            {isLoading ? (
              <><span className="spinner"></span> Đang đăng nhập...</>
            ) : (
              <>🚀 Đăng nhập</>
            )}
          </button>
        </div>

        {/* Divider - GIỮ NGUYÊN */}
        <div className="divider-sports">
          <div className="divider-line"></div>
          <span className="divider-text">HOẶC</span>
          <div className="divider-line"></div>
        </div>

        {/* Google Login - GIỮ NGUYÊN */}
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
            onClick={handleVerifyOtp} // ✅ Same function for both
            disabled={otpData.isVerifying || otpData.otpCode.length !== 6}
          >
            {otpData.isVerifying ? (
              <><span className="spinner"></span> Đang xác thực...</>
            ) : (
              <>✅ Xác thực</>
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

  const renderGoogleOtpStep = () => (
    <div className="google-otp-step">
      <div className="google-otp-header">
        <div className="google-otp-icon">🔐</div>
        <h2>Xác thực Google Login</h2>
        <p>Mã OTP đã được gửi đến email Google</p>
        <div className="email-display">{googleOtpData.email}</div>
      </div>

      <div className="otp-input-section">
        <div className="otp-input-wrapper">
          <input
            type="text"
            placeholder="000000"
            value={googleOtpData.otpCode}
            onChange={(e) => {
              const value = e.target.value.replace(/\D/g, '').slice(0, 6);
              setGoogleOtpData(prev => ({ ...prev, otpCode: value }));
            }}
            className="otp-input"
            maxLength={6}
            disabled={googleOtpData.isVerifying}
          />
        </div>

        <div className="otp-actions">
          <button
            className="sport-btn success"
            onClick={handleVerifyOtp} // ✅ Same function for both
            disabled={googleOtpData.isVerifying || googleOtpData.otpCode.length !== 6}
          >
            {googleOtpData.isVerifying ? (
              <><span className="spinner"></span> Đang xác thực...</>
            ) : (
              <>🔓 Xác thực Google</>
            )}
          </button>
        </div>

        <div className="otp-footer">
          {countdown > 0 ? (
            <p className="countdown">Gửi lại sau {countdown}s</p>
          ) : (
            <button className="resend-btn" onClick={handleResendGoogleOtp}>
              🔄 Thử lại Google Login
            </button>
          )}
          <button className="back-btn" onClick={handleBackToLogin}>
            ← Quay lại
          </button>
        </div>
      </div>
    </div>
  );

  return (
    <div className="sports-login-container">
      <div className="sports-background">
        <div className="bg-shape shape1"></div>
        <div className="bg-shape shape2"></div>
        <div className="bg-shape shape3"></div>
      </div>

      <div className="login-card-sports">
        {currentStep === 'login' && renderLoginStep()}
        {currentStep === 'otp' && renderOtpStep()}
        {currentStep === 'google-otp' && renderGoogleOtpStep()}
      </div>

      {/* Sports Footer */}
      <div className="sports-footer">
        <p>🏆 Tham gia cộng đồng thể thao lớn nhất Việt Nam</p>
      </div>
    </div>
  );
};

export default Login;