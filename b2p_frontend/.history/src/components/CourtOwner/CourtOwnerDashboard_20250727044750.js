import React, { useEffect, useState } from "react";
import { Card, Row, Col, Button } from "react-bootstrap";
import { getReport, getTotalReport } from "../../services/apiService";
import "./OwnerDashboard.scss";

const OwnerDashboard = () => {
  const [dashboardData, setDashboardData] = useState({
    totalFacilities: 0,
    totalCourts: 0,
    totalBookings: 0,
    totalRevenue: 0,
    recentBookings: [],
  });

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const totalReportResponse = await getTotalReport(6, null, null);
        const reportResponse = await getReport(6, null, null, null, 1, 5);

        console.log("Total Report Data:", totalReportResponse.data);
        console.log("Report Data:", reportResponse.data);

        setDashboardData({
          totalFacilities: totalReportResponse.data.totalFacility || 0,
          totalCourts: totalReportResponse.data.totalCourt || 0,
          totalBookings: totalReportResponse.data.totalBooking || 0,
          totalRevenue: totalReportResponse.data.totalCost || 0,
          recentBookings: reportResponse.data.items || [],
        });
      } catch (error) {
        console.error("Error fetching dashboard data:", error);
        setError("Không thể tải dữ liệu dashboard. Vui lòng thử lại sau.");
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  if (loading) {
    return <div className="loading-spinner">Loading...</div>;
  }

  if (error) {
    return <div className="error-message">{error}</div>;
  }

  return (
    <div className="owner-dashboard">
      <h2 className="dashboard-title">Xin Chào, Nguyễn Văn A</h2>
      <p className="dashboard-subtitle">Đây là trang web quản lý dành cho chủ sân</p>
      <div className="header-actions">
      <Button variant="success" className="export-excel-btn">
        <i className="fas fa-file-excel me-2"></i>
        Export Excel
      </Button>
    </div>

      <Row className="stats-row">
        <Col md={3}>
          <Card className="stat-card">
            <Card.Body>
              <Card.Title>Số Cơ Sở Hiện Tại</Card.Title>
              <Card.Text className="stat-value">
                {dashboardData.totalFacilities}
              </Card.Text>
            </Card.Body>
          </Card>
        </Col>
        <Col md={3}>
          <Card className="stat-card">
            <Card.Body>
              <Card.Title>Tổng Số Đơn Tháng Này</Card.Title>
              <Card.Text className="stat-value">
                {dashboardData.totalBookings}
              </Card.Text>
            </Card.Body>
          </Card>
        </Col>
        <Col md={3}>
          <Card className="stat-card">
            <Card.Body>
              <Card.Title>Số Sân Hiện Tại</Card.Title>
              <Card.Text className="stat-value">
                {dashboardData.totalCourts}
              </Card.Text>
            </Card.Body>
          </Card>
        </Col>
        <Col md={3}>
          <Card className="stat-card">
            <Card.Body>
              <Card.Title>Tổng Doanh Thu</Card.Title>
              <Card.Text className="stat-value">
                {formatPrice(dashboardData.totalRevenue)}đ
              </Card.Text>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      <Card className="recent-bookings-card">
        <Card.Body>
          <Card.Title className="section-title">Đơn Đặt Sân Gần Đây</Card.Title>
          
          {dashboardData.recentBookings.length > 0 ? (
            <div className="booking-list">
              {dashboardData.recentBookings.map((booking) => (
                <div key={booking.bookingId} className="booking-item">
                  <div className="booking-info">
                    <div className="customer-info">
                      <strong>{booking.customerName}</strong>
                      <div>{booking.customerPhone}</div>
                      <div>{booking.customerEmail}</div>
                    </div>
                    <div className="booking-details">
                      <div>
                        <strong>Ngày check-in:</strong> {booking.checkInDate}
                      </div>
                      <div>
                        <strong>Số sân:</strong> {booking.timeSlotCount}
                      </div>
                      <div>
                        <strong>Loại sân:</strong> {booking.courtCategories}
                      </div>
                    </div>
                    <div className="booking-status">
                      <div className={`status-badge ${getStatusClass(booking.bookingStatus)}`}>
                        {booking.bookingStatus}
                      </div>
                      <div className="booking-price">
                        {formatPrice(booking.totalPrice)}đ
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="no-bookings">Không có đơn đặt sân gần đây</div>
          )}

          <Button variant="primary" className="view-all-btn">
            Xem Tất Cả Đơn Đặt Sân
          </Button>
        </Card.Body>
      </Card>
    </div>
  );
};

// Hàm format giá tiền
const formatPrice = (price) => {
  if (!price || price === 0) return "0";
  return parseInt(price).toLocaleString("vi-VN");
};

// Hàm xác định class CSS dựa trên trạng thái
const getStatusClass = (status) => {
  switch (status) {
    case "Đã hoàn thành":
      return "completed";
    case "Đã thanh toán":
      return "paid";
    case "Đang chờ":
      return "pending";
    case "Đã hủy":
      return "cancelled";
    default:
      return "";
  }
};

export default OwnerDashboard;