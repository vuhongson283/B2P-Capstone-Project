import Container from "react-bootstrap/Container";
import Nav from "react-bootstrap/Nav";
import Navbar from "react-bootstrap/Navbar";
import logo from "../../assets/images/logo3.png";
import { useAuth } from '../../context/AuthContext';
import { NavLink, useNavigate, useLocation } from "react-router-dom";
import { GiShuttlecock } from "react-icons/gi";
import { useDispatch, useSelector } from "react-redux";
import { setSearchFacility } from "../../store/action/searchFacilityAction";
import { useState, useEffect } from "react";
import UserInfo from '../Test/UserInfor';
import "./CommonHeader.scss";

const CommonHeader = (props) => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const location = useLocation();
  const searchFacility = useSelector((state) => state.searchFacility);

  // 🎯 Get auth context
  const { user, logout } = useAuth();

  // 🎯 Track active sport category
  const [activeSportCategory, setActiveSportCategory] = useState(null);

  // 🎯 Update active sport category based on location and Redux state
  useEffect(() => {
    if (location.pathname === "/search" && searchFacility?.categoryId) {
      setActiveSportCategory(parseInt(searchFacility.categoryId));
    } else {
      setActiveSportCategory(null);
    }
  }, [location.pathname, searchFacility?.categoryId]);

  // 🎯 Handle sport category navigation
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

  // 🎯 Check if sport category is active
  const isSportActive = (categoryId) => {
    return (
      location.pathname === "/search" && activeSportCategory === categoryId
    );
  };

  // 🎯 Handle logout
  const handleLogout = () => {
    logout();
    navigate("/");
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

            <button
              className={`nav-link px-3 btn-sport-link ${isSportActive(1) ? "active" : ""
                }`}
              onClick={() => handleSportNavigation(1, "bong-da")}
              type="button"
            >
              <i className="fas fa-futbol me-1"></i>
              <span className="nav-text">Bóng đá</span>
            </button>

            <span className="separator mx-2">|</span>

            <button
              className={`nav-link px-3 btn-sport-link ${isSportActive(2) ? "active" : ""
                }`}
              onClick={() => handleSportNavigation(2, "cau-long")}
              type="button"
            >
              <GiShuttlecock className="me-1" />
              <span className="nav-text">Cầu lông</span>
            </button>

            <span className="separator mx-2">|</span>

            <button
              className={`nav-link px-3 btn-sport-link ${isSportActive(3) ? "active" : ""
                }`}
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

            {/* 🎯 Show booking history only for logged in users */}
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

          {/* 🎯 Authentication Section */}
          <Nav className="ms-auto align-items-center auth-buttons">
            {user ? (
              // 🎯 Logged in user section
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
              // 🎯 Guest user section
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
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
};

export default CommonHeader;