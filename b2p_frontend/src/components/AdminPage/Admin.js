import React from "react";
import AdminSideBar from "../SideBar/AdminSideBar";
import "./Admin.scss";
import CommonFooter from "../Footer/CommonFooter";
import { Outlet } from "react-router-dom";
const CourtOwner = (props) => {
    return (
        <div className="admin-container">
            <div className="admin-sidebar">
                <AdminSideBar />
            </div>

            <div className="admin-main">
                <div className="court-owner-content">
                    <Outlet />
                </div>
            </div>
        </div>
    );
};

export default CourtOwner;
