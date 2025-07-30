import React, { useState, useEffect } from 'react';
import './BookingHistory.scss';

const BookingHistory = () => {
    const [bookings, setBookings] = useState([]);
    const [filterStatus, setFilterStatus] = useState('all');
    const [searchTerm, setSearchTerm] = useState('');
    const [currentPage, setCurrentPage] = useState(1);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const bookingsPerPage = 8;

    // Sample data
    useEffect(() => {
        const sampleBookings = [
            {
                id: 1,
                courtName: 'Sân bóng đá mini A1',
                courtType: 'Bóng đá',
                date: '2024-01-15',
                timeSlot: '08:00 - 10:00',
                duration: '2 giờ',
                price: 200000,
                status: 'completed',
                bookingDate: '2024-01-10',
                address: 'Quận 1, TP.HCM',
                contactPhone: '0123456789',
                paymentMethod: 'Chuyển khoản',
                notes: 'Yêu cầu sân có mái che'
            },
            {
                id: 2,
                courtName: 'Sân tennis VIP B2',
                courtType: 'Tennis',
                date: '2024-01-20',
                timeSlot: '14:00 - 16:00',
                duration: '2 giờ',
                price: 300000,
                status: 'confirmed',
                bookingDate: '2024-01-12',
                address: 'Quận 3, TP.HCM',
                contactPhone: '0987654321',
                paymentMethod: 'Tiền mặt',
                notes: 'Cần chuẩn bị vợt tennis'
            },
            {
                id: 3,
                courtName: 'Sân cầu lông C3',
                courtType: 'Cầu lông',
                date: '2024-01-25',
                timeSlot: '18:00 - 20:00',
                duration: '2 giờ',
                price: 150000,
                status: 'pending',
                bookingDate: '2024-01-14',
                address: 'Quận 7, TP.HCM',
                contactPhone: '0369258147',
                paymentMethod: 'Ví điện tử',
                notes: 'Đặt sân cho 4 người'
            },
            {
                id: 4,
                courtName: 'Sân bóng chuyền D4',
                courtType: 'Bóng chuyền',
                date: '2024-01-18',
                timeSlot: '10:00 - 12:00',
                duration: '2 giờ',
                price: 180000,
                status: 'cancelled',
                bookingDate: '2024-01-08',
                address: 'Quận 5, TP.HCM',
                contactPhone: '0741852963',
                paymentMethod: 'Chuyển khoản',
                notes: 'Hủy do thời tiết xấu'
            },
            {
                id: 5,
                courtName: 'Sân pickleball E5',
                courtType: 'Pickleball',
                date: '2024-01-30',
                timeSlot: '16:00 - 18:00',
                duration: '2 giờ',
                price: 120000,
                status: 'confirmed',
                bookingDate: '2024-01-16',
                address: 'Quận 2, TP.HCM',
                contactPhone: '0852741963',
                paymentMethod: 'Ví điện tử',
                notes: 'Sân trong nhà có điều hòa'
            }
        ];
        setBookings(sampleBookings);
    }, []);

    const getStatusText = (status) => {
        const statusMap = {
            'completed': 'Đã hoàn thành',
            'confirmed': 'Đã xác nhận',
            'pending': 'Chờ xác nhận',
            'cancelled': 'Đã hủy'
        };
        return statusMap[status] || status;
    };

    const getStatusClass = (status) => {
        return `status-${status}`;
    };

    const formatPrice = (price) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(price);
    };

    const formatDate = (dateString) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('vi-VN', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    };

    const openModal = (booking) => {
        setSelectedBooking(booking);
        setIsModalOpen(true);
    };

    const closeModal = () => {
        setIsModalOpen(false);
        setSelectedBooking(null);
    };

    // Filter bookings
    const filteredBookings = bookings.filter(booking => {
        const matchesStatus = filterStatus === 'all' || booking.status === filterStatus;
        const matchesSearch = booking.courtName.toLowerCase().includes(searchTerm.toLowerCase()) ||
            booking.courtType.toLowerCase().includes(searchTerm.toLowerCase());
        return matchesStatus && matchesSearch;
    });

    // Sort by booking date (newest first)
    const sortedBookings = filteredBookings.sort((a, b) => new Date(b.bookingDate) - new Date(a.bookingDate));

    // Pagination
    const indexOfLastBooking = currentPage * bookingsPerPage;
    const indexOfFirstBooking = indexOfLastBooking - bookingsPerPage;
    const currentBookings = sortedBookings.slice(indexOfFirstBooking, indexOfLastBooking);
    const totalPages = Math.ceil(filteredBookings.length / bookingsPerPage);

    const handlePageChange = (pageNumber) => {
        setCurrentPage(pageNumber);
    };

    return (
        <div className="booking-history-page">
            {/* Main Content Container */}
            <div className="main-container">
                <div className="page-header" style={{ marginTop: '4%' }}>
                    <div className="header-content">
                        <h1 className="page-title">
                            <span className="title-icon">🏟️</span>
                            Lịch Sử Đặt Sân
                        </h1>
                        <p className="page-subtitle">
                            Quản lý và theo dõi tất cả các lần đặt sân của bạn
                        </p>
                    </div>
                </div>

                <div className="content-wrapper">
                    {/* Filters Section */}
                    <div className="filters-section">
                        <div className="search-filter">
                            <div className="search-input-wrapper">
                                <svg className="search-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                                </svg>
                                <input
                                    type="text"
                                    placeholder="Tìm kiếm theo tên sân, loại sân..."
                                    value={searchTerm}
                                    onChange={(e) => setSearchTerm(e.target.value)}
                                    className="search-input"
                                />
                            </div>
                        </div>

                        <div className="status-filter">
                            <select
                                value={filterStatus}
                                onChange={(e) => setFilterStatus(e.target.value)}
                                className="status-select"
                            >
                                <option value="all">🎯 Tất cả trạng thái</option>
                                <option value="completed">✅ Đã hoàn thành</option>
                                <option value="confirmed">🎫 Đã xác nhận</option>
                                <option value="pending">⏳ Chờ xác nhận</option>
                                <option value="cancelled">❌ Đã hủy</option>
                            </select>
                        </div>

                        <div className="results-count">
                            <span>{filteredBookings.length} kết quả</span>
                        </div>
                    </div>

                    {/* Bookings List */}
                    <div className="bookings-section">
                        {currentBookings.length > 0 ? (
                            <>
                                <div className="bookings-list">
                                    {currentBookings.map(booking => (
                                        <div key={booking.id} className="booking-row">
                                            <div className="booking-main">
                                                <div className="booking-id-section">
                                                    <span className="booking-id">#{booking.id.toString().padStart(4, '0')}</span>
                                                    <span className="booking-created">
                                                        Đặt ngày: {formatDate(booking.bookingDate)}
                                                    </span>
                                                </div>

                                                <div className="court-info">
                                                    <h3 className="court-name">{booking.courtName}</h3>
                                                    <span className={`court-type type-${booking.courtType.toLowerCase().replace(/\s+/g, '-')}`}>
                                                        {booking.courtType}
                                                    </span>
                                                </div>

                                                <div className="booking-details">
                                                    <div className="detail-item">
                                                        <svg className="detail-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3a1 1 0 011-1h6a1 1 0 011 1v4h3a2 2 0 012 2v1l-2 13H5L3 10V9a2 2 0 012-2h3z" />
                                                        </svg>
                                                        <div className="detail-content">
                                                            <span className="detail-label">Ngày chơi</span>
                                                            <span className="detail-value">{formatDate(booking.date)}</span>
                                                        </div>
                                                    </div>

                                                    <div className="detail-item">
                                                        <svg className="detail-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                                                        </svg>
                                                        <div className="detail-content">
                                                            <span className="detail-label">Thời gian</span>
                                                            <span className="detail-value">{booking.timeSlot}</span>
                                                        </div>
                                                    </div>

                                                    <div className="detail-item">
                                                        <svg className="detail-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                                                        </svg>
                                                        <div className="detail-content">
                                                            <span className="detail-label">Địa điểm</span>
                                                            <span className="detail-value">{booking.address}</span>
                                                        </div>
                                                    </div>

                                                    <div className="detail-item">
                                                        <svg className="detail-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
                                                        </svg>
                                                        <div className="detail-content">
                                                            <span className="detail-label">Giá tiền</span>
                                                            <span className="detail-value price">{formatPrice(booking.price)}</span>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="booking-status-section">
                                                <div className={`status-badge ${getStatusClass(booking.status)}`}>
                                                    <span className="status-dot"></span>
                                                    {getStatusText(booking.status)}
                                                </div>

                                                <div className="booking-actions">
                                                    <button
                                                        className="btn btn-outline btn-sm"
                                                        onClick={() => openModal(booking)}
                                                    >
                                                        <svg className="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                                                        </svg>
                                                        Chi tiết
                                                    </button>

                                                    {booking.status === 'confirmed' && (
                                                        <button className="btn btn-danger btn-sm">
                                                            <svg className="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                                            </svg>
                                                            Hủy
                                                        </button>
                                                    )}

                                                    {booking.status === 'completed' && (
                                                        <button className="btn btn-primary btn-sm">
                                                            <svg className="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                                            </svg>
                                                            Đặt lại
                                                        </button>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>

                                {/* Pagination */}
                                {totalPages > 1 && (
                                    <div className="pagination-wrapper">
                                        <div className="pagination">
                                            <button
                                                className="pagination-btn prev"
                                                onClick={() => handlePageChange(currentPage - 1)}
                                                disabled={currentPage === 1}
                                            >
                                                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                                                </svg>
                                                Trước
                                            </button>

                                            <div className="pagination-numbers">
                                                {[...Array(totalPages)].map((_, index) => (
                                                    <button
                                                        key={index + 1}
                                                        className={`pagination-number ${currentPage === index + 1 ? 'active' : ''}`}
                                                        onClick={() => handlePageChange(index + 1)}
                                                    >
                                                        {index + 1}
                                                    </button>
                                                ))}
                                            </div>

                                            <button
                                                className="pagination-btn next"
                                                onClick={() => handlePageChange(currentPage + 1)}
                                                disabled={currentPage === totalPages}
                                            >
                                                Sau
                                                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                                                </svg>
                                            </button>
                                        </div>
                                    </div>
                                )}
                            </>
                        ) : (
                            <div className="empty-state">
                                <div className="empty-illustration">
                                    <svg viewBox="0 0 200 200" className="empty-svg">
                                        <circle cx="100" cy="100" r="80" fill="#f0f9ff" stroke="#e0f2fe" strokeWidth="2" />
                                        <path d="M70 85h60M70 105h40M70 125h50" stroke="#64b5f6" strokeWidth="3" strokeLinecap="round" />
                                        <circle cx="140" cy="70" r="25" fill="#fff" stroke="#64b5f6" strokeWidth="2" />
                                        <path d="M130 70l5 5 10-10" stroke="#64b5f6" strokeWidth="2" fill="none" strokeLinecap="round" />
                                    </svg>
                                </div>
                                <h3 className="empty-title">Chưa có lịch sử đặt sân</h3>
                                <p className="empty-description">
                                    Bạn chưa có lần đặt sân nào hoặc không tìm thấy kết quả phù hợp với bộ lọc hiện tại.
                                </p>
                                <button className="btn btn-primary btn-lg">
                                    <svg className="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                                    </svg>
                                    Đặt sân ngay
                                </button>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {/* Modal Chi tiết đơn - giữ nguyên như cũ */}
            {isModalOpen && selectedBooking && (
                <div className="modal-overlay" onClick={closeModal}>
                    <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h2 className="modal-title">
                                <span className="modal-icon">📋</span>
                                Chi tiết đơn đặt sân
                            </h2>
                            <button className="modal-close" onClick={closeModal}>
                                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </div>

                        <div className="modal-body">
                            <div className="booking-detail-card">
                                <div className="detail-header">
                                    <div className="booking-id-section">
                                        <span className="booking-id">#{selectedBooking.id.toString().padStart(4, '0')}</span>
                                        <span className={`status-badge ${getStatusClass(selectedBooking.status)}`}>
                                            <span className="status-dot"></span>
                                            {getStatusText(selectedBooking.status)}
                                        </span>
                                    </div>
                                    <div className="booking-dates">
                                        <span className="booking-created">Đặt ngày: {formatDate(selectedBooking.bookingDate)}</span>
                                    </div>
                                </div>

                                <div className="detail-content">
                                    <div className="detail-section">
                                        <h3 className="section-title">
                                            <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h1a1 1 0 011 1v5m-4 0h4" />
                                            </svg>
                                            Thông tin sân
                                        </h3>
                                        <div className="detail-grid">
                                            <div className="detail-item">
                                                <span className="label">Tên sân:</span>
                                                <span className="value">{selectedBooking.courtName}</span>
                                            </div>
                                            <div className="detail-item">
                                                <span className="label">Loại sân:</span>
                                                <span className={`value court-type type-${selectedBooking.courtType.toLowerCase().replace(/\s+/g, '-')}`}>
                                                    {selectedBooking.courtType}
                                                </span>
                                            </div>
                                            <div className="detail-item">
                                                <span className="label">Địa chỉ:</span>
                                                <span className="value">{selectedBooking.address}</span>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="detail-section">
                                        <h3 className="section-title">
                                            <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3a1 1 0 011-1h6a1 1 0 011 1v4h3a2 2 0 012 2v1l-2 13H5L3 10V9a2 2 0 012-2h3z" />
                                            </svg>
                                            Thông tin đặt sân
                                        </h3>
                                        <div className="detail-grid">
                                            <div className="detail-item">
                                                <span className="label">Ngày chơi:</span>
                                                <span className="value">{formatDate(selectedBooking.date)}</span>
                                            </div>
                                            <div className="detail-item">
                                                <span className="label">Giờ chơi:</span>
                                                <span className="value">{selectedBooking.timeSlot}</span>
                                            </div>
                                            <div className="detail-item">
                                                <span className="label">Thời lượng:</span>
                                                <span className="value">{selectedBooking.duration}</span>
                                            </div>
                                            <div className="detail-item">
                                                <span className="label">Số điện thoại:</span>
                                                <span className="value">{selectedBooking.contactPhone}</span>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="detail-section">
                                        <h3 className="section-title">
                                            <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
                                            </svg>
                                            Thông tin thanh toán
                                        </h3>
                                        <div className="detail-grid">
                                            <div className="detail-item">
                                                <span className="label">Tổng tiền:</span>
                                                <span className="value price">{formatPrice(selectedBooking.price)}</span>
                                            </div>
                                            <div className="detail-item">
                                                <span className="label">Phương thức:</span>
                                                <span className="value">{selectedBooking.paymentMethod}</span>
                                            </div>
                                        </div>
                                    </div>

                                    {selectedBooking.notes && (
                                        <div className="detail-section">
                                            <h3 className="section-title">
                                                <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                                                </svg>
                                                Ghi chú
                                            </h3>
                                            <div className="notes-content">
                                                {selectedBooking.notes}
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="modal-footer">
                            <button className="btn btn-outline" onClick={closeModal}>
                                Đóng
                            </button>
                            {selectedBooking.status === 'confirmed' && (
                                <button className="btn btn-danger">
                                    Hủy đặt sân
                                </button>
                            )}
                            {selectedBooking.status === 'completed' && (
                                <button className="btn btn-primary">
                                    Đặt lại sân này
                                </button>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default BookingHistory;