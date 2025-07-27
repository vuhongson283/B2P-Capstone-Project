import React, { useEffect, useState } from "react";
import { Card, Row, Col, Button } from "react-bootstrap";
import { getReport, getTotalReport, exportReportToExcel } from "../../services/apiService";
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
  const [exportLoading, setExportLoading] = useState(false);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const totalReportResponse = await getTotalReport(6, null, null);
        const reportResponse = await getReport(6, null, null, null, 1, 5);

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

  const handleExportExcel = async () => {
    setExportLoading(true);
    try {
      const response = await exportReportToExcel(
        6,       // userId
        null,     // startDate 
        null,     // endDate
        null,     // facilityId
        1,        // pageNumber
        10        // pageSize
      );
      
      // Tạo URL tạm thời cho file blob
      const url = window.URL.createObjectURL(new Blob([response.data]));
      
      // Tạo thẻ a để tải file
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'bao_cao_don_dat_san.xlsx');
      document.body.appendChild(link);
      link.click();
      
      // Dọn dẹp
      link.parentNode.removeChild(link);
      window.URL.revokeObjectURL(url);
      
    } catch (error) {
      console.error('Lỗi khi export excel:', error);
      alert('Xuất file Excel thất bại. Vui lòng thử lại!');
    } finally {
      setExportLoading(false);
    }
  };

  const handleViewDetail = (bookingId) => {
    // Xử lý khi click vào nút Xem chi tiết
    console.log("Xem chi tiết booking:", bookingId);
    // Có thể dùng navigate để chuyển trang chi tiết
    // navigate(`/bookings/${bookingId}`);
    alert(`Xem chi tiết đơn đặt sân #${bookingId}`);
  };

  if (loading) {
    return <div className="loading-spinner">Loading...</div>;
  }

  if (error) {
    return <div className="error-message">{error}</div>;
  }

  return (
    <div className="owner-dashboard">
      <div className="dashboard-header">
        <div>
          <h2 className="dashboard-title">Xin Chào, Nguyễn Văn A</h2>
          <p className="dashboard-subtitle">Đây là trang web quản lý dành cho chủ sân</p>
        </div>
        <Button 
          variant="success" 
          className="export-excel-btn mb-3"
          onClick={handleExportExcel}
          disabled={exportLoading}
        >
          {exportLoading ? (
            <>
              <i className="fas fa-spinner fa-spin me-2"></i>
              Đang xuất file...
            </>
          ) : (
            <>
              <i className="fas fa-file-excel me-2"></i>
              Export Excel
            </>
          )}
        </Button>
      </div>

      <Row className="stats-row">
        {/* Các thẻ thống kê giữ nguyên */}
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
          <div className="card-header">
            <Card.Title className="section-title">Đơn Đặt Sân Gần Đây</Card.Title>
          </div>
          
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
                  <div className="booking-actions d-flex justify-content-center mt-3">
                    <Button 
                      variant="success" 
                      className="detail-btn"
                    >
                      <i className="fas fa-eye me-2"></i>
                      Xem chi tiết
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="no-bookings">Không có đơn đặt sân gần đây</div>
          )}
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