import logo from "./logo.svg";
import "./App.scss";
import { Outlet } from "react-router-dom";
import CommonHeader from "./components/Header/CommonHeader";
import { useLocation } from "react-router-dom";
import SliderField from "./components/HomePage/SliderField";
import SearchField from "./components/HomePage/SearchField";

const App = (props) => {
  const location = useLocation();
  const showSliderAndSearch =
    location.pathname === "/" || location.pathname === "/homepage";
  return (
    <div className="app-container">
      <div className="header-container">
        <CommonHeader />
      </div>
      <div className="main-container">
        <div className="app-content">
          {showSliderAndSearch && (
            <>
              <div className="slider-container">
                <SliderField />
              </div>
              <div className="search-container">
                <SearchField />
              </div>
            </>
          )}

          <Outlet />
        </div>
      </div>
      <div className="footer-container"></div>
    </div>
  );
};

export default App;
