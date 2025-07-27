import { BrowserRouter, Routes, Route } from "react-router-dom";
import "react-toastify/dist/ReactToastify.css";
import App from "../App";
import { ToastContainer } from "react-toastify";
import FacilitiesWithCondition from "./HomePage/FacilitiesWithCondition";
import UserProfile from "./Common/UserProfile";
import ForgotPassword from "./Common/ForgotPassword";
import CourtOwner from "./CourtOwner"; // Assuming you have a CourtOwner component
import CourtOwnerSideBar from "./SideBar/CourtOwnerSideBar";
import DashboardField from "./CourtOwner/CourtOwnerDashboard";
const Layout = (props) => {
  return (
    <>
      <Routes>
        <Route path="/" element={<App />}>
          <Route path="/search" element={<FacilitiesWithCondition />} />
          <Route path="/user-profile" element={<UserProfile />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
        </Route>

        <Route path="/court-owner" element={<CourtOwner />}>
        <Route index element={<DashboardField />} />
          <Route
            path="/court-owner/search"
            element={<FacilitiesWithCondition />}
          />
        </Route>
      </Routes>

      <ToastContainer
        position="top-right"
        autoClose={5000}
        hideProgressBar={false}
        newestOnTop={false}
        closeOnClick={false}
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
        theme="light"
      />
    </>
  );
};

export default Layout;
