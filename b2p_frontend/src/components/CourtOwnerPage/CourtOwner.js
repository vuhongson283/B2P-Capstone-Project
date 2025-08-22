import React, { useState, useEffect } from "react";
import CourtOwnerSideBar from "../SideBar/CourtOwnerSideBar";
import "./CourtOwner.scss";
import { Outlet } from "react-router-dom";
import { Button } from "antd";
import { MenuOutlined } from "@ant-design/icons";
import { GlobalNotificationProvider } from "../../contexts/GlobalNotificationContext";
import NotificationBell from "../../components/NotificationBell";
import { getFacilitiesByCourtOwnerId } from "../../services/apiService";
import { useAuth } from "../../contexts/AuthContext";

// ✅ ADD: Import comment notification components
import { SignalRProvider } from "../../contexts/SignalRContext";
import { GlobalCommentNotificationProvider } from "../../contexts/GlobalCommentNotificationContext";
import NotificationDropdown from "../NotificationDropdown.js"; // Assuming you have this component

const CourtOwner = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const [isTablet, setIsTablet] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  // ✅ NEW: State for global notifications
  const [facilityIds, setFacilityIds] = useState([]);
  const { user, userId, isLoading: authLoading, isLoggedIn } = useAuth();
  const USER_ID = userId || user?.userId;

  // ✅ NEW: Prepare currentUser for comment notifications (same as App.js)
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

  // ✅ NEW: Load facilities for global notifications
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

  // Generate dynamic classes for container
  const getContainerClasses = () => {
    const classes = ["court-owner-container"];

    if (sidebarCollapsed && !isMobile) {
      classes.push("sidebar-collapsed");
    }

    return classes.join(" ");
  };

  // Generate dynamic classes for sidebar
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

  // ✅ Don't render if user not loaded
  if (authLoading || !currentUser) {
    return (
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
        fontSize: '16px'
      }}>
        🔄 Đang tải thông tin người dùng...
      </div>
    );
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

            {/* ✅ UPDATED: Notification Area with both Bell and Comment notifications */}
            <div style={{
              position: "fixed",
              top: "20px",
              right: "20px",
              zIndex: 10002,
              display: "flex",
              gap: "12px",
              alignItems: "center"
            }}>
              {/* ✅ Comment Notifications */}
              <div style={{
                background: "linear-gradient(135deg, #1890ff, #40a9ff)", // Xanh dương cho comment
                borderRadius: "50%",
                padding: "12px",
                boxShadow: "0 6px 20px rgba(24, 144, 255, 0.4)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}>
                <NotificationDropdown />
              </div>

              {/* ✅ Booking Notifications */}
              <div style={{
                background: "linear-gradient(135deg, #52c41a, #73d13d)", // Xanh lá cho booking
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

            <div className="court-owner-main">
              <div className="court-owner-content">
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