import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import "./UnauthorizedPage.scss";

const UnauthorizedPage = () => {
  const navigate = useNavigate();
  const { user, isAdmin, isPlayer, isCourtOwner } = useAuth();
  const [countdown, setCountdown] = useState(10);

  // Auto redirect after countdown
  useEffect(() => {
    const timer = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          handleRedirectToDashboard();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const handleRedirectToDashboard = () => {
    if (isAdmin()) {
      navigate('/admin');
    } else if (isPlayer()) {
      navigate('/');
    } else if (isCourtOwner()) {
      navigate('/court-owner');
    } else {
      navigate('/login');
    }
  };

  const getRoleBasedMessage = () => {
    if (isAdmin()) {
      return {
        role: 'Quản trị viên',
        suggestion: 'Bạn có thể truy cập trang quản trị hệ thống.',
        dashboardLink: '/admin'
      };
    } else if (isPlayer()) {
      return {
        role: 'Người chơi',
        suggestion: 'Bạn có thể đặt sân và quản lý lịch đặt của mình.',
        dashboardLink: '/'
      };
    } else if (isCourtOwner()) {
      return {
        role: 'Chủ sân',
        suggestion: 'Bạn có thể quản lý sân và đơn đặt sân.',
        dashboardLink: '/court-owner'
      };
    }
    return {
      role: 'Khách',
      suggestion: 'Vui lòng đăng nhập để truy cập.',
      dashboardLink: '/login'
    };
  };

  const { role, suggestion, dashboardLink } = getRoleBasedMessage();

  return (
    <div className="unauthorized-page min-h-screen bg-gradient-to-br from-blue-50 via-white to-cyan-50 flex items-center justify-center px-4 sm:px-6 lg:px-8">
      {/* Background decoration */}
      <div className="unauthorized-bg absolute inset-0 overflow-hidden pointer-events-none">
        <div className="decor decor-1 absolute -top-40 -right-40 w-80 h-80 bg-blue-100 rounded-full opacity-20"></div>
        <div className="decor decor-2 absolute -bottom-40 -left-40 w-96 h-96 bg-cyan-100 rounded-full opacity-20"></div>
      </div>

      <div className="u-wrap relative max-w-lg w-full">
        {/* Main card */}
        <div className="u-card bg-white/80 backdrop-blur-sm shadow-2xl rounded-3xl overflow-hidden border border-gray-100">
          {/* Header with gradient */}
          <div className="u-card__header bg-gradient-to-r from-red-500 to-orange-500 px-8 py-6">
            <div className="flex items-center justify-center">
              <div className="bg-white/20 rounded-full p-3">
                <svg 
                  className="w-8 h-8 text-white" 
                  fill="none" 
                  stroke="currentColor" 
                  viewBox="0 0 24 24"
                >
                  <path 
                    strokeLinecap="round" 
                    strokeLinejoin="round" 
                    strokeWidth={2} 
                    d="M12 15v2m0 4h.01M12 7a4 4 0 100 8 4 4 0 000-8z" 
                  />
                </svg>
              </div>
            </div>
            <h1 className="text-2xl font-bold text-white text-center mt-4">
              Truy cập bị từ chối
            </h1>
            <p className="text-white/90 text-center mt-2 text-sm">
              Bạn không có quyền truy cập vào trang này
            </p>
          </div>

          {/* Content */}
          <div className="u-card__content px-8 py-8">
            {/* User info */}
            <div className="u-user-info text-center mb-6">
              <div className="inline-flex items-center px-4 py-2 rounded-full bg-blue-50 border border-blue-200 mb-4">
                <div className="w-2 h-2 bg-green-400 rounded-full mr-3 animate-pulse"></div>
                <span className="text-sm font-medium text-blue-800">
                  Đăng nhập như: <span className="font-bold">{role}</span>
                </span>
              </div>
              
              <p className="text-gray-600 leading-relaxed">
                {suggestion}
              </p>
            </div>

            {/* Countdown */}
            <div className="u-countdown text-center mb-8">
              <div className="u-countdown__circle inline-flex items-center justify-center w-16 h-16 bg-blue-50 rounded-full border-4 border-blue-100 mb-4">
                <span className="text-2xl font-bold text-blue-600">{countdown}</span>
              </div>
              <p className="text-sm text-gray-500">
                Tự động chuyển hướng sau {countdown} giây
              </p>
            </div>

            {/* Action buttons */}
            <div className="u-actions space-y-3">
              <button 
                onClick={handleRedirectToDashboard}
                className="u-btn u-btn--primary w-full bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white font-semibold py-3 px-6 rounded-xl transition-all duration-200 transform hover:scale-[1.02] shadow-lg hover:shadow-xl"
              >
                <span className="flex items-center justify-center">
                  <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                  </svg>
                  Về trang chính
                </span>
              </button>
              
              <button 
                onClick={() => window.history.back()}
                className="u-btn u-btn--ghost w-full bg-gray-100 hover:bg-gray-200 text-gray-700 font-semibold py-3 px-6 rounded-xl transition-all duration-200 border border-gray-200 hover:border-gray-300"
              >
                <span className="flex items-center justify-center">
                  <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
                  </svg>
                  Quay lại trang trước
                </span>
              </button>
            </div>
          </div>

          {/* Footer */}
          <div className="u-card__footer bg-gray-50 px-8 py-4 border-t border-gray-100">
            <div className="flex items-center justify-center text-sm text-gray-500">
              <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Liên hệ quản trị viên nếu bạn cần hỗ trợ
            </div>
          </div>
        </div>

        {/* Floating elements */}
        <div className="u-floating u-floating--1 absolute -top-4 -right-4 w-8 h-8 bg-yellow-400 rounded-full opacity-60"></div>
        <div className="u-floating u-floating--2 absolute -bottom-4 -left-4 w-6 h-6 bg-pink-400 rounded-full opacity-60" style={{ animationDelay: '0.5s' }}></div>
      </div>
    </div>
  );
};

export default UnauthorizedPage;