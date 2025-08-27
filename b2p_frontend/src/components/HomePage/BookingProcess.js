import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import "./BookingProcess.scss";
import { getBookingById } from "../../services/apiService";
import { useSignalR } from "../../contexts/SignalRContext";

// Helper function to format currency
const formatCurrency = (amount) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    minimumFractionDigits: 0,
  }).format(amount);
};

// Helper function to format date
const formatDate = (dateString) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleDateString('vi-VN', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });
};

// Helper function to format time
const formatTime = (timeString) => {
  if (!timeString) return '';
  return timeString.substring(0, 5);
};

export default function BookingProcess() {
  const location = useLocation();
  const bookingId = location.state?.bookingId;
  const navigate = useNavigate();

  console.log('🚀 [BookingProcess] Component mounted with bookingId:', bookingId);

  const { connection, isConnected } = useSignalR();

  const [bookingData, setBookingData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [showUpdateNotification, setShowUpdateNotification] = useState(false);

  // Load booking data function
  const loadBookingData = async (showLoading = true) => {
    if (!bookingId) {
      console.log('❌ [BookingProcess] No bookingId provided');
      setError('Không tìm thấy mã đặt sân');
      setLoading(false);
      return;
    }

    try {
      console.log(`🔄 [BookingProcess] Loading booking data for ID: ${bookingId}`);

      if (showLoading) {
        setLoading(true);
      }
      setError(null);

      const result = await getBookingById(bookingId);
      console.log('📡 [BookingProcess] API Response:', result);

      if (result && result.success && result.data) {
        console.log('✅ [BookingProcess] Booking data loaded successfully:', result.data);
        setBookingData(result.data);
        setLastUpdated(new Date());

        if (!showLoading && bookingData) {
          console.log('🔔 [BookingProcess] Showing update notification');
          setShowUpdateNotification(true);
          setTimeout(() => setShowUpdateNotification(false), 3000);
        }
      } else {
        throw new Error(result?.message || 'Không thể tải thông tin đặt sân');
      }
    } catch (err) {
      console.error('❌ [BookingProcess] Error loading booking data:', err);
      setError(err.message || 'Có lỗi xảy ra khi tải thông tin đặt sân');
    } finally {
      if (showLoading) {
        setLoading(false);
      }
    }
  };

  // Load booking data khi component mount
  useEffect(() => {
    console.log('🔄 [BookingProcess] Initial data load');
    loadBookingData(true);
  }, [bookingId]);

  // SignalR listener
  useEffect(() => {
    console.log('🔄 [BookingProcess] Setting up SignalR listener');
    console.log('🔍 Connection state:', {
      hasConnection: !!connection,
      isConnected,
      connectionState: connection?.state,
      bookingId
    });

    if (!connection || !isConnected || !bookingId) {
      console.log('❌ Missing requirements for SignalR:', {
        connection: !!connection,
        isConnected,
        bookingId
      });
      return;
    }

    const handleBookingStatusChanged = (data) => {
      console.log('📨 [SignalR] Received BookingStatusChanged RAW:', data);

      let receivedId = null;
      if (data && data.BookingId) {
        receivedId = data.BookingId;
      } else if (data && data.bookingId) {
        receivedId = data.bookingId;
      } else if (typeof data === 'number' || typeof data === 'string') {
        receivedId = data;
      }

      const currentId = bookingId.toString();
      const receivedIdStr = receivedId ? receivedId.toString() : '';

      if (receivedIdStr === currentId) {
        console.log('✅ [SignalR] BookingId matched! Reloading data...');
        loadBookingData(false);

        setShowUpdateNotification(true);
        setTimeout(() => setShowUpdateNotification(false), 3000);
      }
    };

    console.log('📡 [SignalR] Registering BookingStatusChanged listener');

    connection.off('BookingStatusChanged', handleBookingStatusChanged);
    connection.on('BookingStatusChanged', handleBookingStatusChanged);

    return () => {
      console.log('🧹 [SignalR] Cleaning up listener');
      if (connection) {
        connection.off('BookingStatusChanged', handleBookingStatusChanged);
      }
    };
  }, [connection, isConnected, bookingId]);

  // Manual refresh
  const handleManualRefresh = () => {
    console.log('🔄 [Manual] Refreshing data...');
    loadBookingData(true);
  };

  // Other handlers
  const handleRetryPayment = () => {
    alert('Vui lòng liên hệ để được hỗ trợ thanh toán');
  };

  const handleBackToHome = () => {
    navigate('/');
  };

  // Loading state
  if (loading) {
    return (
      <div className="booking-process-container">
        <div className="loading-section">
          <div className="loading-spinner-large"></div>
          <h2>Đang tải thông tin đặt sân...</h2>
          <p>Vui lòng đợi trong giây lát</p>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="booking-process-container">
        <div className="error-section">
          <div className="error-icon"></div>
          <h2>Có lỗi xảy ra</h2>
          <p>{error}</p>
          <div className="error-actions">
            <button className="btn-secondary" onClick={handleManualRefresh}>
              Thử lại
            </button>
            <button className="btn-primary" onClick={handleBackToHome}>
              Về trang chủ
            </button>
          </div>
        </div>
      </div>
    );
  }

  // No data state
  if (!bookingData) {
    return (
      <div className="booking-process-container">
        <div className="error-section">
          <div className="error-icon"></div>
          <h2>Không tìm thấy thông tin đặt sân</h2>
          <p>Mã đặt sân không tồn tại hoặc đã bị xóa</p>
          <div className="error-actions">
            <button className="btn-primary" onClick={handleBackToHome}>
              Về trang chủ
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Get status info
  const getStatusInfo = (status) => {
    switch (status?.toLowerCase()) {
      case 'pending':
        return {
          text: 'Đang chờ xử lý',
          className: 'status-pending',
          description: 'Đặt sân đang được xử lý'
        };
      case 'confirmed':
        return {
          text: 'Đã xác nhận',
          className: 'status-confirmed',
          description: 'Đặt sân đã được xác nhận'
        };
      case 'paid':
        return {
          text: 'Đã thanh toán cọc (30%)',
          className: 'status-paid',
          description: 'Đã thanh toán thành công'
        };
      case 'unpaid':
        return {
          text: 'Chưa thanh toán',
          className: 'status-unpaid',
          description: 'Chưa thanh toán'
        };
      case 'cancelled':
        return {
          text: 'Đã hủy',
          className: 'status-cancelled',
          description: 'Đặt sân đã bị hủy'
        };
      case 'completed':
        return {
          text: 'Hoàn thành',
          className: 'status-completed',
          description: 'Đã sử dụng dịch vụ'
        };
      default:
        return {
          text: status || 'Không xác định',
          className: 'status-unknown',
          description: 'Trạng thái không xác định'
        };
    }
  };

  const statusInfo = getStatusInfo(bookingData.status);
  const isPendingPayment = ['pending', 'confirmed', 'unpaid'].includes(bookingData.status?.toLowerCase());

  // Get facility info
  const facilityInfo = bookingData.slots && bookingData.slots.length > 0
    ? {
      courtName: bookingData.slots[0].courtName,
      categoryName: bookingData.slots[0].categoryName,
      allCourts: [...new Set(bookingData.slots.map(slot => slot.courtName))],
    }
    : null;

  return (
    <div className="booking-process-container" style={{ paddingTop: '120px' }}>
      {/* Update Notification */}
      {showUpdateNotification && (
        <div className="update-notification">
          <div className="notification-content">
            <span>Thông tin đặt sân đã được cập nhật!</span>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="process-header">
        <div className="header-content">
          <h1 className="page-title">
            Thông tin đặt sân
          </h1>
        </div>
        <div className="header-actions">
          <button className="btn-back" onClick={handleBackToHome}>
            Về trang chủ
          </button>
        </div>
      </div>

      {/* Status Section */}
      <div className="status-section">
        <div className={`status-card ${statusInfo.className}`}>
          <div className="status-info">
            <h3 className="status-title">{statusInfo.text}</h3>
            <p className="status-description">{statusInfo.description}</p>
          </div>
        </div>
      </div>

      {/* Booking Details */}
      <div className="details-section">
        <h2 className="section-title">
          Chi tiết đặt sân
        </h2>

        <div className="details-grid">
          {/* Facility Info */}
          <div className="detail-card">
            <h3 className="card-title">
              Thông tin sân
            </h3>
            <div className="card-content">
              <div className="detail-item">
                <span className="detail-label">Tên sân:</span>
                <span className="detail-value">
                  {facilityInfo ? facilityInfo.allCourts.join(', ') : 'N/A'}
                </span>
              </div>
              <div className="detail-item">
                <span className="detail-label">Loại sân:</span>
                <span className="detail-value">
                  {facilityInfo ? facilityInfo.categoryName : 'N/A'}
                </span>
              </div>
            </div>
          </div>

          {/* Booking Info */}
          <div className="detail-card">
            <h3 className="card-title">
              Thông tin đặt sân
            </h3>
            <div className="card-content">
              <div className="detail-item">
                <span className="detail-label">Ngày đặt:</span>
                <span className="detail-value">{formatDate(bookingData.checkInDate)}</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">Thời gian:</span>
                <span className="detail-value">
                  {bookingData.slots && bookingData.slots.length > 0
                    ? bookingData.slots.map(slot =>
                      `${formatTime(slot.startTime)} - ${formatTime(slot.endTime)}`
                    ).join(', ')
                    : 'N/A'
                  }
                </span>
              </div>
              <div className="detail-item">
                <span className="detail-label">Số slot:</span>
                <span className="detail-value">{bookingData.slots ? bookingData.slots.length : 'N/A'}</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">Ngày tạo:</span>
                <span className="detail-value">{formatDate(bookingData.createDate)}</span>
              </div>
            </div>
          </div>

          {/* Customer Info */}
          <div className="detail-card">
            <h3 className="card-title">
              Thông tin khách hàng
            </h3>
            <div className="card-content">
              <div className="detail-item">
                <span className="detail-label">Số điện thoại:</span>
                <span className="detail-value">{bookingData.phone || 'N/A'}</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">Email:</span>
                <span className="detail-value">{bookingData.email || 'N/A'}</span>
              </div>
            </div>
          </div>

          {/* Payment Info */}
          <div className="detail-card">
            <h3 className="card-title">
              Thông tin thanh toán
            </h3>
            <div className="card-content">
              <div className="detail-item">
                <span className="detail-label">Tổng tiền:</span>
                <span className="detail-value total-price">
                  {formatCurrency(bookingData.totalPrice || 0)}
                </span>
              </div>
              <div className="detail-item">
                <span className="detail-label">Trạng thái:</span>
                <span className={`detail-value payment-status ${statusInfo.className}`}>
                  {statusInfo.text}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Time Slots Details */}
      {bookingData.slots && bookingData.slots.length > 0 && (
        <div className="details-section">
          <h2 className="section-title">
            Chi tiết khung giờ
          </h2>

          <div className="details-grid">
            <div className="detail-card full-width">
              <h3 className="card-title">
                Danh sách khung giờ đã đặt
              </h3>
              <div className="card-content">
                <div className="slots-list">
                  {bookingData.slots.map((slot, index) => (
                    <div key={`${slot.courtId}-${slot.timeSlotId}`} className="slot-item">
                      <div className="slot-number">#{index + 1}</div>
                      <div className="slot-info-grid">
                        <div className="detail-item">
                          <span className="detail-label">Thời gian:</span>
                          <span className="detail-value slot-time">
                            {formatTime(slot.startTime)} - {formatTime(slot.endTime)}
                          </span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Sân:</span>
                          <span className="detail-value">{slot.courtName}</span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Loại sân:</span>
                          <span className="detail-value">{slot.categoryName}</span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
      {/* Footer Note */}
      <div className="footer-note">
        <div className="note-content">
          <p>
            Trang này sẽ tự động cập nhật khi có thay đổi trạng thái booking.
          </p>
        </div>
      </div>
    </div>
  );
}