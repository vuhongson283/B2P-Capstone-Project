import React, { useState } from 'react';
import './FacilityDetails.scss';
import BookingModal from "./BookingModal.js";

// Constants
const TODAY = new Date().toISOString().slice(0, 10);
const FACILITY_IMAGES = [
  'https://nads.1cdn.vn/2024/11/22/74da3f39-759b-4f08-8850-4c8f2937e81a-1_mangeshdes.png',
  'https://via.placeholder.com/200x150?text=Image+2',
  'https://via.placeholder.com/200x150?text=Image+3',
];

const BOOKING_SLOTS = [
  { time: '7h - 9h', available: 2 },
  { time: '9h30 - 11h30', available: 3 },
  { time: '13h - 15h', available: 4 },
  { time: '15h30 - 17h30', available: 5 },
];

// Header Component
const FacilityHeader = () => (
  <header className="facility-header">
    <button className="btn-icon btn-back" aria-label="Go back">
      <span className="icon">←</span>
    </button>
    <div className="facility-header__content">
      <h1 className="facility-title">Tên cơ sở</h1>
      <div className="owner-name">Tên chủ sân</div>
    </div>
    <button className="btn-icon btn-favorite" aria-label="Add to favorites">
      <span className="icon">♡</span>
    </button>
  </header>
);

// Image Carousel Component
const ImageCarousel = ({ images }) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  
  const navigateImage = (direction) => {
    setCurrentIndex((prevIndex) => {
      const newIndex = prevIndex + direction;
      return (newIndex + images.length) % images.length;
    });
  };

  return (
    <div className="carousel">
      <button 
        className="carousel__btn carousel__btn--prev" 
        onClick={() => navigateImage(-1)}
        aria-label="Previous image"
      >
        ←
      </button>
      <div className="carousel__container">
        <img 
          src={images[currentIndex]} 
          alt={`Facility view ${currentIndex + 1}`} 
          className="carousel__image" 
        />
      </div>
      <button 
        className="carousel__btn carousel__btn--next" 
        onClick={() => navigateImage(1)}
        aria-label="Next image"
      >
        →
      </button>
      <div className="carousel__dots">
        {images.map((_, idx) => (
          <button
            key={idx}
            className={`carousel__dot ${idx === currentIndex ? 'active' : ''}`}
            onClick={() => setCurrentIndex(idx)}
            aria-label={`Go to image ${idx + 1}`}
          >
            •
          </button>
        ))}
      </div>
    </div>
  );
};

// Facility Info Component
const FacilityInfo = () => (
  <section className="facility-info">
    <h2 className="facility-info__title">Giới thiệu chung</h2>
    <div className="facility-info__details">
      <div className="info-item">
        <span className="info-label">Địa điểm:</span>
        <span className="info-value">Vĩnh Tường, Vĩnh Phúc</span>
      </div>
      <div className="info-item">
        <span className="info-label">Giờ mở cửa:</span>
        <span className="info-value">7h - 22h</span>
      </div>
      <div className="info-item">
        <span className="info-label">Giá thuê/giờ:</span>
        <span className="info-value">100k VNĐ/h</span>
      </div>
      <div className="info-item">
        <span className="info-label">Số lượng sân:</span>
        <span className="info-value">4</span>
      </div>
    </div>
  </section>
);

// Booking Table Component
const BookingTable = ({ onOpenModal }) => (
  <section className="booking-section">
    <h2 className="booking-section__title">Lịch đặt sân</h2>
    <div className="booking-toolbar">
      <div className="booking-controls">
        <select className="form-select" aria-label="Select facility type">
          <option>Sân bóng đá</option>
          <option>Sân cầu lông</option>
          <option>Sân bóng chuyền</option>
          <option>Sân bóng đá mini</option>
        </select>
        <input
          type="date"
          className="form-date"
          defaultValue={TODAY}
          aria-label="Select date"
        />
      </div>
    </div>
    <div className="table-responsive">
      <table className="booking-table">
        <thead>
          <tr>
            <th>Khung giờ</th>
            {BOOKING_SLOTS.map((slot, idx) => (
              <th key={idx}>{slot.time}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>Số sân trống</td>
            {BOOKING_SLOTS.map((slot, idx) => (
              <td key={idx}>{slot.available}</td>
            ))}
          </tr>
        </tbody>
      </table>
    </div>
    <button className="btn-primary btn-booking" onClick={onOpenModal}>
      Đặt sân
    </button>
  </section>
);

// Reviews Component
const Reviews = () => (
  <section className="reviews-section">
    <h2 className="reviews-section__title">Đánh giá</h2>
    <div className="rating-summary">
      <span className="rating-value">4,6</span>
      <span className="star">★</span>
      <span className="rating-count">100 đánh giá</span>
    </div>
    <div className="review-card">
      <div className="review-card__avatar" />
      <div className="review-card__content">
        <div className="review-card__header">
          <span className="reviewer-name">Reviewer</span>
          <span className="review-time">• review time</span>
        </div>
        <div className="review-stars">
          {[...Array(5)].map((_, idx) => (
            <span key={idx} className="star">★</span>
          ))}
        </div>
        <p className="review-text">review content</p>
      </div>
    </div>
  </section>
);

// Main Component
const FacilityDetails = () => {
  const [modalOpen, setModalOpen] = useState(false);

  return (
    <div className="facility-page">
      <FacilityHeader />
      <main className="facility-main">
        <ImageCarousel images={FACILITY_IMAGES} />
        <FacilityInfo />
      </main>
      <BookingTable onOpenModal={() => setModalOpen(true)} />
      <Reviews />
      {modalOpen && (
        <BookingModal 
          open={modalOpen} 
          onClose={() => setModalOpen(false)} 
        />
      )}
    </div>
  );
};

export default FacilityDetails;