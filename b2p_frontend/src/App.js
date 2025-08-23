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

// ✅ Import providers
import { SignalRProvider } from "./contexts/SignalRContext";
import { CustomerSignalRProvider } from "./contexts/CustomerSignalRContext"; // ✅ NEW: Add this
import { GlobalCommentNotificationProvider } from "./contexts/GlobalCommentNotificationContext";

// ✅ Import useAuth hook
import { useAuth } from "./contexts/AuthContext";

const App = () => {
  const location = useLocation();

  // ✅ Get user from AuthContext instead of hardcoding
  const { user, isLoading } = useAuth();

  // ✅ FIXED: Only show slider/search on exact homepage, not on child routes
  const showSliderAndSearch = location.pathname === "/";

  const [userLocation, setUserLocation] = useState(null);

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
      console.error('❌ Lỗi khi lấy vị trí:', {
        error: error.message,
        stack: error.stack,
      });
      console.warn("Không thể lấy vị trí: " + error.message);
    }
  };

  // ✅ Debug logging
  useEffect(() => {
    console.log("🚀 [App] Component mounted");
    console.log("🚀 [App] Current location:", location.pathname);
    console.log("🚀 [App] Show slider and search:", showSliderAndSearch);
    console.log("🚀 [App] Auth loading:", isLoading);
    console.log("🚀 [App] Current user from AuthContext:", user);

    if (user) {
      console.log("✅ [App] User loaded from AuthContext:");
      console.log("  - User ID:", user.userId);
      console.log("  - Username:", user.userName || user.username);
      console.log("  - Full Name:", user.fullName || user.name);
      console.log("  - Role ID:", user.roleId);
      console.log("  - Role Name:", user.roleName);
    } else if (!isLoading) {
      console.log("⚠️ [App] No user found (not logged in)");
    }
  }, [location.pathname, showSliderAndSearch, user, isLoading]);

  // ✅ Show loading while AuthContext is loading
  if (isLoading) {
    return (
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
        fontSize: '16px'
      }}>
        🔄 Đang tải thông tin người dùng...
      </div>
    );
  }

  // ✅ Create currentUser object for SignalR (normalize the structure)
  const currentUser = user ? {
    userId: user.userId || user.id,
    fullName: user.fullName || user.name || user.userName || user.username,
    userName: user.userName || user.username,
    avatar: user.avatar || user.profilePicture ||
      `https://ui-avatars.com/api/?name=${encodeURIComponent(user.fullName || user.userName || user.username || 'User')}&background=27ae60&color=fff&size=200`,
    roleId: user.roleId,
    roleName: user.roleName,
    loginTime: new Date().toISOString(),
  } : null;

  return (
    <SignalRProvider>
      {/* ✅ NEW: Add CustomerSignalRProvider wrapper */}
      <CustomerSignalRProvider>
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
      </CustomerSignalRProvider>
      {/* ✅ END: CustomerSignalRProvider wrapper */}
    </SignalRProvider>
  );
};

export default App;