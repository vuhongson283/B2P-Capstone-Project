import React, { useEffect, useState } from "react";
import { Card, Row, Col, Button, Modal } from "react-bootstrap";
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
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [selectedBooking, setSelectedBooking] = useState(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const totalReportResponse = await getTotalReport(6, null, null);
        const reportResponse = await getReport(6, null, null, null, 1, 10);

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
      const response = await exportReportToExcel(6, null, null, null, 1);
      console.log("Response data:", response);
      // Kiểm tra response
      if (!response || response.byteLength === 0) {
        throw new Error('Dữ liệu file Excel trống');
      }

      // Kiểm tra magic number của file Excel (định dạng chuẩn)
      const data = new Uint8Array(response.data);
      if (data[0] !== 0x50 || data[1] !== 0x4B) { // PK header (zip/xlsx)
        throw new Error('Dữ liệu không phải file Excel hợp lệ');
      }

      const blob = new Blob([response.data], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });

      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'report.xlsx');
      document.body.appendChild(link);
      link.click();

      setTimeout(() => {
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      }, 100);

      alert('Xuất file Excel thành công!');
    } catch (error) {
      console.error('Lỗi khi export excel:', error);
      alert('Xuất file Excel thất bại: ' + (error.message || 'Lỗi không xác định'));
    } finally {
      setExportLoading(false);
    }
  };

  const handleViewDetail = (booking) => {
    setSelectedBooking(booking);
    setShowDetailModal(true);
  };

  const BookingDetailModal = ({ booking, show, onHide }) => {
    if (!booking) return null;
    
    return (
      <Modal show={show} onHide={onHide} size="lg">
        <Modal.Header closeButton className="detail-header">
          <Modal.Title>
            <i className="fas fa-info-circle me-2"></i>
            Chi tiết đơn đặt sân #{booking.bookingId}
          </Modal.Title>
        </Modal.Header>
        <Modal.Body className="booking-detail-body">
          <div className="booking-detail-content">
            <div className="detail-section">
              <h5><i className="fas fa-user me-2"></i>Thông tin khách hàng</h5>
              <Row>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Tên khách hàng:</label>
                    <span>{booking.customerName}</span>
                  </div>
                  <div className="detail-item">
                    <label>Email:</label>
                    <span>{booking.customerEmail}</span>
                  </div>
                </Col>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Số điện thoại:</label>
                    <span>{booking.customerPhone}</span>
                  </div>
                </Col>
              </Row>
            </div>

            <div className="detail-section">
              <h5><i className="fas fa-calendar-alt me-2"></i>Thông tin đặt sân</h5>
              <Row>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Ngày check-in:</label>
                    <span>{booking.checkInDate}</span>
                  </div>
                  <div className="detail-item">
                    <label>Thời gian đặt:</label>
                    <span>{new Date(booking.bookingTime).toLocaleString('vi-VN')}</span>
                  </div>
                </Col>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Số lượt đặt:</label>
                    <span>{booking.timeSlotCount}</span>
                  </div>
                </Col>
              </Row>
              <div className="detail-item">
                <label>Loại sân:</label>
                <span>{booking.courtCategories}</span>
              </div>
            </div>

            <div className="detail-section">
              <h5><i className="fas fa-money-bill-wave me-2"></i>Thông tin thanh toán</h5>
              <Row>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Tổng tiền:</label>
                    <span className="price">{formatPrice(booking.totalPrice)}đ</span>
                  </div>
                  <div className="detail-item">
                    <label>Trạng thái đặt sân:</label>
                    <span className={`status-badge ${getStatusClass(booking.bookingStatus)}`}>
                      {booking.bookingStatus}
                    </span>
                  </div>
                </Col>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Số tiền đã thanh toán:</label>
                    <span className="price">{formatPrice(booking.paymentAmount)}đ</span>
                  </div>
                  <div className="detail-item">
                    <label>Trạng thái thanh toán:</label>
                    <span className={`status-badge ${getStatusClass(booking.paymentStatus)}`}>
                      {booking.paymentStatus}
                    </span>
                  </div>
                </Col>
              </Row>
              {booking.paymentTime && (
                <div className="detail-item">
                  <label>Thời gian thanh toán:</label>
                  <span>{new Date(booking.paymentTime).toLocaleString('vi-VN')}</span>
                </div>
              )}
            </div>
          </div>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={onHide}>
            <i className="fas fa-times me-2"></i>
            Đóng
          </Button>
        </Modal.Footer>
      </Modal>
    );
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
                        <strong>Lượt đặt sân:</strong> {booking.timeSlotCount}
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
                      onClick={() => handleViewDetail(booking)}
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

      <BookingDetailModal 
        show={showDetailModal}
        onHide={() => setShowDetailModal(false)}
        booking={selectedBooking}
      />
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