import React, { useState, useEffect } from 'react';
import { useAuth } from '../../context/AuthContext';
import './FacilityDetails.scss';
import { useParams } from 'react-router-dom';
import BookingModal from "./BookingModal.js";
import BookingDetail from "./BookingDetail.js"; // Import BookingDetail modal mới
import { getFacilityDetailsById, getAvailableSlots, createBookingForPlayer, createPaymentOrder, createStripePaymentOrder } from "../../services/apiService";
import { parseInt } from 'lodash';

// Constants
const TODAY_DATE = new Date().toISOString().slice(0, 10);
const FACILITY_IMAGES = [
  'https://nads.1cdn.vn/2024/11/22/74da3f39-759b-4f08-8850-4c8f2937e81a-1_mangeshdes.png',
  'https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800&h=600&fit=crop',
  'https://images.unsplash.com/photo-1544966503-7cc5ac882d5e?w=800&h=600&fit=crop',
  'https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=800&h=600&fit=crop',
];

// Helper function to convert Google Drive share link to viewable image link
const convertGoogleDriveLink = (url) => {
  if (!url) return null;

  // Method 1: Extract file ID from various Google Drive URL formats
  let fileId = null;

  // Format: https://drive.google.com/file/d/FILE_ID/view?usp=sharing
  const shareMatch = url.match(/\/file\/d\/([a-zA-Z0-9_-]+)/);
  if (shareMatch) {
    fileId = shareMatch[1];
  }

  // Format: https://drive.google.com/open?id=FILE_ID
  const openMatch = url.match(/[?&]id=([a-zA-Z0-9_-]+)/);
  if (openMatch) {
    fileId = openMatch[1];
  }

  if (fileId) {
    // Direct download link (works for public images)
    return `https://drive.google.com/uc?export=download&id=${fileId}`;
  }

  // If not a Google Drive link, return as is
  return url;
};

// Helper function to validate image URLs
const isValidImageUrl = (url) => {
  if (!url) return false;
  return true;
};


// Helper function to format time
const formatTimeSlot = (startTime, endTime) => {
  const formatTime = (timeString) => {
    if (!timeString) return '';
    return timeString.substring(0, 5); // Format HH:mm from HH:mm:ss
  };
  return `${formatTime(startTime)} - ${formatTime(endTime)}`;
};

// Reviews Modal Component
const ReviewsModal = ({ open, onClose, ratings = [], facilityName = "" }) => {
  const [selectedStars, setSelectedStars] = useState('all');

  // Loại bỏ các rating trùng lặp
  const uniqueRatings = React.useMemo(() => {
    if (!ratings || ratings.length === 0) return [];

    const seen = new Set();
    return ratings.filter(rating => {
      const key = `${rating.ratingId}-${rating.bookingId}`;
      if (seen.has(key)) {
        return false;
      }
      seen.add(key);
      return true;
    });
  }, [ratings]);

  // Lọc ratings theo số sao được chọn
  const filteredRatings = React.useMemo(() => {
    if (selectedStars === 'all') {
      return uniqueRatings;
    }
    return uniqueRatings.filter(rating => rating.stars === parseInt(selectedStars));
  }, [uniqueRatings, selectedStars]);

  // Tính thống kê
  const ratingStats = React.useMemo(() => {
    if (uniqueRatings.length === 0) {
      return {
        averageRating: 0,
        totalReviews: 0,
        breakdown: { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 }
      };
    }

    const breakdown = { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 };
    let totalStars = 0;

    uniqueRatings.forEach(rating => {
      const stars = rating.stars;
      if (stars >= 1 && stars <= 5) {
        breakdown[stars]++;
        totalStars += stars;
      }
    });

    return {
      averageRating: Math.round((totalStars / uniqueRatings.length) * 10) / 10,
      totalReviews: uniqueRatings.length,
      breakdown
    };
  }, [uniqueRatings]);

  // Render stars
  const renderStars = (starCount) => {
    return [...Array(5)].map((_, index) => (
      <span
        key={index}
        className={`star ${index < starCount ? 'filled' : ''}`}
        style={{
          color: index < starCount ? '#fbbf24' : '#e5e7eb'
        }}
      >
        ★
      </span>
    ));
  };

  if (!open) return null;

  return (
    <div className="modal-overlay" onClick={onClose} style={{
      position: 'fixed',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: 'rgba(0, 0, 0, 0.6)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 1000,
      padding: '20px'
    }}>
      <div className="modal-container reviews-modal" onClick={(e) => e.stopPropagation()} style={{
        backgroundColor: 'white',
        borderRadius: '16px',
        width: '90%',
        maxWidth: '900px',
        height: '80vh',
        display: 'flex',
        flexDirection: 'column',
        boxShadow: '0 20px 60px rgba(0, 0, 0, 0.3)',
        overflow: 'hidden'
      }}>
        {/* Header */}
        <div className="modal-header" style={{
          padding: '24px',
          borderBottom: '1px solid #e5e7eb',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexDirection: 'column',
          backgroundColor: '#f8fafc'
        }}>
          <h2 className="modal-title" style={{
            margin: 0,
            fontSize: '20px',
            fontWeight: '600',
            color: '#1f2937',
            textAlign: 'center'
          }}>
            <span className="title-icon">⭐</span>
            Tất cả đánh giá - {facilityName}
          </h2>
          <button className="modal-close" onClick={onClose} aria-label="Close modal" style={{
            background: 'none',
            border: 'none',
            fontSize: '24px',
            color: '#6b7280',
            cursor: 'pointer',
            padding: '8px',
            borderRadius: '8px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: '40px',
            height: '40px',
            transition: 'all 0.2s ease'
          }} onMouseOver={(e) => {
            e.target.style.backgroundColor = '#f3f4f6';
            e.target.style.color = '#374151';
          }} onMouseOut={(e) => {
            e.target.style.backgroundColor = 'transparent';
            e.target.style.color = '#6b7280';
          }}>
            ×
          </button>
        </div>
        {/* Content */}
        <div className="modal-content">
          {/* Rating Summary */}
          <div className="reviews-modal-summary">
            <div className="summary-main">
              <div className="rating-display" style={{
                display: 'flex',
                alignItems: 'center',
                gap: '12px',
                marginBottom: '16px',
                textAlign: 'center'
              }}>
                <span className="rating-value" style={{
                  fontSize: '32px',
                  fontWeight: '700',
                  color: '#1f2937',
                  textAlign: 'center'
                }}>{ratingStats.averageRating}</span>
                <div className="rating-stars" style={{
                  color: '#fbbf24',
                  fontSize: '20px'
                }}>
                  {renderStars(Math.round(ratingStats.averageRating))}
                </div>
                <span className="rating-text" style={{
                  fontSize: '14px',
                  color: '#6b7280'
                }}>({ratingStats.totalReviews} đánh giá)</span>
              </div>
            </div>

            {/* Filter by stars */}
            <div style={{ display: 'flex', justifyContent: 'center', marginTop: '16px' }}>
              <div className="star-filter" style={{ textAlign: 'center' }}>
                <label className="filter-label" style={{
                  fontSize: '14px',
                  fontWeight: '500',
                  color: '#374151',
                  marginBottom: '8px',
                  display: 'block'
                }}>Lọc theo số sao:</label>
                <div className="filter-buttons" style={{
                  display: 'flex',
                  gap: '8px',
                  flexWrap: 'wrap'
                }}>
                  <button
                    className={`filter-btn ${selectedStars === 'all' ? 'active' : ''}`}
                    onClick={() => setSelectedStars('all')}
                    style={{
                      padding: '8px 16px',
                      border: '1px solid #d1d5db',
                      borderRadius: '8px',
                      backgroundColor: selectedStars === 'all' ? '#3b82f6' : 'white',
                      color: selectedStars === 'all' ? 'white' : '#374151',
                      cursor: 'pointer',
                      fontSize: '14px',
                      fontWeight: '500',
                      transition: 'all 0.2s ease'
                    }}
                    onMouseOver={(e) => {
                      if (selectedStars !== 'all') {
                        e.target.style.backgroundColor = '#f3f4f6';
                        e.target.style.borderColor = '#9ca3af';
                      }
                    }}
                    onMouseOut={(e) => {
                      if (selectedStars !== 'all') {
                        e.target.style.backgroundColor = 'white';
                        e.target.style.borderColor = '#d1d5db';
                      }
                    }}
                  >
                    Tất cả ({ratingStats.totalReviews})
                  </button>
                  {[5, 4, 3, 2, 1].map(stars => (
                    <button
                      key={stars}
                      className={`filter-btn ${selectedStars === stars.toString() ? 'active' : ''}`}
                      onClick={() => setSelectedStars(stars.toString())}
                      style={{
                        padding: '8px 16px',
                        border: '1px solid #d1d5db',
                        borderRadius: '8px',
                        backgroundColor: selectedStars === stars.toString() ? '#3b82f6' : 'white',
                        color: selectedStars === stars.toString() ? 'white' : '#374151',
                        cursor: 'pointer',
                        fontSize: '14px',
                        fontWeight: '500',
                        transition: 'all 0.2s ease',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '4px'
                      }}
                      onMouseOver={(e) => {
                        if (selectedStars !== stars.toString()) {
                          e.target.style.backgroundColor = '#f3f4f6';
                          e.target.style.borderColor = '#9ca3af';
                        }
                      }}
                      onMouseOut={(e) => {
                        if (selectedStars !== stars.toString()) {
                          e.target.style.backgroundColor = 'white';
                          e.target.style.borderColor = '#d1d5db';
                        }
                      }}
                    >
                      <span style={{ color: '#fbbf24' }}>{stars}★</span> ({ratingStats.breakdown[stars]})
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Reviews List */}
          <div className="reviews-modal-list">
            {filteredRatings.length === 0 ? (
              <div className="empty-reviews">
                <div className="empty-icon">⭐</div>
                <p>
                  {selectedStars === 'all'
                    ? 'Chưa có đánh giá nào'
                    : `Chưa có đánh giá ${selectedStars} sao nào`
                  }
                </p>
              </div>
            ) : (
              filteredRatings.map((rating, index) => (
                <div key={`${rating.ratingId}-${rating.bookingId}-${index}`} className="review-modal-card" style={{
                  border: '1px solid #e5e7eb',
                  borderRadius: '12px',
                  padding: '16px',
                  marginBottom: '16px',
                  backgroundColor: '#ffffff',
                  boxShadow: '0 2px 4px rgba(0, 0, 0, 0.05)',
                  transition: 'all 0.2s ease'
                }} onMouseOver={(e) => {
                  e.currentTarget.style.boxShadow = '0 4px 12px rgba(0, 0, 0, 0.1)';
                  e.currentTarget.style.borderColor = '#d1d5db';
                }} onMouseOut={(e) => {
                  e.currentTarget.style.boxShadow = '0 2px 4px rgba(0, 0, 0, 0.05)';
                  e.currentTarget.style.borderColor = '#e5e7eb';
                }}>
                  <div className="review-modal-card__avatar" style={{
                    display: 'flex',
                    alignItems: 'flex-start',
                    gap: '12px'
                  }}>
                    <div style={{
                      width: '40px',
                      height: '40px',
                      borderRadius: '50%',
                      backgroundColor: '#3b82f6',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: 'white',
                      fontSize: '14px',
                      fontWeight: '600',
                      flexShrink: 0
                    }}>
                      U{rating.bookingId}
                    </div>
                    <div className="review-modal-card__content" style={{ flex: 1 }}>
                      <div className="review-modal-card__header" style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        marginBottom: '8px'
                      }}>
                        <div className="reviewer-info">
                          <span className="reviewer-name" style={{
                            fontWeight: '600',
                            color: '#1f2937'
                          }}>Người dùng #{rating.bookingId}</span>
                          <span className="review-time" style={{
                            color: '#6b7280',
                            fontSize: '14px'
                          }}> • Booking #{rating.bookingId}</span>
                        </div>
                        <div className="review-stars" style={{
                          color: '#fbbf24',
                          fontSize: '16px'
                        }}>
                          {renderStars(rating.stars)}
                        </div>
                      </div>
                      <p className="review-text" style={{
                        color: '#4b5563',
                        lineHeight: '1.6',
                        margin: 0,
                        fontSize: '15px'
                      }}>
                        {rating.comment || 'Không có bình luận'}
                      </p>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="modal-footer" style={{
          padding: '20px 24px',
          borderTop: '1px solid #e5e7eb',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          backgroundColor: '#f8fafc'
        }}>
          <button className="btn-secondary" onClick={onClose} style={{
            padding: '10px 20px',
            border: '1px solid #d1d5db',
            borderRadius: '8px',
            backgroundColor: 'white',
            color: '#374151',
            cursor: 'pointer',
            fontSize: '14px',
            fontWeight: '500',
            transition: 'all 0.2s ease'
          }}
            onMouseOver={(e) => {
              e.target.style.backgroundColor = '#f3f4f6';
              e.target.style.borderColor = '#9ca3af';
            }}
            onMouseOut={(e) => {
              e.target.style.backgroundColor = 'white';
              e.target.style.borderColor = '#d1d5db';
            }}>
            Đóng
          </button>

        </div>
      </div>
    </div>
  );
};

// Constants
const TODAY = new Date().toISOString().slice(0, 10);

// Header Component
const FacilityHeader = ({ facilityData }) => (
  <header className="facility-header">
    <button className="btn-icon btn-back" aria-label="Go back" onClick={() => window.history.back()}>
      <span className="icon">←</span>
    </button>
    <div className="facility-header__content">
      <h1 className="facility-title">{facilityData?.facilityName || 'Tên cơ sở'}</h1>
      <div className="owner-name">
        {facilityData?.ownerName || 'Chủ sân thể thao'}
      </div>
    </div>
    <button className="btn-icon btn-favorite" aria-label="Add to favorites">
      <span className="icon">♡</span>
    </button>
  </header>
);

// Image Carousel Component
const ImageCarousel = ({ images }) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [failedImages, setFailedImages] = useState(new Set());

  // Process facility images - convert Google Drive links and handle all URLs
  const displayImages = React.useMemo(() => {
    console.log('Raw images from API:', images);

    if (images && images.length > 0) {
      // Process all images, convert Google Drive links
      const processedImages = images
        .filter(img => {
          const hasUrl = img.imageUrl && img.imageUrl.trim() !== '';
          console.log(`Image ${img.imageId}: ${img.imageUrl} - Has URL: ${hasUrl}`);
          return hasUrl;
        })
        .sort((a, b) => (a.order || 0) - (b.order || 0))
        .map(img => {
          const originalUrl = img.imageUrl;
          const convertedUrl = convertGoogleDriveLink(originalUrl);
          console.log(`Converting: ${originalUrl} → ${convertedUrl}`);
          return convertedUrl;
        })
        .filter(url => url && !failedImages.has(url));

      console.log('Processed images:', processedImages);

      if (processedImages.length > 0) {
        return processedImages;
      }
    }

    // No valid images from API, use default images
    console.log('No images from API, using fallback images');
    return FACILITY_IMAGES;
  }, [images, failedImages]);

  // Reset current index if it's out of bounds
  React.useEffect(() => {
    if (currentIndex >= displayImages.length) {
      setCurrentIndex(0);
    }
  }, [displayImages.length, currentIndex]);

  const navigateImage = (direction) => {
    setCurrentIndex((prevIndex) => {
      const newIndex = prevIndex + direction;
      return (newIndex + displayImages.length) % displayImages.length;
    });
  };

  const handleImageError = (failedUrl) => {
    console.error('Image failed to load:', failedUrl);

    setFailedImages(prev => new Set([...prev, failedUrl]));

    if (displayImages[currentIndex] === failedUrl) {
      const remainingImages = displayImages.filter(url => !failedImages.has(url) && url !== failedUrl);

      if (remainingImages.length === 0) {
        setCurrentIndex(0);
      } else {
        const nextIndex = displayImages.findIndex(url => !failedImages.has(url) && url !== failedUrl);
        if (nextIndex !== -1) {
          setCurrentIndex(nextIndex);
        }
      }
    }
  };

  return (
    <div className="carousel">
      {displayImages.length > 1 && (
        <button
          className="carousel__btn carousel__btn--prev"
          onClick={() => navigateImage(-1)}
          aria-label="Previous image"
        >
          ‹
        </button>
      )}
      <div className="carousel__container">
        <div className="carousel__image-wrapper">
          <img
            src={displayImages[currentIndex]}
            alt={`Facility view ${currentIndex + 1}`}
            className="carousel__image"
            onError={() => handleImageError(displayImages[currentIndex])}
            onLoad={() => console.log('Image loaded successfully:', displayImages[currentIndex])}
          />
          <div className="carousel__overlay">
            <div className="carousel__image-counter">
              {currentIndex + 1} / {displayImages.length}
            </div>
          </div>
        </div>
      </div>
      {displayImages.length > 1 && (
        <button
          className="carousel__btn carousel__btn--next"
          onClick={() => navigateImage(1)}
          aria-label="Next image"
        >
          ›
        </button>
      )}
      {displayImages.length > 1 && (
        <div className="carousel__dots">
          {displayImages.map((_, idx) => (
            <button
              key={idx}
              className={`carousel__dot ${idx === currentIndex ? 'active' : ''}`}
              onClick={() => setCurrentIndex(idx)}
              aria-label={`Go to image ${idx + 1}`}
            />
          ))}
        </div>
      )}
    </div>
  );
};

// Facility Info Component
const FacilityInfo = ({ facilityData }) => {
  const formatTime = (timeString) => {
    if (!timeString) return '';
    return timeString.substring(0, 5);
  };

  const infoItems = [
    {
      icon: '📍',
      label: 'Địa điểm',
      value: facilityData?.location || 'Chưa có thông tin'
    },
    {
      icon: '🕐',
      label: 'Giờ hoạt động',
      value: facilityData?.openTime && facilityData?.closeTime
        ? `${formatTime(facilityData.openTime)} - ${formatTime(facilityData.closeTime)}`
        : 'Chưa có thông tin'
    },
    {
      icon: '📞',
      label: 'Số điện thoại',
      value: facilityData?.contact || 'Chưa có thông tin'
    },
    {
      icon: '🏟️',
      label: 'Loại sân',
      value: `${facilityData?.categories?.length || 0} loại sân`
    },
    {
      icon: '⚡',
      label: 'Trạng thái',
      value: facilityData?.status?.statusDescription || 'Đang hoạt động'
    }
  ];

  return (
    <section className="facility-info">
      <h2 className="facility-info__title">Thông tin cơ sở</h2>
      <div className="facility-info__details">
        {infoItems.map((item, index) => (
          <div key={index} className="info-item">
            <div className="info-label">
              <span className="info-icon">{item.icon}</span>
              <span>{item.label}</span>
            </div>
            <div className="info-value">{item.value}</div>
          </div>
        ))}
      </div>
      {facilityData?.categories && facilityData.categories.length > 0 && (
        <div className="facility-categories">
          <h3 className="categories-title">Các loại sân có sẵn</h3>
          <div className="categories-list">
            {facilityData.categories.map((category) => (
              <div key={category.categoryId} className="category-item">
                <span className="category-icon">🏟️</span>
                <span className="category-name">{category.categoryName}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </section>
  );
};

// Booking Table Component
const BookingTable = ({
  onOpenModal,
  courtCategories,
  selectedCategory,
  onCategoryChange,
  selectedDate,
  onDateChange,
  timeSlots,
  loading,
  loadingSlots
}) => (
  <section className="booking-section">
    <h2 className="booking-section__title">Đặt lịch sân thể thao</h2>
    <div className="booking-toolbar">
      <div className="booking-controls">
        <div className="control-group">
          <label htmlFor="category-select" className="control-label">
            <span className="label-icon">🏟️</span>
            Chọn loại sân
          </label>
          <select
            id="category-select"
            className="form-select"
            aria-label="Select facility type"
            value={selectedCategory}
            onChange={(e) => onCategoryChange(e.target.value)}
            disabled={loading}
          >
            {courtCategories.length === 0 && (
              <option value="">
                {loading ? 'Đang tải...' : 'Không có loại sân'}
              </option>
            )}
            {courtCategories.map((category) => (
              <option key={category.categoryId} value={category.categoryId}>
                {category.categoryName}
              </option>
            ))}
          </select>
        </div>

        <div className="control-group">
          <label htmlFor="date-select" className="control-label">
            <span className="label-icon">📅</span>
            Chọn ngày
          </label>
          <input
            id="date-select"
            type="date"
            className="form-date"
            value={selectedDate}
            onChange={(e) => onDateChange(e.target.value)}
            aria-label="Select date"
            min={TODAY_DATE}
          />
        </div>
      </div>
    </div>

    {loadingSlots && (
      <div className="loading-state">
        Đang tải lịch trống...
      </div>
    )}

    {!loadingSlots && timeSlots.length > 0 && (
      <div className="table-container">
        <div className="table-responsive">
          <table className="booking-table">
            <thead>
              <tr>
                <th className="time-header">
                  <span className="header-icon">⏰</span>
                  Khung giờ
                </th>
                {timeSlots.map((slot) => (
                  <th key={slot.timeSlotId} className="slot-header">
                    <div className="slot-time">
                      {formatTimeSlot(slot.startTime, slot.endTime)}
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className="row-label">
                  <span className="label-icon">🏟️</span>
                  Số sân trống
                </td>
                {timeSlots.map((slot) => (
                  <td
                    key={slot.timeSlotId}
                    className={`availability-cell ${slot.availableCourtCount > 0 ? 'available' : 'unavailable'}`}
                  >
                    <div className="availability-info">
                      <span className="count">{slot.availableCourtCount}</span>
                      <span className="status-text">
                        {slot.availableCourtCount > 0 ? 'Còn trống' : 'Hết chỗ'}
                      </span>
                    </div>
                  </td>
                ))}
              </tr>
            </tbody>
          </table>
        </div>

        <div className="table-legend">
          <div className="legend-item">
            <div className="legend-color available"></div>
            <span>Còn sân trống</span>
          </div>
          <div className="legend-item">
            <div className="legend-color unavailable"></div>
            <span>Hết sân</span>
          </div>
        </div>
      </div>
    )}

    {!loadingSlots && timeSlots.length === 0 && selectedCategory && (
      <div className="empty-state">
        <div className="empty-icon">📅</div>
        <p>Không có khung giờ nào khả dụng cho loại sân này</p>
      </div>
    )}

    {!loadingSlots && timeSlots.length === 0 && !selectedCategory && (
      <div className="empty-state">
        <div className="empty-icon">🏟️</div>
        <p>Vui lòng chọn loại sân để xem lịch trống</p>
      </div>
    )}

    <div className="booking-action">
      <button
        className="btn-primary btn-booking"
        onClick={onOpenModal}
        disabled={!selectedCategory || timeSlots.length === 0}
      >
        <span className="btn-icon">⚽</span>
        Đặt sân ngay
      </button>
    </div>
  </section>
);

// Reviews Component - Cải tiến với dữ liệu thực từ API
const Reviews = ({ ratings = [], onOpenReviewsModal }) => {
  // Loại bỏ các rating trùng lặp dựa trên ratingId và bookingId
  const uniqueRatings = React.useMemo(() => {
    if (!ratings || ratings.length === 0) return [];

    const seen = new Set();
    return ratings.filter(rating => {
      const key = `${rating.ratingId}-${rating.bookingId}`;
      if (seen.has(key)) {
        return false;
      }
      seen.add(key);
      return true;
    });
  }, [ratings]);

  // Tính toán thống kê đánh giá
  const ratingStats = React.useMemo(() => {
    if (uniqueRatings.length === 0) {
      return {
        averageRating: 0,
        totalReviews: 0,
        breakdown: { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 }
      };
    }

    const breakdown = { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 };
    let totalStars = 0;

    uniqueRatings.forEach(rating => {
      const stars = rating.stars;
      if (stars >= 1 && stars <= 5) {
        breakdown[stars]++;
        totalStars += stars;
      }
    });

    const averageRating = totalStars / uniqueRatings.length;

    return {
      averageRating: Math.round(averageRating * 10) / 10,
      totalReviews: uniqueRatings.length,
      breakdown
    };
  }, [uniqueRatings]);

  // Render stars dựa trên số sao - FIX: Logic đúng
  const renderStars = (starCount) => {
    console.log('Rendering stars for:', starCount); // Debug log
    return [...Array(5)].map((_, index) => {
      const isFilled = index < starCount;
      console.log(`Star ${index + 1}: ${isFilled ? 'filled' : 'empty'}`); // Debug log
      return (
        <span
          key={index}
          className={`star ${isFilled ? 'filled' : ''}`}
        >
          ★
        </span>
      );
    });
  };

  // Nếu không có đánh giá
  if (uniqueRatings.length === 0) {
    return (
      <section className="reviews-section">
        <h2 className="reviews-section__title">
          <span className="title-icon">⭐</span>
          Đánh giá từ khách hàng
        </h2>

        <div className="empty-state">
          <div className="empty-icon">⭐</div>
          <p>Chưa có đánh giá nào cho cơ sở này</p>
          <button className="btn-write-review">
            <span className="btn-icon">📝</span>
            <span>Viết đánh giá đầu tiên</span>
          </button>
        </div>
      </section>
    );
  }

  // Hiển thị tối đa 3 đánh giá gần nhất
  const displayedReviews = uniqueRatings.slice(0, 3);

  return (
    <section className="reviews-section">
      <h2 className="reviews-section__title">
        <span className="title-icon">⭐</span>
        Đánh giá từ khách hàng
      </h2>

      <div className="rating-summary">
        <div className="rating-main">
          <span className="rating-value">{ratingStats.averageRating}</span>
          <div className="rating-stars">
            {renderStars(Math.round(ratingStats.averageRating))}
          </div>
        </div>

        <div className="rating-breakdown">
          <div className="breakdown-header">
            <span className="total-reviews">{ratingStats.totalReviews} đánh giá</span>
          </div>
          <div className="breakdown-list">
            {[5, 4, 3, 2, 1].map(stars => {
              const count = ratingStats.breakdown[stars];
              const percentage = ratingStats.totalReviews > 0
                ? Math.round((count / ratingStats.totalReviews) * 100)
                : 0;

              return (
                <div key={stars} className="breakdown-item">
                  <span className="star-label">{stars}★</span>
                  <div className="progress-bar">
                    <div
                      className="progress-fill"
                      style={{ width: `${percentage}%` }}
                    ></div>
                  </div>
                  <span className="count-label">{count}</span>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      <div className="reviews-list">
        {displayedReviews.map((rating, index) => {
          console.log(`Review ${index}: ${rating.stars} stars`); // Debug log
          return (
            <div key={`${rating.ratingId}-${rating.bookingId}-${index}`} className="review-card">
              <div className="review-card__avatar">
                <span className="avatar-text">U{rating.bookingId}</span>
              </div>
              <div className="review-card__content">
                <div className="review-card__header">
                  <div className="reviewer-info">
                    <span className="reviewer-name">Người dùng #{rating.bookingId}</span>
                    <span className="review-time">• Booking #{rating.bookingId}</span>
                  </div>
                  <div className="review-stars">
                    {renderStars(rating.stars)}
                  </div>
                </div>
                <p className="review-text">
                  {rating.comment || 'Không có bình luận'}
                </p>
                <div className="review-actions">
                  <button className="review-action-btn helpful">
                    <span className="action-icon">👍</span>
                    <span className="action-text">Hữu ích</span>
                  </button>
                  <button className="review-action-btn reply">
                    <span className="action-icon">💬</span>
                    <span className="action-text">Trả lời</span>
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      <div className="reviews-bottom">
        <button className="btn-write-review">
          <span className="btn-icon">📝</span>
          <span>Viết đánh giá</span>
        </button>
        <button
          className="btn-view-all"
          onClick={onOpenReviewsModal}
        >
          <span>Xem tất cả đánh giá ({ratingStats.totalReviews})</span>
          <span className="btn-arrow">→</span>
        </button>
      </div>
    </section>
  );
};

// Main Component
const FacilityDetails = () => {
  const { userId } = useAuth(); //lay userid
  const [modalOpen, setModalOpen] = useState(false);
  const [reviewsModalOpen, setReviewsModalOpen] = useState(false);
  const [bookingDetailOpen, setBookingDetailOpen] = useState(false); // State cho BookingDetail modal
  const [bookingDetailData, setBookingDetailData] = useState(null); // Data cho BookingDetail
  const [facilityData, setFacilityData] = useState(null);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedDate, setSelectedDate] = useState(TODAY_DATE);
  const [timeSlots, setTimeSlots] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [error, setError] = useState(null);
  const { facilityId } = useParams();

  // Fetch facility details on component mount
  useEffect(() => {
    const fetchFacilityDetails = async () => {
      setLoading(true);
      setError(null);

      try {
        const response = await getFacilityDetailsById(parseInt(facilityId));

        if (response.data) {
          const facilityInfo = response.data;
          console.log('Facility data with ratings:', facilityInfo);

          setFacilityData(facilityInfo);

          // Set default category to the first available category
          if (facilityInfo.categories && facilityInfo.categories.length > 0) {
            setSelectedCategory(facilityInfo.categories[0].categoryId.toString());
          }
        } else {
          setError('Không tìm thấy thông tin cơ sở');
        }
      } catch (error) {
        console.error('Error fetching facility details:', error);
        setError('Không thể tải thông tin cơ sở');
      } finally {
        setLoading(false);
      }
    };

    if (facilityId) {
      fetchFacilityDetails();
    }
  }, [facilityId]);

  // Fetch available slots when category or date changes
  useEffect(() => {
    const fetchAvailableSlots = async () => {
      if (!selectedCategory || !selectedDate || !facilityId) {
        setTimeSlots([]);
        return;
      }

      setLoadingSlots(true);
      try {
        const response = await getAvailableSlots(facilityId, selectedCategory, selectedDate);

        if (response.data && response.data.data) {
          setTimeSlots(response.data.data);
        } else if (response.data) {
          setTimeSlots(response.data);
        } else {
          setTimeSlots([]);
        }
      } catch (error) {
        console.error('Error fetching available slots:', error);
        setTimeSlots([]);
      } finally {
        setLoadingSlots(false);
      }
    };

    fetchAvailableSlots();
  }, [facilityId, selectedCategory, selectedDate]);

  const handleCategoryChange = (categoryId) => {
    setSelectedCategory(categoryId);
    setTimeSlots([]);
  };

  const handleDateChange = (date) => {
    setSelectedDate(date);
    setTimeSlots([]);
  };

  // Handle proceed to booking detail - callback từ BookingModal
  const handleProceedToBookingDetail = (data) => {
    setBookingDetailData(data);
    setBookingDetailOpen(true);
  };

  // Handle close booking detail modal
  const handleCloseBookingDetail = () => {
    setBookingDetailOpen(false);
    setBookingDetailData(null);
  };

  if (loading) {
    return (
      <div className="facility-page">
        <div className="loading-state">
          <div className="loading-spinner"></div>
          Đang tải thông tin cơ sở...
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="facility-page">
        <div className="error-state">
          <div className="error-icon">⚠️</div>
          <h2>Có lỗi xảy ra</h2>
          <p>{error}</p>
          <button className="btn-primary" onClick={() => window.location.reload()}>
            Thử lại
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="facility-page">
      <FacilityHeader facilityData={facilityData} />
      <main className="facility-main">
        <ImageCarousel images={facilityData?.images} />
        <FacilityInfo facilityData={facilityData} />
      </main>

      <BookingTable
        onOpenModal={() => setModalOpen(true)}
        courtCategories={facilityData?.categories || []}
        selectedCategory={selectedCategory}
        onCategoryChange={handleCategoryChange}
        selectedDate={selectedDate}
        onDateChange={handleDateChange}
        timeSlots={timeSlots}
        loading={loading}
        loadingSlots={loadingSlots}
      />

      <Reviews
        ratings={facilityData?.ratings}
        onOpenReviewsModal={() => setReviewsModalOpen(true)}
      />

      {/* BookingModal với callback để chuyển sang BookingDetail */}
      {modalOpen && (
        <BookingModal
          open={modalOpen}
          onClose={() => setModalOpen(false)}
          timeSlots={timeSlots}
          selectedDate={selectedDate}
          facilityData={facilityData}
          selectedCategory={selectedCategory}
          onProceedToDetail={handleProceedToBookingDetail} // Pass callback
        />
      )}

      {/* ReviewsModal */}
      {reviewsModalOpen && (
        <ReviewsModal
          open={reviewsModalOpen}
          onClose={() => setReviewsModalOpen(false)}
          ratings={facilityData?.ratings}
          facilityName={facilityData?.facilityName}
        />
      )}

      {/* BookingDetail Modal mới */}
      {bookingDetailOpen && bookingDetailData && (
        <BookingDetail
          open={bookingDetailOpen}
          onClose={handleCloseBookingDetail}
          facilityId={bookingDetailData.facilityId}
          categoryId={bookingDetailData.categoryId}
          totalPrice={bookingDetailData.totalPrice}
          facilityData={bookingDetailData.facilityData}
          selectedDate={bookingDetailData.selectedDate}
          selectedSlots={bookingDetailData.selectedSlots}
          quantities={bookingDetailData.quantities}
          listSlotId={bookingDetailData?.listSlotId}
          createBooking={createBookingForPlayer}
          createPayment={createPaymentOrder}
          createStripePaymentOrder={createStripePaymentOrder}
          userId = {userId}
        />
      )}
    </div>
  );
};

export default FacilityDetails;