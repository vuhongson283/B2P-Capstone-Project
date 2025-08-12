import React, { useState, useEffect, useMemo } from "react";
import "./BookingModal.scss";

// ✅ UTILITY FUNCTIONS
const formatTimeSlot = (startTime, endTime) => {
  const formatTime = (timeString) => {
    if (!timeString) return '';
    return timeString.substring(0, 5);
  };
  return `${formatTime(startTime)} - ${formatTime(endTime)}`;
};

const formatCurrency = (amount) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    minimumFractionDigits: 0,
  }).format(amount);
};

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

// ✅ LOADING SPINNER COMPONENT
const LoadingSpinner = ({ size = 'small' }) => (
  <div className={`loading-spinner ${size}`}>
    <div className="spinner-dot"></div>
    <div className="spinner-dot"></div>
    <div className="spinner-dot"></div>
  </div>
);

// ✅ QUANTITY CONTROL COMPONENT
const QuantityControl = ({ value, max, onChange, disabled = false }) => (
  <div className="quantity-selector">
    <button 
      className="quantity-btn decrease"
      onClick={() => onChange(Math.max(1, value - 1))}
      disabled={disabled || value <= 1}
      aria-label="Giảm số lượng"
    >
      <span>−</span>
    </button>
    <span className="quantity-display">{value}</span>
    <button 
      className="quantity-btn increase"
      onClick={() => onChange(Math.min(max, value + 1))}
      disabled={disabled || value >= max}
      aria-label="Tăng số lượng"
    >
      <span>+</span>
    </button>
  </div>
);

// ✅ MAIN BOOKING MODAL COMPONENT
export default function BookingModal({ 
  open, 
  onClose, 
  timeSlots = [], 
  selectedDate, 
  facilityData,
  selectedCategory 
}) {
  const [selectedSlots, setSelectedSlots] = useState({});
  const [quantities, setQuantities] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  // ✅ COMPUTED VALUES
  const availableSlots = useMemo(() => 
    timeSlots.filter(slot => slot.availableCourtCount > 0),
    [timeSlots]
  );

  const selectedCategoryName = useMemo(() => {
    if (!facilityData?.categories || !selectedCategory) return 'N/A';
    const category = facilityData.categories.find(cat => 
      cat.categoryId.toString() === selectedCategory.toString()
    );
    return category?.categoryName || 'N/A';
  }, [facilityData?.categories, selectedCategory]);

  const bookingSummary = useMemo(() => {
    const PRICE_PER_COURT = 100000;
    const selectedCount = Object.values(selectedSlots).filter(Boolean).length;
    const totalCourts = Object.entries(selectedSlots).reduce((sum, [slotId, isSelected]) => {
      return isSelected ? sum + (quantities[slotId] || 1) : sum;
    }, 0);
    
    return {
      selectedSlots: selectedCount,
      totalCourts,
      totalPrice: totalCourts * PRICE_PER_COURT,
      pricePerCourt: PRICE_PER_COURT
    };
  }, [selectedSlots, quantities]);

  const isAllSelected = availableSlots.length > 0 && 
    Object.values(selectedSlots).filter(Boolean).length === availableSlots.length;

  // ✅ RESET STATE ON MODAL OPEN
  useEffect(() => {
    if (open) {
      setSelectedSlots({});
      setQuantities({});
      setIsSubmitting(false);
    }
  }, [open, timeSlots]);

  // ✅ HANDLE ESCAPE KEY & BODY SCROLL
  useEffect(() => {
    const handleEscape = (e) => {
      if (e.key === 'Escape') onClose();
    };

    if (open) {
      document.addEventListener('keydown', handleEscape);
      document.body.style.overflow = 'hidden';
      return () => {
        document.removeEventListener('keydown', handleEscape);
        document.body.style.overflow = 'unset';
      };
    }
  }, [open, onClose]);

  // ✅ EVENT HANDLERS
  const handleOverlayClick = (e) => {
    if (e.target === e.currentTarget) onClose();
  };

  const handleSelectAll = (checked) => {
    if (checked) {
      const newSelected = {};
      const newQuantities = {};
      availableSlots.forEach(slot => {
        newSelected[slot.timeSlotId] = true;
        newQuantities[slot.timeSlotId] = 1;
      });
      setSelectedSlots(newSelected);
      setQuantities(newQuantities);
    } else {
      setSelectedSlots({});
      setQuantities({});
    }
  };

  const handleSlotToggle = (slotId, checked) => {
    setSelectedSlots(prev => ({ ...prev, [slotId]: checked }));
    if (checked && !quantities[slotId]) {
      setQuantities(prev => ({ ...prev, [slotId]: 1 }));
    }
  };

  const handleQuantityChange = (slotId, value) => {
    const slot = availableSlots.find(s => s.timeSlotId === slotId);
    if (slot) {
      setQuantities(prev => ({ 
        ...prev, 
        [slotId]: Math.max(1, Math.min(value, slot.availableCourtCount))
      }));
    }
  };

  const handleBooking = async () => {
    setIsSubmitting(true);
    try {
      // TODO: API call
      console.log('Booking data:', {
        facilityId: facilityData?.facilityId,
        categoryId: selectedCategory,
        date: selectedDate,
        slots: selectedSlots,
        quantities: quantities,
        summary: bookingSummary
      });
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Success
      onClose();
      // Show success toast/notification here
    } catch (error) {
      console.error('Booking error:', error);
      // Show error toast/notification here
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!open) return null;

  return (
    <div className="booking-modal-overlay" onClick={handleOverlayClick}>
      <div className="booking-modal">
        {/* ✅ MODAL HEADER */}
        <header className="modal-header">
          <div className="header-content">
            <h2 className="modal-title">
              <span className="title-icon">🏟️</span>
              Đặt sân thể thao
            </h2>
            <p className="modal-subtitle">{formatDate(selectedDate)}</p>
          </div>
          <button className="close-button" onClick={onClose} aria-label="Đóng modal">
            <span>×</span>
          </button>
        </header>
        
        {/* ✅ BOOKING INFORMATION */}
        <div className="booking-info">
          <div className="info-cards">
            <div className="info-card">
              <div className="info-icon">🏢</div>
              <div className="info-content">
                <span className="info-label">Cơ sở</span>
                <span className="info-value">{facilityData?.facilityName || 'N/A'}</span>
              </div>
            </div>
            <div className="info-card">
              <div className="info-icon">⚽</div>
              <div className="info-content">
                <span className="info-label">Loại sân</span>
                <span className="info-value">{selectedCategoryName}</span>
              </div>
            </div>
          </div>
        </div>

        {/* ✅ MAIN CONTENT */}
        <div className="modal-content">
          {availableSlots.length === 0 ? (
            <div className="empty-state">
              <div className="empty-icon">📅</div>
              <h3>Không có khung giờ</h3>
              <p>Không có sân trống cho ngày này</p>
            </div>
          ) : (
            <div className="slots-section">
              {/* ✅ SELECT ALL */}
              <div className="select-all-bar">
                <label className="select-all-control">
                  <input 
                    type="checkbox"
                    checked={isAllSelected}
                    onChange={(e) => handleSelectAll(e.target.checked)}
                  />
                  <span className="checkbox-custom"></span>
                  <span className="select-all-text">
                    Chọn tất cả ({availableSlots.length} khung giờ)
                  </span>
                </label>
              </div>
              
              {/* ✅ TIME SLOTS LIST */}
              <div className="slots-grid">
                {availableSlots.map(slot => (
                  <div 
                    key={slot.timeSlotId}
                    className={`slot-card ${selectedSlots[slot.timeSlotId] ? 'selected' : ''}`}
                  >
                    <div className="slot-header">
                      <label className="slot-checkbox">
                        <input 
                          type="checkbox"
                          checked={!!selectedSlots[slot.timeSlotId]}
                          onChange={(e) => handleSlotToggle(slot.timeSlotId, e.target.checked)}
                        />
                        <span className="checkbox-custom"></span>
                      </label>
                      
                      <div className="slot-details">
                        <div className="slot-time">
                          <span className="time-icon">🕐</span>
                          <span>{formatTimeSlot(slot.startTime, slot.endTime)}</span>
                        </div>
                        <div className="slot-availability">
                          <span className="court-icon">🏟️</span>
                          <span>{slot.availableCourtCount} sân trống</span>
                        </div>
                      </div>
                      
                      <div className="slot-price">
                        {formatCurrency(bookingSummary.pricePerCourt)}/sân
                      </div>
                    </div>
                    
                    {selectedSlots[slot.timeSlotId] && (
                      <div className="quantity-section">
                        <div className="quantity-label">Số sân:</div>
                        <QuantityControl
                          value={quantities[slot.timeSlotId] || 1}
                          max={slot.availableCourtCount}
                          onChange={(value) => handleQuantityChange(slot.timeSlotId, value)}
                          disabled={isSubmitting}
                        />
                        <div className="slot-subtotal">
                          {formatCurrency((quantities[slot.timeSlotId] || 1) * bookingSummary.pricePerCourt)}
                        </div>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
        
        {/* ✅ MODAL FOOTER */}
        <footer className="modal-footer">
          <div className="booking-summary">
            <div className="summary-stats">
              <div className="summary-item">
                <span className="summary-icon">🕐</span>
                <span className="summary-text">Khung giờ: {bookingSummary.selectedSlots}</span>
              </div>
              <div className="summary-item">
                <span className="summary-icon">🏟️</span>
                <span className="summary-text">Tổng sân: {bookingSummary.totalCourts}</span>
              </div>
            </div>
            <div className="total-price">
              <span className="total-label">Tổng tiền:</span>
              <span className="total-amount">{formatCurrency(bookingSummary.totalPrice)}</span>
            </div>
          </div>
          
          <div className="action-buttons">
            <button 
              className="btn-secondary"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Hủy bỏ
            </button>
            <button 
              className="btn-primary"
              onClick={handleBooking}
              disabled={bookingSummary.selectedSlots === 0 || isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <LoadingSpinner size="small" />
                  <span>Đang xử lý...</span>
                </>
              ) : (
                <>
                  <span className="btn-icon">⚽</span>
                  <span>Đặt sân ngay</span>
                </>
              )}
            </button>
          </div>
        </footer>
      </div>
    </div>
  );
}