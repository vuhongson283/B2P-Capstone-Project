import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
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
    listSlotId,
    totalPrice,
    facilityData,
    selectedDate,
    selectedSlots,
    quantities,
    createBooking,
    createPayment,
    createStripePaymentOrder,
    userId
}) {
    const navigate = useNavigate();
    const [formData, setFormData] = useState({
        phone: '',
        email: '',
        paymentMethod: 'domestic'
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

        if (!userId) {
            if (!formData.phone.trim()) {
                newErrors.phone = 'Vui lòng nhập số điện thoại';
            } else if (!/^[0-9]{10,11}$/.test(formData.phone.replace(/\s/g, ''))) {
                newErrors.phone = 'Số điện thoại không hợp lệ (10-11 chữ số)';
            }

            if (formData.email.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
                newErrors.email = 'Email không hợp lệ';
            }
        }

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

    // Helper function to convert VND to USD cents for Stripe
    const convertVNDtoUSDCents = (vndAmount) => {
        const exchangeRate = 24000;
        const usdAmount = vndAmount / exchangeRate;
        const usdCents = Math.round(usdAmount * 100);
        return usdCents;
    };

    // Helper function to calculate platform fee (5%)
    const calculatePlatformFee = (amount) => {
        return Math.round(amount * 0.05);
    };

    // Handle final booking submission
    const handleFinalBooking = async () => {
        if (!validateForm()) {
            return;
        }

        setIsSubmitting(true);
        try {
            const formatDateForAPI = (dateString) => {
                if (!dateString) return new Date().toISOString();

                if (dateString.length === 10) {
                    return new Date(dateString + 'T00:00:00.000Z').toISOString();
                }

                return new Date(dateString).toISOString();
            };

            console.log('listSlotId received:', listSlotId);
            console.log('selectedSlots:', selectedSlots);
            console.log('facilityId:', facilityId);
            console.log('categoryId:', categoryId);
            console.log('paymentMethod:', formData.paymentMethod);
            console.log('userId:', userId);

            const paymentTypeId = formData.paymentMethod === 'international' ? 1 : 2;

            const apiData = {
                checkInDate: formatDateForAPI(selectedDate),
                timeSlotIds: listSlotId && listSlotId.length > 0 ? listSlotId : [],
                facilityId: parseInt(facilityId),
                categoryId: parseInt(categoryId),
                paymentTypeId: paymentTypeId
            };

            if (userId) {
                apiData.userId = parseInt(userId);
            }

            if (!userId && formData.phone.trim()) {
                apiData.phone = formData.phone.trim();
            }

            if (!userId && formData.email.trim()) {
                apiData.email = formData.email.trim();
            }

            console.log('Final API request data:', apiData);

            if (!apiData.timeSlotIds || apiData.timeSlotIds.length === 0) {
                alert('Lỗi: Không có khung giờ được chọn!');
                return;
            }

            const result = await createBooking(apiData);
            console.log('API response:', result);

            if (result && result.success === true) {
                const bookingInfo = result.data;
                const bookingId = bookingInfo.bookingId;

                if (formData.paymentMethod === 'international') {
                    const stripePaymentData = {
                        amount: convertVNDtoUSDCents(totalPrice),
                        currency: 'usd',
                        platformFee: calculatePlatformFee(convertVNDtoUSDCents(totalPrice)),
                        destinationAccountId: 'acct_1RuuxcATZut0ML00',
                        bookingId: bookingId.toString()
                    };

                    console.log('Stripe Payment request data:', stripePaymentData);

                    try {
                        const stripePaymentResult = await createStripePaymentOrder(stripePaymentData);
                        console.log('Stripe Payment API response:', stripePaymentResult);

                        const paymentId = stripePaymentResult?.data?.id || stripePaymentResult?.id;

                        onClose();

                        if (paymentId) {
                            const usdAmount = convertVNDtoUSDCents(totalPrice) / 100;
                            const stripePaymentUrl = `/stripepayment?payment_id=${paymentId}&booking_id=${bookingId}&amount_vnd=${totalPrice}&amount_usd=${usdAmount.toFixed(2)}`;
                            window.open(stripePaymentUrl, '_blank');
                        } else {
                            console.warn('Không có payment ID trong Stripe response:', stripePaymentResult);
                            alert('Lỗi tạo đơn thanh toán Stripe!');
                            return;
                        }
                    } catch (stripePaymentError) {
                        console.error('Stripe Payment creation error:', stripePaymentError);
                        alert('Lỗi tạo đơn thanh toán Stripe!');
                        onClose();
                    }
                } else {
                    const paymentData = {
                        amount: totalPrice,
                        description: `Thanh toán đặt sân - Mã booking: ${bookingId}`,
                        redirectUrl: window.location.origin + "/payment-success",
                        callbackUrl: window.location.origin + "/payment-callback",
                        appUser: userId ? userId.toString() : formData.phone,
                        paymentGateway: formData.paymentMethod,
                        embedData: {
                            bookingid: bookingId.toString()
                        }
                    };

                    console.log('Payment request data:', paymentData);

                    try {
                        const paymentResult = await createPayment(paymentData);
                        console.log('Payment API response:', paymentResult);

                        const orderUrl = paymentResult?.data?.order_url;

                        onClose();

                        if (orderUrl) {
                            window.open(orderUrl, '_blank');
                        } else {
                            console.warn('Không có order_url trong payment response:', paymentResult);
                        }
                    } catch (paymentError) {
                        console.error('Payment creation error:', paymentError);
                        onClose();
                    }
                }

                navigate('/bookingprocess', {
                    state: {
                        bookingId,
                        bookingData: bookingInfo,
                        fromBooking: true
                    }
                });

            } else if (result && result.success === false) {
                const errorMessage = result.message || 'Đặt sân thất bại';
                alert(errorMessage);
                console.error('Booking failed:', result);
            } else {
                throw new Error(result?.message || 'Đặt sân thất bại - Response không hợp lệ');
            }
        } catch (error) {
            console.error('Final booking error:', error);

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

    const shouldShowContactForm = !userId;

    return (
        <div className="booking-detail-overlay" onClick={handleOverlayClick}>
            <div className="booking-detail-modal" onClick={e => e.stopPropagation()}>
                {/* Header */}
                <div className="modal-header">
                    <div className="header-content">
                        <h2 className="modal-title">
                            Chi tiết đặt sân
                        </h2>
                        <p className="modal-subtitle">
                            {userId ? 'Xác nhận thông tin đặt sân' : 'Vui lòng điền thông tin liên hệ'}
                        </p>
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
                            Thông tin đặt sân
                        </h3>
                        <div className="summary-grid">
                            <div className="summary-item">
                                <span className="summary-label">Cơ sở:</span>
                                <span className="summary-value">{facilityData?.facilityName || 'N/A'}</span>
                            </div>
                            <div className="summary-item">
                                <span className="summary-label">Loại sân:</span>
                                <span className="summary-value">{getSelectedCategoryName()}</span>
                            </div>
                            <div className="summary-item">
                                <span className="summary-label">Ngày đặt:</span>
                                <span className="summary-value">{formatDate(selectedDate)}</span>
                            </div>
                            <div className="summary-item">
                                <span className="summary-label">Số khung giờ:</span>
                                <span className="summary-value">{selectedSlotsCount}</span>
                            </div>
                            <div className="summary-item">
                                <span className="summary-label">Tổng số sân:</span>
                                <span className="summary-value">{totalCourts}</span>
                            </div>
                            <div className="summary-item total">
                                <span className="summary-label">Tổng tiền:</span>
                                <span className="summary-value total-price">
                                    {formatCurrency(totalPrice)}
                                </span>
                            </div>
                        </div>
                    </div>


                    {/* Contact Form */}
                    {shouldShowContactForm && (
                        <div className="contact-form-section">
                            <h3 className="section-title">
                                Thông tin liên hệ
                            </h3>
                            <div className="form-grid">
                                <div className="form-group">
                                    <label className="form-label" htmlFor="phone">
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
                                        Email
                                    </label>
                                    <input
                                        type="email"
                                        id="email"
                                        className={`form-input ${errors.email ? 'error' : ''}`}
                                        placeholder="Nhập email (tùy chọn)"
                                        value={formData.email}
                                        onChange={(e) => handleInputChange('email', e.target.value)}
                                    />
                                    {errors.email && <span className="error-message">{errors.email}</span>}
                                </div>
                            </div>

                            <div className="form-note">
                                Chúng tôi sẽ liên hệ với bạn qua số điện thoại hoặc email để xác nhận đặt sân.
                            </div>
                        </div>
                    )}

                    {/* Payment Method Section */}
                    <div className="payment-method-section">
                        <h3 className="section-title">
                            Phương thức thanh toán
                        </h3>
                        <div className="form-group">
                            <label className="form-label" htmlFor="paymentMethod">
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
                                    Cổng thanh toán nội địa (ZaloPay, MoMo, VietQR)
                                </option>
                                <option value="international">
                                    Cổng thanh toán quốc tế (Visa, Mastercard, PayPal)
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
                                        <div className="info-content">
                                            <strong>Cổng thanh toán nội địa</strong>
                                            <p>Hỗ trợ: ZaloPay, MoMo, VietQR, Internet Banking các ngân hàng Việt Nam</p>
                                            <p>✅ Phí giao dịch thấp • ✅ Thanh toán nhanh chóng</p>
                                        </div>
                                    </div>
                                ) : (
                                    <div className="payment-detail">
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
                                'Xác nhận đặt sân'
                            )}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}