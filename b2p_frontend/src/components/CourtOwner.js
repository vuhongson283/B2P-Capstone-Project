import React from "react";
import CourtOwnerSideBar from "./SideBar/CourtOwnerSideBar";
import "./CourtOwner.scss";
import { Outlet } from "react-router-dom";
import DashboardField from "./CourtOwnerPage/CourtOwnerDashboard";

const CourtOwner = (props) => {
  return (
    <div className="court-owner-container">
      <div className="court-owner-sidebar">
        <CourtOwnerSideBar />
      </div>

      <div className="court-owner-main">
        <div className="court-owner-content">
          {/* Main content for Court Owner */}
          <Outlet />
        </div>
      </div>
    </div>
  );
};

export default CourtOwner;
