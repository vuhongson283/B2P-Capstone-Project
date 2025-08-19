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
import CourtOwnerPolicy from "./Common/CourtOwnerPolicy";
import BookingHistory from "./Common/BookingHistory";
import TimeslotManagement from "./CourtOwnerPage/TimeslotManagement";
import UnauthorizedPage from "./Common/UnauthorizedPage";
import Login from './Auth/Login';
import { AuthProvider, ProtectedRoute, PublicRoute, RoleBasedRedirect, ROLES } from "../context/AuthContext";

const Layout = (props) => {
  return (
    <AuthProvider>
      <Routes>
        {/* 🚪 PUBLIC ROUTES */}
        <Route path="/login" element={
          <PublicRoute>
            <Login />
          </PublicRoute>
        } />
        
        {/* 🚫 Unauthorized page */}
        <Route path="/unauthorized" element={<UnauthorizedPage />} />

        {/* 👑 ADMIN ROUTES - Only for Admin */}
        <Route path="/admin" element={
          <ProtectedRoute adminOnly fallbackPath="/unauthorized">
            <Admin />
          </ProtectedRoute>
        }>
          <Route index element={<AdminDashboard />} />
          <Route path="accounts" element={<AccountTable />} />
          <Route path="sliders" element={<SliderManagement />} />
          <Route path="manage-court-categories" element={<ManageCourtCategories />} />
          <Route path="user-profile" element={<UserProfile />} />
          <Route path="blog" element={<Blog />} />
        </Route>

        {/* 🏢 COURT OWNER ROUTES - Only for Court Owner */}
        <Route path="/court-owner" element={
          <ProtectedRoute courtOwnerOnly fallbackPath="/unauthorized">
            <CourtOwner />
          </ProtectedRoute>
        }>
          <Route index element={<DashboardField />} />
          <Route path="user-profile" element={<UserProfile />} />
          <Route path="search" element={<FacilitiesWithCondition />} />
          <Route path="booking-management" element={<BookingManagement />} />
          <Route path="facilities/:facilityId/courts" element={<CourtManagement />} />
          <Route path="facility/general" element={<FacilityTable />} />
          <Route path="facility/time-slots/:facilityId" element={<TimeslotManagement />} />
          <Route path="facility/time-slots" element={<TimeslotManagement />} />
          <Route path="blog" element={<Blog />} />
        </Route>

        {/* 🌐 HOME PAGE ROUTES - For Guest and Player (some routes require login) */}
        <Route path="/" element={<App />}>
          {/* 📍 PUBLIC ROUTES - Accessible by Guest and Player */}
          <Route index element={<div />} /> {/* Empty div for homepage - content handled by App.js */}
          <Route path="search" element={<FacilitiesWithCondition />} />
          <Route path="facility-details/:facilityId" element={<FacilityDetails />} />
          <Route path="blog" element={
            <ProtectedRoute playerOnly fallbackPath="/login">
              <Blog />
            </ProtectedRoute>
          } />
          <Route path="court-owner-policy" element={<CourtOwnerPolicy />} />
          <Route path="forgot-password" element={<ForgotPassword />} />
          <Route path="court-owner-register" element={<CourtOwnerRegister />} />

          {/* 🔒 PLAYER ONLY ROUTES - Require login */}
          <Route path="user-profile" element={
            <ProtectedRoute playerOnly fallbackPath="/login">
              <UserProfile />
            </ProtectedRoute>
          } />
          <Route path="booking-history" element={
            <ProtectedRoute playerOnly fallbackPath="/login">
              <BookingHistory />
            </ProtectedRoute>
          } />
          <Route path="bookingprocess" element={
            <ProtectedRoute playerOnly fallbackPath="/login">
              <BookingProcess />
            </ProtectedRoute>
          } />
          <Route path="stripepayment" element={
            <ProtectedRoute playerOnly fallbackPath="/login">
              <StripePayment />
            </ProtectedRoute>
          } />
        </Route>

        {/* 🏠 ROOT REDIRECT - Redirect to appropriate dashboard based on role */}
        <Route path="/dashboard" element={
          <ProtectedRoute>
            <RoleBasedRedirect 
              adminRedirect="/admin"
              playerRedirect="/"
              courtOwnerRedirect="/court-owner"
            />
          </ProtectedRoute>
        } />

        {/* 🚫 Catch all - redirect to home or login */}
        <Route path="*" element={
          <RoleBasedRedirect 
            adminRedirect="/admin"
            playerRedirect="/"
            courtOwnerRedirect="/court-owner"
            defaultRedirect="/"
          />
        } />
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