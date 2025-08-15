import logo from "./logo.svg";
import "./App.scss";
import React, { useEffect, useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import CommonHeader from "./components/Header/CommonHeader";
import SliderField from "./components/HomePage/SliderField";
import CommonFooter from "./components/Footer/CommonFooter";
import FacilitiesRecommend from "./components/HomePage/FacilitiesRecommend";
import { SignalRProvider } from "./contexts/SignalRContext";
import NearbyCourts from "./components/HomePage/NearbyFacilities";
import { getCurrentLocation } from "./services/locationService";

const App = (props) => {
  const location = useLocation();
  const showSliderAndSearch =
    location.pathname === "/" || location.pathname === "/homepage";

  const [userLocation, setUserLocation] = useState(null);

  const getUserLocation = async () => {
    try {
      console.info("🔍 Requesting location permission");
      const location = await getCurrentLocation();
      console.info("✅ Location retrieved successfully", {
        lat: location.lat,
        lng: location.lng,
      });
      setUserLocation(location);
      alert(`Vị trí của bạn: ${location.lat}, ${location.lng}`);
    } catch (error) {
      console.error("❌ Location retrieval failed", {
        error: error.message,
        stack: error.stack,
      });
      alert("Không thể lấy vị trí: " + error.message);
    }
  };

  useEffect(() => {
    if (showSliderAndSearch) {
      getUserLocation();
    }
  }, [showSliderAndSearch]);

  useEffect(() => {
    // Extra debug logs if needed
    console.log("🚀 [App] Component mounted");
    console.log("🚀 [App] Current location:", location.pathname);
    console.log("🚀 [App] Show slider and search:", showSliderAndSearch);
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

                {userLocation && (
                  <div
                    className="nearby-facilities-container"
                    style={{ marginTop: "40px" }}
                  >
                    <NearbyCourts userLocation={userLocation} />
                  </div>
                )}
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
