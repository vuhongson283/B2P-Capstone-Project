import React, { useState, useEffect } from "react";
import { message } from "antd";
import "./CourtOwnerSideBar.scss";
import { useNavigate } from "react-router-dom";
import { getFacilitiesByCourtOwnerId } from "../../services/apiService";
import { useAuth } from "../../context/AuthContext";

const CourtOwnerSideBar = ({
  onClose,
  isMobile,
  isTablet,
  isOpen,
  collapsed,
  onToggleCollapse,
}) => {
  const [activeMenu, setActiveMenu] = useState("statistics");
  const [expandedMenus, setExpandedMenus] = useState({});
  const navigate = useNavigate();
  
  // ✅ Use real user data from AuthContext
  const { user, isLoggedIn, logout } = useAuth();
  
  // ✅ Real user info from AuthContext
  const userInfo = {
    fullName: user?.fullName || user?.name || "Court Owner",
    email: user?.email || "owner@example.com",
    phone: user?.phone || "Chưa cập nhật",
    avatar: user?.avatar || user?.picture || "",
    role: "Chủ sân",
    userId: user?.userId || user?.id
  };

  useEffect(() => {
    // Fetch facilities data on component mount
    const fetchFacilities = async () => {
      try {
        const response = await getFacilitiesByCourtOwnerId(6);
        if (response.data && response.data.items) {
          setFacilities(response.data.items);
        }
      } catch (error) {
        console.error("Error fetching facilities:", error);
        message.error("Không thể tải danh sách cơ sở");
      } finally {
        setLoading(false);
      }
    };

    fetchFacilities();
  }, []);

  // Menu items configuration
  const menuItems = [
    {
      id: "statistics",
      title: "Thống kê",
      icon: "fas fa-chart-bar",
      path: "/court-owner/",
    },
    {
      id: "order-management",
      title: "Quản lý đơn",
      icon: "fas fa-clipboard-list",
      path: "/court-owner/booking-management",
    },
    {
      id: "facility-management",
      title: "Quản lý cơ sở",
      icon: "fas fa-building",
      hasSubmenu: true,
      submenu: [
        {
          id: "general-info",
          title: "Thông tin chung",
          icon: "fas fa-info-circle",
          path: "/court-owner/facility/general",
        },
        {
          id: "time-slots",
          title: "Khung giờ",
          icon: "fas fa-clock",
          path: "/court-owner/facility/time-slots",
        },
      ],
    },
    {
      id: "court-management",
      title: "Quản lý sân",
      icon: "fas fa-futbol",
      hasSubmenu: true,
      isDynamic: true,
    },
  ];

  // Toggle menu expansion
  const toggleMenu = (menuId) => {
    setExpandedMenus((prev) => ({
      ...prev,
      [menuId]: !prev[menuId],
    }));
  };

  // Handle menu item click
  const handleMenuClick = (menuId, path, item) => {
    setActiveMenu(menuId);

    if (menuId === "time-slots") {
      if (facilities.length > 0) {
        navigate(`/court-owner/facility/time-slots/${facilities[0].id}`);
      } else {
        message.warning('Chưa có cơ sở nào để quản lý khung giờ');
        navigate("/court-owner/facility/general");
      }
    } else if (path) {
      navigate(path);
    }

    if (isMobile && onClose) {
      onClose();
    }
  };

  // Handle logout
  const handleLogout = async () => {
    try {
      await logout();
      message.success('Đăng xuất thành công!');
      navigate("/login");
    } catch (error) {
      console.error('Logout error:', error);
      message.error('Lỗi khi đăng xuất!');
      localStorage.removeItem('user');
      navigate("/login");
    }
    
    if (isMobile && onClose) {
      onClose();
    }
  };

  // Generate avatar initials
  const getAvatarInitials = (fullName) => {
    if (!fullName) return "CO";
    return fullName
      .split(" ")
      .map((name) => name.charAt(0))
      .join("")
      .substring(0, 2)
      .toUpperCase();
  };

  // Render user info section
  const renderUserInfo = () => (
    <div className="sidebar__user-info">
      <div className="user-avatar">
        {userInfo.avatar ? (
          <img src={userInfo.avatar} alt="Avatar" />
        ) : (
          <div className="avatar-placeholder">
            {getAvatarInitials(userInfo.fullName)}
          </div>
        )}
        <div className="online-indicator"></div>
      </div>

      {(!collapsed || isMobile) && (
        <div className="user-details">
          <h3 className="user-name">{userInfo.fullName}</h3>
          <div className="user-role">
            <i className="fas fa-crown"></i>
            <span>{userInfo.role}</span>
          </div>
          <div className="user-contact">
            <div className="contact-item">
              <i className="fas fa-envelope"></i>
              <span>{userInfo.email}</span>
            </div>
            <div className="contact-item">
              <i className="fas fa-phone"></i>
              <span>{userInfo.phone}</span>
            </div>
          </div>
          {userInfo.userId && (
            <div className="user-id">
              <small>ID: {userInfo.userId}</small>
            </div>
          )}
        </div>
      )}
    </div>
  );

  // ✅ UPDATED: Render menu item with hover support
  const renderMenuItem = (item) => {
    const isActive = activeMenu === item.id;
    const isExpanded = expandedMenus[item.id];

    return (
      <div key={item.id} className="menu-item">
        <div
          className={`menu-link ${isActive ? "active" : ""} ${item.hasSubmenu ? "has-submenu" : ""}`}
          onClick={() => {
            // ✅ Handle collapsed submenu click - navigate to first submenu item
            if (item.hasSubmenu && collapsed && !isMobile) {
              const firstSubmenuItem = item.submenu?.[0];
              if (firstSubmenuItem) {
                handleMenuClick(firstSubmenuItem.id, firstSubmenuItem.path);
              }
            } else if (item.hasSubmenu) {
              toggleMenu(item.id);
            } else {
              handleMenuClick(item.id, item.path);
            }
          }}
          // ✅ Add tooltip for collapsed state
          title={collapsed && !isMobile ? item.title : undefined}
        >
          <div className="menu-content">
            <div className="menu-icon-wrapper">
              <i className={item.icon}></i>
            </div>
            {(!collapsed || isMobile) && (
              <span className="menu-title">{item.title}</span>
            )}
          </div>

          {item.hasSubmenu && (!collapsed || isMobile) && (
            <i className={`fas fa-chevron-down submenu-arrow ${isExpanded ? "expanded" : ""}`}></i>
          )}
        </div>

        {/* ✅ UPDATED: Always render submenu for hover effect */}
        {item.hasSubmenu && (
          <div className={`submenu ${(!collapsed || isMobile) && isExpanded ? 'show' : ''}`}>
            {item.isDynamic
              ? renderDynamicSubmenu()
              : renderStaticSubmenu(item.submenu)}
          </div>
        )}
      </div>
    );
  };

  // Render static submenu
  const renderStaticSubmenu = (submenuItems) => {
    return submenuItems.map((subItem) => (
      <div
        key={subItem.id}
        className={`submenu-item ${activeMenu === subItem.id ? "active" : ""}`}
        onClick={() => handleMenuClick(subItem.id, subItem.path)}
      >
        <div className="submenu-icon-wrapper">
          <i className={subItem.icon}></i>
        </div>
        <span>{subItem.title}</span>
      </div>
    ));
  };

  const [selectedFacilityId, setSelectedFacilityId] = useState(null);
  
  // Render dynamic submenu
  const renderDynamicSubmenu = () => {
    if (loading) {
      return <div className="submenu-loading">Đang tải...</div>;
    }

    if (facilities.length === 0) {
      return <div className="submenu-empty">Không có cơ sở nào</div>;
    }

    return facilities.map((facility) => (
      <div
        key={`facility-${facility.facilityId}`}
        className={`submenu-item ${activeMenu === `facility-${facility.facilityId}` ? "active" : ""}`}
        onClick={() => {
          handleMenuClick(
            `facility-${facility.facilityId}`,
            `/court-owner/facilities/${facility.facilityId}/courts`
          );
          setSelectedFacilityId(facility.facilityId);
        }}
      >
        <div className="submenu-icon-wrapper">
          <i className="fas fa-map-marker-alt"></i>
        </div>
        <span>{facility.facilityName || facility.name}</span>
      </div>
    ));
  };

  // Generate sidebar classes
  const getSidebarClasses = () => {
    const classes = ["court-owner-sidebar"];

    if (collapsed && !isMobile) {
      classes.push("collapsed");
    }

    if (isMobile && isOpen) {
      classes.push("mobile-open");
    }

    return classes.join(" ");
  };

  return (
    <div className={getSidebarClasses()}>
      {/* Header */}
      <div className="sidebar__header">
        <div className="logo-section">
          <div className="logo-icon-wrapper">
            <i className="fas fa-crown logo-icon"></i>
          </div>
          {(!collapsed || isMobile) && (
            <div className="logo-text-wrapper">
              <span className="logo-text">Court Owner</span>
              <span className="logo-subtitle">Book2Play</span>
            </div>
          )}
        </div>

        <div className="header-controls">
          {isMobile && (
            <button
              className="collapse-btn mobile-close"
              onClick={onClose}
              title="Đóng menu"
            >
              <i className="fas fa-times"></i>
            </button>
          )}

          {!isMobile && onToggleCollapse && (
            <button
              className="collapse-btn desktop-toggle"
              onClick={onToggleCollapse}
              title={collapsed ? "Mở rộng menu" : "Thu gọn menu"}
            >
              <i className={`fas ${collapsed ? "fa-angle-right" : "fa-angle-left"}`}></i>
            </button>
          )}
        </div>
      </div>

      {/* User Info */}
      {renderUserInfo()}

      {/* Navigation Menu */}
      <nav className="sidebar__navigation">
        <div className="menu-list">{menuItems.map(renderMenuItem)}</div>
      </nav>

      {/* Footer */}
      <div className="sidebar__footer">
        <div className="footer-item logout-btn" onClick={handleLogout}>
          <div className="footer-icon-wrapper">
            <i className="fas fa-sign-out-alt"></i>
          </div>
          {(!collapsed || isMobile) && <span>Đăng xuất</span>}
        </div>

        {(!collapsed || isMobile) && (
          <div className="sidebar-version">
            <span>v1.0.0</span>
          </div>
        )}
      </div>
    </div>
  );
};

export default CourtOwnerSideBar;