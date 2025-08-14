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
  return timeString.substring(0, 5); // Lấy HH:MM từ HH:MM:SS
};

export default function BookingProcess() {
  const location = useLocation();
  const bookingId = location.state?.bookingId;
  const navigate = useNavigate();

  console.log('🚀 [BookingProcess] Component mounted with bookingId:', bookingId);

  // Chỉ sử dụng 1 SignalR hook
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
        
        // Show notification nếu đây là update (không phải lần đầu load)
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

  // SignalR listener - SIMPLIFIED
  useEffect(() => {
    console.log('🔄 [BookingProcess] Setting up SignalR listener');
    console.log('🔍 Connection state:', { 
      hasConnection: !!connection, 
      isConnected,
      connectionState: connection?.state,
      bookingId
    });

    // Kiểm tra điều kiện cần thiết
    if (!connection || !isConnected || !bookingId) {
      console.log('❌ Missing requirements for SignalR:', { 
        connection: !!connection, 
        isConnected,
        bookingId 
      });
      return;
    }

    // Handler function
    const handleBookingStatusChanged = (data) => {
      console.log('📨 [SignalR] Received BookingStatusChanged RAW:', data);
      console.log('📨 [SignalR] Data type:', typeof data);
      console.log('📨 [SignalR] Data keys:', Object.keys(data));
      console.log('📨 [SignalR] Data.BookingId:', data.BookingId);
      console.log('📨 [SignalR] Current bookingId:', bookingId);
      
      // Thử các cách lấy bookingId
      let receivedId = null;
      if (data && data.BookingId) {
        receivedId = data.BookingId;
      } else if (data && data.bookingId) {
        receivedId = data.bookingId;
      } else if (typeof data === 'number' || typeof data === 'string') {
        receivedId = data;
      }
      
      console.log('📨 [SignalR] Extracted receivedId:', receivedId);
      
      // Convert both to string để so sánh
      const currentId = bookingId.toString();
      const receivedIdStr = receivedId ? receivedId.toString() : '';
      
      console.log('📨 [SignalR] ID comparison:', { 
        currentId, 
        receivedIdStr, 
        match: currentId === receivedIdStr 
      });
      
      // Nếu BookingId khớp thì reload data
      if (receivedIdStr === currentId) {
        console.log('✅ [SignalR] BookingId matched! Reloading data...');
        loadBookingData(false); // false = không show loading spinner
        
        // Show notification
        setShowUpdateNotification(true);
        setTimeout(() => setShowUpdateNotification(false), 3000);
      } else {
        console.log('❌ [SignalR] BookingId mismatch:', {
          expected: currentId,
          received: receivedIdStr,
          rawData: data
        });
      }
    };

    console.log('📡 [SignalR] Registering BookingStatusChanged listener');
    
    // Clean up existing listeners
    connection.off('BookingStatusChanged', handleBookingStatusChanged);
    
    // Register new listener
    connection.on('BookingStatusChanged', handleBookingStatusChanged);

    // Test connection ngay sau khi đăng ký
    console.log('🧪 [SignalR] Connection test:', {
      connectionId: connection.connectionId,
      state: connection.state
    });

    // Cleanup
    return () => {
      console.log('🧹 [SignalR] Cleaning up listener');
      if (connection) {
        connection.off('BookingStatusChanged', handleBookingStatusChanged);
      }
    };
  }, [connection, isConnected, bookingId]); // Chỉ depend vào những gì thật sự cần

  // Test SignalR function
  const testSignalRConnection = () => {
    console.log('🧪 [Test] SignalR Connection Details:');
    console.log('- Connection exists:', !!connection);
    console.log('- Is connected:', isConnected);
    console.log('- Connection ID:', connection?.connectionId);
    console.log('- Connection state:', connection?.state);
    console.log('- Current booking ID:', bookingId);
    
    if (connection && isConnected) {
      alert(`SignalR Connected!\nConnection ID: ${connection.connectionId}\nBooking ID: ${bookingId}`);
    } else {
      alert('SignalR Not Connected. Check console for details.');
    }
  };

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
          <div style={{ marginTop: '20px', fontSize: '12px', color: '#666' }}>
            BookingId: {bookingId}
          </div>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="booking-process-container">
        <div className="error-section">
          <div className="error-icon">⚠️</div>
          <h2>Có lỗi xảy ra</h2>
          <p>{error}</p>
          <div className="error-actions">
            <button className="btn-secondary" onClick={handleManualRefresh}>
              <span className="btn-icon">🔄</span>
              Thử lại
            </button>
            <button className="btn-primary" onClick={handleBackToHome}>
              <span className="btn-icon">🏠</span>
              Về trang chủ
            </button>
            <button className="btn-secondary" onClick={testSignalRConnection}>
              <span className="btn-icon">🧪</span>
              Test SignalR
            </button>
          </div>
          <div style={{ marginTop: '20px', fontSize: '12px', color: '#666' }}>
            BookingId: {bookingId} | SignalR: {isConnected ? 'Connected' : 'Disconnected'}
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
          <div className="error-icon">📋</div>
          <h2>Không tìm thấy thông tin đặt sân</h2>
          <p>Mã đặt sân không tồn tại hoặc đã bị xóa</p>
          <div className="error-actions">
            <button className="btn-primary" onClick={handleBackToHome}>
              <span className="btn-icon">🏠</span>
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
          icon: '⏳',
          text: 'Đang chờ xử lý',
          className: 'status-pending',
          description: 'Đặt sân đang được xử lý'
        };
      case 'confirmed':
        return {
          icon: '✅',
          text: 'Đã xác nhận',
          className: 'status-confirmed',
          description: 'Đặt sân đã được xác nhận'
        };
      case 'paid':
        return {
          icon: '💰',
          text: 'Đã thanh toán',
          className: 'status-paid',
          description: 'Đã thanh toán thành công'
        };
      case 'unpaid':
        return {
          icon: '💳',
          text: 'Chưa thanh toán',
          className: 'status-unpaid',
          description: 'Chưa thanh toán'
        };
      case 'cancelled':
        return {
          icon: '❌',
          text: 'Đã hủy',
          className: 'status-cancelled',
          description: 'Đặt sân đã bị hủy'
        };
      case 'completed':
        return {
          icon: '🎉',
          text: 'Hoàn thành',
          className: 'status-completed',
          description: 'Đã sử dụng dịch vụ'
        };
      default:
        return {
          icon: '❓',
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
            <span className="notification-icon">🔄</span>
            <span>Thông tin đặt sân đã được cập nhật!</span>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="process-header">
        <div className="header-content">
          <h1 className="page-title">
            <span className="title-icon">📋</span>
            Thông tin đặt sân
          </h1>
          <p className="page-subtitle">
            Mã đặt sân: <strong>#{bookingData.bookingId}</strong>
          </p>
          
          {/* Connection Status */}
          <div className="header-status">
            <div className={`connection-status ${isConnected ? 'connected' : 'disconnected'}`}>
              <span className="status-dot"></span>
              <span className="status-text">
                {isConnected ? 'Đang theo dõi real-time' : 'Mất kết nối real-time'}
              </span>
            </div>
            {lastUpdated && (
              <div className="last-updated">
                Cập nhật lúc: {lastUpdated.toLocaleTimeString('vi-VN')}
              </div>
            )}
          </div>
        </div>
        <div className="header-actions">
          <button className="btn-secondary" onClick={handleManualRefresh}>
            <span className="btn-icon">🔄</span>
            Làm mới
          </button>
          <button className="btn-secondary" onClick={testSignalRConnection}>
            <span className="btn-icon">🧪</span>
            Test SignalR
          </button>
          <button className="btn-back" onClick={handleBackToHome}>
            <span className="btn-icon">🏠</span>
            Về trang chủ
          </button>
        </div>
      </div>

      {/* Status Section */}
      <div className="status-section">
        <div className={`status-card ${statusInfo.className}`}>
          <div className="status-icon">{statusInfo.icon}</div>
          <div className="status-info">
            <h3 className="status-title">{statusInfo.text}</h3>
            <p className="status-description">{statusInfo.description}</p>
          </div>
        </div>
      </div>

      {/* Booking Details */}
      <div className="details-section">
        <h2 className="section-title">
          <span className="section-icon">📊</span>
          Chi tiết đặt sân
        </h2>

        <div className="details-grid">
          {/* Facility Info */}
          <div className="detail-card">
            <h3 className="card-title">
              <span className="card-icon">🏢</span>
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
              <span className="card-icon">📅</span>
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
              <span className="card-icon">👤</span>
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
              <span className="card-icon">💰</span>
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
            <span className="section-icon">🕐</span>
            Chi tiết khung giờ
          </h2>

          <div className="details-grid">
            <div className="detail-card full-width">
              <h3 className="card-title">
                <span className="card-icon">📅</span>
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

      {/* Actions */}
      {isPendingPayment && (
        <div className="actions-section">
          <h3 className="section-title">
            <span className="section-icon">🔧</span>
            Thao tác
          </h3>
          <div className="actions-content">
            <p className="actions-description">
              Đặt sân của bạn đang chờ thanh toán. Vui lòng liên hệ với chúng tôi để được hỗ trợ.
            </p>
            <button
              className="btn-payment"
              onClick={handleRetryPayment}
            >
              <span className="btn-icon">📞</span>
              Liên hệ hỗ trợ
            </button>
          </div>
        </div>
      )}

      {/* Debug Footer */}
      <div className="footer-note">
        <div className="note-content">
          <span className="note-icon">ℹ️</span>
          <p>
            Trang này sẽ tự động cập nhật khi có thay đổi trạng thái booking.
          </p>
          {/* Debug Info */}
          <div style={{ 
            marginTop: '10px', 
            padding: '10px', 
            backgroundColor: '#f8f9fa', 
            borderRadius: '4px',
            fontSize: '11px',
            color: '#666'
          }}>
            <strong>Debug Info:</strong><br/>
            BookingId: {bookingId}<br/>
            SignalR Connected: {isConnected ? '✅' : '❌'}<br/>
            Connection ID: {connection?.connectionId || 'N/A'}<br/>
            Connection State: {connection?.state || 'N/A'}<br/>
            Last Updated: {lastUpdated?.toLocaleTimeString('vi-VN') || 'N/A'}
          </div>
        </div>
      </div>
    </div>
  );
}