import React, { useEffect, useState } from "react";
import { Card, Row, Col, Button, Modal, Form } from "react-bootstrap";
import { DatePicker } from "antd";
import dayjs from "dayjs";
import { useAuth } from '../../contexts/AuthContext';
import {
  getReport,
  getTotalReport,
  exportReportToExcel,
  checkCommission, // ✅ THÊM MỚI
} from "../../services/apiService";
import "./CourtOwnerDashboard.scss";
import { Chart as ChartJS, registerables } from 'chart.js';
import { Bar, Pie } from 'react-chartjs-2';
import { Pagination } from "antd";
ChartJS.register(...registerables);

const { RangePicker } = DatePicker;

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
  const [selectedMonth, setSelectedMonth] = useState(() => {
    // Mặc định là tháng hiện tại
    return new Date();
  });
  const { userId, user, isLoggedIn, isLoading: authLoading } = useAuth();

  // Thêm state cho phân trang booking
  const [bookingPagination, setBookingPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 0,
  });

  // ✅ THÊM STATE CHO COMMISSION
  const [commissionExists, setCommissionExists] = useState(true); // Mặc định ẩn button
  const [checkingCommission, setCheckingCommission] = useState(false);

  // ✅ THÊM STATE CHO COMMISSION MODAL
  const [showCommissionModal, setShowCommissionModal] = useState(false);

  // Hàm lấy ngày đầu và cuối tháng từ selectedMonth
  const getMonthRange = (dateObj) => {
    const year = dateObj.getFullYear();
    const month = dateObj.getMonth();
    const start = new Date(year, month, 1, 0, 0, 0, 0);
    const end = new Date(year, month + 1, 0, 23, 59, 59, 999);
    return [start, end];
  };

  // ✅ HÀM KIỂM TRA COMMISSION
  const checkCommissionStatus = async () => {
    if (!userId || !selectedMonth) return;

    try {
      setCheckingCommission(true);
      const month = selectedMonth.getMonth() + 1; // getMonth() trả về 0-11
      const year = selectedMonth.getFullYear();

      console.log(`🔍 Checking commission for userId: ${userId}, month: ${month}, year: ${year}`);

      const response = await checkCommission(userId, month, year);

      console.log('📊 Commission check response:', response);

      if (response && response.data) {
        setCommissionExists(response.data.exists);
        console.log(`✅ Commission exists: ${response.data.exists}`);
      } else {
        setCommissionExists(false);
        console.log('❌ No response data, setting commission exists to false');
      }
    } catch (error) {
      console.error("Error checking commission:", error);
      setCommissionExists(false); // Nếu lỗi, hiển thị button
    } finally {
      setCheckingCommission(false);
    }
  };

  useEffect(() => {
    const fetchDashboardData = async () => {
      setError(null);
      setLoading(true);

      try {
        // Lấy ngày đầu và cuối tháng
        const [start, end] = getMonthRange(selectedMonth);

        // ✅ KIỂM TRA COMMISSION TRƯỚC KHI FETCH DATA
        await checkCommissionStatus();

        // Fetch total report
        const totalReportResponse = await getTotalReport(userId, start, end);

        if (totalReportResponse && totalReportResponse.success) {
          setDashboardData((prev) => ({
            ...prev,
            totalFacilities: totalReportResponse.data?.totalFacility || 0,
            totalCourts: totalReportResponse.data?.totalCourt || 0,
            totalBookings: totalReportResponse.data?.totalBooking || 0,
            totalRevenue: totalReportResponse.data?.totalCost || 0,
            commissionPayment: totalReportResponse.data?.commissionPayment || 0
          }));
        } else {
          setError(totalReportResponse?.message || "Không thể tải dữ liệu tổng quan");
        }

        // Fetch recent bookings với phân trang
        try {
          const reportResponse = await getReport(
            userId,
            start,
            end,
            null,
            bookingPagination.pageNumber,
            bookingPagination.pageSize
          );

          if (reportResponse && reportResponse.success) {
            setDashboardData((prev) => ({
              ...prev,
              recentBookings: reportResponse.data?.items || [],
            }));
            setBookingPagination((prev) => ({
              ...prev,
              totalItems: reportResponse.data?.totalItems || 0,
              totalPages: reportResponse.data?.totalPages || 0,
            }));
          } else {
            console.warn("Không thể tải dữ liệu đơn đặt sân:", reportResponse?.message);
          }
        } catch (reportError) {
          console.error("Error fetching recent bookings:", reportError);
        }
      } catch (error) {
        console.error("Error fetching dashboard data:", error);
        setError(error.message || "Không thể tải dữ liệu dashboard");
      } finally {
        setLoading(false);
      }
    };

    if (userId && !authLoading && isLoggedIn) {
      fetchDashboardData();
    } else if (!authLoading && !isLoggedIn) {
      setLoading(false);
      setError("Vui lòng đăng nhập để xem dashboard");
    }
  }, [userId, authLoading, isLoggedIn, selectedMonth, bookingPagination.pageNumber, bookingPagination.pageSize]);

  // ✅ THÊM useEffect RIÊNG ĐỂ CHECK COMMISSION KHI THAY ĐỔI THÁNG
  useEffect(() => {
    if (userId && selectedMonth) {
      checkCommissionStatus();
    }
  }, [userId, selectedMonth]);

  const handleExportExcel = async () => {
    setExportLoading(true);
    try {
      // Lấy ngày đầu và cuối tháng từ selectedMonth
      const [startDate, endDate] = getMonthRange(selectedMonth);

      const response = await exportReportToExcel(
        userId,
        startDate, // Ngày bắt đầu
        endDate,   // Ngày kết thúc
        null,      // facilityId (nếu cần)
        1          // pageNumber
      );

      // Kiểm tra nếu response không phải là ArrayBuffer
      if (!response || !(response instanceof ArrayBuffer)) {
        throw new Error("Dữ liệu trả về không hợp lệ");
      }

      // Kiểm tra magic number
      const header = new Uint8Array(response.slice(0, 4));
      if (
        header[0] !== 0x50 ||
        header[1] !== 0x4b ||
        header[2] !== 0x03 ||
        header[3] !== 0x04
      ) {
        throw new Error("Dữ liệu không phải file Excel hợp lệ");
      }

      // Tạo blob với MIME type chính xác
      const blob = new Blob([response], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      });

      const now = new Date();
      const formattedDate = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(
        2,
        "0"
      )}-${String(now.getDate()).padStart(2, "0")}`;
      const formattedTime = `${String(now.getHours()).padStart(2, "0")}h${String(
        now.getMinutes()
      ).padStart(2, "0")}m${String(now.getSeconds()).padStart(2, "0")}s`;

      // Tạo URL tạm
      const url = URL.createObjectURL(blob);

      // Tạo thẻ a ẩn để tải xuống
      const a = document.createElement("a");
      a.href = url;
      a.download = `Report_${formattedDate}_${formattedTime}.xlsx`;
      document.body.appendChild(a);
      a.click();

      // Dọn dẹp
      setTimeout(() => {
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      }, 100);
    } catch (error) {
      console.error("Lỗi khi xuất Excel:", error);
      alert("Lỗi: " + error.message);
    } finally {
      setExportLoading(false);
    }
  };

  const handleViewDetail = (booking) => {
    setSelectedBooking(booking);
    setShowDetailModal(true);
  };

  // Handler đổi trang
  const handleBookingPageChange = (page, pageSize) => {
    setBookingPagination((prev) => ({
      ...prev,
      pageNumber: page,
      pageSize: pageSize || prev.pageSize,
    }));
  };

  // Handler đổi pageSize
  const handleBookingPageSizeChange = (current, size) => {
    setBookingPagination((prev) => ({
      ...prev,
      pageNumber: 1, // Reset về trang 1 khi đổi pageSize
      pageSize: size,
    }));
  };

  // Xử lý khi chọn tháng mới
  const handleMonthChange = (date) => {
    if (date) {
      setSelectedMonth(date.toDate());
    }
  };

  const prepareChartData = () => {
    const revenueByCourtType = {};
    const statusDistribution = {};

    dashboardData.recentBookings.forEach(booking => {
      // CHỈ TÍNH DOANH THU NẾU ĐÃ THANH TOÁN
      const isPaid = booking.bookingStatus === "Đã hoàn thành";

      if (booking.courtCategories && booking.totalPrice && isPaid) {
        const courtTypes = booking.courtCategories.split(', ').filter(type => type.trim());
        const courtCount = courtTypes.length;

        if (courtCount > 0) {
          const revenuePerCourt = booking.totalPrice / courtCount;

          courtTypes.forEach(type => {
            const courtType = type.trim();
            if (courtType) {
              if (!revenueByCourtType[courtType]) {
                revenueByCourtType[courtType] = 0;
              }
              revenueByCourtType[courtType] += revenuePerCourt;
            }
          });
        }
      }

      // Phần status distribution vẫn tính tất cả
      if (booking.bookingStatus) {
        if (!statusDistribution[booking.bookingStatus]) {
          statusDistribution[booking.bookingStatus] = 0;
        }
        statusDistribution[booking.bookingStatus]++;
      }
    });

    return {
      revenueData: {
        labels: Object.keys(revenueByCourtType),
        datasets: [{
          label: 'Doanh thu (VND)',
          data: Object.values(revenueByCourtType),
          backgroundColor: 'rgba(54, 162, 235, 0.5)',
          borderColor: 'rgba(54, 162, 235, 1)',
          borderWidth: 1
        }]
      },
      statusData: {
        labels: Object.keys(statusDistribution),
        datasets: [{
          data: Object.values(statusDistribution),
          backgroundColor: [
            'rgba(75, 192, 192, 0.5)',
            'rgba(255, 206, 86, 0.5)',
            'rgba(255, 99, 132, 0.5)'
          ],
          borderColor: [
            'rgba(75, 192, 192, 1)',
            'rgba(255, 206, 86, 1)',
            'rgba(255, 99, 132, 1)'
          ],
          borderWidth: 1
        }]
      }
    };
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
              <h5>
                <i className="fas fa-user me-2"></i>Thông tin khách hàng
              </h5>
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
              <h5>
                <i className="fas fa-calendar-alt me-2"></i>Thông tin đặt sân
              </h5>
              <Row>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Ngày check-in:</label>
                    <span>{booking.checkInDate}</span>
                  </div>
                  <div className="detail-item">
                    <label>Thời gian đặt:</label>
                    <span>
                      {new Date(booking.bookingTime).toLocaleString("vi-VN")}
                    </span>
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
              <h5>
                <i className="fas fa-money-bill-wave me-2"></i>Thông tin thanh
                toán
              </h5>
              <Row>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Tổng tiền:</label>
                    <span className="price">
                      {formatPrice(booking.totalPrice)}đ
                    </span>
                  </div>

                </Col>
                <Col md={6}>
                  <div className="detail-item">
                    <label>Trạng thái đặt sân:</label>
                    <span
                      className={`status-badge ${getStatusClass(
                        booking.bookingStatus
                      )}`}
                    >
                      {booking.bookingStatus}
                    </span>
                  </div>
                </Col>
              </Row>
              {booking.paymentTime && (
                <div className="detail-item">
                  <label>Thời gian thanh toán:</label>
                  <span>
                    {new Date(booking.paymentTime).toLocaleString("vi-VN")}
                  </span>
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

  // ✅ HÀM MỞ MODAL COMMISSION
  const handleCreateCommissionClick = () => {
    setShowCommissionModal(true);
  };

  // ✅ HÀM ĐÓNG MODAL
  const handleCloseCommissionModal = () => {
    setShowCommissionModal(false);
  };

  // ✅ COMPONENT MODAL THANH TOÁN COMMISSION
  const CommissionModal = ({ show, onHide }) => {
    const commissionAmount = dashboardData.commissionPayment;
    const month = selectedMonth.getMonth() + 1;
    const year = selectedMonth.getFullYear();

    return (
      <Modal show={show} onHide={onHide} size="md" centered>
        <Modal.Header closeButton className="commission-modal-header">
          <Modal.Title>
            <i className="fas fa-percentage me-2"></i>
            Thanh toán Commission
          </Modal.Title>
        </Modal.Header>
        <Modal.Body className="commission-modal-body">
          <div className="commission-info">
            <div className="info-section">
              <h5>
                <i className="fas fa-info-circle me-2"></i>
                Thông tin Commission
              </h5>
              <div className="info-grid">
                <div className="info-item">
                  <label>Tháng/Năm:</label>
                  <span className="value">{month}/{year}</span>
                </div>
                <div className="info-item">
                  <label>Chủ sân:</label>
                  <span className="value">{user?.fullName || 'N/A'}</span>
                </div>
                <div className="info-item">
                  <label>Tổng doanh thu:</label>
                  <span className="value revenue">{formatPrice(dashboardData.totalRevenue)}đ</span>
                </div>
                <div className="info-item">
                  <label>Tỷ lệ commission:</label>
                  <span className="value percentage">5%</span>
                </div>
              </div>
            </div>

            <div className="payment-section">
              <h5>
                <i className="fas fa-money-bill-wave me-2"></i>
                Thông tin thanh toán
              </h5>
              <div className="payment-amount">
                <label>Số tiền cần thanh toán:</label>
                <div className="amount-display">
                  {formatPrice(commissionAmount)}đ
                </div>
              </div>

              <div className="payment-note">
                <i className="fas fa-exclamation-triangle me-2"></i>
                <small>
                  Commission sẽ được tính dựa trên 5% tổng doanh thu của tháng {month}/{year}
                </small>
              </div>
            </div>

            <div className="calculation-breakdown">
              <h6>Chi tiết tính toán:</h6>
              <div className="breakdown-item">
                <span>Doanh thu tháng {month}/{year}:</span>
                <span>{formatPrice(dashboardData.totalRevenue)}đ</span>
              </div>
              <div className="breakdown-item">
                <span>Commission (5%):</span>
                <span>{formatPrice(commissionAmount)}đ</span>
              </div>
              <hr />
              <div className="breakdown-total">
                <span><strong>Tổng cần thanh toán:</strong></span>
                <span className="total-amount"><strong>{formatPrice(commissionAmount)}đ</strong></span>
              </div>
            </div>
          </div>
        </Modal.Body>
        <Modal.Footer className="commission-modal-footer">
          <Button variant="secondary" onClick={onHide}>
            <i className="fas fa-times me-2"></i>
            Hủy
          </Button>
          <Button
            variant="primary"
            className="confirm-payment-btn"
            disabled={commissionAmount <= 0}
          >
            <i className="fas fa-credit-card me-2"></i>
            Thanh toán
          </Button>
        </Modal.Footer>
      </Modal>
    );
  };

  // Hiển thị loading khi đang xác thực
  if (authLoading) {
    return <div className="loading-spinner">Đang xác thực...</div>;
  }

  // Hiển thị yêu cầu đăng nhập
  if (!isLoggedIn) {
    return <div className="error-alert">Vui lòng đăng nhập để xem dashboard</div>;
  }

  if (loading) {
    return <div className="loading-spinner">Đang tải dữ liệu...</div>;
  }

  return (
    <div className="owner-dashboard">
      {error && (
        <div className="error-alert">
          <i className="fas fa-exclamation-circle"></i>
          <span className="error-text">{error}</span>
          <Button
            variant="link"
            size="sm"
            onClick={() => window.location.reload()}
            className="ms-2"
          >
            Thử lại
          </Button>
        </div>
      )}

      <div className="dashboard-header">
        <div>
          <h2 className="dashboard-title">Xin Chào, {user?.fullName || 'Chủ sân'}</h2>
          <p className="dashboard-subtitle">
            Đây là trang web quản lý dành cho chủ sân
          </p>
        </div>
        <div className="d-flex align-items-center gap-3">
          <DatePicker
            picker="month"
            onChange={handleMonthChange}
            format="MM/YYYY"
            placeholder="Chọn tháng"
            allowClear={false}
            value={selectedMonth ? dayjs(selectedMonth) : null}
            style={{ width: "180px" }}
          />

          {/* ✅ BUTTON CHỈ HIỂN THỊ KHI CHƯA CÓ COMMISSION */}
          {!commissionExists && dashboardData.totalRevenue > 0 && (
            <Button
              variant="warning"
              className="create-commission-btn"
              onClick={handleCreateCommissionClick} // ✅ THÊM ONCLICK
              disabled={checkingCommission}
            >
              {checkingCommission ? (
                <>
                  <i className="fas fa-spinner fa-spin me-2"></i>
                  Đang kiểm tra...
                </>
              ) : (
                <>
                  <i className="fas fa-percentage me-2"></i>
                  Tạo Commission
                </>
              )}
            </Button>
          )}

          <Button
            variant="success"
            className="export-excel-btn"
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
      </div>

      {/* ✅ HIỂN THỊ TRẠNG THÁI COMMISSION (Tùy chọn) */}
      {checkingCommission && (
        <div className="commission-status mb-3">
          <i className="fas fa-spinner fa-spin me-2"></i>
          Đang kiểm tra commission...
        </div>
      )}

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

      <div className="chart-container">
        <Row>
          <Col md={8}>
            <div className="chart-card">
              <h4>Doanh thu theo loại sân</h4>
              {dashboardData.recentBookings.length > 0 ? (
                <Bar
                  data={prepareChartData().revenueData}
                  options={{
                    responsive: true,
                    plugins: {
                      legend: {
                        display: false
                      },
                      tooltip: {
                        callbacks: {
                          label: (context) => {
                            return ` ${formatPrice(context.raw)}đ`;
                          }
                        }
                      }
                    },
                    scales: {
                      y: {
                        beginAtZero: true,
                        ticks: {
                          callback: (value) => formatPrice(value) + 'đ'
                        }
                      }
                    }
                  }}
                />
              ) : (
                <p>Không có dữ liệu để hiển thị</p>
              )}
            </div>
          </Col>
          <Col md={4}>
            <div className="chart-card">
              <h4>Phân bổ trạng thái đơn</h4>
              {dashboardData.recentBookings.length > 0 ? (
                <Pie
                  data={prepareChartData().statusData}
                  options={{
                    responsive: true,
                    plugins: {
                      legend: {
                        position: 'bottom'
                      },
                      tooltip: {
                        callbacks: {
                          label: (context) => {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = Math.round((context.raw / total) * 100);
                            return `${context.label}: ${context.raw} đơn (${percentage}%)`;
                          }
                        }
                      }
                    }
                  }}
                />
              ) : (
                <p>Không có dữ liệu để hiển thị</p>
              )}
            </div>
          </Col>
        </Row>
      </div>

      <Card className="recent-bookings-card">
        <Card.Body>
          <div className="card-header">
            <Card.Title className="section-title">
              Đơn Đặt Sân Gần Đây
            </Card.Title>
          </div>

          {dashboardData.recentBookings.length > 0 ? (
            <>
              <div className="booking-list">
                {dashboardData.recentBookings.map((booking) => (
                  <div key={booking.bookingId} className="booking-item">
                    <div className="booking-info">
                      <div className="customer-info">
                        <strong>{booking.customerName ? booking.customerName : "N/A"}</strong>
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
                      </div>
                      <div className="booking-status">
                        <div
                          className={`status-badge ${getStatusClass(
                            booking.bookingStatus
                          )}`}
                        >
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

              {/* Thêm phân trang */}
              {bookingPagination.totalItems > 0 && (
                <div className="booking-pagination mt-3 d-flex justify-content-center">
                  <Pagination
                    current={bookingPagination.pageNumber}
                    pageSize={bookingPagination.pageSize}
                    total={bookingPagination.totalItems}
                    showSizeChanger
                    pageSizeOptions={['3', '5', '10']}
                    onChange={handleBookingPageChange}
                    onShowSizeChange={handleBookingPageSizeChange}
                    showTotal={(total, range) =>
                      `${range[0]}-${range[1]} / ${total} đơn đặt sân`
                    }
                    showQuickJumper
                    size="default"
                    className="custom-pagination"
                  />
                </div>
              )}
            </>
          ) : (
            <div className="no-bookings">
              {error ? "Không thể tải dữ liệu booking" : "Không có đơn đặt sân gần đây"}
            </div>
          )}
        </Card.Body>
      </Card>

      {/* ✅ MODAL COMMISSION */}
      <CommissionModal
        show={showCommissionModal}
        onHide={handleCloseCommissionModal}
      />

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