import logo from "./logo.svg";
import "./App.scss";
import { Outlet } from "react-router-dom";
import CommonHeader from "./components/Header/CommonHeader";
import { useLocation } from "react-router-dom";
import SliderField from "./components/HomePage/SliderField";
import CommonFooter from "./components/Footer/CommonFooter";
import FacilitiesRecommend from "./components/HomePage/FacilitiesRecommend";
import { SignalRProvider } from "./contexts/SignalRContext";
import { useEffect } from "react";

const App = (props) => {
  const location = useLocation();
  const showSliderAndSearch =
    location.pathname === "/" || location.pathname === "/homepage";

  useEffect(() => {
    console.log('🚀 [App] Component mounted');
    console.log('🚀 [App] Current location:', location.pathname);
    console.log('🚀 [App] Show slider and search:', showSliderAndSearch);
  }, [location.pathname, showSliderAndSearch]);

  return (
    <SignalRProvider>
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

                <div className="facilities-container">
                  <FacilitiesRecommend />
                </div>
              </>
            )}

            <Outlet />
          </div>
        </div>
        <div className="footer-container">
          <CommonFooter />
        </div>
      </div>
    </SignalRProvider>
  );
};

export default App;