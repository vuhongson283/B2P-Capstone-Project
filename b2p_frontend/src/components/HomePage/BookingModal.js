import React, { useState, useEffect } from "react";
import "./BookingModal.scss";

// Helper function to format time slot
const formatTimeSlot = (startTime, endTime) => {
  const formatTime = (timeString) => {
    if (!timeString) return '';
    return timeString.substring(0, 5); // Format HH:mm from HH:mm:ss
  };
  return `${formatTime(startTime)} - ${formatTime(endTime)}`;
};

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

  // Reset state when modal opens/closes or timeSlots change
  useEffect(() => {
    if (open) {
      setSelectedSlots({});
      setQuantities({});
    }
  }, [open, timeSlots]);

  if (!open) return null;

  // Filter slots that have available courts
  const availableSlots = timeSlots.filter(slot => slot.availableCourtCount > 0);

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
  const totalPrice = Object.keys(selectedSlots).reduce((sum, slotId) => {
    if (selectedSlots[slotId]) {
      return sum + (quantities[slotId] || 1) * 100000;
    }
    return sum;
  }, 0);

  // Format date for display
  const formatDate = (dateString) => {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN');
  };

  // Get selected category name
  const getSelectedCategoryName = () => {
    if (!facilityData?.categories || !selectedCategory) return '';
    const category = facilityData.categories.find(cat => 
      cat.categoryId.toString() === selectedCategory.toString()
    );
    return category ? category.categoryName : '';
  };

  return (
    <div className="booking-modal-overlay">
      <div className="booking-modal">
        <div className="modal-header">
          <h2>Đặt sân ngày {formatDate(selectedDate)}</h2>
          <button className="close-btn" onClick={onClose}>×</button>
        </div>
        
        {/* Display facility and category info */}
        <div className="booking-info">
          <p><strong>Cơ sở:</strong> {facilityData?.facilityName || 'N/A'}</p>
          <p><strong>Loại sân:</strong> {getSelectedCategoryName()}</p>
        </div>

        <div className="modal-content">
          {availableSlots.length === 0 ? (
            <div className="no-slots">
              <p>Không có khung giờ nào khả dụng cho ngày này.</p>
            </div>
          ) : (
            <>
              <label className="check-all">
                <input 
                  type="checkbox"
                  checked={
                    availableSlots.length > 0 && 
                    Object.values(selectedSlots).filter(Boolean).length === availableSlots.length
                  }
                  onChange={handleCheckAll}
                />
                Tất cả
              </label>
              
              {availableSlots.map(slot => (
                <div 
                  className={`slot-row${selectedSlots[slot.timeSlotId] ? " active" : ""}`} 
                  key={slot.timeSlotId}
                >
                  <label>
                    <input 
                      type="checkbox"
                      checked={!!selectedSlots[slot.timeSlotId]}
                      onChange={e => handleCheckSlot(slot.timeSlotId, e.target.checked)}
                    />
                    {formatTimeSlot(slot.startTime, slot.endTime)}
                  </label>
                  
                  <span className="slot-available">
                    {slot.availableCourtCount} sân trống
                  </span>
                  
                  <div className="quantity-box">
                    <button 
                      onClick={() => handleQuantity(slot.timeSlotId, (quantities[slot.timeSlotId] || 1) - 1)}
                      disabled={!selectedSlots[slot.timeSlotId] || (quantities[slot.timeSlotId] || 1) <= 1}
                    >
                      -
                    </button>
                    <span>{quantities[slot.timeSlotId] || 1}</span>
                    <button 
                      onClick={() => handleQuantity(slot.timeSlotId, (quantities[slot.timeSlotId] || 1) + 1)}
                      disabled={
                        !selectedSlots[slot.timeSlotId] || 
                        (quantities[slot.timeSlotId] || 1) >= slot.availableCourtCount
                      }
                    >
                      +
                    </button>
                  </div>
                </div>
              ))}
            </>
          )}
        </div>
        
        <div className="modal-footer">
          <span>Tổng: {totalPrice.toLocaleString()} VND</span>
          <button 
            className="pay-btn"
            disabled={Object.values(selectedSlots).filter(Boolean).length === 0}
          >
            Thanh toán
          </button>
        </div>
      </div>
    </div>
  );
}