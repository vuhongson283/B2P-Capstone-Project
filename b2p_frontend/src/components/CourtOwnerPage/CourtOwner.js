import React from "react";
import CourtOwnerSideBar from "../SideBar/CourtOwnerSideBar";
import "./CourtOwner.scss";
import { Outlet } from "react-router-dom";

const CourtOwner = (props) => {
  return (
    <div className="court-owner-container">
      <div className="court-owner-sidebar">
        <CourtOwnerSideBar />
      </div>

      <div className="court-owner-main">
        <div className="court-owner-content">
          {/* Main content for Court Owner */}
          <h1>Court Owner Dashboard</h1>
          <p>Chọn một mục từ menu để bắt đầu quản lý.</p>
          <Outlet />
        </div>
      </div>
    </div>
  );
};

export default CourtOwner;
