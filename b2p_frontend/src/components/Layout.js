import { BrowserRouter, Routes, Route } from "react-router-dom";
import "react-toastify/dist/ReactToastify.css";
import { ConfigProvider, App as AntdApp } from 'antd';
import 'antd/dist/reset.css'; 
import App from "../App";
import { ToastContainer } from "react-toastify";
import FacilitiesWithCondition from "./HomePage/FacilitiesWithCondition";
import FacilityDetails from "./HomePage/FacilityDetails";
import StripePayment from './HomePage/StripePayment.js';
import BookingProcess from "./HomePage/BookingProcess";
import UserProfile from "./Common/UserProfile";
import ForgotPassword from "./Common/ForgotPassword";
import CourtOwner from "./CourtOwnerPage/CourtOwner";
import Admin from "./AdminPage/Admin";
import AdminDashboard from "./AdminPage/AdminDashboard";
import AccountTable from "./AdminPage/AccountTable";
import ManageCourtCategories from "./AdminPage/ManageCourtCategories";
import Blog from "./Common/Blog";
import FacilityTable from "./CourtOwnerPage/FacilityTable";
import SliderManagement from "./AdminPage/SliderManagement";
import CourtOwnerRegister from "./CourtOwnerRegister/CourtOwnerRegister";
import DashboardField from "./CourtOwnerPage/CourtOwnerDashboard";
import BookingManagement from "./CourtOwnerPage/BookingManagement"
import CourtManagement from "./CourtOwnerPage/CourtManagement";
import PaymentManager from "./CourtOwnerPage/PaymentManager";
import CourtOwnerPolicy from "./Common/CourtOwnerPolicy";
import BookingHistory from "./Common/BookingHistory";
import TimeslotManagement from "./CourtOwnerPage/TimeslotManagement";
import Login from './Auth/Login';
import { AuthProvider } from "../context/AuthContext";

const Layout = (props) => {
  return (
    
    <AuthProvider>
      {/* ✅ WRAP tất cả Routes trong 1 Routes element */}
      <Routes>
        {/* ✅ Login route - standalone */}
        <Route path="/login" element={<Login />} />
        
        {/* ✅ Main App routes với nested routes */}
        <Route path="/" element={<App />}>
          <Route path="search" element={<FacilitiesWithCondition />} />
          <Route path="court-owner-register" element={<CourtOwnerRegister />} />
          <Route path="court-owner-policy" element={<CourtOwnerPolicy />} />
          <Route path="user-profile" element={<UserProfile />} />
          <Route path="forgot-password" element={<ForgotPassword />} />
          <Route path="blog" element={<Blog />} />
          <Route path="booking-history" element={<BookingHistory />} />
          <Route
            path="facility-details/:facilityId"
            element={<FacilityDetails />}
          />
          <Route
            path="/bookingprocess"
            element={
              <BookingProcess />
            }
          />
           <Route
    path="stripepayment"
    element={<StripePayment />}
  />
        </Route>

        {/* ✅ Court Owner routes */}
        <Route path="/court-owner" element={<CourtOwner />}>
          <Route index element={<DashboardField />} />
          <Route path="search" element={<FacilitiesWithCondition />} />
          <Route path="booking-management" element={<BookingManagement />} />
          <Route path="payment-management" element={<PaymentManager />} />
          <Route path="facilities/:facilityId/courts" element={<CourtManagement />} />
          <Route path="facility/general" element={<FacilityTable />} />
          <Route path="facility/time-slots/:facilityId" element={<TimeslotManagement />} />
          <Route path="facility/time-slots" element={<TimeslotManagement />} />
        </Route>

        {/* ✅ Admin routes */}
        <Route path="/admin" element={<Admin />}>
        <Route index element={<AdminDashboard />} />
          <Route path="accounts" element={<AccountTable />} />
          <Route path="sliders" element={<SliderManagement />} />
          <Route path="manage-court-categories" element={<ManageCourtCategories />} />
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
    </AuthProvider>
  );
};

export default Layout;