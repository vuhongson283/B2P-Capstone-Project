import { BrowserRouter, Routes, Route } from "react-router-dom";
import "react-toastify/dist/ReactToastify.css";
import App from "../App";
import { ToastContainer } from "react-toastify";
import FacilitiesWithCondition from "./HomePage/FacilitiesWithCondition";
import FacilityDetails from "./HomePage/FacilityDetails";
import BookingProcess from "./HomePage/BookingProcess";
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
import BookingManagement from "./CourtOwnerPage/BookingManagement"
import CourtManagement from "./CourtOwnerPage/CourtManagement";
import CourtOwnerPolicy from "./Common/CourtOwnerPolicy";
import BookingHistory from "./Common/BookingHistory";
import TimeslotManagement from "./CourtOwnerPage/TimeslotManagement";



const Layout = (props) => {
  return (
    <>
      <Routes>
        <Route path="/" element={<App />}>
          <Route path="/search" element={<FacilitiesWithCondition />} />
          <Route path="/court-owner-register" element={<CourtOwnerRegister />} />
          <Route path="/court-owner-policy" element={<CourtOwnerPolicy />} />
          <Route path="/user-profile" element={<UserProfile />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/blog" element={<Blog />} />
          <Route path="/booking-history" element={<BookingHistory />} />
          <Route
            path="/facility-details/:facilityId"
            element={<FacilityDetails />}
          />
          <Route
            path="/bookingprocess"
            element={
              <BookingProcess />
            }
          />
        </Route>

        <Route path="/court-owner" element={<CourtOwner />}>
          <Route index element={<DashboardField />} />
          <Route
            path="/court-owner/search"
            element={<FacilitiesWithCondition />}
          />
          <Route
            path="/court-owner/booking-management"
            element={<BookingManagement />}
          />
          <Route
            path="facilities/:facilityId/courts"
            element={<CourtManagement />}
          />
        </Route>
        <Route path="/court-owner" element={<CourtOwner />}>
          <Route
            path="/court-owner/search"
            element={<FacilitiesWithCondition />}
          />
          <Route path="facility/general" element={<FacilityTable />} />
          <Route path="facility/time-slots/:facilityId" element={<TimeslotManagement />} />
          <Route path="facility/time-slots" element={<TimeslotManagement />} />
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