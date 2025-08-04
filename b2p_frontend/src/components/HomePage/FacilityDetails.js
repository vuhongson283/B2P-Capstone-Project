import React, { useState, useEffect } from 'react';
import './FacilityDetails.scss';
import { useParams } from 'react-router-dom';
import BookingModal from "./BookingModal.js";
import { getFacilityDetailsById, getAvailableSlots } from "../../services/apiService";
import { parseInt } from 'lodash';

// Constants
const TODAY = new Date().toISOString().slice(0, 10);
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
            min={TODAY}
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

// Reviews Component - Cải tiến
const Reviews = () => (
  <section className="reviews-section">
    <h2 className="reviews-section__title">
      <span className="title-icon">⭐</span>
      Đánh giá từ khách hàng
    </h2>
    
    <div className="rating-summary">
      <div className="rating-main">
        <span className="rating-value">4.6</span>
        <div className="rating-stars">
          {[...Array(5)].map((_, idx) => (
            <span key={idx} className="star filled">★</span>
          ))}
        </div>
      </div>
      
      <div className="rating-breakdown">
        <div className="breakdown-header">
          <span className="total-reviews">100 đánh giá</span>
        </div>
        <div className="breakdown-list">
          <div className="breakdown-item">
            <span className="star-label">5★</span>
            <div className="progress-bar">
              <div className="progress-fill" style={{width: '70%'}}></div>
            </div>
            <span className="count-label">70</span>
          </div>
          <div className="breakdown-item">
            <span className="star-label">4★</span>
            <div className="progress-bar">
              <div className="progress-fill" style={{width: '20%'}}></div>
            </div>
            <span className="count-label">20</span>
          </div>
          <div className="breakdown-item">
            <span className="star-label">3★</span>
            <div className="progress-bar">
              <div className="progress-fill" style={{width: '7%'}}></div>
            </div>
            <span className="count-label">7</span>
          </div>
          <div className="breakdown-item">
            <span className="star-label">2★</span>
            <div className="progress-bar">
              <div className="progress-fill" style={{width: '2%'}}></div>
            </div>
            <span className="count-label">2</span>
          </div>
          <div className="breakdown-item">
            <span className="star-label">1★</span>
            <div className="progress-bar">
              <div className="progress-fill" style={{width: '1%'}}></div>
            </div>
            <span className="count-label">1</span>
          </div>
        </div>
      </div>
    </div>
    
    <div className="reviews-list">
      {[1, 2, 3].map((reviewId) => (
        <div key={reviewId} className="review-card">
          <div className="review-card__avatar">
            <span className="avatar-text">U{reviewId}</span>
          </div>
          <div className="review-card__content">
            <div className="review-card__header">
              <div className="reviewer-info">
                <span className="reviewer-name">Người dùng {reviewId}</span>
                <span className="review-time">• 2 ngày trước</span>
              </div>
              <div className="review-stars">
                {[...Array(5)].map((_, idx) => (
                  <span key={idx} className="star filled">★</span>
                ))}
              </div>
            </div>
            <p className="review-text">
              Sân rất đẹp, cỏ xanh mướt, tiện ích đầy đủ. Nhân viên phục vụ nhiệt tình, 
              giá cả hợp lý. Sẽ quay lại lần sau!
            </p>
            <div className="review-actions">
              <button className="review-action-btn helpful">
                <span className="action-icon">👍</span>
                <span className="action-text">Hữu ích (12)</span>
              </button>
              <button className="review-action-btn reply">
                <span className="action-icon">💬</span>
                <span className="action-text">Trả lời</span>
              </button>
            </div>
          </div>
        </div>
      ))}
    </div>
    
    <div className="reviews-bottom">
      <button className="btn-write-review">
        <span className="btn-icon">📝</span>
        <span>Viết đánh giá</span>
      </button>
      <button className="btn-view-all">
        <span>Xem tất cả đánh giá</span>
        <span className="btn-arrow">→</span>
      </button>
    </div>
  </section>
);

// Main Component
const FacilityDetails = () => {
  const [modalOpen, setModalOpen] = useState(false);
  const [facilityData, setFacilityData] = useState(null);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedDate, setSelectedDate] = useState(TODAY);
  const [timeSlots, setTimeSlots] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [error, setError] = useState(null);
  const {facilityId} = useParams();

  // Fetch facility details on component mount
  useEffect(() => {
    const fetchFacilityDetails = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const response = await getFacilityDetailsById(parseInt(facilityId));
        
        if (response.data) {
          setFacilityData(response.data);
          
          // Set default category to the first available category
          if (response.data.categories && response.data.categories.length > 0) {
            setSelectedCategory(response.data.categories[0].categoryId.toString());
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
      
      <Reviews />
      
      {modalOpen && (
        <BookingModal 
          open={modalOpen} 
          onClose={() => setModalOpen(false)}
          timeSlots={timeSlots}
          selectedDate={selectedDate}
          facilityData={facilityData}
          selectedCategory={selectedCategory}
        />
      )}
    </div>
  );
};

export default FacilityDetails;