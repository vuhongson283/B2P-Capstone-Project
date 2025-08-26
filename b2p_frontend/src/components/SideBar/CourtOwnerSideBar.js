import React, { useState, useEffect, useCallback } from "react";
import { Link } from 'react-router-dom';
import { message } from "antd";
import "./CourtOwnerSideBar.scss";
import { useNavigate } from "react-router-dom";
import { getFacilitiesByCourtOwnerId } from "../../services/apiService";
import { useAuth } from "../../contexts/AuthContext";

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
  const [facilities, setFacilities] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  // ✅ Use real user data from AuthContext
  const { user, isLoggedIn, logout } = useAuth();

  // ✅ Get real Court Owner ID from AuthContext
  const getCourtOwnerId = useCallback(() => {
    console.log('🔍 Getting court owner ID - isLoggedIn:', isLoggedIn, 'user:', user);
    
    if (isLoggedIn && user) {
      const userId = user.userId || user.id;
      console.log('✅ Found court owner ID:', userId);
      return userId;
    }
    
    console.warn('⚠️ Court owner not logged in or user data not available');
    return null;
  }, [isLoggedIn, user]);

  // ✅ Real user info from AuthContext
  const userInfo = {
    fullName: user?.fullName || user?.name || "Court Owner",
    email: user?.email || "owner@example.com",
    phone: user?.phone || "Chưa cập nhật",
    avatar: user?.avatar || user?.picture || "",
    role: "Chủ sân",
    userId: user?.userId || user?.id
  };

  // ✅ FIXED: Fetch facilities with real user ID
  useEffect(() => {
    const fetchFacilities = async () => {
      try {
        setLoading(true);
        const courtOwnerId = getCourtOwnerId();
        
        if (!courtOwnerId) {
          console.error('❌ No court owner ID available');
          message.error("Không thể xác định người dùng. Vui lòng đăng nhập lại.");
          setFacilities([]);
          return;
        }

        console.log('📡 Fetching facilities for court owner ID:', courtOwnerId);
        const response = await getFacilitiesByCourtOwnerId(courtOwnerId);
        
        console.log('📊 Facilities API response:', response);
        
        if (response.data && response.data.items) {
          console.log('✅ Found facilities:', response.data.items.length);
          setFacilities(response.data.items);
        } else {
          console.log('⚠️ No facilities found in response');
          setFacilities([]);
        }
      } catch (error) {
        console.error("❌ Error fetching facilities:", error);
        message.error("Không thể tải danh sách cơ sở: " + (error.message || 'Unknown error'));
        setFacilities([]);
      } finally {
        setLoading(false);
      }
    };

    // Only fetch when user data is available
    if (user && isLoggedIn) {
      fetchFacilities();
    } else {
      console.log('⏳ Waiting for user authentication...');
      setLoading(false);
      setFacilities([]);
    }
  }, [user, isLoggedIn, getCourtOwnerId]);

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
     {
      id: "payment-management",
      title: "Quản lý thanh toán",
      icon: "fas fa-credit-card",
      path: "/court-owner/payment-management",
    },
    {
      id: "blog",
      title: "Blog",
      icon: "fas fa-newspaper",
      path: "/court-owner/blog",
    },
    {
      id: "Create-Payment-Account-Guide",
      title: "Create Merchant Account Guide",
      icon: "fas fa-credit-card",
      path: "/court-owner/Create-Payment-Account-Guide",
    },
  ];

  // Toggle menu expansion
  const toggleMenu = (menuId) => {
    setExpandedMenus((prev) => ({
      ...prev,
      [menuId]: !prev[menuId],
    }));
  };

  // ✅ FIXED: Handle menu item click with better facility checking
  const handleMenuClick = (menuId, path, item) => {
    setActiveMenu(menuId);

    if (menuId === "time-slots") {
      console.log('🕐 Time slots clicked, facilities:', facilities);
      
      if (loading) {
        message.info('Đang tải danh sách cơ sở...');
        return;
      }
      
      if (facilities.length > 0) {
        const firstFacility = facilities[0];
        const facilityId = firstFacility.facilityId || firstFacility.id;
        console.log('✅ Navigating to time slots for facility:', facilityId);
        navigate(`/court-owner/facility/time-slots/${facilityId}`);
      } else {
        console.log('⚠️ No facilities available for time slots');
        message.warning('Bạn chưa có cơ sở nào. Vui lòng tạo cơ sở trước khi quản lý khung giờ.');
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
  const handleLogout = () => {
    try {
      logout(); // ✅ Sync function, không cần await
      message.success('Đăng xuất thành công!');
      
    } catch (error) {
      console.error('Logout error:', error);
      message.error('Lỗi khi đăng xuất!');
      localStorage.clear(); // Xóa toàn bộ localStorage
    } finally {
      navigate("/login");
      
      if (isMobile && onClose) {
        onClose();
      }
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
            <div className="user-actions">
              <Link
                to="/court-owner/user-profile"
                className="profile-link"
                style={{
                  color: 'white',
                  textDecoration: 'none',
                  fontSize: '12px',
                  padding: '4px 8px',
                  borderRadius: '4px',
                  border: '2px solid white',
                  display: 'inline-block',
                  marginTop: '4px'
                }}
              >
                Thông tin cá nhân
              </Link>
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

  // ✅ FIXED: Render dynamic submenu with better error handling
  const renderDynamicSubmenu = () => {
    if (loading) {
      return <div className="submenu-loading">Đang tải...</div>;
    }

    if (!isLoggedIn) {
      return <div className="submenu-empty">Vui lòng đăng nhập</div>;
    }

    if (facilities.length === 0) {
      return (
        <div className="submenu-empty">
          <div>Không có cơ sở nào</div>
          <div style={{ fontSize: '11px', marginTop: '4px', opacity: 0.7 }}>
            Tạo cơ sở trong "Thông tin chung"
          </div>
        </div>
      );
    }

    return facilities.map((facility) => {
      const facilityId = facility.facilityId || facility.id;
      const facilityName = facility.facilityName || facility.name;
      
      return (
        <div
          key={`facility-${facilityId}`}
          className={`submenu-item ${activeMenu === `facility-${facilityId}` ? "active" : ""}`}
          onClick={() => {
            handleMenuClick(
              `facility-${facilityId}`,
              // ✅ TRUYỀN CẢ facilityName VÀO URL
              `/court-owner/facilities/${facilityId}/courts?name=${encodeURIComponent(facilityName)}`
            );
            setSelectedFacilityId(facilityId);
          }}
        >
          <div className="submenu-icon-wrapper">
            <i className="fas fa-map-marker-alt"></i>
          </div>
          <span title={facilityName}>{facilityName}</span>
        </div>
      );
    });
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

  // ✅ DEBUG: Log current state
  console.log('🔍 CourtOwnerSideBar Debug:', {
    isLoggedIn,
    user: user ? { id: user.id || user.userId, name: user.name || user.fullName } : null,
    facilitiesCount: facilities.length,
    loading
  });

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