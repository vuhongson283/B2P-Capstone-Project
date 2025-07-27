import { BrowserRouter, Routes, Route } from "react-router-dom";
import "react-toastify/dist/ReactToastify.css";
import App from "../App";
import { ToastContainer } from "react-toastify";
import FacilitiesWithCondition from "./HomePage/FacilitiesWithCondition";
import FacilityDetails from "./HomePage/FacilityDetails";
import UserProfile from "./Common/UserProfile";
import ForgotPassword from "./Common/ForgotPassword";
import CourtOwner from "./CourtOwnerPage/CourtOwner";
import Admin from "./AdminPage/Admin";
import AccountTable from "./AdminPage/AccountTable";
import ManageCourtCategories from "./AdminPage/ManageCourtCategories";
import Blog from "./Common/Blog";
import FacilityTable from "./CourtOwnerPage/FacilityTable";
import SliderManagement from "./AdminPage/SliderManagement";
import CourtOwnerRegister from "./CourtOwnerRegister/CourtOwnerRegister";
import DashboardField from "./CourtOwnerPage/CourtOwnerDashboard";
import CourtManagement from "./CourtOwnerPage/CourtManagement";

const Layout = (props) => {
  return (
    <>
      <Routes>
        <Route path="/" element={<App />}>
          <Route path="/search" element={<FacilitiesWithCondition />} />
          <Route path="/courtowner-register" element={<CourtOwnerRegister />} />
          <Route path="/user-profile" element={<UserProfile />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/blog" element={<Blog />} />
          <Route path="/facility-details" element={<FacilityDetails />} />
        </Route>

        <Route path="/court-owner" element={<CourtOwner />}>
          <Route index element={<DashboardField />} />
          <Route
            path="/court-owner/search"
            element={<FacilitiesWithCondition />}
          />
          <Route path="facilities/:facilityId/courts" element={<CourtManagement />} />
        </Route>
        <Route path="/court-owner" element={<CourtOwner />}>
          <Route
            path="/court-owner/search"
            element={<FacilitiesWithCondition />}  
          />
          <Route path="facility/general" element={<FacilityTable />} />
        </Route>
        <Route path="/admin" element={<Admin />}>
          <Route path="accounts" element={<AccountTable />} />
          <Route path="sliders" element={<SliderManagement />} />
          <Route
            path="manage-court-categories"
            element={<ManageCourtCategories />}
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
