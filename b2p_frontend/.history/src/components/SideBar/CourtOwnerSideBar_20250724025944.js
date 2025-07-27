import React, { useState, useEffect } from "react";
import "./CourtOwnerSideBar.scss";
import { useNavigate } from "react-router-dom";

const CourtOwnerSideBar = (props) => {
  const [activeMenu, setActiveMenu] = useState("statistics");
  const [expandedMenus, setExpandedMenus] = useState({});
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [userInfo, setUserInfo] = useState({
    fullName: "Nguyễn Văn A",
    email: "owner@example.com",
    phone: "0987654321",
    avatar: "", // URL to avatar image
  });
  const navigate = useNavigate();

  // Mock facilities data - replace with real data
  const [facilities, setFacilities] = useState([
    { id: 1, name: "Cơ sở Quận 1", courts: 5 },
    { id: 2, name: "Cơ sở Quận 7", courts: 8 },
    { id: 3, name: "Cơ sở Thủ Đức", courts: 12 },
  ]);

  // Menu items configuration
  const menuItems = [
    {
      id: "statistics",
      title: "Thống kê",
      icon: "fas fa-chart-bar",
      path: "/court-owner/statistics",
    },
    {
      id: "order-management",
      title: "Quản lý đơn",
      icon: "fas fa-clipboard-list",
      path: "/court-owner/search",
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
      isDynamic: true, // Dynamic submenu based on facilities
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
  };

  // Toggle sidebar collapse
  const toggleSidebar = () => {
    setIsCollapsed(!isCollapsed);
  };

  // Generate avatar initials
  const getAvatarInitials = (fullName) => {
    if (!fullName) return "U";
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
      </div>

      {!isCollapsed && (
        <div className="user-details">
          <h3 className="user-name">{userInfo.fullName}</h3>
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
          className={`menu-link ${isActive ? "active" : ""} ${
            item.hasSubmenu ? "has-submenu" : ""
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
            <i className={item.icon}></i>
            {!isCollapsed && <span className="menu-title">{item.title}</span>}
          </div>

          {item.hasSubmenu && !isCollapsed && (
            <i
              className={`fas fa-chevron-down submenu-arrow ${
                isExpanded ? "expanded" : ""
              }`}
            ></i>
          )}
        </div>

        {/* Render submenu */}
        {item.hasSubmenu && isExpanded && !isCollapsed && (
          <div className="submenu">
            {item.isDynamic
              ? renderDynamicSubmenu()
              : renderStaticSubmenu(item.submenu)}
          </div>
        )}
      </div>
    );
  };

  // Render static submenu (for facility management)
  const renderStaticSubmenu = (submenuItems) => {
    return submenuItems.map((subItem) => (
      <div
        key={subItem.id}
        className={`submenu-item ${activeMenu === subItem.id ? "active" : ""}`}
        onClick={() => handleMenuClick(subItem.id, subItem.path)}
      >
        <i className={subItem.icon}></i>
        <span>{subItem.title}</span>
      </div>
    ));
  };

  // Render dynamic submenu (for court management - list of facilities)
  const renderDynamicSubmenu = () => {
    return facilities.map((facility) => (
      <div
        key={`facility-${facility.id}`}
        className={`submenu-item ${
          activeMenu === `facility-${facility.id}` ? "active" : ""
        }`}
        onClick={() =>
          handleMenuClick(
            `facility-${facility.id}`,
            `/court-owner/courts/${facility.id}`
          )
        }
      >
        <i className="fas fa-map-marker-alt"></i>
        <div className="facility-info">
          <span className="facility-name">{facility.name}</span>
          <span className="court-count">{facility.courts} sân</span>
        </div>
      </div>
    ));
  };

  return (
    <div className={`court-owner-sidebar ${isCollapsed ? "collapsed" : ""}`}>
      {/* Header */}
      <div className="sidebar__header">
        <div className="logo-section">
          <i className="fas fa-crown logo-icon"></i>
          {!isCollapsed && <span className="logo-text">Court Owner</span>}
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
        <div className="footer-item logout-btn">
          <i className="fas fa-sign-out-alt"></i>
          {!isCollapsed && <span>Đăng xuất</span>}
        </div>
      </div>
    </div>
  );
};

export default CourtOwnerSideBar;
