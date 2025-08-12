import logo from "./logo.svg";
import "./App.scss";
import React, { useEffect, useState } from "react";
import { Outlet } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext"; // ✅ Import AuthProvider
import CommonHeader from "./components/Header/CommonHeader";
import { useLocation } from "react-router-dom";
import SliderField from "./components/HomePage/SliderField";
import CommonFooter from "./components/Footer/CommonFooter";
import FacilitiesRecommend from "./components/HomePage/FacilitiesRecommend";
import NearbyCourts from "./components/HomePage/NearbyFacilities";
import { getCurrentLocation } from "./services/locationService";

const AppContent = (props) => {
  const location = useLocation();
  const showSliderAndSearch =
    location.pathname === "/" || location.pathname === "/homepage";
  
  const [userLocation, setUserLocation] = useState(null);

  useEffect(() => {
    if (showSliderAndSearch) {
      getUserLocation();
    }
  }, [showSliderAndSearch]);

  const getUserLocation = async () => {
    try {
      console.log('🔍 Đang xin permission location...');
      const location = await getCurrentLocation();
      console.log('✅ Lấy vị trí thành công:', location);
      setUserLocation(location);
      alert(`Vị trí của bạn: ${location.lat}, ${location.lng}`);
    } catch (error) {
      console.log('❌ Lỗi:', error.message);
      alert('Không thể lấy vị trí: ' + error.message);
    }
  };

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

              <div className="facilities-container">
                <FacilitiesRecommend />
              </div>

              {userLocation && (
                <div className="nearby-facilities-container" style={{ marginTop: '40px' }}>
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
  );
};

// ✅ Main App component với AuthProvider
const App = () => {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
};

export default App;