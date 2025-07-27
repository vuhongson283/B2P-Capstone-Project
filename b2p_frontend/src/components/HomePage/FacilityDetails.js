import React, { useState, useEffect } from 'react';
import './FacilityDetails.scss';
import BookingModal from "./BookingModal.js";
import { getFacilityDetailsById, getAvailableSlots } from "../../services/apiService";

// Constants
const TODAY = new Date().toISOString().slice(0, 10);
const FACILITY_IMAGES = [
  'https://nads.1cdn.vn/2024/11/22/74da3f39-759b-4f08-8850-4c8f2937e81a-1_mangeshdes.png',
  'https://via.placeholder.com/200x150?text=Image+2',
  'https://via.placeholder.com/200x150?text=Image+3',
];

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
    <button className="btn-icon btn-back" aria-label="Go back">
      <span className="icon">←</span>
    </button>
    <div className="facility-header__content">
      <h1 className="facility-title">{facilityData?.facilityName || 'Tên cơ sở'}</h1>
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
  
  // Process facility images - only use valid images from API
  const displayImages = React.useMemo(() => {
    if (images && images.length > 0) {
      // Filter out invalid images and sort by order
      const validImages = images
        .filter(img => img.imageUrl && img.imageUrl.trim() !== '')
        .sort((a, b) => (a.order || 0) - (b.order || 0))
        .map(img => img.imageUrl);
      
      // Return valid images if any exist, otherwise fallback to default
      return validImages.length > 0 ? validImages : FACILITY_IMAGES;
    }
    // No images from API, use default images
    return FACILITY_IMAGES;
  }, [images]);
  
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

  return (
    <div className="carousel">
      {displayImages.length > 1 && (
        <button 
          className="carousel__btn carousel__btn--prev" 
          onClick={() => navigateImage(-1)}
          aria-label="Previous image"
        >
          ←
        </button>
      )}
      <div className="carousel__container">
        <img 
          src={displayImages[currentIndex]} 
          alt={`Facility view ${currentIndex + 1}`} 
          className="carousel__image"
          onError={(e) => {
            console.error('Image failed to load:', displayImages[currentIndex]);
            // If current image fails and we have fallback images, switch to first fallback
            if (displayImages !== FACILITY_IMAGES) {
              // This will trigger a re-render with fallback images
              setCurrentIndex(0);
            }
          }}
        />
      </div>
      {displayImages.length > 1 && (
        <button 
          className="carousel__btn carousel__btn--next" 
          onClick={() => navigateImage(1)}
          aria-label="Next image"
        >
          →
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
            >
              •
            </button>
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
    return timeString.substring(0, 5); // Format HH:mm from HH:mm:ss
  };

  return (
    <section className="facility-info">
      <h2 className="facility-info__title">Giới thiệu chung</h2>
      <div className="facility-info__details">
        <div className="info-item">
          <span className="info-label">Địa điểm:</span>
          <span className="info-value">{facilityData?.location || 'Chưa có thông tin'}</span>
        </div>
        <div className="info-item">
          <span className="info-label">Giờ mở cửa:</span>
          <span className="info-value">
            {facilityData?.openTime && facilityData?.closeTime 
              ? `${formatTime(facilityData.openTime)} - ${formatTime(facilityData.closeTime)}`
              : 'Chưa có thông tin'
            }
          </span>
        </div>
        <div className="info-item">
          <span className="info-label">Liên hệ:</span>
          <span className="info-value">{facilityData?.contact || 'Chưa có thông tin'}</span>
        </div>
        <div className="info-item">
          <span className="info-label">Số loại sân:</span>
          <span className="info-value">{facilityData?.categories?.length || 0}</span>
        </div>
      </div>
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
    <h2 className="booking-section__title">Lịch đặt sân</h2>
    <div className="booking-toolbar">
      <div className="booking-controls">
        <select 
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
        <input
          type="date"
          className="form-date"
          value={selectedDate}
          onChange={(e) => onDateChange(e.target.value)}
          aria-label="Select date"
          min={TODAY}
        />
      </div>
    </div>
    
    {loadingSlots && (
      <div style={{ textAlign: 'center', padding: '20px' }}>
        Đang tải lịch trống...
      </div>
    )}
    
    {!loadingSlots && timeSlots.length > 0 && (
      <div className="table-responsive">
        <table className="booking-table">
          <thead>
            <tr>
              <th>Khung giờ</th>
              {timeSlots.map((slot) => (
                <th key={slot.timeSlotId}>
                  {formatTimeSlot(slot.startTime, slot.endTime)}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Số sân trống</td>
              {timeSlots.map((slot) => (
                <td key={slot.timeSlotId} style={{
                  color: slot.availableCourtCount > 0 ? 'green' : 'red',
                  fontWeight: 'bold'
                }}>
                  {slot.availableCourtCount}
                </td>
              ))}
            </tr>
          </tbody>
        </table>
      </div>
    )}
    
    {!loadingSlots && timeSlots.length === 0 && selectedCategory && (
      <div style={{ textAlign: 'center', padding: '20px', color: '#666' }}>
        Không có khung giờ nào khả dụng cho loại sân này
      </div>
    )}
    
    {!loadingSlots && timeSlots.length === 0 && !selectedCategory && (
      <div style={{ textAlign: 'center', padding: '20px', color: '#666' }}>
        Vui lòng chọn loại sân để xem lịch trống
      </div>
    )}
    
    <button 
      className="btn-primary btn-booking" 
      onClick={onOpenModal}
      disabled={!selectedCategory || timeSlots.length === 0}
    >
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
const FacilityDetails = ({ facilityId = 7 }) => { // Add facilityId prop with default value
  const [modalOpen, setModalOpen] = useState(false);
  const [facilityData, setFacilityData] = useState(null);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedDate, setSelectedDate] = useState(TODAY);
  const [timeSlots, setTimeSlots] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [error, setError] = useState(null);

  // Fetch facility details on component mount
  useEffect(() => {
    const fetchFacilityDetails = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const response = await getFacilityDetailsById(facilityId);
        
        // Assuming the API returns data in response.data format
        // Adjust this based on your actual API response structure
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
        
        // Assuming the API returns data in response.data.data format (based on ApiResponse structure)
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
        // You might want to show an error message here
      } finally {
        setLoadingSlots(false);
      }
    };

    fetchAvailableSlots();
  }, [facilityId, selectedCategory, selectedDate]);

  const handleCategoryChange = (categoryId) => {
    setSelectedCategory(categoryId);
    setTimeSlots([]); // Clear previous slots
  };

  const handleDateChange = (date) => {
    setSelectedDate(date);
    setTimeSlots([]); // Clear previous slots
  };

  if (loading) {
    return (
      <div className="facility-page" style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '50vh' 
      }}>
        <div>Đang tải thông tin cơ sở...</div>
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
      
      {error && (
        <div className="error-message" style={{ 
          color: 'red', 
          padding: '10px', 
          margin: '10px', 
          backgroundColor: '#fee', 
          borderRadius: '4px' 
        }}>
          {error}
        </div>
      )}
      
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
        />
      )}
    </div>
  );
};

export default FacilityDetails;