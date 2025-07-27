import React, { useEffect, useState } from "react";
import { Card, Row, Col, Button } from "react-bootstrap";
import { getOwnerDashboardStats } from "../../services/apiService";
import "./OwnerDashboard.scss";

const OwnerDashboard = () => {
  const [dashboardData, setDashboardData] = useState({
    totalFacilities: 0,
    totalCourts: 0,
    monthlyOrders: 0,
    facilitiesStats: [],
  });

  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const response = await getOwnerDashboardStats();
        if (response && response.data) {
          setDashboardData({
            totalFacilities: response.data.totalFacilities || 0,
            totalCourts: response.data.totalCourts || 0,
            monthlyOrders: response.data.monthlyOrders || 0,
            facilitiesStats: response.data.facilitiesStats || [],
          });
        }
      } catch (error) {
        console.error("Error fetching dashboard data:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  if (loading) {
    return <div className="loading-spinner">Loading...</div>;
  }

  return (
    <div className="owner-dashboard">
      <h2 className="dashboard-title">Xin Chào, Vũ Văn Trọng</h2>
      <p className="dashboard-subtitle">Đây là trang web quản lý dành cho chủ sân</p>

      <Row className="stats-row">
        <Col md={4}>
          <Card className="stat-card">
            <Card.Body>
              <Card.Title>Số Cơ Sở Hiện Tại</Card.Title>
              <Card.Text className="stat-value">
                {dashboardData.totalFacilities}
              </Card.Text>
            </Card.Body>
          </Card>
        </Col>
        <Col md={4}>
          <Card className="stat-card">
            <Card.Body>
              <Card.Title>Tổng Số Đơn Tháng Này</Card.Title>
              <Card.Text className="stat-value">
                {dashboardData.monthlyOrders}
              </Card.Text>
            </Card.Body>
          </Card>
        </Col>
        <Col md={4}>
          <Card className="stat-card">
            <Card.Body>
              <Card.Title>Số Sân Hiện Tại</Card.Title>
              <Card.Text className="stat-value">
                {dashboardData.totalCourts}
              </Card.Text>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      {dashboardData.facilitiesStats.map((facility) => (
        <Card key={facility.facilityId} className="facility-stat-card">
          <Card.Body>
            <Card.Title className="facility-stat-title">
              Thống Kê Cho Cơ Sở {facility.facilityName}
            </Card.Title>
            <ul className="stat-list">
              <li>Doanh thu của cơ sở tháng này: {formatPrice(facility.monthlyRevenue)}đ</li>
              <li>Thể loại sân đã được đặt nhiều nhất: {facility.mostBookedCourtType}</li>
              <li>Tổng số đơn đặt sân: {facility.totalBookings}</li>
            </ul>
            <Button variant="primary" className="detail-button">
              Xem Chi Tiết
            </Button>
          </Card.Body>
        </Card>
      ))}
    </div>
  );
};

// Hàm format giá tiền
const formatPrice = (price) => {
  if (!price || price === 0) return "0";
  return parseInt(price).toLocaleString("vi-VN");
};

export default OwnerDashboard;