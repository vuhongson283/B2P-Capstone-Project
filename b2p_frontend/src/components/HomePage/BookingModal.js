import React, { useState, useEffect } from "react";
import "./BookingModal.scss";

// Helper function to format time slot
const formatTimeSlot = (startTime, endTime) => {
  const formatTime = (timeString) => {
    if (!timeString) return '';
    return timeString.substring(0, 5);
  };
  return `${formatTime(startTime)} - ${formatTime(endTime)}`;
};

// Helper function to format currency
const formatCurrency = (amount) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    minimumFractionDigits: 0,
  }).format(amount);
};

export default function BookingModal({
  open,
  onClose,
  timeSlots = [],
  selectedDate,
  facilityData,
  selectedCategory,
  onProceedToDetail
}) {
  const [selectedSlots, setSelectedSlots] = useState({});
  const [quantities, setQuantities] = useState({});

  // Reset state when modal opens/closes or timeSlots change
  useEffect(() => {
    if (open) {
      setSelectedSlots({});
      setQuantities({});
    }
  }, [open, timeSlots]);

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

  // Filter slots that have available courts
  const availableSlots = timeSlots.filter(slot => slot.availableCourtCount > 0);

  // Check if all slots are selected
  const isAllSelected = availableSlots.length > 0 &&
    Object.values(selectedSlots).filter(Boolean).length === availableSlots.length;

  // Chọn tất cả
  const handleCheckAll = (e) => {
    const checked = e.target.checked;
    let newSelected = {};
    let newQuantities = {};

    availableSlots.forEach(slot => {
      newSelected[slot.timeSlotId] = checked;
      newQuantities[slot.timeSlotId] = 1;
    });

    setSelectedSlots(checked ? newSelected : {});
    setQuantities(checked ? newQuantities : {});
  };

  // Chọn từng slot
  const handleCheckSlot = (slotId, checked) => {
    setSelectedSlots(prev => ({
      ...prev,
      [slotId]: checked
    }));
    setQuantities(prev => ({
      ...prev,
      [slotId]: checked ? 1 : prev[slotId]
    }));
  };

  // Tăng/giảm số lượng với giới hạn theo số sân trống
  const handleQuantity = (slotId, value) => {
    const slot = timeSlots.find(s => s.timeSlotId === slotId);
    const maxQuantity = slot ? slot.availableCourtCount : 1;

    setQuantities(prev => ({
      ...prev,
      [slotId]: Math.max(1, Math.min(value, maxQuantity))
    }));
  };

  // Tính tổng (giả sử giá 100k/sân)
  const PRICE_PER_COURT = 100000;
  const selectedSlotsCount = Object.keys(selectedSlots).filter(slotId => selectedSlots[slotId]).length;
  const totalCourts = Object.keys(selectedSlots).reduce((sum, slotId) => {
    if (selectedSlots[slotId]) {
      return sum + (quantities[slotId] || 1);
    }
    return sum;
  }, 0);
  const totalPrice = totalCourts * PRICE_PER_COURT;

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
    if (!facilityData?.categories || !selectedCategory) return '';
    const category = facilityData.categories.find(cat =>
      cat.categoryId.toString() === selectedCategory.toString()
    );
    return category ? category.categoryName : '';
  };

  // Handle proceed to booking detail
  const handleProceedToDetail = () => {
    if (selectedSlotsCount === 0) {
      alert('Vui lòng chọn ít nhất một khung giờ!');
      return;
    }

    // Tạo danh sách ID của các slot đã chọn
    const listSlotId = Object.keys(selectedSlots)
      .filter(slotId => selectedSlots[slotId])
      .map(slotId => parseInt(slotId));

    console.log('Selected slots:', selectedSlots);
    console.log('List slot IDs:', listSlotId);

    // Đóng modal hiện tại và chuyển sang BookingDetail
    onClose();

    // Gọi callback để mở BookingDetail với dữ liệu cần thiết
    if (onProceedToDetail) {
      onProceedToDetail({
        facilityId: facilityData?.facilityId,
        categoryId: selectedCategory,
        listSlotId: listSlotId,
        totalPrice,
        selectedSlots,
        quantities,
        selectedDate,
        facilityData
      });
    }
  };

  return (
    <div className="booking-modal-overlay" onClick={handleOverlayClick}>
      <div className="booking-modal" onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div className="modal-header">
          <div className="header-content">
            <h2 className="modal-title">
              Đặt sân thể thao
            </h2>
            <p className="modal-subtitle">{formatDate(selectedDate)}</p>
          </div>
          <button className="close-btn" onClick={onClose} aria-label="Đóng">
            <span>×</span>
          </button>
        </div>

        {/* Booking Info */}
        <div className="booking-info">
          <div className="info-grid">
            <div className="info-item">
              <span className="info-icon"></span>
              <div className="info-content">
                <span className="info-label">Cơ sở</span>
                <span className="info-value">{facilityData?.facilityName || 'N/A'}</span>
              </div>
            </div>
            <div className="info-item">
              <span className="info-icon"></span>
              <div className="info-content">
                <span className="info-label">Loại sân</span>
                <span className="info-value">{getSelectedCategoryName()}</span>
              </div>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="modal-content">
          {availableSlots.length === 0 ? (
            <div className="empty-state">
              <div className="empty-icon"></div>
              <h3>Không có khung giờ khả dụng</h3>
              <p>Vui lòng chọn ngày khác hoặc loại sân khác</p>
            </div>
          ) : (
            <div className="slots-container">
              {/* Select All */}
              <div className="select-all-section">
                <label className="select-all-checkbox">
                  <input
                    type="checkbox"
                    checked={isAllSelected}
                    onChange={handleCheckAll}
                  />
                  <span className="checkmark"></span>
                  <span className="select-all-text">
                    Chọn tất cả ({availableSlots.length} khung giờ)
                  </span>
                </label>
              </div>

              {/* Time Slots */}
              <div className="slots-list">
                {availableSlots.map(slot => (
                  <div
                    className={`slot-item ${selectedSlots[slot.timeSlotId] ? "selected" : ""}`}
                    key={slot.timeSlotId}
                  >
                    <div className="slot-main">
                      <label className="slot-checkbox">
                        <input
                          type="checkbox"
                          checked={!!selectedSlots[slot.timeSlotId]}
                          onChange={e => handleCheckSlot(slot.timeSlotId, e.target.checked)}
                        />
                        <span className="checkmark"></span>
                      </label>

                      <div className="slot-info">
                        <div className="slot-time">
                          {formatTimeSlot(slot.startTime, slot.endTime)}
                        </div>
                        <div className="slot-availability">
                          {slot.availableCourtCount} sân trống
                        </div>
                      </div>

                      <div className="slot-price">
                        {formatCurrency(PRICE_PER_COURT)}/sân
                      </div>
                    </div>

                    {selectedSlots[slot.timeSlotId] && (
                      <div className="quantity-section">
                        <label className="quantity-label">Số sân:</label>
                        <div className="quantity-controls">
                          <button
                            className="quantity-btn decrease"
                            onClick={() => handleQuantity(slot.timeSlotId, (quantities[slot.timeSlotId] || 1) - 1)}
                            disabled={(quantities[slot.timeSlotId] || 1) <= 1}
                            aria-label="Giảm số lượng"
                          >
                            <span>−</span>
                          </button>
                          <span className="quantity-value">
                            {quantities[slot.timeSlotId] || 1}
                          </span>
                          <button
                            className="quantity-btn increase"
                            onClick={() => handleQuantity(slot.timeSlotId, (quantities[slot.timeSlotId] || 1) + 1)}
                            disabled={(quantities[slot.timeSlotId] || 1) >= slot.availableCourtCount}
                            aria-label="Tăng số lượng"
                          >
                            <span>+</span>
                          </button>
                        </div>
                        <div className="slot-subtotal">
                          {formatCurrency((quantities[slot.timeSlotId] || 1) * PRICE_PER_COURT)}
                        </div>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="modal-footer">
          <div className="booking-summary">
            <div className="summary-info">
              <div className="summary-item">
                <span className="summary-label">Khung giờ đã chọn</span>
                <span className="summary-value">{selectedSlotsCount}</span>
              </div>
              <div className="summary-item">
                <span className="summary-label">Tổng số sân</span>
                <span className="summary-value">{totalCourts}</span>
              </div>
            </div>

            <div className="summary-total">
              <span className="total-label">Tổng tiền</span>
              <span className="total-value">{formatCurrency(totalPrice)}</span>
            </div>
          </div>

          <div className="footer-actions">
            <button
              className="btn btn-secondary"
              onClick={onClose}
            >
              Hủy
            </button>
            <button
              className={`btn btn-primary ${selectedSlotsCount === 0 ? 'disabled' : ''}`}
              onClick={handleProceedToDetail}
              disabled={selectedSlotsCount === 0}
            >
              Tiếp tục đặt sân
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}