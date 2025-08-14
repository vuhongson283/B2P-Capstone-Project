import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom"; // Thêm useNavigate để điều hướng
import "./BookingDetail.scss";

// Helper function to format currency
const formatCurrency = (amount) => {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0,
    }).format(amount);
};

export default function BookingDetail({
    open,
    onClose,
    facilityId,
    categoryId,
    listSlotId, // Thêm prop mới cho danh sách slot ID
    totalPrice,
    facilityData,
    selectedDate,
    selectedSlots,
    quantities,
    createBooking, // Thêm prop function từ parent để gọi API
    createPayment // Thêm prop function để gọi API tạo thanh toán
}) {
    const navigate = useNavigate(); // Hook để điều hướng
    const [formData, setFormData] = useState({
        phone: '',
        email: '',
        paymentMethod: 'domestic' // Thêm phương thức thanh toán mặc định
    });
    const [errors, setErrors] = useState({});
    const [isSubmitting, setIsSubmitting] = useState(false);

    // Reset form when modal opens
    useEffect(() => {
        if (open) {
            setFormData({
                phone: '',
                email: '',
                paymentMethod: 'domestic'
            });
            setErrors({});
        }
    }, [open]);

    // Close modal when clicking outside
    const handleOverlayClick = (e) => {
        if (e.target === e.currentTarget) {
            onClose();
        }
    };

    // Close modal with Escape key
    useEffect(() => {
        const handleEscape = (e) => {
            if (e.key === 'Escape') {
                onClose();
            }
        };

        if (open) {
            document.addEventListener('keydown', handleEscape);
            document.body.style.overflow = 'hidden';
        }

        return () => {
            document.removeEventListener('keydown', handleEscape);
            document.body.style.overflow = 'unset';
        };
    }, [open, onClose]);

    if (!open) return null;

    // Validate form
    const validateForm = () => {
        const newErrors = {};

        // Phone validation
        if (!formData.phone.trim()) {
            newErrors.phone = 'Vui lòng nhập số điện thoại';
        } else if (!/^[0-9]{10,11}$/.test(formData.phone.replace(/\s/g, ''))) {
            newErrors.phone = 'Số điện thoại không hợp lệ (10-11 chữ số)';
        }

        // Email validation
        if (!formData.email.trim()) {
            newErrors.email = 'Vui lòng nhập email';
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
            newErrors.email = 'Email không hợp lệ';
        }

        // Payment method validation
        if (!formData.paymentMethod) {
            newErrors.paymentMethod = 'Vui lòng chọn phương thức thanh toán';
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    // Handle input change
    const handleInputChange = (field, value) => {
        setFormData(prev => ({
            ...prev,
            [field]: value
        }));

        // Clear error when user starts typing
        if (errors[field]) {
            setErrors(prev => ({
                ...prev,
                [field]: ''
            }));
        }
    };

    // Format date for display
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

    // Get selected category name
    const getSelectedCategoryName = () => {
        if (!facilityData?.categories || !categoryId) return '';
        const category = facilityData.categories.find(cat =>
            cat.categoryId.toString() === categoryId.toString()
        );
        return category ? category.categoryName : '';
    };

    // Handle final booking submission
    const handleFinalBooking = async () => {
        if (!validateForm()) {
            return;
        }

        setIsSubmitting(true);
        try {
            // Format ngày theo chuẩn ISO với timezone
            const formatDateForAPI = (dateString) => {
                if (!dateString) return new Date().toISOString();

                // Nếu dateString chỉ là date (YYYY-MM-DD), thêm time
                if (dateString.length === 10) {
                    return new Date(dateString + 'T00:00:00.000Z').toISOString();
                }

                // Nếu đã có full datetime, convert sang ISO
                return new Date(dateString).toISOString();
            };

            // Debug: Kiểm tra dữ liệu trước khi gửi
            console.log('listSlotId received:', listSlotId);
            console.log('selectedSlots:', selectedSlots);
            console.log('facilityId:', facilityId);
            console.log('categoryId:', categoryId);
            console.log('paymentMethod:', formData.paymentMethod);

            // Chuẩn bị dữ liệu theo đúng thứ tự API yêu cầu
            const apiData = {
                email: formData.email,
                phone: formData.phone,
                checkInDate: formatDateForAPI(selectedDate),
                timeSlotIds: listSlotId && listSlotId.length > 0 ? listSlotId : [],
                facilityId: parseInt(facilityId),
                categoryId: parseInt(categoryId)
            };

            console.log('Final API request data:', apiData);

            // Kiểm tra dữ liệu trước khi gửi
            if (!apiData.timeSlotIds || apiData.timeSlotIds.length === 0) {
                alert('Lỗi: Không có khung giờ được chọn!');
                return;
            }

            // Gọi API thông qua prop function được truyền từ parent
            const result = await createBooking(apiData);
            console.log('API response:', result);

            // FIX: Sửa lại logic kiểm tra response
            // Từ API response, ta thấy structure là: result.success và result.data
            if (result && result.success) {
                const bookingInfo = result.data;
                const bookingId = bookingInfo.bookingId;

                // Sau khi đặt sân thành công, gọi API tạo đơn thanh toán
                const paymentData = {
                    amount: totalPrice,
                    description: `Thanh toán đặt sân - Mã booking: ${bookingId}`,
                    redirectUrl: window.location.origin + "/payment-success", // URL redirect sau thanh toán thành công
                    callbackUrl: window.location.origin + "/payment-callback", // URL callback
                    appUser: formData.phone,
                    paymentGateway: formData.paymentMethod, // Thêm thông tin cổng thanh toán
                    embedData: {
                        bookingid: bookingId.toString()
                    }
                };

                console.log('Payment request data:', paymentData);

                try {
                    // Gọi API tạo đơn thanh toán
                    const paymentResult = await createPayment(paymentData);
                    console.log('Payment API response:', paymentResult);

                    const orderUrl = paymentResult?.data?.order_url;

                    // Đóng modal trước khi chuyển trang
                    onClose();

                    if (orderUrl) {
                        // Mở trang thanh toán trong tab mới
                        window.open(orderUrl, '_blank');
                    } else {
                        console.warn('Không có order_url trong payment response:', paymentResult);
                    }
                } catch (paymentError) {
                    console.error('Payment creation error:', paymentError);
                    // Đóng modal và vẫn chuyển đến booking process ngay cả khi payment thất bại
                    onClose();
                }

                // Chuyển trang hiện tại đến booking process với bookingId
                navigate('/bookingprocess', {
                    state: {
                        bookingId,
                        bookingData: bookingInfo,
                        fromBooking: true
                    }
                });

            } else {
                throw new Error(result?.message || 'Đặt sân thất bại');
            }
        } catch (error) {
            console.error('Final booking error:', error);

            // Xử lý các loại lỗi khác nhau
            let errorMessage = 'Có lỗi xảy ra khi đặt sân!';

            if (error.response && error.response.data && error.response.data.message) {
                errorMessage = error.response.data.message;
            } else if (error.message) {
                errorMessage = error.message;
            }

            alert(errorMessage);
        } finally {
            setIsSubmitting(false);
        }
    };

    // Calculate number of selected slots and courts
    const selectedSlotsCount = selectedSlots ? Object.keys(selectedSlots).filter(slotId => selectedSlots[slotId]).length : 0;
    const totalCourts = selectedSlots && quantities ? Object.keys(selectedSlots).reduce((sum, slotId) => {
        if (selectedSlots[slotId]) {
            return sum + (quantities[slotId] || 1);
        }
        return sum;
    }, 0) : 0;

    return (
        <div className="booking-detail-overlay" onClick={handleOverlayClick}>
            <div className="booking-detail-modal" onClick={e => e.stopPropagation()}>
                {/* Header */}
                <div className="modal-header">
                    <div className="header-content">
                        <h2 className="modal-title">
                            <span className="title-icon">📋</span>
                            Chi tiết đặt sân
                        </h2>
                        <p className="modal-subtitle">Vui lòng điền thông tin liên hệ</p>
                    </div>
                    <button className="close-btn" onClick={onClose} aria-label="Đóng">
                        <span>×</span>
                    </button>
                </div>

                {/* Scrollable Content */}
                <div className="modal-content">
                    {/* Booking Summary */}
                    <div className="booking-summary-section">
                    <h3 className="section-title">
                        <span className="section-icon">📊</span>
                        Thông tin đặt sân
                    </h3>
                    <div className="summary-grid">
                        <div className="summary-item">
                            <span className="summary-label">
                                <span className="label-icon">🏢</span>
                                Cơ sở:
                            </span>
                            <span className="summary-value">{facilityData?.facilityName || 'N/A'}</span>
                        </div>
                        <div className="summary-item">
                            <span className="summary-label">
                                <span className="label-icon">⚽</span>
                                Loại sân:
                            </span>
                            <span className="summary-value">{getSelectedCategoryName()}</span>
                        </div>
                        <div className="summary-item">
                            <span className="summary-label">
                                <span className="label-icon">📅</span>
                                Ngày đặt:
                            </span>
                            <span className="summary-value">{formatDate(selectedDate)}</span>
                        </div>
                        <div className="summary-item">
                            <span className="summary-label">
                                <span className="label-icon">🕐</span>
                                Số khung giờ:
                            </span>
                            <span className="summary-value">{selectedSlotsCount}</span>
                        </div>
                        <div className="summary-item">
                            <span className="summary-label">
                                <span className="label-icon">🏟️</span>
                                Tổng số sân:
                            </span>
                            <span className="summary-value">{totalCourts}</span>
                        </div>
                        <div className="summary-item total">
                            <span className="summary-label">
                                <span className="label-icon">💰</span>
                                Tổng tiền:
                            </span>
                            <span className="summary-value total-price">
                                {formatCurrency(totalPrice)}
                            </span>
                        </div>
                    </div>
                </div>

                {/* Contact Form */}
                <div className="contact-form-section">
                    <h3 className="section-title">
                        <span className="section-icon">📞</span>
                        Thông tin liên hệ
                    </h3>
                    <div className="form-grid">
                        <div className="form-group">
                            <label className="form-label" htmlFor="phone">
                                <span className="label-icon">📱</span>
                                Số điện thoại *
                            </label>
                            <input
                                type="tel"
                                id="phone"
                                className={`form-input ${errors.phone ? 'error' : ''}`}
                                placeholder="Nhập số điện thoại"
                                value={formData.phone}
                                onChange={(e) => handleInputChange('phone', e.target.value)}
                            />
                            {errors.phone && <span className="error-message">{errors.phone}</span>}
                        </div>

                        <div className="form-group">
                            <label className="form-label" htmlFor="email">
                                <span className="label-icon">📧</span>
                                Email *
                            </label>
                            <input
                                type="email"
                                id="email"
                                className={`form-input ${errors.email ? 'error' : ''}`}
                                placeholder="Nhập email"
                                value={formData.email}
                                onChange={(e) => handleInputChange('email', e.target.value)}
                            />
                            {errors.email && <span className="error-message">{errors.email}</span>}
                        </div>
                    </div>

                    <div className="form-note">
                        <span className="note-icon">ℹ️</span>
                        Chúng tôi sẽ liên hệ với bạn qua số điện thoại hoặc email để xác nhận đặt sân.
                    </div>
                </div>

                {/* Payment Method Section */}
                <div className="payment-method-section">
                    <h3 className="section-title">
                        <span className="section-icon">💳</span>
                        Phương thức thanh toán
                    </h3>
                    <div className="form-group">
                        <label className="form-label" htmlFor="paymentMethod">
                            <span className="label-icon">🏦</span>
                            Cổng thanh toán *
                        </label>
                        <select
                            id="paymentMethod"
                            className={`form-input form-select ${errors.paymentMethod ? 'error' : ''}`}
                            value={formData.paymentMethod}
                            onChange={(e) => handleInputChange('paymentMethod', e.target.value)}
                        >
                            <option value="">-- Chọn cổng thanh toán --</option>
                            <option value="domestic">
                                🏧 Cổng thanh toán nội địa (ZaloPay, MoMo, VietQR)
                            </option>
                            <option value="international">
                                🌍 Cổng thanh toán quốc tế (Visa, Mastercard, PayPal)
                            </option>
                        </select>
                        {errors.paymentMethod && (
                            <span className="error-message">{errors.paymentMethod}</span>
                        )}
                    </div>

                    {/* Payment Method Info */}
                    {formData.paymentMethod && (
                        <div className="payment-info">
                            {formData.paymentMethod === 'domestic' ? (
                                <div className="payment-detail">
                                    <span className="info-icon">🏧</span>
                                    <div className="info-content">
                                        <strong>Cổng thanh toán nội địa</strong>
                                        <p>Hỗ trợ: ZaloPay, MoMo, VietQR, Internet Banking các ngân hàng Việt Nam</p>
                                        <p>✅ Phí giao dịch thấp • ✅ Thanh toán nhanh chóng</p>
                                    </div>
                                </div>
                            ) : (
                                <div className="payment-detail">
                                    <span className="info-icon">🌍</span>
                                    <div className="info-content">
                                        <strong>Cổng thanh toán quốc tế</strong>
                                        <p>Hỗ trợ: Visa, Mastercard, American Express, PayPal</p>
                                        <p>✅ Thanh toán toàn cầu • ✅ Bảo mật cao</p>
                                    </div>
                                </div>
                            )}
                        </div>
                    )}
                </div>
                </div>

                {/* Footer */}
                <div className="modal-footer">
                    <div className="action-buttons">
                        <button
                            className="btn-cancel"
                            onClick={onClose}
                            disabled={isSubmitting}
                        >
                            <span className="btn-icon">❌</span>
                            Hủy bỏ
                        </button>
                        <button
                            className="btn-confirm"
                            onClick={handleFinalBooking}
                            disabled={isSubmitting}
                        >
                            {isSubmitting ? (
                                <>
                                    <span className="loading-spinner"></span>
                                    Đang xử lý...
                                </>
                            ) : (
                                <>
                                    <span className="btn-icon">✅</span>
                                    Xác nhận đặt sân
                                </>
                            )}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}