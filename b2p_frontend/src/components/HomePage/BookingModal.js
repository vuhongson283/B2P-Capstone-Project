import React, { useState } from "react";
import "./BookingModal.scss";

const slots = [
  { id: 1, label: "Slot1 (7 - 9h)", available: 2 },
  { id: 2, label: "Slot 2 (9h30 - 11h30)", available: 2 },
  { id: 3, label: "Slot3", available: 2 },
  { id: 4, label: "Slot4", available: 2 }
];

export default function BookingModal({ open, onClose }) {
  const [selectedSlots, setSelectedSlots] = useState({});
  const [quantities, setQuantities] = useState({});

  if (!open) return null;

  // Chọn tất cả
  const handleCheckAll = (e) => {
    const checked = e.target.checked;
    let newSelected = {};
    let newQuantities = {};
    slots.forEach(slot => {
      newSelected[slot.id] = checked;
      newQuantities[slot.id] = 1;
    });
    setSelectedSlots(checked ? newSelected : {});
    setQuantities(checked ? newQuantities : {});
  };

  // Chọn từng slot
  const handleCheckSlot = (id, checked) => {
    setSelectedSlots(prev => ({
      ...prev,
      [id]: checked
    }));
    setQuantities(prev => ({
      ...prev,
      [id]: checked ? 1 : prev[id]
    }));
  };

  // Tăng/giảm số lượng
  const handleQuantity = (id, value) => {
    setQuantities(prev => ({
      ...prev,
      [id]: Math.max(1, value)
    }));
  };

  // Tính tổng (giả sử giá 100k/sân)
  const totalPrice = Object.keys(selectedSlots).reduce((sum, id) => {
    if (selectedSlots[id]) {
      return sum + (quantities[id] || 1) * 100000;
    }
    return sum;
  }, 0);

  return (
    <div className="booking-modal-overlay">
      <div className="booking-modal">
        <div className="modal-header">
          <h2>Đặt sân ngày</h2>
          <button className="close-btn" onClick={onClose}>×</button>
        </div>
        <div className="modal-content">
          <label className="check-all">
            <input type="checkbox"
              checked={Object.values(selectedSlots).filter(Boolean).length === slots.length}
              onChange={handleCheckAll}
            />
            Tất cả
          </label>
          {slots.map(slot => (
            <div className={`slot-row${selectedSlots[slot.id] ? " active" : ""}`} key={slot.id}>
              <label>
                <input type="checkbox"
                  checked={!!selectedSlots[slot.id]}
                  onChange={e => handleCheckSlot(slot.id, e.target.checked)}
                />
                {slot.label}
              </label>
              <span className="slot-available">{slot.available} sân trống</span>
              <div className="quantity-box">
                <button onClick={() => handleQuantity(slot.id, (quantities[slot.id] || 1) - 1)}>-</button>
                <span>{quantities[slot.id] || 1}</span>
                <button onClick={() => handleQuantity(slot.id, (quantities[slot.id] || 1) + 1)}>+</button>
              </div>
            </div>
          ))}
        </div>
        <div className="modal-footer">
          <span>Tổng: {totalPrice.toLocaleString()} VND</span>
          <button className="pay-btn">Thanh toán</button>
        </div>
      </div>
    </div>
  );
}
