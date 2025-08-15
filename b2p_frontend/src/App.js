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

// ✅ MERGED: Import both providers
import { SignalRProvider } from "./contexts/SignalRContext";
import { GlobalCommentNotificationProvider } from "./contexts/GlobalCommentNotificationContext";

const App = () => {
  const location = useLocation();
  const showSliderAndSearch =
    location.pathname === "/" || location.pathname === "/homepage";

  const [userLocation, setUserLocation] = useState(null);

  // ✅ MERGED: Current user info from branch 1
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

  // ✅ MERGED: Better getUserLocation function (from branch 1 but with improved error handling)
  const getUserLocation = async () => {
    try {
      console.log("🔍 Đang xin permission location...");
      const location = await getCurrentLocation();
      console.log("✅ Lấy vị trí thành công:", location);
      setUserLocation(location);
      // ✅ IMPROVED: Use console.log instead of alert (less intrusive)
      console.log(`📍 Vị trí: ${location.lat}, ${location.lng}`);
    } catch (error) {
      console.error('❌ Lỗi khi lấy vị trí:', {
        error: error.message,
        stack: error.stack,
      });
      // ✅ IMPROVED: Keep alert for important location errors
      console.warn("Không thể lấy vị trí: " + error.message);
    }
  };

  // ✅ MERGED: Debug logging from branch 2
  useEffect(() => {
    console.log("🚀 [App] Component mounted");
    console.log("🚀 [App] Current location:", location.pathname);
    console.log("🚀 [App] Show slider and search:", showSliderAndSearch);
  }, [location.pathname, showSliderAndSearch]);

  return (
    // ✅ MERGED: Nested providers - SignalR outer, GlobalCommentNotification inner
    // This ensures SignalR is available throughout the app, and comment notifications have access to SignalR
    <SignalRProvider>
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
    </SignalRProvider>
  );
};

export default App;