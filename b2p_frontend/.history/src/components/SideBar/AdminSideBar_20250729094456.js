import React, { useState, useEffect } from "react";
import "./AdminSideBar.scss";
import { useNavigate } from "react-router-dom";

const AdminSideBar = ({
  onClose,
  isMobile,
  isTablet,
  isOpen,
  collapsed,
  onToggleCollapse,
}) => {
  const [activeMenu, setActiveMenu] = useState("statistics");
  const [expandedMenus, setExpandedMenus] = useState({});
  const [userInfo, setUserInfo] = useState({
    fullName: "Admin System",
    email: "admin@book2play.com",
    phone: "0987654321",
    avatar: "",
  });
  const navigate = useNavigate();

  // Mock facilities data
  const [facilities, setFacilities] = useState([
    { id: 1, name: "Cơ sở Quận 1", courts: 5 },
    { id: 2, name: "Cơ sở Quận 7", courts: 8 },
    { id: 3, name: "Cơ sở Thủ Đức", courts: 12 },
  ]);

  // Menu items configuration
  const menuItems = [
    {
      id: "statistics",
      title: "Thống kê tổng quan",
      icon: "fas fa-chart-line",
      path: "/admin",
    },
    {
      id: "account-management",
      title: "Quản lý tài khoản",
      icon: "fas fa-users-cog",
      path: "/admin/accounts",
    },
    {
      id: "slider-management",
      title: "Quản lý Slider",
      icon: "fas fa-images",
      path: "/admin/sliders",
    },
    {
      id: "court-categories",
      title: "Quản lý thể loại sân",
      icon: "fas fa-list-ul",
      path: "/admin/manage-court-categories",
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
  const handleMenuClick = (menuId, path) => {
    setActiveMenu(menuId);
    if (path) {
      navigate(path);
    }
    // Close mobile sidebar after navigation
    if (isMobile && onClose) {
      onClose();
    }
  };

  // Generate avatar initials
  const getAvatarInitials = (fullName) => {
    if (!fullName) return "AD";
    return fullName
      .split(" ")
      .map((name) => name.charAt(0))
      .join("")
      .substring(0, 2)
      .toUpperCase();
  };

  // Handle logout
  const handleLogout = () => {
    // Add logout logic here
    navigate("/login");
    if (isMobile && onClose) {
      onClose();
    }
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
            <i className="fas fa-shield-alt"></i>
            <span>Administrator</span>
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
        </div>
      )}
    </div>
  );

  // Render menu item
  const renderMenuItem = (item) => {
    const isActive = activeMenu === item.id;
    const isExpanded = expandedMenus[item.id];

    return (
      <div key={item.id} className="menu-item">
        <div
          className={`menu-link ${isActive ? "active" : ""} ${item.hasSubmenu ? "has-submenu" : ""
            }`}
          onClick={() => {
            if (item.hasSubmenu) {
              toggleMenu(item.id);
            } else {
              handleMenuClick(item.id, item.path);
            }
          }}
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
            <i
              className={`fas fa-chevron-down submenu-arrow ${isExpanded ? "expanded" : ""
                }`}
            ></i>
          )}
        </div>

        {/* Render submenu */}
        {item.hasSubmenu && isExpanded && (!collapsed || isMobile) && (
          <div className="submenu">
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

  // Render dynamic submenu
  const renderDynamicSubmenu = () => {
    return facilities.map((facility) => (
      <div
        key={`facility-${facility.id}`}
        className={`submenu-item ${activeMenu === `facility-${facility.id}` ? "active" : ""
          }`}
        onClick={() =>
          handleMenuClick(
            `facility-${facility.id}`,
            `/admin/courts/${facility.id}`
          )
        }
      >
        <div className="submenu-icon-wrapper">
          <i className="fas fa-map-marker-alt"></i>
        </div>
        <div className="facility-info">
          <span className="facility-name">{facility.name}</span>
          <span className="court-count">{facility.courts} sân</span>
        </div>
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
      {/* Header with Toggle Button */}
      <div className="sidebar__header">
        <div className="logo-section">
          <div className="logo-icon-wrapper">
            <i className="fas fa-tools logo-icon"></i>
          </div>
          {(!collapsed || isMobile) && (
            <div className="logo-text-wrapper">
              <span className="logo-text">Admin Panel</span>
              <span className="logo-subtitle">Book2Play</span>
            </div>
          )}
        </div>

        {/* Toggle/Close Button */}
        <div className="header-controls">
          {/* Close button for mobile */}
          {isMobile && (
            <button
              className="collapse-btn mobile-close"
              onClick={onClose}
              title="Đóng menu"
            >
              <i className="fas fa-times"></i>
            </button>
          )}

          {/* Toggle button for desktop/tablet */}
          {!isMobile && onToggleCollapse && (
            <button
              className="collapse-btn desktop-toggle"
              onClick={onToggleCollapse}
              title={collapsed ? "Mở rộng menu" : "Thu gọn menu"}
            >
              <i
                className={`fas ${collapsed ? "fa-angle-right" : "fa-angle-left"
                  }`}
              ></i>
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
            <span>v2.1.0</span>
          </div>
        )}
      </div>
    </div>
  );
};

export default AdminSideBar;
