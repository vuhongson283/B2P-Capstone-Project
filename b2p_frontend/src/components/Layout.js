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
import PaymentManager from "./CourtOwnerPage/PaymentManager.js";
import CourtOwnerPolicy from "./Common/CourtOwnerPolicy";
import BookingHistory from "./Common/BookingHistory";
import TimeslotManagement from "./CourtOwnerPage/TimeslotManagement";
import UnauthorizedPage from "./Common/UnauthorizedPage.js";
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

        {/* 🏃‍♂️ PLAYER ROUTES */}
        <Route path="/" element={
          <ProtectedRoute playerOnly fallbackPath="/unauthorized">
            <App />
          </ProtectedRoute>
        }>
          {/* Player dashboard - redirect to search as default */}
          <Route index element={<FacilitiesWithCondition />} />
          <Route path="search" element={<FacilitiesWithCondition />} />
          <Route path="user-profile" element={<UserProfile />} />
          <Route path="blog" element={<Blog />} />
          <Route path="booking-history" element={<BookingHistory />} />
          <Route path="facility-details/:facilityId" element={<FacilityDetails />} />
          <Route path="bookingprocess" element={<BookingProcess />} />
          <Route path="stripepayment" element={<StripePayment />} />
        </Route>

        {/* 🏢 COURT OWNER ROUTES */}
        <Route path="/court-owner" element={
          <ProtectedRoute courtOwnerOnly fallbackPath="/unauthorized">
            <CourtOwner />
          </ProtectedRoute>
        }>
          <Route index element={<DashboardField />} />
          <Route path="user-profile" element={<UserProfile />} />
          <Route path="search" element={<FacilitiesWithCondition />} />
          <Route path="booking-management" element={<BookingManagement />} />
          <Route path="payment-management" element={<PaymentManager />} />
          <Route path="facilities/:facilityId/courts" element={<CourtManagement />} />
          <Route path="facility/general" element={<FacilityTable />} />
          <Route path="facility/time-slots/:facilityId" element={<TimeslotManagement />} />
          <Route path="facility/time-slots" element={<TimeslotManagement />} />
        </Route>

        {/* 👑 ADMIN ROUTES */}
        <Route path="/admin" element={
          <ProtectedRoute adminOnly fallbackPath="/unauthorized">
            <Admin />
          </ProtectedRoute>
        }>
          <Route index element={<AdminDashboard />} />
          <Route path="accounts" element={<AccountTable />} />
          <Route path="sliders" element={<SliderManagement />} />
          <Route path="manage-court-categories" element={<ManageCourtCategories />} />
        </Route>

        {/* 🌐 SHARED PUBLIC ROUTES (accessible when logged in) */}
        <Route path="/" element={
          <ProtectedRoute>
            <App />
          </ProtectedRoute>
        }>
          <Route path="court-owner-register" element={<CourtOwnerRegister />} />
          <Route path="court-owner-policy" element={<CourtOwnerPolicy />} />
          <Route path="forgot-password" element={<ForgotPassword />} />
        </Route>

        {/* 🔄 ALTERNATIVE: Routes accessible by multiple roles */}
        <Route path="/" element={
          <ProtectedRoute requiredRoles={[ROLES.ADMIN, ROLES.COURTOWNER]}>
            <App />
          </ProtectedRoute>
        }>
          <Route path="facility-details/:facilityId" element={<FacilityDetails />} />
          <Route path="search" element={<FacilitiesWithCondition />} />
        </Route>

        {/* 🏠 ROOT REDIRECT - Redirect to appropriate dashboard based on role */}
        <Route path="/" element={
          <ProtectedRoute>
            <RoleBasedRedirect
              adminRedirect="/admin"
              playerRedirect="/player"
              courtOwnerRedirect="/court-owner"
            />
          </ProtectedRoute>
        } />

        {/* 🚫 Catch all - redirect to appropriate dashboard */}
        <Route path="*" element={
          <ProtectedRoute fallbackPath="/login">
            <RoleBasedRedirect
              adminRedirect="/admin"
              playerRedirect="/"
              courtOwnerRedirect="/court-owner"
              defaultRedirect="/login"
            />
          </ProtectedRoute>
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