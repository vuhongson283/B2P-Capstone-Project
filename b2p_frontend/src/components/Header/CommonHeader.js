import Container from "react-bootstrap/Container";
import Nav from "react-bootstrap/Nav";
import Navbar from "react-bootstrap/Navbar";
import logo from "../../assets/images/logo3.png";
import { NavLink, useNavigate } from "react-router-dom";
import { GiBirdClaw, GiShuttlecock } from "react-icons/gi";
const CommonHeader = (props) => {
  const navigate = useNavigate();

  return (
    <Navbar
      expand="lg"
      className="bg-body-tertiary border-bottom border-success"
      fixed="top"
    >
      <Container fluid>
        <Navbar.Brand to="/homepage" className="me-4">
          <img
            src={logo}
            width="80"
            height="40"
            className="d-inline-block align-top"
            alt="Book2Play Logo"
          />
        </Navbar.Brand>

        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <span className="text-dark mx-3">|</span>
        <Navbar.Collapse id="basic-navbar-nav">
          <Nav className="me-auto d-flex align-items-center">
            <NavLink to="/homepage" className="nav-link text-dark fw-bold px-3">
              Trang chủ
            </NavLink>

            <span className="text-dark mx-2">|</span>

            <NavLink to="/danh-cho-doi-tac" className="nav-link text-dark px-3">
              Dành cho đối tác
            </NavLink>

            <span className="text-dark mx-2">|</span>

            <NavLink to="/bong-da" className="text-dark px-3 nav-link">
              <i className="fas fa-futbol me-1"></i>
              Bóng đá
            </NavLink>

            <span className="text-dark mx-2">|</span>

            <NavLink to="/cau-long" className="text-dark px-3 nav-link">
              <GiShuttlecock className="me-1" />
              Cầu lông
            </NavLink>

            <span className="text-dark mx-2">|</span>

            <NavLink to="/pickleball" className="text-dark px-3 nav-link">
              <i className="fas fa-table-tennis me-1"></i>
              Pickleball
            </NavLink>

            <span className="text-dark mx-2">|</span>

            <NavLink to="/khu-vuc" className="text-dark px-3 nav-link">
              <i className="fas fa-map-marker-alt me-1"></i>
              Khu vực
            </NavLink>
          </Nav>

          <Nav className="ms-auto align-items-center ">
            <>
              <button
                className="btn-login btn btn-info"
                onClick={() => navigate("/login")}
              >
                Đăng Nhập
              </button>
              <span className="text-dark mx-3">|</span>

              <button
                className="btn-signup"
                onClick={() => navigate("/register")}
              >
                Đăng ký
              </button>
            </>
            {/* <NavLink to="/user" className="text-dark px-3 nav-link">
              <i className="fas fa-user me-1"></i>
              Hello, User
            </NavLink>

            <span className="text-dark mx-2">|</span>

            <Button variant="outline-dark" size="sm" className="ms-2">
              <i className="fas fa-bars"></i>
            </Button> */}
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
};

export default CommonHeader;
