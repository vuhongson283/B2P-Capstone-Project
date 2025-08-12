import React, { useState, useEffect, useCallback, useMemo } from 'react';
import './FacilityDetails.scss';
import { useParams } from 'react-router-dom';
import BookingModal from "./BookingModal.js";
import { getFacilityDetailsById, getAvailableSlots } from "../../services/apiService";

// ✅ CONSTANTS
const TODAY_DATE = new Date().toISOString().slice(0, 10);
const FALLBACK_IMAGES = [
  'https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800&h=600&fit=crop',
  'https://images.unsplash.com/photo-1544966503-7cc5ac882d5e?w=800&h=600&fit=crop',
  'https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=800&h=600&fit=crop',
];

// ✅ UTILITY FUNCTIONS
const convertGoogleDriveLink = (originalUrl) => {
  if (!originalUrl) return "https://placehold.co/400x300?text=No+Image&bg=E8F5E9&color=2E7D32";
  if (originalUrl.includes('thumbnail')) return originalUrl;
  
  const match = originalUrl.match(/id=([^&]+)/);
  if (match) {
    const id = match[1];
    // ✅ THÊM SIZE PARAMETER cho ảnh sắc nét
    return `https://drive.google.com/thumbnail?id=${id}&sz=w800-h600-c`; // HD quality
  }
  return originalUrl;
};
const formatTimeSlot = (startTime, endTime) => {
  const formatTime = (timeString) => timeString?.substring(0, 5) || '';
  return `${formatTime(startTime)} - ${formatTime(endTime)}`;
};

const formatTime = (timeString) => timeString?.substring(0, 5) || '';

// ✅ LOADING SPINNER COMPONENT
const LoadingSpinner = () => (
  <div className="loading-spinner">
    <div className="spinner-ring"></div>
    <div className="spinner-ring"></div>
    <div className="spinner-ring"></div>
  </div>
);

// ✅ STAR RATING COMPONENT
const StarRating = ({ rating, size = 'medium', showValue = false }) => {
  const stars = useMemo(() => {
    return [...Array(5)].map((_, index) => (
      <span 
        key={index} 
        className={`star ${index < rating ? 'filled' : ''} ${size}`}
      >
        ★
      </span>
    ));
  }, [rating, size]);

  return (
    <div className="star-rating">
      <div className="stars">{stars}</div>
      {showValue && <span className="rating-value">({rating})</span>}
    </div>
  );
};

// ✅ ENHANCED REVIEWS MODAL
const ReviewsModal = ({ open, onClose, ratings = [], facilityName = "" }) => {
  const [selectedStars, setSelectedStars] = useState('all');
  
  const { uniqueRatings, ratingStats } = useMemo(() => {
    if (!ratings || ratings.length === 0) {
      return {
        uniqueRatings: [],
        ratingStats: {
          averageRating: 0,
          totalReviews: 0,
          breakdown: { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 }
        }
      };
    }

    const seen = new Set();
    const unique = ratings.filter(rating => {
      const key = `${rating.ratingId}-${rating.bookingId}`;
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });

    const breakdown = { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 };
    let totalStars = 0;

    unique.forEach(rating => {
      const stars = rating.stars;
      if (stars >= 1 && stars <= 5) {
        breakdown[stars]++;
        totalStars += stars;
      }
    });

    return {
      uniqueRatings: unique,
      ratingStats: {
        averageRating: Math.round((totalStars / unique.length) * 10) / 10,
        totalReviews: unique.length,
        breakdown
      }
    };
  }, [ratings]);

  const filteredRatings = useMemo(() => {
    if (selectedStars === 'all') return uniqueRatings;
    return uniqueRatings.filter(rating => rating.stars === parseInt(selectedStars));
  }, [uniqueRatings, selectedStars]);

  if (!open) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="reviews-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2 className="modal-title">
            <span className="title-icon">⭐</span>
            Tất cả đánh giá - {facilityName}
          </h2>
          <button className="modal-close" onClick={onClose} aria-label="Đóng">×</button>
        </div>
        
        <div className="modal-content">
          <div className="rating-summary-modal">
            <div className="summary-stats">
              <div className="rating-display">
                <span className="rating-number">{ratingStats.averageRating}</span>
                <StarRating rating={Math.round(ratingStats.averageRating)} size="large" />
                <span className="total-count">({ratingStats.totalReviews} đánh giá)</span>
              </div>
            </div>

            <div className="rating-filters">
              <label className="filter-label">Lọc theo số sao:</label>
              <div className="filter-buttons">
                <button 
                  className={`filter-btn ${selectedStars === 'all' ? 'active' : ''}`}
                  onClick={() => setSelectedStars('all')}
                >
                  Tất cả ({ratingStats.totalReviews})
                </button>
                {[5, 4, 3, 2, 1].map(stars => (
                  <button 
                    key={stars}
                    className={`filter-btn ${selectedStars === stars.toString() ? 'active' : ''}`}
                    onClick={() => setSelectedStars(stars.toString())}
                  >
                    {stars}★ ({ratingStats.breakdown[stars]})
                  </button>
                ))}
              </div>
            </div>
          </div>

          <div className="reviews-list-modal">
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
                <div key={`${rating.ratingId}-${rating.bookingId}-${index}`} className="review-item-modal">
                  <div className="review-avatar">
                    <span>U{rating.bookingId}</span>
                  </div>
                  <div className="review-content">
                    <div className="review-header">
                      <div className="reviewer-info">
                        <span className="reviewer-name">Người dùng #{rating.bookingId}</span>
                        <span className="booking-id">• Booking #{rating.bookingId}</span>
                      </div>
                      <StarRating rating={rating.stars} size="small" />
                    </div>
                    <p className="review-text">{rating.comment || 'Không có bình luận'}</p>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        <div className="modal-footer">
          <button className="btn-secondary" onClick={onClose}>Đóng</button>
        </div>
      </div>
    </div>
  );
};

// ✅ ENHANCED HEADER COMPONENT
const FacilityHeader = ({ facilityData, onBack, onFavorite, isFavorite }) => (
  <header className="facility-header">
    <button className="header-btn back-btn" onClick={onBack} aria-label="Quay lại">
      <span className="btn-icon">←</span>
    </button>
    <div className="header-content">
      <h1 className="facility-name">{facilityData?.facilityName || 'Cơ sở thể thao'}</h1>
      <div className="facility-owner">
        <span className="owner-icon">👤</span>
        <span>{facilityData?.ownerName || 'Chủ sân thể thao'}</span>
      </div>
    </div>
    <button 
      className={`header-btn favorite-btn ${isFavorite ? 'active' : ''}`} 
      onClick={onFavorite} 
      aria-label="Yêu thích"
    >
      <span className="btn-icon">{isFavorite ? '♥' : '♡'}</span>
    </button>
  </header>
);

// ✅ ENHANCED IMAGE CAROUSEL
const ImageCarousel = ({ images }) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [failedImages, setFailedImages] = useState(new Set());

  const displayImages = useMemo(() => {
    if (images && images.length > 0) {
      const processedImages = images
        .filter(img => img.imageUrl && img.imageUrl.trim() !== '')
        .sort((a, b) => (a.order || 0) - (b.order || 0))
        .map(img => convertGoogleDriveLink(img.imageUrl))
        .filter(url => url && !failedImages.has(url));
      
      return processedImages.length > 0 ? processedImages : FALLBACK_IMAGES;
    }
    return FALLBACK_IMAGES;
  }, [images, failedImages]);

  useEffect(() => {
    if (currentIndex >= displayImages.length) {
      setCurrentIndex(0);
    }
  }, [displayImages.length, currentIndex]);

  const navigateImage = useCallback((direction) => {
    setCurrentIndex((prevIndex) => {
      const newIndex = prevIndex + direction;
      return (newIndex + displayImages.length) % displayImages.length;
    });
  }, [displayImages.length]);

  const handleImageError = useCallback((failedUrl) => {
    setFailedImages(prev => new Set([...prev, failedUrl]));
  }, []);

  const handleImageLoad = useCallback(() => {
    setIsLoading(false);
  }, []);

  return (
    <div className="image-carousel">
      <div className="carousel-container">
        {isLoading && (
          <div className="image-loading">
            <LoadingSpinner />
          </div>
        )}
        
        <img 
          src={displayImages[currentIndex]} 
          alt={`Cơ sở thể thao ${currentIndex + 1}`} 
          className="carousel-image"
          onError={() => handleImageError(displayImages[currentIndex])}
          onLoad={handleImageLoad}
        />
        
        <div className="image-overlay">
          <div className="image-counter">
            {currentIndex + 1} / {displayImages.length}
          </div>
        </div>

        {displayImages.length > 1 && (
          <>
            <button 
              className="carousel-btn prev-btn" 
              onClick={() => navigateImage(-1)}
              aria-label="Ảnh trước"
            >
              ‹
            </button>
            <button 
              className="carousel-btn next-btn" 
              onClick={() => navigateImage(1)}
              aria-label="Ảnh tiếp"
            >
              ›
            </button>
          </>
        )}
      </div>

      {displayImages.length > 1 && (
        <div className="carousel-indicators">
          {displayImages.map((_, index) => (
            <button
              key={index}
              className={`indicator ${index === currentIndex ? 'active' : ''}`}
              onClick={() => setCurrentIndex(index)}
              aria-label={`Chuyển đến ảnh ${index + 1}`}
            />
          ))}
        </div>
      )}
    </div>
  );
};

// ✅ ENHANCED FACILITY INFO
const FacilityInfo = ({ facilityData }) => {
  const infoItems = useMemo(() => [
    {
      icon: '📍',
      label: 'Địa điểm',
      value: facilityData?.location || 'Chưa cập nhật'
    },
    {
      icon: '🕐',
      label: 'Giờ hoạt động',
      value: facilityData?.openTime && facilityData?.closeTime 
        ? `${formatTime(facilityData.openTime)} - ${formatTime(facilityData.closeTime)}`
        : 'Chưa cập nhật'
    },
    {
      icon: '📞',
      label: 'Liên hệ',
      value: facilityData?.contact || 'Chưa cập nhật'
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
  ], [facilityData]);

  return (
    <section className="facility-info">
      <h2 className="section-title">
        <span className="title-icon">📋</span>
        Thông tin cơ sở
      </h2>
      
      <div className="info-grid">
        {infoItems.map((item, index) => (
          <div key={index} className="info-item">
            <div className="info-label">
              <span className="item-icon">{item.icon}</span>
              <span className="item-text">{item.label}</span>
            </div>
            <div className="info-value">{item.value}</div>
          </div>
        ))}
      </div>

      {facilityData?.categories && facilityData.categories.length > 0 && (
        <div className="facility-categories">
          <h3 className="categories-title">
            <span className="title-icon">🏆</span>
            Các loại sân có sẵn
          </h3>
          <div className="categories-grid">
            {facilityData.categories.map((category) => (
              <div key={category.categoryId} className="category-card">
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

// ✅ ENHANCED BOOKING TABLE
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
}) => {
  const isBookingAvailable = selectedCategory && timeSlots.length > 0;

  return (
    <section className="booking-section">
      <h2 className="section-title">
        <span className="title-icon">⚽</span>
        Đặt sân thể thao
      </h2>
      
      <div className="booking-controls">
        <div className="control-row">
          <div className="control-group">
            <label htmlFor="category-select" className="control-label">
              <span className="label-icon">🏟️</span>
              Loại sân
            </label>
            <select 
              id="category-select"
              className="form-control select-control" 
              value={selectedCategory}
              onChange={(e) => onCategoryChange(e.target.value)}
              disabled={loading}
            >
              <option value="">
                {loading ? 'Đang tải...' : 'Chọn loại sân'}
              </option>
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
              Ngày đặt
            </label>
            <input
              id="date-select"
              type="date"
              className="form-control date-control"
              value={selectedDate}
              onChange={(e) => onDateChange(e.target.value)}
              min={TODAY_DATE}
            />
          </div>
        </div>
      </div>
      
      {loadingSlots && (
        <div className="loading-section">
          <LoadingSpinner />
          <p>Đang tải lịch trống...</p>
        </div>
      )}
      
      {!loadingSlots && timeSlots.length > 0 && (
        <div className="schedule-container">
          <div className="schedule-table-wrapper">
            <table className="schedule-table">
              <thead>
                <tr>
                  <th className="time-column">
                    <div className="column-header">
                      <span className="header-icon">⏰</span>
                      <span>Khung giờ</span>
                    </div>
                  </th>
                  {timeSlots.map((slot) => (
                    <th key={slot.timeSlotId} className="slot-column">
                      <div className="slot-header">
                        {formatTimeSlot(slot.startTime, slot.endTime)}
                      </div>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td className="label-cell">
                    <div className="row-label">
                      <span className="label-icon">🏟️</span>
                      <span>Sân trống</span>
                    </div>
                  </td>
                  {timeSlots.map((slot) => (
                    <td 
                      key={slot.timeSlotId} 
                      className={`availability-cell ${slot.availableCourtCount > 0 ? 'available' : 'unavailable'}`}
                    >
                      <div className="availability-content">
                        <span className="court-count">{slot.availableCourtCount}</span>
                        <span className="availability-status">
                          {slot.availableCourtCount > 0 ? 'Còn trống' : 'Hết sân'}
                        </span>
                      </div>
                    </td>
                  ))}
                </tr>
              </tbody>
            </table>
          </div>
          
          <div className="schedule-legend">
            <div className="legend-item">
              <div className="legend-indicator available"></div>
              <span>Còn sân trống</span>
            </div>
            <div className="legend-item">
              <div className="legend-indicator unavailable"></div>
              <span>Hết sân</span>
            </div>
          </div>
        </div>
      )}
      
      {!loadingSlots && timeSlots.length === 0 && selectedCategory && (
        <div className="empty-schedule">
          <div className="empty-icon">📅</div>
          <p>Không có lịch trống cho loại sân này</p>
        </div>
      )}
      
      {!loadingSlots && !selectedCategory && (
        <div className="empty-schedule">
          <div className="empty-icon">🏟️</div>
          <p>Vui lòng chọn loại sân để xem lịch trống</p>
        </div>
      )}
      
      <div className="booking-action">
        <button 
          className={`btn-primary booking-btn ${!isBookingAvailable ? 'disabled' : ''}`}
          onClick={onOpenModal}
          disabled={!isBookingAvailable}
        >
          <span className="btn-icon">⚽</span>
          <span>Đặt sân ngay</span>
        </button>
      </div>
    </section>
  );
};

// ✅ ENHANCED REVIEWS SECTION
const Reviews = ({ ratings = [], onOpenReviewsModal }) => {
  const { uniqueRatings, ratingStats, displayedReviews } = useMemo(() => {
    if (!ratings || ratings.length === 0) {
      return { uniqueRatings: [], ratingStats: null, displayedReviews: [] };
    }

    const seen = new Set();
    const unique = ratings.filter(rating => {
      const key = `${rating.ratingId}-${rating.bookingId}`;
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });

    const breakdown = { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 };
    let totalStars = 0;

    unique.forEach(rating => {
      const stars = rating.stars;
      if (stars >= 1 && stars <= 5) {
        breakdown[stars]++;
        totalStars += stars;
      }
    });

    const stats = {
      averageRating: Math.round((totalStars / unique.length) * 10) / 10,
      totalReviews: unique.length,
      breakdown
    };

    return {
      uniqueRatings: unique,
      ratingStats: stats,
      displayedReviews: unique.slice(0, 3)
    };
  }, [ratings]);

  if (!ratingStats) {
    return (
      <section className="reviews-section">
        <h2 className="section-title">
          <span className="title-icon">⭐</span>
          Đánh giá khách hàng
        </h2>
        
        <div className="empty-reviews">
          <div className="empty-icon">⭐</div>
          <p>Chưa có đánh giá nào</p>
          <button className="btn-secondary">
            <span className="btn-icon">📝</span>
            <span>Viết đánh giá đầu tiên</span>
          </button>
        </div>
      </section>
    );
  }

  return (
    <section className="reviews-section">
      <h2 className="section-title">
        <span className="title-icon">⭐</span>
        Đánh giá khách hàng
      </h2>
      
      <div className="rating-overview">
        <div className="rating-score">
          <div className="score-number">{ratingStats.averageRating}</div>
          <StarRating rating={Math.round(ratingStats.averageRating)} size="large" />
          <div className="score-text">{ratingStats.totalReviews} đánh giá</div>
        </div>
        
        <div className="rating-distribution">
          {[5, 4, 3, 2, 1].map(stars => {
            const count = ratingStats.breakdown[stars];
            const percentage = ratingStats.totalReviews > 0 
              ? Math.round((count / ratingStats.totalReviews) * 100) 
              : 0;
            
            return (
              <div key={stars} className="distribution-item">
                <span className="star-label">{stars}★</span>
                <div className="progress-track">
                  <div 
                    className="progress-fill" 
                    style={{width: `${percentage}%`}}
                  />
                </div>
                <span className="count-label">{count}</span>
              </div>
            );
          })}
        </div>
      </div>
      
      <div className="reviews-list">
        {displayedReviews.map((rating, index) => (
          <div key={`${rating.ratingId}-${rating.bookingId}-${index}`} className="review-card">
            <div className="review-avatar">
              <span>U{rating.bookingId}</span>
            </div>
            <div className="review-content">
              <div className="review-header">
                <div className="reviewer-info">
                  <span className="reviewer-name">Người dùng #{rating.bookingId}</span>
                  <span className="review-meta">• Booking #{rating.bookingId}</span>
                </div>
                <StarRating rating={rating.stars} size="small" />
              </div>
              <p className="review-text">{rating.comment || 'Không có bình luận'}</p>
              <div className="review-actions">
                <button className="action-btn helpful-btn">
                  <span className="action-icon">👍</span>
                  <span>Hữu ích</span>
                </button>
                <button className="action-btn reply-btn">
                  <span className="action-icon">💬</span>
                  <span>Trả lời</span>
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
      
      <div className="reviews-footer">
        <button className="btn-primary write-review-btn">
          <span className="btn-icon">📝</span>
          <span>Viết đánh giá</span>
        </button>
        <button className="btn-secondary view-all-btn" onClick={onOpenReviewsModal}>
          <span>Xem tất cả ({ratingStats.totalReviews})</span>
          <span className="btn-arrow">→</span>
        </button>
      </div>
    </section>
  );
};

// ✅ MAIN COMPONENT
const FacilityDetails = () => {
  const { facilityId } = useParams();
  
  // State management
  const [facilityData, setFacilityData] = useState(null);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedDate, setSelectedDate] = useState(TODAY_DATE);
  const [timeSlots, setTimeSlots] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [error, setError] = useState(null);
  
  // Modal states
  const [bookingModalOpen, setBookingModalOpen] = useState(false);
  const [reviewsModalOpen, setReviewsModalOpen] = useState(false);
  
  // UI states
  const [isFavorite, setIsFavorite] = useState(false);

  // Fetch facility details
  useEffect(() => {
    const fetchFacilityDetails = async () => {
      if (!facilityId) return;
      
      setLoading(true);
      setError(null);
      
      try {
        const response = await getFacilityDetailsById(parseInt(facilityId));
        
        if (response.data) {
          setFacilityData(response.data);
          
          // Auto-select first category
          if (response.data.categories?.length > 0) {
            setSelectedCategory(response.data.categories[0].categoryId.toString());
          }
        } else {
          setError('Không tìm thấy thông tin cơ sở');
        }
      } catch (err) {
        console.error('Error fetching facility details:', err);
        setError('Không thể tải thông tin cơ sở');
      } finally {
        setLoading(false);
      }
    };

    fetchFacilityDetails();
  }, [facilityId]);

  // Fetch available slots
  useEffect(() => {
    const fetchAvailableSlots = async () => {
      if (!selectedCategory || !selectedDate || !facilityId) {
        setTimeSlots([]);
        return;
      }

      setLoadingSlots(true);
      try {
        const response = await getAvailableSlots(facilityId, selectedCategory, selectedDate);
        const slots = response.data?.data || response.data || [];
        setTimeSlots(slots);
      } catch (err) {
        console.error('Error fetching available slots:', err);
        setTimeSlots([]);
      } finally {
        setLoadingSlots(false);
      }
    };

    fetchAvailableSlots();
  }, [facilityId, selectedCategory, selectedDate]);

  // Event handlers
  const handleCategoryChange = useCallback((categoryId) => {
    setSelectedCategory(categoryId);
    setTimeSlots([]);
  }, []);

  const handleDateChange = useCallback((date) => {
    setSelectedDate(date);
    setTimeSlots([]);
  }, []);

  const handleBack = useCallback(() => {
    window.history.back();
  }, []);

  const handleFavoriteToggle = useCallback(() => {
    setIsFavorite(prev => !prev);
    // TODO: Implement favorite API call
  }, []);

  // Loading state
  if (loading) {
    return (
      <div className="facility-page">
        <div className="loading-container">
          <LoadingSpinner />
          <p className="loading-text">Đang tải thông tin cơ sở...</p>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="facility-page">
        <div className="error-container">
          <div className="error-icon">⚠️</div>
          <h2 className="error-title">Có lỗi xảy ra</h2>
          <p className="error-message">{error}</p>
          <button className="btn-primary" onClick={() => window.location.reload()}>
            Thử lại
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="facility-page">
      <FacilityHeader 
        facilityData={facilityData}
        onBack={handleBack}
        onFavorite={handleFavoriteToggle}
        isFavorite={isFavorite}
      />
      
      <main className="facility-main">
        <div className="main-content">
          <ImageCarousel images={facilityData?.images} />
        </div>
        <div className="main-sidebar">
          <FacilityInfo facilityData={facilityData} />
        </div>
      </main>
      
      <BookingTable 
        onOpenModal={() => setBookingModalOpen(true)}
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
      
      {/* Modals */}
      {bookingModalOpen && (
        <BookingModal 
          open={bookingModalOpen} 
          onClose={() => setBookingModalOpen(false)}
          timeSlots={timeSlots}
          selectedDate={selectedDate}
          facilityData={facilityData}
          selectedCategory={selectedCategory}
        />
      )}

      {reviewsModalOpen && (
        <ReviewsModal 
          open={reviewsModalOpen} 
          onClose={() => setReviewsModalOpen(false)}
          ratings={facilityData?.ratings}
          facilityName={facilityData?.facilityName}
        />
      )}
    </div>
  );
};

export default FacilityDetails;