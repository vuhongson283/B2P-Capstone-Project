import React, { useState, useEffect, useMemo } from 'react';
import './BookingHistory.scss';
import { useAuth } from '../../context/AuthContext';
import { message, Spin, Rate, Input, Button } from 'antd';
import {
    getBookingsByUserId,
    getAccountById,
    getCourtDetail,
    createRating
} from '../../services/apiService';
import dayjs from 'dayjs';

const { TextArea } = Input;

const BookingHistory = () => {
    const [bookings, setBookings] = useState([]);
    const [filterStatus, setFilterStatus] = useState('all');
    const [searchTerm, setSearchTerm] = useState('');
    const [currentPage, setCurrentPage] = useState(1);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [customerLoading, setCustomerLoading] = useState(false);

    // Rating states
    const [ratingData, setRatingData] = useState({
        rating: 0,
        comment: ''
    });
    const [isSubmittingRating, setIsSubmittingRating] = useState(false);
    const [hasRated, setHasRated] = useState(false);
    const [existingRating, setExistingRating] = useState(null);

    const bookingsPerPage = 8;
    const { userId } = useAuth();

    useEffect(() => {
        loadBookingHistory();
    }, []);

    const calculateDuration = (startTime, endTime) => {
        if (!startTime || !endTime) return 'N/A';
        try {
            const start = dayjs(`2000-01-01 ${startTime}`);
            const end = dayjs(`2000-01-01 ${endTime}`);
            const diffInHours = end.diff(start, 'hour');
            return `${diffInHours} giờ`;
        } catch (error) {
            return 'N/A';
        }
    };

    const mapBookingStatus = (apiStatus, statusId) => {
        if (statusId === 10) {
            return 'completed';
        } else if (statusId === 7) {
            return 'deposit-paid';
        }

        const statusMap = {
            'Active': 'confirmed',
            'Paid': 'deposit-paid',
            'Confirmed': 'confirmed',
            'Pending': 'pending',
            'Cancelled': 'cancelled',
            'Completed': 'completed'
        };
        return statusMap[apiStatus] || 'pending';
    };

    const getPaymentMethod = (status, statusId) => {
        if (statusId === 10) {
            return 'Đã thanh toán đầy đủ';
        } else if (statusId === 7) {
            return 'Đã thanh toán cọc';
        }

        const paymentMap = {
            'Paid': 'Đã thanh toán cọc',
            'Active': 'Chuyển khoản',
            'Confirmed': 'Tiền mặt',
            'Pending': 'Chưa thanh toán'
        };
        return paymentMap[status] || 'N/A';
    };

    const loadCourtDetails = async (courtId) => {
        try {
            console.log(`🏟️ [DEBUG] Loading court details for courtId: ${courtId}`);

            const response = await getCourtDetail(courtId);
            console.log(`📋 [DEBUG] Full response:`, response);

            const data = response.data;
            const result = {
                facilityName: data.facilityName || 'N/A',
                facilityAddress: data.location || 'N/A',
                facilityContact: data.contact || 'N/A',
                facilityId: data.facilityId || null
            };

            console.log(`🎯 [DEBUG] Extracted result:`, result);
            return result;

        } catch (error) {
            console.error(`❌ [DEBUG] Error:`, error);
            return {
                facilityName: 'Error',
                facilityAddress: 'Error',
                facilityContact: 'Error',
                facilityId: null
            };
        }
    };

    const processBookingData = async (bookingsData) => {
        console.log('🔄 [DEBUG] Processing booking data...', bookingsData);
        const processedBookings = [];

        for (const booking of bookingsData) {
            console.log(`📝 [DEBUG] Processing booking:`, booking);

            if (booking.slots && Array.isArray(booking.slots)) {
                console.log(`🎫 [DEBUG] Found ${booking.slots.length} slots for booking ${booking.bookingId}`);

                for (const slot of booking.slots) {
                    console.log(`🎫 [DEBUG] Processing slot:`, slot);
                    console.log(`🏟️ [DEBUG] Court ID from slot: ${slot.courtId}`);

                    const courtDetails = await loadCourtDetails(slot.courtId);
                    console.log(`🏟️ [DEBUG] Court details loaded:`, courtDetails);

                    const processedBooking = {
                        id: booking.bookingId || booking.id,
                        courtId: slot.courtId,
                        courtName: slot.courtName || `Sân ${slot.courtId}`,
                        courtType: slot.categoryName || 'Sân thể thao',
                        date: booking.checkInDate,
                        timeSlot: `${slot.startTime?.substring(0, 5)} - ${slot.endTime?.substring(0, 5)}`,
                        startTime: slot.startTime,
                        endTime: slot.endTime,
                        duration: calculateDuration(slot.startTime, slot.endTime),
                        price: booking.totalPrice || 0,
                        status: mapBookingStatus(booking.status, booking.statusId),
                        originalStatus: booking.status,
                        statusId: booking.statusId,
                        bookingDate: booking.checkInDate,
                        checkInDate: booking.checkInDate,
                        userId: booking.userId,
                        timeSlotId: slot.timeSlotId,
                        contactPhone: 'N/A',
                        paymentMethod: getPaymentMethod(booking.status, booking.statusId),
                        notes: booking.notes || '',
                        facilityName: courtDetails.facilityName,
                        facilityAddress: courtDetails.facilityAddress,
                        facilityContact: courtDetails.facilityContact,
                        facilityId: courtDetails.facilityId,
                        customerName: 'Đang tải...',
                        customerPhone: 'Đang tải...',
                        customerEmail: 'Đang tải...',
                        uniqueKey: `${booking.bookingId}-${slot.courtId}-${slot.timeSlotId}`,

                        // Thông tin rating từ API (nếu có)
                        hasRated: booking.hasRated || booking.isRated || false,
                        ratingInfo: booking.rating || booking.ratingData || null,
                        existingRating: booking.existingRating || null
                    };

                    console.log(`✅ [DEBUG] Processed booking:`, processedBooking);
                    processedBookings.push(processedBooking);
                }
            } else {
                console.warn(`⚠️ [DEBUG] No slots found for booking ${booking.bookingId}`);
            }
        }

        console.log('✅ [DEBUG] All processed bookings with court details:', processedBookings);
        return processedBookings;
    };

    const loadBookingHistory = async () => {
        try {
            setLoading(true);
            console.log('📚 Loading booking history for userId:', userId);

            const response = await getBookingsByUserId(userId, 1, 1000);
            console.log('📅 Bookings API Response:', response.data);

            let bookingsData = [];
            if (response.data && response.data.items) {
                bookingsData = response.data.items;
            }

            console.log('📅 Raw bookings data:', bookingsData);

            if (bookingsData.length === 0) {
                message.info('Không có lịch sử đặt sân nào');
                setBookings([]);
                return;
            }

            const processedBookings = await processBookingData(bookingsData);
            setBookings(processedBookings);

            if (processedBookings.length > 0) {
                loadCustomerInfoForBookings(processedBookings.slice(0, bookingsPerPage));
            }

        } catch (error) {
            console.error('❌ Error loading booking history:', error);
            message.error('Không thể tải lịch sử đặt sân');
            setBookings([]);
        } finally {
            setLoading(false);
        }
    };

    const loadCustomerInfoForBookings = async (bookingsToLoad) => {
        for (const booking of bookingsToLoad) {
            if (booking.userId) {
                try {
                    const customerInfo = await loadCustomerDetails(booking.userId);
                    if (customerInfo) {
                        setBookings(prevBookings =>
                            prevBookings.map(b =>
                                b.uniqueKey === booking.uniqueKey
                                    ? {
                                        ...b,
                                        customerName: customerInfo.customerName,
                                        customerPhone: customerInfo.customerPhone,
                                        customerEmail: customerInfo.customerEmail,
                                        contactPhone: customerInfo.customerPhone
                                    }
                                    : b
                            )
                        );
                    }
                } catch (error) {
                    console.error('Error loading customer info for booking:', booking.id, error);
                }
            }
        }
    };

    const loadCustomerDetails = async (userId) => {
        try {
            console.log('👤 Loading customer details for userId:', userId);
            const response = await getAccountById(userId);

            let customerData = null;
            if (response.data) {
                if (response.data.data) {
                    customerData = response.data.data;
                } else if (response.data.user) {
                    customerData = response.data.user;
                } else {
                    customerData = response.data;
                }
            }

            if (customerData) {
                return {
                    customerName: customerData.fullName || customerData.name || customerData.userName || 'N/A',
                    customerPhone: customerData.phoneNumber || customerData.phone || 'N/A',
                    customerEmail: customerData.email || 'N/A'
                };
            }

            return null;
        } catch (error) {
            console.error('❌ Error loading customer details:', error);
            return null;
        }
    };

    // Rating functions
    const handleRatingSubmit = async () => {
        if (ratingData.rating === 0) {
            message.warning('Vui lòng chọn số sao đánh giá');
            return;
        }

        try {
            setIsSubmittingRating(true);

            const ratingPayload = {
                bookingId: selectedBooking.id,
                userId: userId,
                courtId: selectedBooking.courtId,
                facilityId: selectedBooking.facilityId,
                rating: ratingData.rating,
                comment: ratingData.comment.trim(),
                ratingDate: new Date().toISOString()
            };

            console.log('📝 Submitting rating:', ratingPayload);

            const response = await createRating(ratingPayload);

            if (response.status === 200 || response.status === 201) {
                message.success('Đánh giá của bạn đã được gửi thành công!');

                // Set existing rating data để hiển thị
                setExistingRating({
                    rating: ratingData.rating,
                    comment: ratingData.comment,
                    ratingDate: new Date().toISOString()
                });

                setHasRated(true);
                setRatingData({ rating: 0, comment: '' });

                // Cập nhật booking trong state để reflect hasRated = true
                setBookings(prevBookings =>
                    prevBookings.map(b =>
                        b.id === selectedBooking.id
                            ? { ...b, hasRated: true, ratingInfo: { rating: ratingData.rating, comment: ratingData.comment } }
                            : b
                    )
                );
            } else {
                throw new Error('Failed to submit rating');
            }
        } catch (error) {
            console.error('❌ Error submitting rating:', error);
            message.error('Không thể gửi đánh giá. Vui lòng thử lại!');
        } finally {
            setIsSubmittingRating(false);
        }
    };

    const resetRatingForm = () => {
        setRatingData({ rating: 0, comment: '' });
        setHasRated(false);
        setExistingRating(null);
    };

    const getStatusText = (status) => {
        const statusMap = {
            'completed': 'Đã hoàn thành',
            'confirmed': 'Đã xác nhận',
            'pending': 'Chờ xác nhận',
            'cancelled': 'Đã hủy',
            'deposit-paid': 'Đã thanh toán cọc'
        };
        return statusMap[status] || status;
    };

    const getStatusClass = (status) => {
        return `status-${status}`;
    };

    const formatPrice = (price) => {
        if (!price || price === 0) return '0 VNĐ';
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(price);
    };

    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        try {
            const date = dayjs(dateString);
            if (!date.isValid()) return 'N/A';
            return date.format('DD/MM/YYYY');
        } catch (error) {
            return 'N/A';
        }
    };

    const formatDateTime = (dateTimeString) => {
        if (!dateTimeString) return 'N/A';
        try {
            const date = dayjs(dateTimeString);
            if (!date.isValid()) return 'N/A';
            return date.format('DD/MM/YYYY [lúc] HH:mm');
        } catch (error) {
            return 'N/A';
        }
    };

    const openModal = async (booking) => {
        setSelectedBooking(booking);
        setIsModalOpen(true);
        resetRatingForm();

        // Set rating status từ booking data
        if (booking.status === 'completed') {
            if (booking.hasRated) {
                setHasRated(true);
                setExistingRating(booking.ratingInfo || booking.existingRating);
                console.log('✅ Booking already rated:', booking.ratingInfo);
            } else {
                setHasRated(false);
                setExistingRating(null);
                console.log('❌ Booking not rated yet');
            }
        }

        if (booking.customerName === 'Đang tải...' && booking.userId) {
            setCustomerLoading(true);
            try {
                const customerInfo = await loadCustomerDetails(booking.userId);
                if (customerInfo) {
                    setSelectedBooking(prev => ({
                        ...prev,
                        ...customerInfo,
                        contactPhone: customerInfo.customerPhone
                    }));
                }
            } catch (error) {
                console.error('Error loading customer details in modal:', error);
            } finally {
                setCustomerLoading(false);
            }
        }
    };

    const closeModal = () => {
        setIsModalOpen(false);
        setSelectedBooking(null);
        setCustomerLoading(false);
        resetRatingForm();
    };

    const filteredBookings = useMemo(() => {
        return bookings.filter(booking => {
            const matchesStatus = filterStatus === 'all' || booking.status === filterStatus;
            const matchesSearch = booking.courtName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                booking.courtType.toLowerCase().includes(searchTerm.toLowerCase());
            return matchesStatus && matchesSearch;
        });
    }, [bookings, filterStatus, searchTerm]);

    const sortedBookings = useMemo(() => {
        return filteredBookings.sort((a, b) => {
            const dateA = dayjs(a.bookingDate);
            const dateB = dayjs(b.bookingDate);
            return dateB.diff(dateA);
        });
    }, [filteredBookings]);

    const indexOfLastBooking = currentPage * bookingsPerPage;
    const indexOfFirstBooking = indexOfLastBooking - bookingsPerPage;
    const currentBookings = sortedBookings.slice(indexOfFirstBooking, indexOfLastBooking);
    const totalPages = Math.ceil(filteredBookings.length / bookingsPerPage);

    useEffect(() => {
        if (currentBookings.length > 0) {
            const bookingsNeedingCustomerInfo = currentBookings.filter(b => b.customerName === 'Đang tải...');
            if (bookingsNeedingCustomerInfo.length > 0) {
                loadCustomerInfoForBookings(bookingsNeedingCustomerInfo);
            }
        }
    }, [currentPage, filteredBookings]);

    const handlePageChange = (pageNumber) => {
        setCurrentPage(pageNumber);
    };

    if (loading) {
        return (
            <div className="booking-history-page">
                <div className="main-container">
                    <div style={{
                        display: 'flex',
                        justifyContent: 'center',
                        alignItems: 'center',
                        minHeight: '400px',
                        flexDirection: 'column',
                        gap: '16px'
                    }}>
                        <Spin size="large" />
                        <span>Đang tải lịch sử đặt sân...</span>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="booking-history-page">
            <div className="main-container">
                <div className="page-header" style={{ marginTop: '2%' }}>
                    <div className="header-content">
                        <h1 className="page-title">Lịch Sử Đặt Sân</h1>
                        <p className="page-subtitle">
                            Quản lý và theo dõi tất cả các lần đặt sân của bạn ({bookings.length} đơn đặt)
                        </p>
                    </div>
                </div>

                <div className="content-wrapper">
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
                                <option value="all">Tất cả trạng thái</option>
                                <option value="completed">Đã hoàn thành</option>
                                <option value="deposit-paid">Đã thanh toán cọc</option>
                                <option value="confirmed">Đã xác nhận</option>
                                <option value="pending">Chờ xác nhận</option>
                                <option value="cancelled">Đã hủy</option>
                            </select>
                        </div>

                        <div className="results-count">
                            <span>{filteredBookings.length} kết quả</span>
                        </div>
                    </div>

                    <div className="bookings-section">
                        {currentBookings.length > 0 ? (
                            <>
                                <div className="bookings-list">
                                    {currentBookings.map((booking) => (
                                        <div key={booking.uniqueKey} className="booking-row">
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
                                                    <div className="facility-info">
                                                        <small>{booking.facilityName}</small>
                                                    </div>
                                                    {booking.hasRated && (
                                                        <div className="rated-indicator">
                                                            <span className="rated-badge">⭐ Đã đánh giá</span>
                                                        </div>
                                                    )}
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

                                                    {booking.status === 'deposit-paid' && (
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

            {/* MODAL */}
            {isModalOpen && selectedBooking && (
                <div className="modal-overlay" onClick={closeModal}>
                    <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h2 className="modal-title">Chi tiết đơn đặt sân</h2>
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
                                        <div className="booking-info-left">
                                            <span className="booking-id">#{selectedBooking.id.toString().padStart(4, '0')}</span>
                                            <span className="booking-created">Đặt lúc: {formatDateTime(selectedBooking.bookingDate)}</span>
                                        </div>
                                        <span className={`status-badge ${getStatusClass(selectedBooking.status)}`}>
                                            <span className="status-dot"></span>
                                            {getStatusText(selectedBooking.status)}
                                        </span>
                                    </div>
                                </div>

                                <div className="detail-content">
                                    <div className="quick-info-cards">
                                        <div className="quick-card time-card">
                                            <div className="card-label">Thời gian</div>
                                            <div className="card-value">{selectedBooking.timeSlot}</div>
                                        </div>
                                        <div className="quick-card price-card">
                                            <div className="card-label">Tổng tiền</div>
                                            <div className="card-value">{formatPrice(selectedBooking.price)}</div>
                                        </div>
                                        <div className="quick-card duration-card">
                                            <div className="card-label">Thời lượng</div>
                                            <div className="card-value">{selectedBooking.duration}</div>
                                        </div>
                                    </div>

                                    <div className="detail-section">
                                        <h3 className="section-title">
                                            <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h1a1 1 0 011 1v5m-4 0h4" />
                                            </svg>
                                            Thông tin cơ sở
                                        </h3>
                                        <div className="detail-grid">
                                            <div className="detail-item">
                                                <div className="label">Tên cơ sở</div>
                                                <div className="value">{selectedBooking.facilityName}</div>
                                            </div>
                                            <div className="detail-item">
                                                <div className="label">Địa chỉ</div>
                                                <div className="value">{selectedBooking.facilityAddress}</div>
                                            </div>
                                            <div className="detail-item">
                                                <div className="label">Số liên hệ cơ sở</div>
                                                <div className="value">{selectedBooking.facilityContact}</div>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="detail-section">
                                        <h3 className="section-title">
                                            <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h1a1 1 0 011 1v5m-4 0h4" />
                                            </svg>
                                            Thông tin sân
                                        </h3>
                                        <div className="detail-grid">
                                            <div className="detail-item">
                                                <div className="label">Tên sân</div>
                                                <div className="value">{selectedBooking.courtName}</div>
                                            </div>
                                            <div className="detail-item">
                                                <div className="label">Loại sân</div>
                                                <div className="value court-type">{selectedBooking.courtType}</div>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="detail-section">
                                        <h3 className="section-title">
                                            <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3a1 1 0 011-1h6a1 1 0 011 1v4h3a2 2 0 012 2v1l-2 13H5L3 10V9a2 2 0 012-2h3z" />
                                            </svg>
                                            Chi tiết đặt sân
                                        </h3>
                                        <div className="detail-grid">
                                            <div className="detail-item">
                                                <div className="label">Ngày chơi</div>
                                                <div className="value">{formatDate(selectedBooking.date)}</div>
                                            </div>
                                            <div className="detail-item">
                                                <div className="label">Số điện thoại khách</div>
                                                <div className="value">
                                                    {customerLoading ? <Spin size="small" /> : selectedBooking.contactPhone}
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="detail-section">
                                        <h3 className="section-title">
                                            <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                                            </svg>
                                            Thông tin khách hàng
                                        </h3>
                                        <div className="detail-grid">
                                            <div className="detail-item">
                                                <div className="label">Tên khách hàng</div>
                                                <div className="value">
                                                    {customerLoading ? <Spin size="small" /> : selectedBooking.customerName}
                                                </div>
                                            </div>
                                            <div className="detail-item">
                                                <div className="label">Email</div>
                                                <div className="value">
                                                    {customerLoading ? <Spin size="small" /> : selectedBooking.customerEmail}
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="detail-section">
                                        <h3 className="section-title">
                                            <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
                                            </svg>
                                            Thanh toán
                                        </h3>
                                        <div className="detail-grid">
                                            <div className="detail-item">
                                                <div className="label">Tổng tiền</div>
                                                <div className="value price">{formatPrice(selectedBooking.price)}</div>
                                            </div>
                                            <div className="detail-item">
                                                <div className="label">Trạng Thái</div>
                                                <div className="value">{selectedBooking.paymentMethod}</div>
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

                                    {/* RATING SECTION - CHỈ HIỂN THỊ KHI ĐÃ HOÀN THÀNH */}
                                    {selectedBooking.status === 'completed' && (
                                        <div className="detail-section rating-section">
                                            <h3 className="section-title">
                                                <svg className="section-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
                                                </svg>
                                                Đánh giá dịch vụ
                                            </h3>

                                            {hasRated || existingRating ? (
                                                <div className="rating-already-exists">
                                                    <div className="existing-rating-icon">⭐</div>
                                                    <h4>Bạn đã đánh giá</h4>
                                                    <p>Cảm ơn bạn đã đánh giá dịch vụ này trước đó.</p>

                                                    {existingRating && (
                                                        <div className="existing-rating-details">
                                                            <div className="existing-rating-stars">
                                                                <Rate
                                                                    disabled
                                                                    value={existingRating.rating || existingRating.stars || 5}
                                                                    style={{ fontSize: '1.5rem', color: '#fbbf24' }}
                                                                />
                                                                <span className="rating-value">
                                                                    {existingRating.rating || existingRating.stars || 5}/5 sao
                                                                </span>
                                                            </div>

                                                            {(existingRating.comment || existingRating.review) && (
                                                                <div className="existing-rating-comment">
                                                                    <strong>Nhận xét của bạn:</strong>
                                                                    <p>"{existingRating.comment || existingRating.review}"</p>
                                                                </div>
                                                            )}

                                                            <div className="rating-date">
                                                                <small>
                                                                    Đánh giá vào: {formatDateTime(existingRating.ratingDate || existingRating.createdAt || selectedBooking.checkInDate)}
                                                                </small>
                                                            </div>
                                                        </div>
                                                    )}
                                                </div>
                                            ) : (
                                                <div className="rating-form">
                                                    <div className="rating-stars">
                                                        <label className="rating-label">Đánh giá của bạn:</label>
                                                        <Rate
                                                            value={ratingData.rating}
                                                            onChange={(value) => setRatingData(prev => ({ ...prev, rating: value }))}
                                                            style={{ fontSize: '2rem', color: '#fbbf24' }}
                                                            allowHalf={false}
                                                        />
                                                        <span className="rating-text">
                                                            {ratingData.rating > 0 && (
                                                                <>
                                                                    {ratingData.rating} {ratingData.rating === 1 ? 'sao' : 'sao'}
                                                                    {ratingData.rating <= 2 && ' - Không hài lòng'}
                                                                    {ratingData.rating === 3 && ' - Bình thường'}
                                                                    {ratingData.rating === 4 && ' - Hài lòng'}
                                                                    {ratingData.rating === 5 && ' - Rất hài lòng'}
                                                                </>
                                                            )}
                                                        </span>
                                                    </div>

                                                    <div className="rating-comment">
                                                        <label className="rating-label">Nhận xét của bạn:</label>
                                                        <TextArea
                                                            rows={4}
                                                            placeholder="Chia sẻ trải nghiệm của bạn về cơ sở và dịch vụ..."
                                                            value={ratingData.comment}
                                                            onChange={(e) => setRatingData(prev => ({ ...prev, comment: e.target.value }))}
                                                            maxLength={500}
                                                            showCount
                                                        />
                                                    </div>

                                                    <div className="rating-actions">
                                                        <Button
                                                            type="primary"
                                                            size="large"
                                                            loading={isSubmittingRating}
                                                            onClick={handleRatingSubmit}
                                                            disabled={ratingData.rating === 0}
                                                            className="rating-submit-btn"
                                                        >
                                                            <svg className="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                                                            </svg>
                                                            Gửi đánh giá
                                                        </Button>
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="modal-footer">
                            {selectedBooking.status === 'confirmed' && (
                                <button className="btn btn-danger btn-action">
                                    Hủy đặt sân
                                </button>
                            )}
                            {selectedBooking.status === 'completed' && (
                                <button className="btn btn-primary btn-action">
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