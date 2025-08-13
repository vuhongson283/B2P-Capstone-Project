import React, { useState, useEffect } from "react";
import CourtOwnerSideBar from "../SideBar/CourtOwnerSideBar";
import "./CourtOwner.scss";
import { Outlet } from "react-router-dom";
import { Button } from "antd";
import { MenuOutlined } from "@ant-design/icons";
import { GlobalNotificationProvider } from "../../contexts/GlobalNotificationContext";
import NotificationBell from "../../components/NotificationBell";
import { getFacilitiesByCourtOwnerId } from "../../services/apiService";

const USER_ID = 13; // Your user ID

const CourtOwner = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const [isTablet, setIsTablet] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  // ✅ NEW: State for global notifications
  const [facilityIds, setFacilityIds] = useState([]);

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

    loadFacilities();
  }, []);

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

  return (
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

        {/* ✅ NEW: Notification Bell - positioned absolutely */}
        <div style={{
          position: "fixed",
          top: "20px",
          right: "20px",
          zIndex: 10002,
        }}>
          <NotificationBell />
        </div>

        <div>
          Buồi
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
  );
};

export default CourtOwner;