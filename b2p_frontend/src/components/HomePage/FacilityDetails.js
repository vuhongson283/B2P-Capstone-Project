import React, { useState } from 'react';
import './FacilityDetails.scss';
import BookingModal from "./BookingModal.js";

const today = new Date().toISOString().slice(0, 10);

const images = [
  'https://nads.1cdn.vn/2024/11/22/74da3f39-759b-4f08-8850-4c8f2937e81a-1_mangeshdes.png',
  'https://via.placeholder.com/200x150?text=Image+2',
  'https://via.placeholder.com/200x150?text=Image+3',
];

function FacilityHeader() {
  return (
    <div className="facility-header">
      <button className="back-btn">{'<'}</button>
      <div>
        <h1 className="facility-title">Tên cơ sở</h1>
        <div className="owner-name">Tên chủ sân</div>
      </div>
      <button className="fav-btn">♡</button>
    </div>
  );
}

function ImageCarousel({ images }) {
  const [idx, setIdx] = useState(0);
  return (
    <div className="carousel">
      <button onClick={() => setIdx((idx - 1 + images.length) % images.length)}>{'<'}</button>
      <img src={images[idx]} alt="facility" className="carousel-img" />
      <button onClick={() => setIdx((idx + 1) % images.length)}>{'>'}</button>
      <div className="carousel-dots">
        {images.map((_, i) => (
          <span key={i} className={i === idx ? 'active' : ''}>•</span>
        ))}
      </div>
    </div>
  );
}

function FacilityInfo() {
  return (
    <div className="facility-info">
      <h2>Giới thiệu chung</h2>
      <div>Địa điểm: Vĩnh Tường, Vĩnh Phúc</div>
      <div>Giờ mở cửa: 7h - 22h</div>
      <div>Giá thuê/giờ: 100k VNĐ/h</div>
      <div>Số lượng sân: 4</div>
    </div>
  );
}

function BookingTable({ onOpenModal }) {
  const slots = [
    { time: '7h - 9h', available: 2 },
    { time: '9h30 - 11h30', available: 3 },
    { time: '13h - 15h', available: 4 },
    { time: '15h30 - 17h30', available: 5 },
  ];
  return (
    <div className="booking-section">
      <h2>Lịch đặt sân</h2>
      <div className="booking-toolbar">
        <span></span>
        <div className="booking-controls">
          <select className="facility-type-dropdown">
            <option>Sân bóng đá</option>
            <option>Sân cầu lông</option>
            <option>Sân bóng chuyền</option>
            <option>Sân bóng đá mini</option>
          </select>
          <input
            type="date"
            className="choose-date-input"
            defaultValue={today}
          />
        </div>
      </div>
      <table className="booking-table">
        <thead>
          <tr>
            <th>Khung giờ</th>
            {slots.map((slot, i) => (
              <th key={i}>{slot.time}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>Số sân trống</td>
            {slots.map((slot, i) => (
              <td key={i}>{slot.available}</td>
            ))}
          </tr>
        </tbody>
      </table>
      <button className="booking-btn" onClick={onOpenModal}>Đặt sân</button>
    </div>
  );
}

function Reviews() {
  return (
    <div className="reviews-section">
      <h2>Đánh giá</h2>
      <div className="rating-summary">
        <span className="rating-value">4,6</span>
        <span className="star">★</span>
        <span className="rating-count">100 đánh giá</span>
      </div>
      <div className="review-card">
        <div className="reviewer-avatar"></div>
        <div>
          <div className="reviewer-info">
            <span>Reviewer</span>
            <span className="review-time">• review time</span>
          </div>
          <div className="review-stars">
            {[...Array(5)].map((_, i) => (
              <span key={i} className="star">★</span>
            ))}
          </div>
          <div className="review-content">review content</div>
        </div>
      </div>
    </div>
  );
}

export default function FacilityDetails() {
  const [modalOpen, setModalOpen] = useState(false);

  return (
    <div className="facility-page">
      <FacilityHeader />
      <div className="facility-main">
        <ImageCarousel images={images} />
        <FacilityInfo />
      </div>

      <BookingTable onOpenModal={() => setModalOpen(true)} />

      <Reviews />

      {modalOpen && (
        <BookingModal open={modalOpen} onClose={() => setModalOpen(false)} />
      )}
    </div>
  );
}
