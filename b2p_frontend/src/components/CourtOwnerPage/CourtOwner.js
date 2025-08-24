import React, { useState, useEffect } from "react";
import { Outlet, Navigate, useLocation } from "react-router-dom";
import { Button } from "antd";
import { MenuOutlined } from "@ant-design/icons";
import CourtOwnerSideBar from "../SideBar/CourtOwnerSideBar";
import "./CourtOwner.scss";

// Contexts
import { GlobalNotificationProvider } from "../../contexts/GlobalNotificationContext";
import { SignalRProvider } from "../../contexts/SignalRContext";
import { GlobalCommentNotificationProvider } from "../../contexts/GlobalCommentNotificationContext";
import { useAuth } from "../../contexts/AuthContext";

// Components
import NotificationBell from "../../components/NotificationBell";
import NotificationDropdown from "../NotificationDropdown.js";

// Services
import { getFacilitiesByCourtOwnerId, getMerchantPaymentsByUserId } from "../../services/apiService";

const CourtOwner = () => {
  // UI States
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const [isTablet, setIsTablet] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [facilityIds, setFacilityIds] = useState([]);

  // Payment Access States
  const [hasPaymentAccess, setHasPaymentAccess] = useState(null); // null = loading
  const [paymentLoading, setPaymentLoading] = useState(false);

  // Auth & Location
  const { user, userId, isLoading: authLoading, isLoggedIn } = useAuth();
  const USER_ID = userId || user?.userId;
  const location = useLocation();

  // Prepare currentUser for comment notifications
  const currentUser = user ? {
    userId: user.userId || user.id,
    fullName: user.fullName || user.name || user.userName || user.username,
    userName: user.userName || user.username,
    avatar: user.avatar || user.profilePicture ||
      `https://ui-avatars.com/api/?name=${encodeURIComponent(user.fullName || user.userName || user.username || 'User')}&background=27ae60&color=fff&size=200`,
    roleId: user.roleId,
    roleName: user.roleName,
    loginTime: new Date().toISOString(),
  } : null;

  // Payment Access Check Function
  const checkPaymentAccess = (paymentData) => {
    console.log('🔍 Raw paymentData:', paymentData);
    
    if (!paymentData || !Array.isArray(paymentData)) {
      console.log('❌ PaymentData is not array or null');
      return false;
    }
    
    const activePayments = paymentData.filter(payment => 
      payment.statusName === "Active"
    );
    
    console.log('✅ Active payments:', activePayments);
    
    const paymentMethods = [...new Set(
      activePayments.map(payment => payment.paymentMethodName)
    )];
    
    console.log('🎯 Unique payment methods:', paymentMethods);
    console.log('🎯 Payment methods as string:', JSON.stringify(paymentMethods));
    
    const hasZaloPay = paymentMethods.includes('ZaloPay');
    const hasStripe = paymentMethods.includes('Stripe');
    
    console.log('💰 Has ZaloPay:', hasZaloPay, '(looking for "ZaloPay")');
    console.log('💳 Has Stripe:', hasStripe, '(looking for "Stripe")');
    console.log('🏁 Final result:', hasZaloPay && hasStripe);
    
    return hasZaloPay && hasStripe;
  };

  // Load facilities for global notifications
  useEffect(() => {
    const loadFacilities = async () => {
      try {
        const response = await getFacilitiesByCourtOwnerId(USER_ID, "", null, 1, 100);
        const facilities = response.data?.items || [];
        const ids = facilities.map(f => f.facilityId);
        setFacilityIds(ids);
        console.log('🏢 Loaded facility IDs for global notifications:', ids);
      } catch (error) {
        console.error('❌ Error loading facilities for notifications:', error);
      }
    };

    if (USER_ID) {
      loadFacilities();
    }
  }, [USER_ID]);

  // Payment Access Check
  useEffect(() => {
    const fetchAndCheckPayments = async () => {
      if (!USER_ID) return;
      
      try {
        setPaymentLoading(true);
        console.log('🚀 Calling getMerchantPaymentsByUserId with USER_ID:', USER_ID);
        
        const response = await getMerchantPaymentsByUserId(USER_ID);
        
        console.log('📡 Full API response:', response);
        console.log('📡 Response status:', response?.status);
        console.log('📡 Response data:', response?.data);
        console.log('📡 Response data success:', response?.data?.success);
        console.log('📡 Response data type:', typeof response?.data);
        console.log('📡 Response data isArray:', Array.isArray(response?.data));
        
        if (response?.status === 200) {
          // Check if response.data has success property or is array directly
          let paymentArray;
          
          if (response.data?.success && Array.isArray(response.data.data)) {
            // Case: { success: true, data: [...] }
            paymentArray = response.data.data;
            console.log('📋 Using response.data.data (nested)');
          } else if (Array.isArray(response.data)) {
            // Case: response.data is array directly
            paymentArray = response.data;
            console.log('📋 Using response.data (direct array)');
          } else {
            console.log('❌ Unexpected response structure');
            paymentArray = [];
          }
          
          console.log('📋 Final payment array to check:', paymentArray);
          const hasAccess = checkPaymentAccess(paymentArray);
          console.log('🎯 Final payment access result:', hasAccess);
          setHasPaymentAccess(hasAccess);
        } else {
          console.log('❌ API call failed or no success flag');
          console.log('❌ Status:', response?.status);
          console.log('❌ Success flag:', response.data?.success);
          setHasPaymentAccess(false);
        }
      } catch (error) {
        console.error('💥 Error fetching payments:', error);
        setHasPaymentAccess(false);
      } finally {
        setPaymentLoading(false);
      }
    };

    // Chỉ fetch khi auth đã load xong và có USER_ID
    if (!authLoading && USER_ID && isLoggedIn) {
      fetchAndCheckPayments();
    }
  }, [USER_ID, authLoading, isLoggedIn]);

  // Check screen size and set responsive states
  useEffect(() => {
    const checkScreenSize = () => {
      const width = window.innerWidth;
      const mobile = width <= 768;
      const tablet = width > 768 && width <= 1200;
      const desktop = width > 1200;

      setIsMobile(mobile);
      setIsTablet(tablet);

      // Auto-close mobile sidebar when resizing to desktop/tablet
      if (!mobile && sidebarOpen) {
        setSidebarOpen(false);
      }

      // Auto-collapse on tablet, expand on desktop
      if (tablet) {
        setSidebarCollapsed(true);
      } else if (desktop) {
        setSidebarCollapsed(false);
      }
    };

    checkScreenSize();
    window.addEventListener("resize", checkScreenSize);
    return () => window.removeEventListener("resize", checkScreenSize);
  }, [sidebarOpen]);

  // Sidebar functions
  const toggleSidebar = () => {
    if (isMobile) {
      setSidebarOpen(!sidebarOpen);
    } else {
      setSidebarCollapsed(!sidebarCollapsed);
    }
  };

  const closeSidebar = () => {
    setSidebarOpen(false);
  };

  // Generate dynamic classes
  const getContainerClasses = () => {
    const classes = ["court-owner-container"];
    if (sidebarCollapsed && !isMobile) {
      classes.push("sidebar-collapsed");
    }
    return classes.join(" ");
  };

  const getSidebarClasses = () => {
    const classes = ["court-owner-sidebar"];
    if (sidebarCollapsed && !isMobile) {
      classes.push("collapsed");
    }
    if (isMobile && sidebarOpen) {
      classes.push("sidebar-open");
    }
    return classes.join(" ");
  };

  // Loading states
  if (authLoading || paymentLoading || !currentUser) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        flexDirection: 'column',
        gap: '10px'
      }}>
        <div>🔄 Đang kiểm tra quyền truy cập...</div>
        {authLoading && <small>Đang xác thực...</small>}
        {paymentLoading && <small>Đang kiểm tra phương thức thanh toán...</small>}
      </div>
    );
  }

  // Nếu chưa login hoặc không có USER_ID
  if (!isLoggedIn || !USER_ID) {
    return <Navigate to="/login" replace />;
  }

  // Check redirect logic
  const isOnPaymentPage = location.pathname.includes('/payment-management');
  
  // Nếu chưa đủ payment methods và không ở trang payment-management
  if (hasPaymentAccess === false && !isOnPaymentPage) {
    console.log('Redirecting to payment-management - insufficient payment methods');
    return <Navigate to="/court-owner/payment-management" replace />;
  }

  return (
    <SignalRProvider>
      <GlobalCommentNotificationProvider currentUser={currentUser}>
        <GlobalNotificationProvider userId={USER_ID} facilityIds={facilityIds}>
          <div className={getContainerClasses()}>
            {/* Mobile Menu Toggle */}
            {isMobile && (
              <Button
                className="mobile-menu-toggle"
                type="primary"
                icon={<MenuOutlined />}
                onClick={toggleSidebar}
                style={{
                  position: "fixed",
                  top: "16px",
                  left: "16px",
                  zIndex: 10001,
                  borderRadius: "8px",
                  background: "linear-gradient(135deg, #27ae60, #2ecc71)",
                  border: "none",
                  boxShadow: "0 4px 15px rgba(39, 174, 96, 0.3)",
                }}
              />
            )}

            {/* Notification Area */}
            <div style={{
              position: "fixed",
              top: "20px",
              right: "20px",
              zIndex: 10002,
              display: "flex",
              gap: "12px",
              alignItems: "center"
            }}>
              {/* Comment Notifications */}
              <div style={{
                background: "linear-gradient(135deg, #1890ff, #40a9ff)",
                borderRadius: "50%",
                padding: "12px",
                boxShadow: "0 6px 20px rgba(24, 144, 255, 0.4)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}>
                <NotificationDropdown />
              </div>

              {/* Booking Notifications */}
              <div style={{
                background: "linear-gradient(135deg, #52c41a, #73d13d)",
                borderRadius: "50%",
                padding: "12px",
                boxShadow: "0 6px 20px rgba(82, 196, 26, 0.4)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}>
                <NotificationBell />
              </div>
            </div>

            {/* Sidebar */}
            <div className={getSidebarClasses()}>
              <CourtOwnerSideBar
                onClose={closeSidebar}
                isMobile={isMobile}
                isTablet={isTablet}
                isOpen={sidebarOpen}
                collapsed={sidebarCollapsed}
                onToggleCollapse={toggleSidebar}
              />
            </div>

            {/* Main Content */}
            <div className="court-owner-main">
              <div className="court-owner-content">
                {/* Debug info - remove in production
                {process.env.NODE_ENV === 'development' && (
                  <div style={{ 
                    position: 'fixed', 
                    top: '80px', 
                    right: '20px', 
                    background: 'rgba(0,0,0,0.8)', 
                    color: 'white', 
                    padding: '10px',
                    fontSize: '12px',
                    zIndex: 9999,
                    borderRadius: '4px'
                  }}>
                    <div>USER_ID: {USER_ID}</div>
                    <div>Has Payment Access: {String(hasPaymentAccess)}</div>
                    <div>Current Path: {location.pathname}</div>
                  </div>
                )}
                 */}
                <Outlet />
              </div>
            </div>

            {/* Mobile Overlay */}
            {isMobile && sidebarOpen && (
              <div
                className="mobile-overlay"
                onClick={closeSidebar}
                style={{
                  position: "fixed",
                  top: 0,
                  left: 0,
                  right: 0,
                  bottom: 0,
                  background: "rgba(0, 0, 0, 0.5)",
                  zIndex: 999,
                  animation: "fadeIn 0.3s ease",
                }}
              />
            )}
          </div>
        </GlobalNotificationProvider>
      </GlobalCommentNotificationProvider>
    </SignalRProvider>
  );
};

export default CourtOwner;