import logo from "./logo.svg";
import "./App.scss";
import React, { useEffect, useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import CommonHeader from "./components/Header/CommonHeader";
import SliderField from "./components/HomePage/SliderField";
import CommonFooter from "./components/Footer/CommonFooter";
import FacilitiesRecommend from "./components/HomePage/FacilitiesRecommend";
import NearbyCourts from "./components/HomePage/NearbyFacilities";
import { getCurrentLocation } from "./services/locationService";
// ✅ NEW: Import Global Comment Notification Provider
import { GlobalCommentNotificationProvider } from "./contexts/GlobalCommentNotificationContext";

const App = () => {
  const location = useLocation();
  const showSliderAndSearch =
    location.pathname === "/" || location.pathname === "/homepage";

  const [userLocation, setUserLocation] = useState(null);

  // ✅ NEW: Current user info - get from localStorage or API
  const [currentUser] = useState(() => {
    const savedUser = localStorage.getItem("currentUser");
    if (savedUser) {
      return JSON.parse(savedUser);
    }
    // Fallback current user info
    return {
      userId: 26,
      fullName: "DuyQuan226",
      userName: "DuyQuan226",
      avatar:
        "https://ui-avatars.com/api/?name=DuyQuan226&background=27ae60&color=fff&size=200",
      roleId: 2,
      loginTime: "2025-08-14 07:30:27",
    };
  });

  useEffect(() => {
    if (showSliderAndSearch) {
      getUserLocation();
    }
  }, [showSliderAndSearch]);

  const getUserLocation = async () => {
    try {
      console.log("🔍 Đang xin permission location...");
      const location = await getCurrentLocation();
      console.log("✅ Lấy vị trí thành công:", location);
      setUserLocation(location);
      console.log(`📍 Vị trí: ${location.lat}, ${location.lng}`);
    } catch (error) {
      console.log("❌ Lỗi:", error.message);
      console.log("Không thể lấy vị trí: " + error.message);
    }
  };

  return (
    // ✅ NEW: Wrap entire app with Global Comment Notification Provider
    <GlobalCommentNotificationProvider currentUser={currentUser}>
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
    </GlobalCommentNotificationProvider>
  );
};

export default App;
