import React, { useState, useEffect } from "react";
import AdminSideBar from "../SideBar/AdminSideBar";
import "./Admin.scss";
import { Outlet } from "react-router-dom";
import { Button } from "antd";
import { MenuOutlined } from "@ant-design/icons";

// ✅ ADD: Import comment notification components
import { SignalRProvider } from "../../contexts/SignalRContext";
import { GlobalCommentNotificationProvider } from "../../contexts/GlobalCommentNotificationContext";
import NotificationDropdown from "../NotificationDropdown";
import { useAuth } from "../../contexts/AuthContext";

const Admin = (props) => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const [isTablet, setIsTablet] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  // ✅ ADD: Get user info for comment notifications
  const { user, userId, isLoading: authLoading } = useAuth();

  // ✅ ADD: Prepare currentUser for comment notifications (same as App.js)
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
    const classes = ["admin-container"];

    if (sidebarCollapsed && !isMobile) {
      classes.push("sidebar-collapsed");
    }

    return classes.join(" ");
  };

  // Generate dynamic classes for sidebar
  const getSidebarClasses = () => {
    const classes = ["admin-sidebar"];

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

          {/* ✅ ADD: Comment Notification Bell */}
          <div style={{
            position: "fixed",
            top: "20px",
            right: "20px",
            zIndex: 10002,
          }}>
            <div style={{
              background: "linear-gradient(135deg, #722ed1, #9254de)", // Tím cho admin
              borderRadius: "50%",
              padding: "12px",
              boxShadow: "0 6px 20px rgba(114, 46, 209, 0.4)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}>
              <NotificationDropdown />
            </div>
          </div>

          <div className={getSidebarClasses()}>
            <AdminSideBar
              onClose={closeSidebar}
              isMobile={isMobile}
              isTablet={isTablet}
              isOpen={sidebarOpen}
              collapsed={sidebarCollapsed}
              onToggleCollapse={toggleSidebar}
            />
          </div>

          <div className="admin-main">
            <div className="admin-content">
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
      </GlobalCommentNotificationProvider>
    </SignalRProvider>
  );
};

export default Admin;