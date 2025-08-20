import Container from "react-bootstrap/Container";
import Nav from "react-bootstrap/Nav";
import Navbar from "react-bootstrap/Navbar";
import logo from "../../assets/images/logo3.png";
import { useAuth } from '../../contexts/AuthContext';
import { NavLink, useNavigate, useLocation } from "react-router-dom";
import { GiShuttlecock } from "react-icons/gi";
import { useDispatch, useSelector } from "react-redux";
import { setSearchFacility } from "../../store/action/searchFacilityAction";
import { useState, useEffect } from "react";
import "./CommonHeader.scss";

// Import Ant Design components for notification
import { Badge, Button, Dropdown, Avatar, Typography, Tooltip } from 'antd';
import { BellOutlined, MessageOutlined, UserOutlined, ClockCircleOutlined } from '@ant-design/icons';

// Import comment notification context
import { useGlobalCommentNotification } from "../../contexts/GlobalCommentNotificationContext";

const { Text } = Typography;

const CommonHeader = (props) => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const location = useLocation();
  const searchFacility = useSelector((state) => state.searchFacility);

  // Get auth context
  const { user, logout } = useAuth();

  // Get comment notification context
  const { notifications, unreadCount, markAsRead, clearAll } = useGlobalCommentNotification();

  // Track active sport category
  const [activeSportCategory, setActiveSportCategory] = useState(null);

  // Update active sport category based on location and Redux state
  useEffect(() => {
    if (location.pathname === "/search" && searchFacility?.categoryId) {
      setActiveSportCategory(parseInt(searchFacility.categoryId));
    } else {
      setActiveSportCategory(null);
    }
  }, [location.pathname, searchFacility?.categoryId]);

  // Handle sport category navigation
  const handleSportNavigation = (categoryId, sportName) => {
    console.log(`Navigating to ${sportName} with category ID: ${categoryId}`);

    setActiveSportCategory(categoryId);

    dispatch(
      setSearchFacility({
        searchText: "",
        categoryId: categoryId,
        province: "",
        district: "",
        timestamp: Date.now(),
      })
    );

    if (location.pathname === "/search") {
      navigate(
        `/search?category=${categoryId}&sport=${sportName}&t=${Date.now()}`
      );
    } else {
      navigate("/search");
    }
  };

  // Check if sport category is active
  const isSportActive = (categoryId) => {
    return (
      location.pathname === "/search" && activeSportCategory === categoryId
    );
  };

  // Handle logout
  const handleLogout = () => {
    logout();
    navigate("/");
  };

  // Format notification time
  const formatNotificationTime = (timestamp) => {
    const now = Date.now();
    const diff = now - timestamp;
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'Vừa xong';
    if (minutes < 60) return `${minutes} phút trước`;
    if (hours < 24) return `${hours} giờ trước`;
    return `${days} ngày trước`;
  };

  // Recent notifications (last 5)
  const recentNotifications = notifications.slice(0, 5);

  // Notification dropdown menu
  const notificationDropdown = {
    items: [
      {
        key: 'header',
        label: (
          <div style={{
            padding: '12px 16px',
            borderBottom: '1px solid #f0f0f0',
            fontWeight: 'bold',
            color: '#1890ff',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center'
          }}>
            <span>💬 Thông báo bình luận</span>
            {unreadCount > 0 && (
              <Badge count={unreadCount} style={{ backgroundColor: '#52c41a' }} />
            )}
          </div>
        ),
        disabled: true,
      },
      ...recentNotifications.length > 0
        ? recentNotifications.map(notif => ({
          key: notif.id,
          label: (
            <div
              style={{
                padding: '12px 16px',
                borderBottom: '1px solid #f5f5f5',
                opacity: notif.read ? 0.7 : 1,
                cursor: 'pointer',
                transition: 'background-color 0.2s',
              }}
              onClick={() => {
                markAsRead(notif.id);
                // Navigate to blog comment
                navigate(`/blog#comment-${notif.data.commentId}`);
              }}
              onMouseEnter={(e) => {
                e.target.style.backgroundColor = '#f8f9fa';
              }}
              onMouseLeave={(e) => {
                e.target.style.backgroundColor = 'transparent';
              }}
            >
              <div style={{ display: 'flex', alignItems: 'flex-start', gap: '12px' }}>
                <Avatar
                  size="small"
                  icon={<UserOutlined />}
                  src={notif.data.userAvatar}
                  style={{ flexShrink: 0 }}
                />
                <div style={{ flex: 1, fontSize: '13px', lineHeight: '1.4' }}>
                  <div style={{
                    fontWeight: 'bold',
                    color: '#1890ff',
                    marginBottom: '4px'
                  }}>
                    <MessageOutlined style={{ marginRight: '4px' }} />
                    {notif.data.userName} {notif.data.isReply ? 'trả lời bình luận' : 'bình luận mới'}
                  </div>

                  <div style={{
                    color: '#666',
                    marginBottom: '4px',
                    display: '-webkit-box',
                    WebkitLineClamp: 2,
                    WebkitBoxOrient: 'vertical',
                    overflow: 'hidden'
                  }}>
                    "{notif.data.content}"
                  </div>

                  {notif.data.isReply && notif.data.parentComment && (
                    <div style={{
                      background: '#f0f2f5',
                      padding: '6px 8px',
                      borderRadius: '4px',
                      fontSize: '11px',
                      color: '#666',
                      marginBottom: '4px'
                    }}>
                      <strong>↳ Trả lời:</strong> "{notif.data.parentComment}"
                    </div>
                  )}

                  <div style={{
                    color: '#333',
                    fontSize: '12px',
                    marginBottom: '4px',
                    fontWeight: '500'
                  }}>
                    📝 {notif.data.blogTitle?.substring(0, 40)}...
                  </div>

                  <div style={{
                    color: '#999',
                    fontSize: '11px',
                    display: 'flex',
                    alignItems: 'center'
                  }}>
                    <ClockCircleOutlined style={{ marginRight: '4px' }} />
                    {formatNotificationTime(notif.timestamp)}
                  </div>
                </div>
                {!notif.read && (
                  <div style={{
                    width: '8px',
                    height: '8px',
                    backgroundColor: '#1890ff',
                    borderRadius: '50%',
                    marginTop: '6px',
                    flexShrink: 0
                  }} />
                )}
              </div>
            </div>
          ),
        }))
        : [{
          key: 'empty',
          label: (
            <div style={{
              textAlign: 'center',
              padding: '40px 20px',
              color: '#999'
            }}>
              <MessageOutlined style={{ fontSize: '32px', marginBottom: '12px', color: '#d9d9d9' }} />
              <div style={{ fontSize: '14px', fontWeight: '500' }}>Chưa có thông báo nào</div>
              <div style={{ fontSize: '12px', marginTop: '4px' }}>
                Bạn sẽ nhận thông báo khi có người bình luận vào bài viết của bạn
              </div>
            </div>
          ),
          disabled: true,
        }],
      ...(recentNotifications.length > 0 ? [
        {
          key: 'divider',
          type: 'divider',
        },
        {
          key: 'actions',
          label: (
            <div style={{
              padding: '8px 16px',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center'
            }}>
              <Button
                type="link"
                size="small"
                style={{ padding: 0, height: 'auto', color: '#1890ff' }}
                onClick={(e) => {
                  e.stopPropagation();
                  navigate('/blog');
                }}
              >
                Xem tất cả
              </Button>
              {unreadCount > 0 && (
                <Button
                  type="link"
                  size="small"
                  style={{ padding: 0, height: 'auto', color: '#666' }}
                  onClick={(e) => {
                    e.stopPropagation();
                    // Mark all as read
                    recentNotifications.forEach(notif => {
                      if (!notif.read) markAsRead(notif.id);
                    });
                  }}
                >
                  Đánh dấu đã đọc
                </Button>
              )}
            </div>
          ),
          disabled: true,
        }
      ] : [])
    ],
  };

  return (
    <Navbar
      expand="lg"
      className="custom-navbar bg-body-tertiary border-bottom border-success"
    >
      <Container fluid>
        <Navbar.Brand as={NavLink} to="/" className="me-4">
          <img
            src={logo}
            width="80"
            height="40"
            className="align-top"
            alt="Book2Play Logo"
          />
        </Navbar.Brand>

        <Navbar.Toggle aria-controls="basic-navbar-nav" />

        <Navbar.Collapse id="basic-navbar-nav">
          <Nav className="me-auto d-flex align-items-center">
            <NavLink
              to="/"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active fw-bold" : ""}`
              }
            >
              Trang chủ
            </NavLink>

            <span className="separator mx-2">|</span>

            <NavLink
              to="/court-owner-policy"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active" : ""}`
              }
            >
              Dành cho đối tác
            </NavLink>

            <span className="separator mx-2">|</span>

            {/* Blog link */}
            <NavLink
              to="/blog"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active" : ""}`
              }
            >
              <i className="fas fa-newspaper me-1"></i>
              <span className="nav-text">Blog</span>
            </NavLink>

            <span className="separator mx-2">|</span>

            {/* Sport navigation buttons */}
            <button
              className={`nav-link px-3 btn-sport-link ${isSportActive(1) ? "active" : ""}`}
              onClick={() => handleSportNavigation(1, "bong-da")}
              type="button"
            >
              <i className="fas fa-futbol me-1"></i>
              <span className="nav-text">Bóng đá</span>
            </button>

            <span className="separator mx-2">|</span>

            <button
              className={`nav-link px-3 btn-sport-link ${isSportActive(2) ? "active" : ""}`}
              onClick={() => handleSportNavigation(2, "cau-long")}
              type="button"
            >
              <GiShuttlecock className="me-1" />
              <span className="nav-text">Cầu lông</span>
            </button>

            <span className="separator mx-2">|</span>

            <button
              className={`nav-link px-3 btn-sport-link ${isSportActive(3) ? "active" : ""}`}
              onClick={() => handleSportNavigation(3, "pickleball")}
              type="button"
            >
              <i className="fas fa-table-tennis me-1"></i>
              <span className="nav-text">Pickleball</span>
            </button>

            <span className="separator mx-2">|</span>

            <NavLink
              to="/search"
              className={({ isActive }) => {
                const isKhuVucActive = isActive && !activeSportCategory;
                return `nav-link px-3 ${isKhuVucActive ? "active" : ""}`;
              }}
            >
              <i className="fas fa-map-marker-alt me-1"></i>
              <span className="nav-text">Khu vực</span>
            </NavLink>

            <span className="separator mx-2">|</span>

            {/* Show booking history only for logged in users */}
            {user && (
              <NavLink
                to="/booking-history"
                className={({ isActive }) => {
                  const isBookingActive = isActive && !activeSportCategory;
                  return `nav-link px-3 ${isBookingActive ? "active" : ""}`;
                }}
              >
                <i className="fas fa-history me-1"></i>
                <span className="nav-text">Lịch Sử Đặt Sân</span>
              </NavLink>
            )}
          </Nav>

          {/* Right side with notification bell and auth buttons */}
          <Nav className="ms-auto align-items-center">
            {/* Comment Notification Bell - Show only for logged in users */}
            {user && (
              <div className="me-3">
                <Tooltip title="Thông báo bình luận" placement="bottom">
                  <Dropdown
                    menu={notificationDropdown}
                    trigger={['click']}
                    placement="bottomRight"
                    overlayStyle={{
                      width: '380px',
                      maxHeight: '500px',
                      overflow: 'auto',
                      borderRadius: '8px',
                      boxShadow: '0 6px 16px 0 rgba(0, 0, 0, 0.08), 0 3px 6px -4px rgba(0, 0, 0, 0.12), 0 9px 28px 8px rgba(0, 0, 0, 0.05)'
                    }}
                    overlayClassName="comment-notification-dropdown"
                  >
                    <div style={{ position: 'relative', cursor: 'pointer' }}>
                      <Badge
                        count={unreadCount}
                        size="small"
                        style={{
                          backgroundColor: '#52c41a',
                          boxShadow: '0 0 0 1px #d9d9d9 inset'
                        }}
                      >
                        <div
                          style={{
                            fontSize: '20px',
                            color: unreadCount > 0 ? '#1890ff' : '#666',
                            padding: '8px',
                            borderRadius: '6px',
                            transition: 'all 0.2s',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            backgroundColor: 'transparent',
                            border: 'none'
                          }}
                          className="comment-notification-bell"
                          onMouseEnter={(e) => {
                            e.target.style.backgroundColor = '#f5f5f5';
                            e.target.style.color = '#1890ff';
                          }}
                          onMouseLeave={(e) => {
                            e.target.style.backgroundColor = 'transparent';
                            e.target.style.color = unreadCount > 0 ? '#1890ff' : '#666';
                          }}
                        >
                          <BellOutlined />
                        </div>
                      </Badge>
                    </div>
                  </Dropdown>
                </Tooltip>
              </div>
            )}

            {/* Authentication Section */}
            <div className="auth-buttons d-flex align-items-center">
              {user ? (
                // Logged in user section
                <>
                  <NavLink
                    to="/user-profile"
                    className="nav-link user-greeting px-3"
                    style={{
                      color: '#28a745',
                      fontWeight: '500',
                      textDecoration: 'none'
                    }}
                  >
                    <i className="fas fa-user-circle me-1"></i>
                    Xin chào, {user.fullName || user.username || 'User'}
                  </NavLink>
                  
                  <button
                    className="btn-logout btn btn-outline-danger ms-2"
                    onClick={handleLogout}
                    style={{
                      fontSize: '14px',
                      padding: '6px 12px'
                    }}
                  >
                    <i className="fas fa-sign-out-alt me-1"></i>
                    Đăng xuất
                  </button>
                </>
              ) : (
                // Guest user section
                <>
                  <button
                    className="btn-login btn me-2"
                    onClick={() => navigate("/login")}
                  >
                    Đăng Nhập
                  </button>

                  <button
                    className="btn-signup btn"
                    onClick={() => navigate("/register")}
                  >
                    Đăng ký
                  </button>
                </>
              )}
            </div>
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
};

export default CommonHeader;