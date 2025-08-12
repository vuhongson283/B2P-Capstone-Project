import React, { useState, useEffect } from 'react';
import { GoogleLogin } from '@react-oauth/google';
import { jwtDecode } from 'jwt-decode';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import {
  googleLoginAxios,
  sendOtpAxios,
  verifyOtpAxios,
} from '../../services/apiService';
import './Login.scss';

const Login = () => {
  const navigate = useNavigate();

  // 🎯 State management
  const [currentStep, setCurrentStep] = useState('login'); // 'login' | 'otp' | 'google-otp'
  const [isLoading, setIsLoading] = useState(false);
  const [countdown, setCountdown] = useState(0);

  // 🎯 Regular OTP state
  const [otpData, setOtpData] = useState({
    phoneOrEmail: '',
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
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('user', JSON.stringify(user));

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

  // 🔑 Handle Google Login - FIXED for new API format
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

      console.log('🎯 Google login response:', response.data);

      // ✅ FIXED: Handle multiple response formats
      let sessionToken = null;
      let otpMessage = null;

      // Case 1: Wrapped in success/data structure
      if (response.data && response.data.success && response.data.data?.sessionToken) {
        sessionToken = response.data.data.sessionToken;
        otpMessage = response.data.data.message || response.data.message;
      }
      // Case 2: Direct object with sessionToken (YOUR CURRENT CASE)
      else if (response.data && response.data.sessionToken) {
        sessionToken = response.data.sessionToken;
        otpMessage = response.data.message;
      }
      // Case 3: Response nested deeper
      else if (response.data?.data?.sessionToken) {
        sessionToken = response.data.data.sessionToken;
        otpMessage = response.data.data.message;
      }

      console.log('🔍 Extracted data:', { sessionToken, otpMessage });

      // ✅ SUCCESS: Process OTP session
      if (sessionToken) {
        message.info(`📧 ${otpMessage || `Mã OTP đã được gửi đến ${userInfo.email}!`}`);

        setGoogleOtpData({
          email: userInfo.email,
          userName: userInfo.name || userInfo.email,
          sessionToken: sessionToken,
          otpCode: '',
          isNewUser: false, // Will be determined after OTP verification
          isVerifying: false
        });

        setCurrentStep('google-otp');
        setCountdown(60);
        return;
      }

      // ✅ Handle error case
      const errorMsg = response.data?.message || 'No session token received';
      console.error('❌ No session token found in response:', response.data);
      throw new Error(`Unexpected response: ${errorMsg}`);

    } catch (error) {
      console.error('❌ Google login error:', error);

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
      } else if (user.roleId === 2) { // CourtOwner  
        console.log('🔄 Navigating to CourtOwner dashboard');
        navigate('/court-owner');
      } else { // Player or default (roleId = 3 hoặc khác)
        console.log('🔄 Navigating to Player dashboard');
        navigate('/player'); // ✅ Fixed: có path cụ thể
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

      {/* Login Methods */}
      <div className="login-methods">
        {/* Phone/Email Input */}
        <div className="input-section">
          <div className="input-wrapper">
            <div className="input-icon">📱</div>
            <input
              type="text"
              placeholder="Số điện thoại hoặc email"
              value={otpData.phoneOrEmail}
              onChange={(e) => setOtpData(prev => ({ ...prev, phoneOrEmail: e.target.value }))}
              className="sport-input"
              disabled={isLoading}
            />
          </div>
          <button
            className="sport-btn primary pulse"
            onClick={handleSendOtp}
            disabled={isLoading || !otpData.phoneOrEmail.trim()}
          >
            {isLoading ? (
              <><span className="spinner"></span> Đang gửi...</>
            ) : (
              <>🚀 Nhận mã OTP</>
            )}
          </button>
        </div>

        {/* Divider */}
        <div className="divider-sports">
          <div className="divider-line"></div>
          <span className="divider-text">HOẶC</span>
          <div className="divider-line"></div>
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