import Container from "react-bootstrap/Container";
import Nav from "react-bootstrap/Nav";
import Navbar from "react-bootstrap/Navbar";
import logo from "../../assets/images/logo3.png";
import { NavLink, useNavigate } from "react-router-dom";
import { GiShuttlecock } from "react-icons/gi";
import "./CommonHeader.scss";

const CommonHeader = (props) => {
  const navigate = useNavigate();

  return (
    <Navbar
      expand="lg"
      className="custom-navbar bg-body-tertiary border-bottom border-success"
    >
      <Container fluid>
        <Navbar.Brand as={NavLink} to="/homepage" className="me-4">
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
              to="/homepage"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active fw-bold" : ""}`
              }
            >
              Trang chủ
            </NavLink>

            <span className="separator mx-2">|</span>

            <NavLink
              to="/danh-cho-doi-tac"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active" : ""}`
              }
            >
              Dành cho đối tác
            </NavLink>

            <span className="separator mx-2">|</span>

            <NavLink
              to="/bong-da"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active" : ""}`
              }
            >
              <i className="fas fa-futbol me-1"></i>
              <span className="nav-text">Bóng đá</span>
            </NavLink>

            <span className="separator mx-2">|</span>

            <NavLink
              to="/cau-long"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active" : ""}`
              }
            >
              <GiShuttlecock className="me-1" />
              <span className="nav-text">Cầu lông</span>
            </NavLink>

            <span className="separator mx-2">|</span>

            <NavLink
              to="/pickleball"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active" : ""}`
              }
            >
              <i className="fas fa-table-tennis me-1"></i>
              <span className="nav-text">Pickleball</span>
            </NavLink>

            <span className="separator mx-2">|</span>

            <NavLink
              to="/khu-vuc"
              className={({ isActive }) =>
                `nav-link px-3 ${isActive ? "active" : ""}`
              }
            >
              <i className="fas fa-map-marker-alt me-1"></i>
              <span className="nav-text">Khu vực</span>
            </NavLink>
          </Nav>

          <Nav className="ms-auto align-items-center auth-buttons">
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
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
};

export default CommonHeader;
