import React, { useEffect, useState } from "react";
import { Card, Row, Col, Button, Modal, Form } from "react-bootstrap";
import { DatePicker } from "antd";
import dayjs from "dayjs";
import { useAuth } from '../../contexts/AuthContext';
import {
  getReport,
  getTotalReport,
  exportReportToExcel,
  checkCommission,
  createPaymentOrder, // ✅ THÊM IMPORT
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

  // ✅ THAY ĐỔI STATE CHO COMMISSION
  const [showCommissionListModal, setShowCommissionListModal] = useState(false);
  const [monthlyCommissions, setMonthlyCommissions] = useState({});
  const [loadingCommissions, setLoadingCommissions] = useState(false);

  // ✅ THÊM STATE ĐỂ THEO DÕI SỐ THÁNG CẦN TẠO COMMISSION
  const [pendingCommissionCount, setPendingCommissionCount] = useState(0);

  // Hàm lấy ngày đầu và cuối tháng từ selectedMonth
  const getMonthRange = (dateObj) => {
    const year = dateObj.getFullYear();
    const month = dateObj.getMonth();
    const start = new Date(year, month, 1, 0, 0, 0, 0);
    const end = new Date(year, month + 1, 0, 23, 59, 59, 999);
    return [start, end];
  };


  // ✅ HÀM KIỂM TRA COMMISSION CHO NHIỀU THÁNG - SỬA LẠI
  const checkMultipleMonthsCommission = async () => {
    if (!userId) return;

    try {
      setLoadingCommissions(true);
      const currentDate = new Date();
      const currentMonth = currentDate.getMonth() + 1; // getMonth() trả về 0-11
      const currentYear = currentDate.getFullYear();
      const commissionStatus = {};
      let pendingCount = 0; // ✅ BIẾN ĐẾM SỐ THÁNG CẦN TẠO

      // ✅ CHỈ KIỂM TRA CÁC THÁNG ĐÃ QUA (KHÔNG BAO GỒM THÁNG HIỆN TẠI)
      for (let month = 1; month < currentMonth; month++) {
        try {
          console.log(`🔍 Checking commission for month ${month}/${currentYear}`);
          
          // ✅ KIỂM TRA COMMISSION STATUS
          const commissionResponse = await checkCommission(userId, month, currentYear);
          
          // ✅ KIỂM TRA DOANH THU
          const selectedDate = new Date(currentYear, month - 1, 1);
          const [start, end] = getMonthRange(selectedDate);
          const revenueResponse = await getTotalReport(userId, start, end);
          
          const hasRevenue = revenueResponse?.success && (revenueResponse.data?.totalCost || 0) > 0;
          const hasCommission = revenueResponse?.success && (revenueResponse.data?.commissionPayment || 0) > 0;
          const commissionExists = commissionResponse?.exists || false;

          // ✅ XÁC ĐỊNH TRẠNG THÁI
          let status = 'no-revenue';
          if (hasRevenue && hasCommission) {
            if (commissionExists) {
              status = 'paid';
            } else {
              status = 'pending'; // ✅ CẦN TẠO COMMISSION
              pendingCount++; // ✅ TĂNG COUNTER
            }
          } else if (hasRevenue && !hasCommission) {
            status = 'no-commission';
          }

          commissionStatus[`${month}-${currentYear}`] = {
            month,
            year: currentYear,
            exists: commissionExists,
            hasRevenue,
            hasCommission,
            status, // ✅ THÊM STATUS
            revenue: revenueResponse?.data?.totalCost || 0,
            commission: revenueResponse?.data?.commissionPayment || 0
          };

          console.log(`✅ Month ${month}: Status = ${status}`);
          
        } catch (error) {
          console.error(`❌ Month ${month} error:`, error);
          commissionStatus[`${month}-${currentYear}`] = {
            month,
            year: currentYear,
            exists: false,
            hasRevenue: false,
            hasCommission: false,
            status: 'no-revenue',
            revenue: 0,
            commission: 0
          };
        }
      }

      // ✅ THÊM CÁC THÁNG TƯƠI LAI VÀ TRẠNG THÁI MẶC ĐỊNH
      for (let month = currentMonth; month <= 12; month++) {
        commissionStatus[`${month}-${currentYear}`] = {
          month,
          year: currentYear,
          exists: false,
          status: 'future',
          isFuture: true,
          revenue: 0,
          commission: 0
        };
      }

      console.log(`📋 Final commission status:`, commissionStatus);
      console.log(`🔥 Pending commission count: ${pendingCount}`);
      
      setMonthlyCommissions(commissionStatus);
      setPendingCommissionCount(pendingCount); // ✅ CẬP NHẬT COUNTER
    
    } catch (error) {
      console.error("Error checking multiple months commission:", error);
    } finally {
      setLoadingCommissions(false);
    }
  };

  // ✅ TẠO HÀM RIÊNG CHỈ ĐỂ ĐẾM PENDING COMMISSION (KHÔNG LOAD CHI TIẾT)
  const checkPendingCommissionCount = async () => {
    if (!userId) return;

    try {
      const currentDate = new Date();
      const currentMonth = currentDate.getMonth() + 1;
      const currentYear = currentDate.getFullYear();
      let pendingCount = 0;

      // ✅ CHỈ KIỂM TRA CÁC THÁNG ĐÃ QUA VÀ ĐẾM PENDING
      for (let month = 1; month < currentMonth; month++) {
        try {
          // ✅ KIỂM TRA COMMISSION STATUS
          const commissionResponse = await checkCommission(userId, month, currentYear);
          
          // ✅ KIỂM TRA DOANH THU
          const selectedDate = new Date(currentYear, month - 1, 1);
          const [start, end] = getMonthRange(selectedDate);
          const revenueResponse = await getTotalReport(userId, start, end);
          
          const hasRevenue = revenueResponse?.success && (revenueResponse.data?.totalCost || 0) > 0;
          const hasCommission = revenueResponse?.success && (revenueResponse.data?.commissionPayment || 0) > 0;
          const commissionExists = commissionResponse?.exists || false;

          // ✅ CHỈ ĐẾM NHỮNG THÁNG CÓ COMMISSION NHƯNG CHƯA THANH TOÁN
          if (hasRevenue && hasCommission && !commissionExists) {
            pendingCount++;
          }
          
        } catch (error) {
          console.error(`❌ Month ${month} error:`, error);
        }
      }

      console.log(`🔥 Quick check - Pending commission count: ${pendingCount}`);
      setPendingCommissionCount(pendingCount);
      
    } catch (error) {
      console.error("Error checking pending commission count:", error);
    }
  };

  useEffect(() => {
    const fetchDashboardData = async () => {
      setError(null);
      setLoading(true);

      try {
        // ✅ FETCH DASHBOARD DATA TRƯỚC
        const [start, end] = getMonthRange(selectedMonth);

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

        // ✅ KIỂM TRA PENDING COMMISSION SAU KHI LOAD XONG DASHBOARD
        await checkPendingCommissionCount();
        
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

  // ✅ HÀM MỞ MODAL DANH SÁCH COMMISSION
  const handleShowCommissionList = async () => {
    setShowCommissionListModal(true);
    
    // ✅ CHỈ LOAD CHI TIẾT KHI MỞ MODAL (NẾU CHƯA CÓ DỮ LIỆU)
    if (Object.keys(monthlyCommissions).length === 0) {
      await checkMultipleMonthsCommission();
    }
  };

  // ✅ HÀM ĐÓNG MODAL DANH SÁCH COMMISSION
  const handleCloseCommissionList = () => {
    setShowCommissionListModal(false);
  };

  // ✅ HÀM TẠO COMMISSION CHO THÁNG CỤ THỂ
  const handleCreateCommissionForMonth = async (month, year) => {
    try {
      // ✅ ẨN MODAL DANH SÁCH COMMISSION TRƯỚC
      setShowCommissionListModal(false);
      
      // ✅ SET THÁNG ĐÃ CHỌN VÀO selectedMonth
      const selectedDate = new Date(year, month - 1, 1); // month - 1 vì Date() dùng 0-11
      setSelectedMonth(selectedDate);
      
      // ✅ FETCH DỮ LIỆU DOANH THU CHO THÁNG ĐÃ CHỌN
      const [start, end] = getMonthRange(selectedDate);
      
      console.log(`🔍 Fetching revenue data for ${month}/${year}`);
      
      const totalReportResponse = await getTotalReport(userId, start, end);
      
      if (totalReportResponse && totalReportResponse.success) {
        const revenue = totalReportResponse.data?.totalCost || 0;
        const commission = totalReportResponse.data?.commissionPayment || 0;
        
        console.log(`📊 Revenue data for ${month}/${year}:`, {
          revenue,
          commission
        });

        // ✅ KIỂM TRA DOANH THU VÀ COMMISSION
        if (revenue <= 0 || commission <= 0) {
          // ✅ HIỂN THỊ LẠI MODAL DANH SÁCH
          setShowCommissionListModal(true);
          
          // ✅ HIỂN THỊ THÔNG BÁO TÙY THEO TRƯỜNG HỢP
          if (revenue <= 0) {
            alert(`📊 Tháng ${month}/${year} không có doanh thu!\n\nKhông cần tạo commission cho tháng này.`);
          } else if (commission <= 0) {
            alert(`📊 Tháng ${month}/${year} không có commission cần thanh toán!\n\nDoanh thu: ${formatPrice(revenue)}đ\nCommission: 0đ`);
          }
          
          return; // ✅ DỪNG XỬ LÝ
        }
        
        // ✅ CẬP NHẬT DỮ LIỆU CHO THÁNG ĐÃ CHỌN (CHỈ KHI CÓ DOANH THU)
        setDashboardData((prev) => ({
          ...prev,
          totalRevenue: revenue,
          commissionPayment: commission
        }));
        
        // ✅ MỞ MODAL COMMISSION SAU KHI ẨN MODAL LIST
        setTimeout(() => {
          setShowCommissionModal(true);
        }, 300); // Delay nhỏ để smooth transition
        
        console.log(`✅ Opened commission modal for ${month}/${year} with commission: ${formatPrice(commission)}đ`);
        
      } else {
        // ✅ NẾU LỖI, HIỂN THỊ LẠI MODAL DANH SÁCH
        setShowCommissionListModal(true);
        alert("Không thể tải dữ liệu doanh thu cho tháng này!");
      }
      
    } catch (error) {
      console.error("Error fetching commission data:", error);
      // ✅ NẾU LỖI, HIỂN THỊ LẠI MODAL DANH SÁCH
      setShowCommissionListModal(true);
      alert("Có lỗi xảy ra khi tải dữ liệu: " + error.message);
    }
  };

  // ✅ COMPONENT MODAL DANH SÁCH COMMISSION - SỬA LẠI
  const CommissionListModal = ({ show, onHide }) => {
    const monthNames = [
      'Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6',
      'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'
    ];

    const currentDate = new Date();
    const currentMonth = currentDate.getMonth() + 1;
    const currentYear = currentDate.getFullYear();

    return (
      <Modal show={show} onHide={onHide} size="lg" centered>
        <Modal.Header closeButton className="commission-modal-header">
          <Modal.Title>
            <i className="fas fa-list me-2"></i>
            Quản lý Commission theo tháng
            {pendingCommissionCount > 0 && (
              <span className="pending-count-badge ms-2">
                {pendingCommissionCount} tháng cần thanh toán
              </span>
            )}
          </Modal.Title>
        </Modal.Header>
        <Modal.Body className="commission-list-body">
          {loadingCommissions ? (
            <div className="loading-container">
              <i className="fas fa-spinner fa-spin me-2"></i>
              Đang kiểm tra trạng thái commission...
            </div>
          ) : (
            <div className="commission-grid">
              {monthNames.map((monthName, index) => {
                const month = index + 1;
                const year = currentYear;
                const key = `${month}-${year}`;
                const commissionData = monthlyCommissions[key];
                const isFutureMonth = month >= currentMonth;

                if (!commissionData) return null;

                const { status, revenue, commission, exists } = commissionData;

                return (
                  <div 
                    key={key} 
                    className={`commission-month-card ${status}`}
                  >
                    <div className="month-header">
                      <h6>{monthName} {year}</h6>
                      {/* ✅ HIỂN THỊ ICON TRẠNG THÁI */}
                      <div className="status-icon">
                        {status === 'pending' && <i className="fas fa-exclamation-circle text-danger"></i>}
                        {status === 'paid' && <i className="fas fa-check-circle text-success"></i>}
                        {status === 'no-revenue' && <i className="fas fa-minus-circle text-muted"></i>}
                        {status === 'no-commission' && <i className="fas fa-info-circle text-info"></i>}
                        {status === 'future' && <i className="fas fa-clock text-secondary"></i>}
                      </div>
                      
                      {/* ✅ HIỂN THỊ THÔNG TIN DOANH THU */}
                      {!isFutureMonth && revenue > 0 && (
                        <div className="revenue-info">
                          <small>DT: {formatPrice(revenue)}đ</small>
                          {commission > 0 && (
                            <small>Commission: {formatPrice(commission)}đ</small>
                          )}
                        </div>
                      )}
                    </div>
                    
                    <div className="month-status">
                      {status === 'future' ? (
                        <div className="status-badge status-future">
                          <i className="fas fa-clock me-2"></i>
                          Chưa đến thời gian
                        </div>
                      ) : status === 'no-revenue' ? (
                        <div className="status-badge status-no-revenue">
                          <i className="fas fa-minus-circle me-2"></i>
                          Không có doanh thu
                        </div>
                      ) : status === 'no-commission' ? (
                        <div className="status-badge status-no-commission">
                          <i className="fas fa-info-circle me-2"></i>
                          Không có commission
                        </div>
                      ) : status === 'paid' ? (
                        <div className="status-badge status-paid">
                          <i className="fas fa-check-circle me-2"></i>
                          Đã thanh toán
                        </div>
                      ) : status === 'pending' ? (
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => handleCreateCommissionForMonth(month, year)}
                          className="create-commission-btn pending-btn"
                        >
                          <i className="fas fa-exclamation-triangle me-2"></i>
                          Cần thanh toán ngay!
                        </Button>
                      ) : null}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </Modal.Body>
        <Modal.Footer className="commission-modal-footer">
          <Button variant="secondary" onClick={onHide}>
            <i className="fas fa-times me-2"></i>
            Đóng
          </Button>
          {pendingCommissionCount > 0 && (
            <div className="footer-warning">
              <i className="fas fa-exclamation-triangle me-2"></i>
              Bạn có {pendingCommissionCount} tháng cần thanh toán commission
            </div>
          )}
        </Modal.Footer>
      </Modal>
    );
  };

  // ✅ COMPONENT MODAL THANH TOÁN COMMISSION - CẬP NHẬT
  const CommissionModal = ({ show, onHide }) => {
    const commissionAmount = dashboardData.commissionPayment;
    const month = selectedMonth.getMonth() + 1;
    const year = selectedMonth.getFullYear();

    // ✅ HÀM XỬ LÝ THANH TOÁN
    const handlePayment = async () => {
      try {
        const commissionAmount = dashboardData.commissionPayment;
        const month = selectedMonth.getMonth() + 1;
        const year = selectedMonth.getFullYear();
        
        console.log(`💳 Processing payment for ${month}/${year}: ${commissionAmount}đ`);
        
        // ✅ KIỂM TRA DỮ LIỆU TRƯỚC KHI GỬI
        if (!userId) {
          throw new Error("Không tìm thấy userId");
        }
        
        if (!commissionAmount || commissionAmount <= 0) {
          throw new Error("Số tiền commission không hợp lệ");
        }
        
        // ✅ SỬA LẠI CẤU TRÚC DỮ LIỆU THEO YÊU CẦU API
        const paymentData = {
          amount: commissionAmount,
          description: `Thanh toán tiền hoa hồng tháng ${month}/${year}`,
          appUser: userId?.toString() || user?.userId?.toString(),
          embedData: {
            forMonth: month.toString(),
            forYear: year.toString()
          }
        };

        console.log("📤 Payment request data:", JSON.stringify(paymentData, null, 2));

        // ✅ GỌI API TẠO ĐƠN THANH TOÁN
        console.log("🚀 Calling createPaymentOrder API...");
        const paymentResponse = await createPaymentOrder(paymentData);
        
        console.log("📥 Full payment response:", paymentResponse);

        // ✅ SỬA LẠI: KIỂM TRA RESPONSE TRỰC TIẾP (KHÔNG CẦN .status VÀ .data)
        // Vì axios interceptor có thể đã xử lý và trả về response.data trực tiếp
        if (paymentResponse && paymentResponse.success) {
          console.log("✅ Payment API returned success=true");
          
          // ✅ KIỂM TRA order_url TRỰC TIẾP TRONG paymentResponse
          if (paymentResponse.data && paymentResponse.data.order_url) {
            console.log("✅ Payment order created successfully");
            console.log("🔗 Opening payment page:", paymentResponse.data.order_url);
            
            // ✅ MỞ TAB MỚI
            const paymentWindow = window.open(
              paymentResponse.data.order_url, 
              '_blank', 
              'noopener,noreferrer'
            );
            // ✅ ĐÓNG MODAL SAU KHI MỞ THANH TOÁN
            setShowCommissionModal(false);
            
            
          } else {
            console.error("❌ Missing order_url in response:", paymentResponse);
            alert("Không thể tạo đơn thanh toán: Thiếu đường link thanh toán");
          }
        } else {
          console.error("❌ Payment creation failed:", paymentResponse);
          const errorMsg = paymentResponse?.message || "Lỗi không xác định từ API";
          alert("Không thể tạo đơn thanh toán: " + errorMsg);
        }
        
      } catch (error) {
        console.error("❌ Error processing payment:", error);
        
        let errorMessage = "Có lỗi xảy ra khi thanh toán: ";
        
        if (error.response) {
          // ✅ API trả về lỗi HTTP
          console.error("❌ HTTP Error response:", error.response);
          const responseData = error.response.data;
          
          if (responseData && responseData.message) {
            errorMessage += responseData.message;
          } else if (responseData && typeof responseData === 'string') {
            errorMessage += responseData;
          } else {
            errorMessage += `HTTP ${error.response.status}`;
          }
          
        } else if (error.request) {
          console.error("❌ Network Error:", error.request);
          errorMessage += "Không thể kết nối đến server";
        } else {
          console.error("❌ Other Error:", error);
          errorMessage += error.message;
        }
        
        alert(errorMessage);
      }
    };

    // ✅ HÀM XỬ LÝ HỦY - QUAY LẠI MODAL DANH SÁCH
    const handleCancel = () => {
      setShowCommissionModal(false);
      setTimeout(() => {
        setShowCommissionListModal(true);
      }, 300);
    };

    return (
      <Modal show={show} onHide={handleCancel} size="md" centered>
        <Modal.Header closeButton className="commission-modal-header">
          <Modal.Title>
            <i className="fas fa-percentage me-2"></i>
            Thanh toán Commission - Tháng {month}/{year}
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
          <Button variant="secondary" onClick={handleCancel}>
            <i className="fas fa-arrow-left me-2"></i>
            Quay lại
          </Button>
          <Button
            variant="primary"
            className="confirm-payment-btn"
            disabled={commissionAmount <= 0}
            onClick={handlePayment}
          >
            <i className="fas fa-credit-card me-2"></i>
            Thanh toán {formatPrice(commissionAmount)}đ
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

          {/* ✅ COMMISSION BUTTON VỚI NOTIFICATION BADGE */}
          <div className="commission-button-container">
            <Button
              variant={pendingCommissionCount > 0 ? "danger" : "warning"}
              className={`manage-commission-btn ${pendingCommissionCount > 0 ? 'has-pending' : ''}`}
              onClick={handleShowCommissionList}
              disabled={checkingCommission}
            >
              <i className="fas fa-percentage me-2"></i>
              Commission
              {pendingCommissionCount > 0 && (
                <span className="commission-badge">
                  {pendingCommissionCount}
                </span>
              )}
            </Button>
            
            {/* ✅ THÔNG BÁO NẾU CÓ COMMISSION CẦN THANH TOÁN */}
            {pendingCommissionCount > 0 && (
              <div className="commission-alert">
                <i className="fas fa-exclamation-triangle"></i>
                <span>Có {pendingCommissionCount} tháng chưa thanh toán commission!</span>
              </div>
            )}
          </div>

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
          </Col >
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

      {/* ✅ MODAL DANH SÁCH COMMISSION */}
      <CommissionListModal
        show={showCommissionListModal}
        onHide={handleCloseCommissionList}
      />

      <BookingDetailModal
        show={showDetailModal}
        onHide={() => setShowDetailModal(false)}
        booking={selectedBooking}
      />

      {/* ✅ THÊM THÔNG BÁO TRÊN DASHBOARD KHI CÓ COMMISSION CẦN THANH TOÁN */}
      {pendingCommissionCount > 0 && (
        <div className="commission-warning-banner mb-4">
          <div className="warning-content">
            <div className="warning-icon">
              <i className="fas fa-exclamation-triangle"></i>
            </div>
            <div className="warning-text">
              <h5>Cần thanh toán Commission</h5>
              <p>
                Bạn có <strong>{pendingCommissionCount} tháng</strong> chưa thanh toán commission. 
                Vui lòng thanh toán để tránh bị tạm khóa tài khoản.
              </p>
            </div>
            <div className="warning-action">
              <Button
                variant="danger"
                onClick={handleShowCommissionList}
                className="commission-cta-btn"
              >
                <i className="fas fa-credit-card me-2"></i>
                Thanh toán ngay
              </Button>
            </div>
          </div>
        </div>
      )}
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