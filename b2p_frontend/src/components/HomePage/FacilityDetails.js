import React, { useState, useEffect } from "react";
import { useAuth } from "../../contexts/AuthContext.js";
import { useCustomerSignalR } from "../../contexts/CustomerSignalRContext.js";
import "./FacilityDetails.scss";
import { useParams } from "react-router-dom";
import BookingModal from "./BookingModal.js";
import BookingDetail from "./BookingDetail.js";
import {
  getFacilityDetailsById,
  getAvailableSlots,
  createBookingForPlayer,
  createPaymentOrder,
  createStripePaymentOrder,
} from "../../services/apiService";
import { parseInt } from "lodash";

// Constants
const TODAY_DATE = new Date().toISOString().slice(0, 10);
const FACILITY_IMAGES = [
  "https://nads.1cdn.vn/2024/11/22/74da3f39-759b-4f08-8850-4c8f2937e81a-1_mangeshdes.png",
  "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800&h=600&fit=crop",
  "https://images.unsplash.com/photo-1544966503-7cc5ac882d5e?w=800&h=600&fit=crop",
  "https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=800&h=600&fit=crop",
];

// ✅ Safe hook wrapper to handle missing provider
const useSafeCustomerSignalR = () => {
  try {
    return useCustomerSignalR();
  } catch (error) {
    console.warn(
      "CustomerSignalRProvider not found, using fallback:",
      error.message
    );
    return {
      isConnected: false,
      connectionState: "Disconnected",
      joinFacilityForUpdates: () => console.log("SignalR not available"),
      leaveFacilityUpdates: () => console.log("SignalR not available"),
      joinedFacilities: [],
    };
  }
};

// Helper function to convert Google Drive share link to viewable image link
const convertGoogleDriveUrl = (originalUrl) => {
  if (!originalUrl) return "/src/assets/images/default.jpg";

  console.log('Converting URL:', originalUrl);

  // Bỏ qua nếu đã là định dạng mong muốn
  if (originalUrl.includes('lh3.googleusercontent.com')) {
    console.log('Already in googleusercontent format');
    return originalUrl;
  }
  if (originalUrl.includes('thumbnail')) {
    console.log('Already in thumbnail format');
    return originalUrl;
  }
  if (originalUrl.includes('/assets/')) {
    console.log('Local asset path');
    return originalUrl;
  }
  if (!originalUrl.includes('drive.google.com')) {
    console.log('Not a Google Drive URL');
    return originalUrl;
  }

  // Extract file ID từ nhiều định dạng Google Drive URL
  let fileId = null;

  // Format: https://drive.google.com/file/d/FILE_ID/view?usp=sharing
  // hoặc https://drive.google.com/file/d/FILE_ID/edit
  fileId = originalUrl.match(/\/d\/([a-zA-Z0-9_-]+)/)?.[1];

  // Format: https://drive.google.com/open?id=FILE_ID
  // hoặc https://drive.google.com/uc?id=FILE_ID
  if (!fileId) {
    fileId = originalUrl.match(/[?&]id=([a-zA-Z0-9_-]+)/)?.[1];
  }

  if (fileId) {
    console.log('Extracted fileId:', fileId);

    // Thử nhiều format khác nhau, ưu tiên googleusercontent (bypass CORS tốt hơn)
    const formats = [
      // Format 1: Google Photos format (often bypasses CORS)
      `https://lh3.googleusercontent.com/d/${fileId}=s800-c`,

      // Format 2: Google Photos with no-cache
      `https://lh3.googleusercontent.com/d/${fileId}=w800-h600-p-k-no-nu`,

      // Format 3: Original format (your current one) 
      `https://lh3.googleusercontent.com/d/${fileId}=s0`,

      // Format 4: Simple googleusercontent
      `https://lh3.googleusercontent.com/d/${fileId}`,

      // Format 5: Drive direct (often blocked by CORS)
      `https://drive.google.com/uc?export=view&id=${fileId}`
    ];

    // Trả về format đầu tiên (thường work nhất)
    const selectedFormat = formats[0];
    console.log('Selected format:', selectedFormat);

    return selectedFormat;
  }

  console.warn('Could not extract fileId from:', originalUrl);
  return "/src/assets/images/default.jpg";
};

// Helper function để check xem URL có cần convert không
const needsGoogleDriveConversion = (url) => {
  return url &&
    url.includes('drive.google.com') &&
    !url.includes('lh3.googleusercontent.com') &&
    !url.includes('thumbnail');
};

// Helper function để extract fileId từ bất kỳ Google Drive URL nào
const extractGoogleDriveFileId = (url) => {
  if (!url) return null;

  // Try multiple patterns
  const patterns = [
    /\/d\/([a-zA-Z0-9_-]+)/, // /d/FILE_ID
    /[?&]id=([a-zA-Z0-9_-]+)/, // ?id=FILE_ID or &id=FILE_ID
    /googleusercontent\.com\/d\/([a-zA-Z0-9_-]+)/, // Already converted format
  ];

  for (const pattern of patterns) {
    const match = url.match(pattern);
    if (match) return match[1];
  }

  return null;
};

// Version có thể tùy chỉnh kích thước (optional)
const convertGoogleDriveUrlWithSize = (originalUrl, options = {}) => {
  // Default options
  const {
    width = 300,
    height = 200,
    crop = true, // 'c' parameter for cropping
    defaultImage = "/src/assets/images/default.jpg"
  } = options;

  if (!originalUrl) return defaultImage;

  // Bỏ qua nếu đã là định dạng mong muốn
  if (originalUrl.includes('lh3.googleusercontent.com')) return originalUrl;
  if (originalUrl.includes('thumbnail')) return originalUrl;
  if (originalUrl.includes('/assets/')) return originalUrl;

  // Extract file ID
  let fileId = originalUrl.match(/\/d\/([a-zA-Z0-9_-]+)/)?.[1] ||
    originalUrl.match(/[?&]id=([a-zA-Z0-9_-]+)/)?.[1];

  if (fileId) {
    const cropParam = crop ? '-c' : '';
    return `https://lh3.googleusercontent.com/d/${fileId}=w${width}-h${height}${cropParam}`;
  }

  return defaultImage;
};

// Helper function to validate image URLs
const isValidImageUrl = (url) => {
  if (!url) return false;
  return true;
};

// Helper function to format time
const formatTimeSlot = (startTime, endTime) => {
  const formatTime = (timeString) => {
    if (!timeString) return "";
    return timeString.substring(0, 5); // Format HH:mm from HH:mm:ss
  };
  return `${formatTime(startTime)} - ${formatTime(endTime)}`;
};

// Helper function to convert date formats
const convertDateFormat = (dateString) => {
  if (!dateString) return null;

  // Check if it's DD/MM/YYYY format (from notification)
  const ddmmyyyyPattern = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/;
  const match = dateString.match(ddmmyyyyPattern);

  if (match) {
    const [, day, month, year] = match;
    return `${year}-${month.padStart(2, "0")}-${day.padStart(2, "0")}`;
  }

  // Already in YYYY-MM-DD format or other format
  return dateString;
};

// Reviews Modal Component
const ReviewsModal = ({ open, onClose, ratings = [], facilityName = "" }) => {
  const [selectedStars, setSelectedStars] = useState("all");

  // Loại bỏ các rating trùng lặp
  const uniqueRatings = React.useMemo(() => {
    if (!ratings || ratings.length === 0) return [];

    const seen = new Set();
    return ratings.filter((rating) => {
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
    if (selectedStars === "all") {
      return uniqueRatings;
    }
    return uniqueRatings.filter(
      (rating) => rating.stars === parseInt(selectedStars)
    );
  }, [uniqueRatings, selectedStars]);

  // Tính thống kê
  const ratingStats = React.useMemo(() => {
    if (uniqueRatings.length === 0) {
      return {
        averageRating: 0,
        totalReviews: 0,
        breakdown: { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 },
      };
    }

    const breakdown = { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 };
    let totalStars = 0;

    uniqueRatings.forEach((rating) => {
      const stars = rating.stars;
      if (stars >= 1 && stars <= 5) {
        breakdown[stars]++;
        totalStars += stars;
      }
    });

    return {
      averageRating: Math.round((totalStars / uniqueRatings.length) * 10) / 10,
      totalReviews: uniqueRatings.length,
      breakdown,
    };
  }, [uniqueRatings]);

  // Render stars
  const renderStars = (starCount) => {
    return [...Array(5)].map((_, index) => (
      <span
        key={index}
        className={`star ${index < starCount ? "filled" : ""}`}
        style={{
          color: index < starCount ? "#fbbf24" : "#e5e7eb",
        }}
      >
        ★
      </span>
    ));
  };

  if (!open) return null;

  return (
    <div
      className="modal-overlay"
      onClick={onClose}
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: "rgba(0, 0, 0, 0.6)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 1000,
        padding: "20px",
      }}
    >
      <div
        className="modal-container reviews-modal"
        onClick={(e) => e.stopPropagation()}
        style={{
          backgroundColor: "white",
          borderRadius: "16px",
          width: "90%",
          maxWidth: "900px",
          height: "80vh",
          display: "flex",
          flexDirection: "column",
          boxShadow: "0 20px 60px rgba(0, 0, 0, 0.3)",
          overflow: "hidden",
        }}
      >
        {/* Header */}
        <div
          className="modal-header"
          style={{
            padding: "24px",
            borderBottom: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            backgroundColor: "#f8fafc",
          }}
        >
          <h2
            className="modal-title"
            style={{
              margin: 0,
              fontSize: "20px",
              fontWeight: "600",
              color: "#1f2937",
              textAlign: "center",
              flex: 1,
            }}
          >
            <span className="title-icon">⭐</span>
            Tất cả đánh giá - {facilityName}
          </h2>
          <button
            className="modal-close"
            onClick={onClose}
            aria-label="Close modal"
            style={{
              background: "none",
              border: "none",
              fontSize: "24px",
              color: "#6b7280",
              cursor: "pointer",
              padding: "8px",
              borderRadius: "8px",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              width: "40px",
              height: "40px",
              transition: "all 0.2s ease",
            }}
            onMouseOver={(e) => {
              e.target.style.backgroundColor = "#f3f4f6";
              e.target.style.color = "#374151";
            }}
            onMouseOut={(e) => {
              e.target.style.backgroundColor = "transparent";
              e.target.style.color = "#6b7280";
            }}
          >
            ×
          </button>
        </div>

        {/* Content */}
        <div
          className="modal-content"
          style={{
            flex: 1,
            overflow: "auto",
            padding: "24px",
          }}
        >
          {/* Rating Summary */}
          <div
            className="reviews-modal-summary"
            style={{
              marginBottom: "32px",
              textAlign: "center",
            }}
          >
            <div className="summary-main">
              <div
                className="rating-display"
                style={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  gap: "12px",
                  marginBottom: "16px",
                }}
              >
                <span
                  className="rating-value"
                  style={{
                    fontSize: "32px",
                    fontWeight: "700",
                    color: "#1f2937",
                  }}
                >
                  {ratingStats.averageRating}
                </span>
                <div
                  className="rating-stars"
                  style={{
                    color: "#fbbf24",
                    fontSize: "20px",
                  }}
                >
                  {renderStars(Math.round(ratingStats.averageRating))}
                </div>
                <span
                  className="rating-text"
                  style={{
                    fontSize: "14px",
                    color: "#6b7280",
                  }}
                >
                  ({ratingStats.totalReviews} đánh giá)
                </span>
              </div>
            </div>

            {/* Filter by stars */}
            <div
              style={{
                display: "flex",
                justifyContent: "center",
                marginTop: "16px",
              }}
            >
              <div className="star-filter" style={{ textAlign: "center" }}>
                <label
                  className="filter-label"
                  style={{
                    fontSize: "14px",
                    fontWeight: "500",
                    color: "#374151",
                    marginBottom: "8px",
                    display: "block",
                  }}
                >
                  Lọc theo số sao:
                </label>
                <div
                  className="filter-buttons"
                  style={{
                    display: "flex",
                    gap: "8px",
                    flexWrap: "wrap",
                    justifyContent: "center",
                  }}
                >
                  <button
                    className={`filter-btn ${
                      selectedStars === "all" ? "active" : ""
                    }`}
                    onClick={() => setSelectedStars("all")}
                    style={{
                      padding: "8px 16px",
                      border: "1px solid #d1d5db",
                      borderRadius: "8px",
                      backgroundColor:
                        selectedStars === "all" ? "#3b82f6" : "white",
                      color: selectedStars === "all" ? "white" : "#374151",
                      cursor: "pointer",
                      fontSize: "14px",
                      fontWeight: "500",
                      transition: "all 0.2s ease",
                    }}
                    onMouseOver={(e) => {
                      if (selectedStars !== "all") {
                        e.target.style.backgroundColor = "#f3f4f6";
                        e.target.style.borderColor = "#9ca3af";
                      }
                    }}
                    onMouseOut={(e) => {
                      if (selectedStars !== "all") {
                        e.target.style.backgroundColor = "white";
                        e.target.style.borderColor = "#d1d5db";
                      }
                    }}
                  >
                    Tất cả ({ratingStats.totalReviews})
                  </button>
                  {[5, 4, 3, 2, 1].map((stars) => (
                    <button
                      key={stars}
                      className={`filter-btn ${
                        selectedStars === stars.toString() ? "active" : ""
                      }`}
                      onClick={() => setSelectedStars(stars.toString())}
                      style={{
                        padding: "8px 16px",
                        border: "1px solid #d1d5db",
                        borderRadius: "8px",
                        backgroundColor:
                          selectedStars === stars.toString()
                            ? "#3b82f6"
                            : "white",
                        color:
                          selectedStars === stars.toString()
                            ? "white"
                            : "#374151",
                        cursor: "pointer",
                        fontSize: "14px",
                        fontWeight: "500",
                        transition: "all 0.2s ease",
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                      onMouseOver={(e) => {
                        if (selectedStars !== stars.toString()) {
                          e.target.style.backgroundColor = "#f3f4f6";
                          e.target.style.borderColor = "#9ca3af";
                        }
                      }}
                      onMouseOut={(e) => {
                        if (selectedStars !== stars.toString()) {
                          e.target.style.backgroundColor = "white";
                          e.target.style.borderColor = "#d1d5db";
                        }
                      }}
                    >
                      <span style={{ color: "#fbbf24" }}>{stars}★</span> (
                      {ratingStats.breakdown[stars]})
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Reviews List */}
          <div className="reviews-modal-list">
            {filteredRatings.length === 0 ? (
              <div
                className="empty-reviews"
                style={{
                  textAlign: "center",
                  padding: "40px",
                  color: "#6b7280",
                }}
              >
                <div
                  className="empty-icon"
                  style={{ fontSize: "48px", marginBottom: "16px" }}
                >
                  ⭐
                </div>
                <p>
                  {selectedStars === "all"
                    ? "Chưa có đánh giá nào"
                    : `Chưa có đánh giá ${selectedStars} sao nào`}
                </p>
              </div>
            ) : (
              filteredRatings.map((rating, index) => (
                <div
                  key={`${rating.ratingId}-${rating.bookingId}-${index}`}
                  className="review-modal-card"
                  style={{
                    border: "1px solid #e5e7eb",
                    borderRadius: "12px",
                    padding: "16px",
                    marginBottom: "16px",
                    backgroundColor: "#ffffff",
                    boxShadow: "0 2px 4px rgba(0, 0, 0, 0.05)",
                    transition: "all 0.2s ease",
                  }}
                  onMouseOver={(e) => {
                    e.currentTarget.style.boxShadow =
                      "0 4px 12px rgba(0, 0, 0, 0.1)";
                    e.currentTarget.style.borderColor = "#d1d5db";
                  }}
                  onMouseOut={(e) => {
                    e.currentTarget.style.boxShadow =
                      "0 2px 4px rgba(0, 0, 0, 0.05)";
                    e.currentTarget.style.borderColor = "#e5e7eb";
                  }}
                >
                  <div
                    className="review-modal-card__avatar"
                    style={{
                      display: "flex",
                      alignItems: "flex-start",
                      gap: "12px",
                    }}
                  >
                    <div
                      style={{
                        width: "40px",
                        height: "40px",
                        borderRadius: "50%",
                        backgroundColor: "#3b82f6",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        color: "white",
                        fontSize: "14px",
                        fontWeight: "600",
                        flexShrink: 0,
                      }}
                    >
                      U{rating.bookingId}
                    </div>
                    <div
                      className="review-modal-card__content"
                      style={{ flex: 1 }}
                    >
                      <div
                        className="review-modal-card__header"
                        style={{
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "space-between",
                          marginBottom: "8px",
                        }}
                      >
                        <div className="reviewer-info">
                          <span
                            className="reviewer-name"
                            style={{
                              fontWeight: "600",
                              color: "#1f2937",
                            }}
                          >
                            Người dùng #{rating.bookingId}
                          </span>
                          <span
                            className="review-time"
                            style={{
                              color: "#6b7280",
                              fontSize: "14px",
                            }}
                          >
                            {" "}
                            • Booking #{rating.bookingId}
                          </span>
                        </div>
                        <div
                          className="review-stars"
                          style={{
                            color: "#fbbf24",
                            fontSize: "16px",
                          }}
                        >
                          {renderStars(rating.stars)}
                        </div>
                      </div>
                      <p
                        className="review-text"
                        style={{
                          color: "#4b5563",
                          lineHeight: "1.6",
                          margin: 0,
                          fontSize: "15px",
                        }}
                      >
                        {rating.comment || "Không có bình luận"}
                      </p>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Footer */}
        <div
          className="modal-footer"
          style={{
            padding: "20px 24px",
            borderTop: "1px solid #e5e7eb",
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            backgroundColor: "#f8fafc",
          }}
        >
          <button
            className="btn-secondary"
            onClick={onClose}
            style={{
              padding: "10px 20px",
              border: "1px solid #d1d5db",
              borderRadius: "8px",
              backgroundColor: "white",
              color: "#374151",
              cursor: "pointer",
              fontSize: "14px",
              fontWeight: "500",
              transition: "all 0.2s ease",
            }}
            onMouseOver={(e) => {
              e.target.style.backgroundColor = "#f3f4f6";
              e.target.style.borderColor = "#9ca3af";
            }}
            onMouseOut={(e) => {
              e.target.style.backgroundColor = "white";
              e.target.style.borderColor = "#d1d5db";
            }}
          >
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
};

// Fixed Image Carousel Component
// Fixed Image Carousel Component
const ImageCarousel = ({ images }) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [failedImages, setFailedImages] = useState(new Set());
  const [isLoading, setIsLoading] = useState(true);
  const [retryAttempts, setRetryAttempts] = useState(new Map());

  // Process facility images - convert Google Drive links and handle all URLs
  const displayImages = React.useMemo(() => {
    console.log("Raw images from API:", images);

    let processedImages = [];

    if (images && images.length > 0) {
      // Process all images, convert Google Drive links

      processedImages = images
        .filter(img => {
          const hasUrl = img.imageUrl && img.imageUrl.trim() !== '';
          console.log(`Image ${img.imageId}: ${img.imageUrl} - Has URL: ${hasUrl}`);
          return hasUrl;
        })
        .sort((a, b) => (a.order || 0) - (b.order || 0))
        .map((img) => {
          const originalUrl = img.imageUrl;
          const convertedUrl = convertGoogleDriveUrl(originalUrl);
          console.log(`Converting: ${originalUrl} → ${convertedUrl}`);
          return convertedUrl;
        })
        .filter((url) => url && !failedImages.has(url));

      console.log('Processed API images after filtering failed:', processedImages);
    }

    // If no valid API images, add fallback images
    if (processedImages.length === 0) {
      console.log('No valid API images, adding fallback images');
      const validFallbackImages = FACILITY_IMAGES.filter(url => !failedImages.has(url));
      processedImages = validFallbackImages;
    }

    // If still no images (all failed), return default image
    if (processedImages.length === 0) {
      console.log('All images failed, using default image');
      processedImages = ["/src/assets/images/default.jpg"];
    }

    console.log('Final displayImages:', processedImages);
    return processedImages;
  }, [images, failedImages]);

  // Reset current index if it's out of bounds
  React.useEffect(() => {
    if (currentIndex >= displayImages.length && displayImages.length > 0) {
      setCurrentIndex(0);
    }
  }, [displayImages.length, currentIndex]);

  const navigateImage = (direction) => {
    if (displayImages.length <= 1) return;

    setCurrentIndex((prevIndex) => {
      const newIndex = prevIndex + direction;
      return (newIndex + displayImages.length) % displayImages.length;
    });
  };

  const handleImageError = (failedUrl) => {
    console.error("Image failed to load:", failedUrl);

    // Don't handle error for default image to prevent infinite loop
    if (failedUrl === "/src/assets/images/default.jpg") {
      console.log('Default image failed - stopping error handling to prevent loop');
      return;
    }

    // Try alternative Google Drive formats before giving up
    if (failedUrl.includes('drive.google.com') || failedUrl.includes('googleusercontent.com')) {
      const currentRetries = retryAttempts.get(failedUrl) || 0;

      if (currentRetries < 4) { // Try 4 different formats
        // Extract fileId from current URL
        let fileId = null;

        if (failedUrl.includes('googleusercontent.com')) {
          fileId = failedUrl.match(/\/d\/([a-zA-Z0-9_-]+)/)?.[1];
        } else {
          fileId = failedUrl.match(/[?&]id=([a-zA-Z0-9_-]+)/)?.[1];
        }

        if (fileId) {
          // Same format order as in convertGoogleDriveUrl (focus on googleusercontent)
          const alternativeFormats = [
            `https://lh3.googleusercontent.com/d/${fileId}=s600-c`,
            `https://lh3.googleusercontent.com/d/${fileId}=w800-h600-p-k-no-nu`,
            `https://lh3.googleusercontent.com/d/${fileId}=s0`,
            `https://lh3.googleusercontent.com/d/${fileId}`,
            `https://drive.google.com/uc?export=view&id=${fileId}`
          ];

          const nextFormat = alternativeFormats[currentRetries + 1]; // Try next format
          if (nextFormat && nextFormat !== failedUrl) {
            console.log(`🔄 Trying Google Drive format ${currentRetries + 2}/5:`, nextFormat);

            setRetryAttempts(prev => {
              const newMap = new Map(prev);
              newMap.set(failedUrl, currentRetries + 1);
              return newMap;
            });

            // Update image src directly
            setTimeout(() => {
              const img = document.querySelector(`img[src="${failedUrl}"]`);
              if (img) {
                console.log(`🔄 Switching from: ${failedUrl}`);
                console.log(`🔄 Switching to: ${nextFormat}`);
                setIsLoading(true); // Show loading for new attempt
                img.src = nextFormat;
              }
            }, 300); // Increase delay to 300ms

            return; // Don't mark as failed yet
          }
        }
      }

      console.warn(`❌ All Google Drive formats failed for: ${failedUrl}`);
      console.warn('💡 Make sure the file is shared as "Anyone with the link can view"');
    }

    setFailedImages(prev => {
      const newFailedImages = new Set([...prev, failedUrl]);
      console.log('Failed images updated:', Array.from(newFailedImages));
      return newFailedImages;
    });
  };

  const handleImageLoad = (loadedUrl) => {
    console.log('✅ Image loaded successfully:', loadedUrl);
    setIsLoading(false);

    // Reset retry attempts for this URL since it worked
    setRetryAttempts(prev => {
      const newMap = new Map(prev);
      // Remove all retry records for this fileId
      const fileId = loadedUrl.match(/[?&]id=([a-zA-Z0-9_-]+)/)?.[1] ||
        loadedUrl.match(/\/d\/([a-zA-Z0-9_-]+)/)?.[1];
      if (fileId) {
        // Clear retry attempts for all formats of this fileId
        for (const [key] of newMap) {
          if (key.includes(fileId)) {
            newMap.delete(key);
          }
        }
      }
      return newMap;
    });
  };

  // Safety check - should not happen with the fixed logic above
  if (displayImages.length === 0) {
    console.log('Emergency fallback - no displayImages available');
    return (
      <div className="carousel">
        <div className="carousel__container">
          <div className="carousel__image-wrapper">
            <div className="carousel__placeholder">
              <p>No images available</p>
            </div>
            <div className="carousel__overlay"></div>
          </div>
        </div>
      </div>
    );
  }

  const currentImage = displayImages[currentIndex];
  const hasMultipleImages = displayImages.length > 1;

  return (
    <div className="carousel">
      {hasMultipleImages && (
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
          {isLoading && (
            <div className="carousel__loading">
              <p>Loading image...</p>
            </div>
          )}

          <img
            src={currentImage}
            alt={`Facility view ${currentIndex + 1}`}
            className="carousel__image"

            onError={() => handleImageError(currentImage)}
            onLoad={() => handleImageLoad(currentImage)}
            onLoadStart={() => setIsLoading(true)} // Set loading when starting
            style={{
              display: isLoading ? 'none' : 'block',
              opacity: isLoading ? 0 : 1,
              transition: 'opacity 0.3s ease'
            }}
          />


          <div className="carousel__overlay"></div>
        </div>
      </div>

      {hasMultipleImages && (
        <button
          className="carousel__btn carousel__btn--next"
          onClick={() => navigateImage(1)}
          aria-label="Next image"
        >
          ›
        </button>
      )}

      {hasMultipleImages && (
        <div className="carousel__dots">
          {displayImages.map((_, idx) => (
            <button
              key={`dot-${idx}`}
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
    if (!timeString) return "";
    return timeString.substring(0, 5);
  };

  const infoItems = [
    {
      icon: "📍",
      label: "Địa điểm",
      value: facilityData?.location || "Chưa có thông tin",
    },
    {
      icon: "🕐",
      label: "Giờ hoạt động",
      value:
        facilityData?.openTime && facilityData?.closeTime
          ? `${formatTime(facilityData.openTime)} - ${formatTime(
              facilityData.closeTime
            )}`
          : "Chưa có thông tin",
    },
    {
      icon: "📞",
      label: "Số điện thoại",
      value: facilityData?.contact || "Chưa có thông tin",
    },
    {
      icon: "🏟️",
      label: "Loại sân",
      value: `${facilityData?.categories?.length || 0} loại sân`,
    },
    {
      icon: "⚡",
      label: "Trạng thái",
      value: facilityData?.status?.statusDescription || "Đang hoạt động",
    },
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

// Main Component
const FacilityDetails = () => {
  useEffect(() => {
    document.title = "Chi tiết cơ sở - B2P";
  }, []);
  const { userId } = useAuth();

  // ✅ Use safe SignalR hook
  const {
    isConnected,
    connectionState,
    joinFacilityForUpdates,
    leaveFacilityUpdates,
    joinedFacilities,
  } = useSafeCustomerSignalR();

  const [modalOpen, setModalOpen] = useState(false);
  const [reviewsModalOpen, setReviewsModalOpen] = useState(false);
  const [bookingDetailOpen, setBookingDetailOpen] = useState(false);
  const [bookingDetailData, setBookingDetailData] = useState(null);
  const [facilityData, setFacilityData] = useState(null);
  const [selectedCategory, setSelectedCategory] = useState("");
  const [selectedDate, setSelectedDate] = useState(TODAY_DATE);
  const [timeSlots, setTimeSlots] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [error, setError] = useState(null);

  // ✅ Real-time update states
  const [lastUpdateTime, setLastUpdateTime] = useState(null);
  const [isRealTimeActive, setIsRealTimeActive] = useState(false);

  const { facilityId } = useParams();

  // ✅ Enhanced refresh function with detailed logging
  const refreshAvailableSlots = async () => {
    if (!selectedCategory || !selectedDate || !facilityId) {
      console.log("⚠️ [FacilityDetails] Missing required data for refresh:", {
        selectedCategory,
        selectedDate,
        facilityId,
      });
      setTimeSlots([]);
      return;
    }

    console.log("🔄 [FacilityDetails] Starting slots refresh...", {
      facilityId,
      categoryId: selectedCategory,
      date: selectedDate,
      timestamp: new Date().toISOString(),
      currentTime: "2025-08-23 06:05:37 UTC",
    });

    setLoadingSlots(true);
    try {
      const response = await getAvailableSlots(
        facilityId,
        selectedCategory,
        selectedDate
      );

      console.log("📊 [FacilityDetails] Slots API response:", response);

      let newSlots = [];
      if (response.data && response.data.data) {
        newSlots = response.data.data;
      } else if (response.data) {
        newSlots = response.data;
      }

      console.log("📊 [FacilityDetails] Processed slots:", newSlots);
      console.log("📊 [FacilityDetails] Slots count:", newSlots.length);

      // ✅ Show detailed slot availability with current user context
      newSlots.forEach((slot, index) => {
        console.log(`📊 [FacilityDetails] Slot ${index + 1}:`, {
          timeSlotId: slot.timeSlotId,
          timeRange: `${slot.startTime} - ${slot.endTime}`,
          availableCount: slot.availableCourtCount,
          totalCourts: slot.totalCourtCount || "N/A",
          updated: new Date().toLocaleTimeString(),
          user: "bachnhhe173308",
        });
      });

      setTimeSlots(newSlots);
      console.log(
        "✅ [FacilityDetails] Slots updated successfully for user bachnhhe173308"
      );
    } catch (error) {
      console.error(
        "❌ [FacilityDetails] Error refreshing available slots:",
        error
      );
      setTimeSlots([]);
    } finally {
      setLoadingSlots(false);
    }
  };

  // ✅ FIXED: Enhanced SignalR event listeners with proper date handling
  useEffect(() => {
    if (!isConnected || !facilityId) {
      console.log(
        "⚠️ [FacilityDetails] SignalR not connected or no facilityId:",
        {
          isConnected,
          facilityId,
          user: "bachnhhe173308",
          timestamp: "2025-08-23 06:05:37 UTC",
        }
      );
      return;
    }

    console.log(
      "🔔 [FacilityDetails] Setting up SignalR event listeners for facility:",
      facilityId
    );
    console.log(
      "🔔 [FacilityDetails] Current selected category:",
      selectedCategory
    );
    console.log("🔔 [FacilityDetails] Current selected date:", selectedDate);
    console.log("🔔 [FacilityDetails] Current user: bachnhhe173308");

    // Import signalRService to listen for events
    const signalRService = require("../../services/signalRService").default;

    // ✅ Enhanced booking notification handler
    const handleBookingNotification = (notification) => {
      console.log(
        "📨 [FacilityDetails] Received booking notification:",
        notification
      );
      console.log("📨 [FacilityDetails] Notification details:", {
        action: notification.action,
        facilityId: notification.facilityId,
        currentFacilityId: parseInt(facilityId),
        courtId: notification.courtId,
        courtName: notification.courtName,
        date: notification.date,
        selectedDate: selectedDate,
        timeSlot: notification.timeSlot,
        customerName: notification.customerName,
        currentUser: "người dùng",
        timestamp: "2025-08-23 06:05:37 UTC",
      });

      // ✅ Check if notification is for current facility
      const notificationFacilityId = parseInt(notification.facilityId);
      const currentFacilityId = parseInt(facilityId);

      if (notificationFacilityId === currentFacilityId) {
        console.log(
          "✅ [FacilityDetails] Notification is for current facility, checking date..."
        );

        // ✅ Enhanced date format conversion
        const notificationDate = convertDateFormat(notification.date);

        console.log("📅 [FacilityDetails] Date comparison:", {
          originalNotificationDate: notification.date,
          convertedNotificationDate: notificationDate,
          selectedDate,
          matches: notificationDate === selectedDate,
          currentUser: "bachnhhe173308",
        });

        if (notificationDate === selectedDate) {
          console.log(
            "🎯 [FacilityDetails] Notification affects current view, refreshing slots..."
          );
          console.log("🎯 [FacilityDetails] Trigger details:", {
            bookingId: notification.bookingId,
            action: notification.action,
            status: notification.status,
            court: notification.courtName,
            timeSlot: notification.timeSlot,
            customer: notification.customerName,
            currentUser: "bachnhhe173308",
            willRefreshAt: new Date(Date.now() + 2000).toLocaleTimeString(),
          });

          // ✅ Add delay to ensure backend has processed
          setTimeout(() => {
            refreshAvailableSlots();
            setLastUpdateTime(new Date().toLocaleTimeString());
            console.log(
              "🔄 [FacilityDetails] Real-time refresh completed for user bachnhhe173308"
            );
          }, 2000); // 2 second delay
        } else {
          console.log(
            "📅 [FacilityDetails] Date mismatch, skipping refresh for user bachnhhe173308"
          );
        }
      } else {
        console.log(
          "🏢 [FacilityDetails] Different facility, skipping refresh"
        );
      }
    };

    // ✅ Register all booking event handlers
    signalRService.on("onBookingCreated", handleBookingNotification);
    signalRService.on("onBookingCancelled", handleBookingNotification);
    signalRService.on("onBookingUpdated", handleBookingNotification);
    signalRService.on("onBookingCompleted", handleBookingNotification);

    // ✅ Also listen to direct SignalR connection events
    if (signalRService.connection) {
      console.log(
        "🎧 [FacilityDetails] Setting up direct SignalR event listeners for user bachnhhe173308..."
      );

      const directBookingHandler = (data) => {
        console.log(
          "📡 [FacilityDetails] Direct SignalR booking event for user bachnhhe173308:",
          data
        );
        handleBookingNotification(data);
      };

      // Listen to direct SignalR events
      signalRService.connection.on("BookingCreated", directBookingHandler);
      signalRService.connection.on("BookingUpdated", directBookingHandler);
      signalRService.connection.on("BookingCancelled", directBookingHandler);
      signalRService.connection.on("BookingCompleted", directBookingHandler);

      // ✅ Listen for slot availability updates
      signalRService.connection.on("SlotAvailabilityChanged", (data) => {
        console.log(
          "🎯 [FacilityDetails] Slot availability changed for user bachnhhe173308:",
          data
        );

        if (data.facilityId === parseInt(facilityId)) {
          console.log(
            "✅ [FacilityDetails] Slot change for current facility, refreshing for user bachnhhe173308..."
          );
          setTimeout(() => {
            refreshAvailableSlots();
            setLastUpdateTime(new Date().toLocaleTimeString());
          }, 1000);
        }
      });

      // ✅ Listen for facility-specific updates
      signalRService.connection.on("FacilityUpdate", (data) => {
        console.log(
          "🏢 [FacilityDetails] Facility update received for user bachnhhe173308:",
          data
        );

        if (data.facilityId === parseInt(facilityId)) {
          console.log(
            "✅ [FacilityDetails] Update for current facility, refreshing for user bachnhhe173308..."
          );
          setTimeout(() => {
            refreshAvailableSlots();
            setLastUpdateTime(new Date().toLocaleTimeString());
          }, 1500);
        }
      });

      // Cleanup function
      return () => {
        signalRService.connection.off("BookingCreated", directBookingHandler);
        signalRService.connection.off("BookingUpdated", directBookingHandler);
        signalRService.connection.off("BookingCancelled", directBookingHandler);
        signalRService.connection.off("BookingCompleted", directBookingHandler);
        signalRService.connection.off("SlotAvailabilityChanged");
        signalRService.connection.off("FacilityUpdate");

        signalRService.off("onBookingCreated");
        signalRService.off("onBookingCancelled");
        signalRService.off("onBookingUpdated");
        signalRService.off("onBookingCompleted");

        console.log(
          "🧹 [FacilityDetails] All SignalR event listeners cleaned up for user bachnhhe173308"
        );
      };
    }

    // Cleanup event handlers
    return () => {
      signalRService.off("onBookingCreated");
      signalRService.off("onBookingCancelled");
      signalRService.off("onBookingUpdated");
      signalRService.off("onBookingCompleted");
      console.log(
        "🧹 [FacilityDetails] SignalR event listeners cleaned up for user bachnhhe173308"
      );
    };
  }, [isConnected, facilityId, selectedDate]); // ✅ Add selectedDate dependency

  // ✅ Join facility group when facility loads
  useEffect(() => {
    if (
      facilityId &&
      isConnected &&
      !joinedFacilities.includes(parseInt(facilityId))
    ) {
      console.log(
        `🔗 [FacilityDetails] Joining facility group: ${facilityId} for user bachnhhe173308`
      );
      joinFacilityForUpdates(parseInt(facilityId));
      setIsRealTimeActive(true);
    }

    // Leave facility group when component unmounts or facility changes
    return () => {
      if (facilityId && joinedFacilities.includes(parseInt(facilityId))) {
        console.log(
          `🔗 [FacilityDetails] Leaving facility group: ${facilityId} for user bachnhhe173308`
        );
        leaveFacilityUpdates(parseInt(facilityId));
        setIsRealTimeActive(false);
      }
    };
  }, [facilityId, isConnected, joinFacilityForUpdates, leaveFacilityUpdates]);

  // ✅ Debug effect to monitor slot changes
  useEffect(() => {
    console.log(
      "📊 [FacilityDetails] TimeSlots changed for user bachnhhe173308:",
      {
        count: timeSlots.length,
        slots: timeSlots.map((slot) => ({
          id: slot.timeSlotId,
          time: `${slot.startTime}-${slot.endTime}`,
          available: slot.availableCourtCount,
        })),
        timestamp: new Date().toISOString(),
        currentTime: "2025-08-23 06:05:37 UTC",
      }
    );
  }, [timeSlots]);

  // ✅ Debug selected values
  useEffect(() => {
    console.log(
      "🎯 [FacilityDetails] Selection changed for user bachnhhe173308:",
      {
        facilityId,
        selectedCategory,
        selectedDate,
        isConnected,
        joinedFacilities,
        isRealTimeActive,
        timestamp: "2025-08-23 06:05:37 UTC",
      }
    );
  }, [
    facilityId,
    selectedCategory,
    selectedDate,
    isConnected,
    joinedFacilities,
    isRealTimeActive,
  ]);

  // Fetch facility details on component mount
  useEffect(() => {
    const fetchFacilityDetails = async () => {
      setLoading(true);
      setError(null);

      try {
        console.log(
          "🏢 [FacilityDetails] Fetching facility details for user bachnhhe173308:",
          facilityId
        );
        const response = await getFacilityDetailsById(parseInt(facilityId));

        if (response.data) {
          const facilityInfo = response.data;
          console.log(
            "✅ [FacilityDetails] Facility data loaded for user bachnhhe173308:",
            facilityInfo.facilityName
          );

          setFacilityData(facilityInfo);

          // Set default category to the first available category
          if (facilityInfo.categories && facilityInfo.categories.length > 0) {
            setSelectedCategory(
              facilityInfo.categories[0].categoryId.toString()
            );
            console.log(
              "🏟️ [FacilityDetails] Default category set for user bachnhhe173308:",
              facilityInfo.categories[0].categoryName
            );
          }
        } else {
          setError("Không tìm thấy thông tin cơ sở");
        }
      } catch (error) {
        console.error(
          "❌ [FacilityDetails] Error fetching facility details for user bachnhhe173308:",
          error
        );
        setError("Không thể tải thông tin cơ sở");
      } finally {
        setLoading(false);
      }
    };

    if (facilityId) {
      fetchFacilityDetails();
    }
  }, [facilityId]);

  // ✅ Use refreshAvailableSlots in existing useEffect
  useEffect(() => {
    refreshAvailableSlots();
  }, [facilityId, selectedCategory, selectedDate]);

  const handleCategoryChange = (categoryId) => {
    console.log(
      "🏟️ [FacilityDetails] Category changed for user bachnhhe173308:",
      categoryId
    );
    setSelectedCategory(categoryId);
    setTimeSlots([]);
  };

  const handleDateChange = (date) => {
    console.log(
      "📅 [FacilityDetails] Date changed for user bachnhhe173308:",
      date
    );
    setSelectedDate(date);
    setTimeSlots([]);
  };

  // ✅ Enhanced manual refresh with user feedback
  const handleManualRefresh = () => {
    console.log(
      "🔄 [FacilityDetails] Manual refresh triggered by user bachnhhe173308"
    );

    // Show immediate feedback
    setLoadingSlots(true);

    setTimeout(() => {
      refreshAvailableSlots();
      setLastUpdateTime(new Date().toLocaleTimeString());
      console.log(
        "✅ [FacilityDetails] Manual refresh completed for user bachnhhe173308"
      );
    }, 100);
  };

  // Handle proceed to booking detail - callback từ BookingModal
  const handleProceedToBookingDetail = (data) => {
    console.log(
      "📝 [FacilityDetails] Proceeding to booking detail for user bachnhhe173308:",
      data
    );
    setBookingDetailData(data);
    setBookingDetailOpen(true);
  };

  // Handle close booking detail modal
  const handleCloseBookingDetail = () => {
    console.log(
      "❌ [FacilityDetails] Closing booking detail for user bachnhhe173308"
    );
    setBookingDetailOpen(false);
    setBookingDetailData(null);
  };

  // ✅ Handle successful booking with slot refresh
  const handleBookingSuccess = () => {
    console.log(
      "✅ [FacilityDetails] Booking successful for user bachnhhe173308, refreshing slots..."
    );
    setTimeout(() => {
      refreshAvailableSlots();
      setLastUpdateTime(new Date().toLocaleTimeString());
      console.log(
        "🔄 [FacilityDetails] Post-booking refresh completed for user bachnhhe173308"
      );
    }, 1000);

    setBookingDetailOpen(false);
    setModalOpen(false);
    setBookingDetailData(null);
  };

  // Use courtCategories from facilityData
  const courtCategories = facilityData?.categories || [];

  if (loading) {
    return (
      <div className="facility-page">
        <div className="loading-state">
          <div className="loading-spinner">⏳</div>
          Đang tải thông tin cơ sở cho người dùng...
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
          <button
            className="btn-primary"
            onClick={() => window.location.reload()}
          >
            Thử lại
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="facility-page">
      {/* Full Width Main Layout */}
      <div className="facility-main" style={{ marginTop: "5%" }}>
        {/* Left: Full width image with title below */}
        <div className="facility-image-section" style={{ marginTop: "-5%" }}>
          <div
            className="facility-title-section"
            style={{ marginBottom: "2%" }}
          >
            <h1 className="facility-title">
              {facilityData?.facilityName || "Tên cơ sở"}
            </h1>
          </div>
          <ImageCarousel images={facilityData?.images} />
        </div>

        {/* Right: Info sidebar */}
        <div className="facility-info-sidebar">
          <FacilityInfo facilityData={facilityData} />
        </div>
      </div>

      {/* Full Width Booking Section with inner container */}
      <section className="booking-section">
        <div className="booking-inner">
          <div className="booking-section-header">
            <h2 className="booking-section__title">Đặt lịch sân thể thao</h2>
          </div>

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
                  onChange={(e) => handleCategoryChange(e.target.value)}
                  disabled={loading}
                >
                  {courtCategories.length === 0 && (
                    <option value="">
                      {loading ? "Đang tải..." : "Không có loại sân"}
                    </option>
                  )}
                  {courtCategories.map((category) => (
                    <option
                      key={category.categoryId}
                      value={category.categoryId}
                    >
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
                  onChange={(e) => handleDateChange(e.target.value)}
                  aria-label="Select date"
                  min={TODAY_DATE}
                />
              </div>

              {/* ✅ Manual refresh button */}
            </div>
          </div>

          {loadingSlots && (
            <div className="loading-state">
              <div className="loading-spinner">⏳</div>
              Đang tải lịch trống cho người dùng...
            </div>
          )}

          {!loadingSlots && timeSlots.length > 0 && (
            <div className="table-container">
              <div className="table-responsive">
                <table className="booking-table">
                  <thead>
                    <tr>
                      <th className="time-header">Khung giờ</th>
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
                        <span style={{ fontSize: "24px" }}>Số sân trống</span>
                      </td>
                      {timeSlots.map((slot) => (
                        <td
                          key={slot.timeSlotId}
                          className={`availability-cell ${
                            slot.availableCourtCount > 0
                              ? "available"
                              : "unavailable"
                          } ${lastUpdateTime ? "updated" : ""}`}
                        >
                          <div className="availability-info">
                            <span className="count">
                              {slot.availableCourtCount}
                            </span>
                            <span className="status-text">
                              {slot.availableCourtCount > 0
                                ? "Còn trống"
                                : "Hết chỗ"}
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
              onClick={() => setModalOpen(true)}
              disabled={!selectedCategory || timeSlots.length === 0}
            >
              <span className="btn-icon">⚽</span>
              Đặt sân ngay
            </button>
          </div>
        </div>
      </section>

      {/* Full Width Reviews Section with inner container */}
      <section className="reviews-section">
        <div className="reviews-inner">
          <h2 className="reviews-section__title">
            <span className="title-icon">⭐</span>
            Đánh giá từ khách hàng
          </h2>

          {/* Reviews Content */}
          {facilityData?.ratings && facilityData.ratings.length > 0 ? (
            <>
              <div className="rating-summary">
                <div className="rating-main">
                  <span className="rating-value">
                    {(() => {
                      const uniqueRatings = facilityData.ratings.filter(
                        (rating, index, self) =>
                          index ===
                          self.findIndex(
                            (r) =>
                              `${r.ratingId}-${r.bookingId}` ===
                              `${rating.ratingId}-${rating.bookingId}`
                          )
                      );
                      if (uniqueRatings.length === 0) return 0;
                      const totalStars = uniqueRatings.reduce(
                        (sum, rating) => sum + rating.stars,
                        0
                      );
                      return (
                        Math.round((totalStars / uniqueRatings.length) * 10) /
                        10
                      );
                    })()}
                  </span>
                  <div className="rating-stars">
                    {[...Array(5)].map((_, index) => {
                      const uniqueRatings = facilityData.ratings.filter(
                        (rating, idx, self) =>
                          idx ===
                          self.findIndex(
                            (r) =>
                              `${r.ratingId}-${r.bookingId}` ===
                              `${rating.ratingId}-${rating.bookingId}`
                          )
                      );
                      const averageRating =
                        uniqueRatings.length > 0
                          ? uniqueRatings.reduce(
                              (sum, rating) => sum + rating.stars,
                              0
                            ) / uniqueRatings.length
                          : 0;
                      const isFilled = index < Math.round(averageRating);
                      return (
                        <span
                          key={index}
                          className={`star ${isFilled ? "filled" : ""}`}
                        >
                          ★
                        </span>
                      );
                    })}
                  </div>
                </div>

                <div className="rating-breakdown">
                  <div className="breakdown-header">
                    <span className="total-reviews">
                      {(() => {
                        const uniqueRatings = facilityData.ratings.filter(
                          (rating, index, self) =>
                            index ===
                            self.findIndex(
                              (r) =>
                                `${r.ratingId}-${r.bookingId}` ===
                                `${rating.ratingId}-${rating.bookingId}`
                            )
                        );
                        return uniqueRatings.length;
                      })()}{" "}
                      đánh giá
                    </span>
                  </div>
                  <div className="breakdown-list">
                    {[5, 4, 3, 2, 1].map((stars) => {
                      const uniqueRatings = facilityData.ratings.filter(
                        (rating, index, self) =>
                          index ===
                          self.findIndex(
                            (r) =>
                              `${r.ratingId}-${r.bookingId}` ===
                              `${rating.ratingId}-${rating.bookingId}`
                          )
                      );
                      const count = uniqueRatings.filter(
                        (rating) => rating.stars === stars
                      ).length;
                      const percentage =
                        uniqueRatings.length > 0
                          ? Math.round((count / uniqueRatings.length) * 100)
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
                {(() => {
                  const uniqueRatings = facilityData.ratings.filter(
                    (rating, index, self) =>
                      index ===
                      self.findIndex(
                        (r) =>
                          `${r.ratingId}-${r.bookingId}` ===
                          `${rating.ratingId}-${rating.bookingId}`
                      )
                  );
                  return uniqueRatings.slice(0, 3);
                })().map((rating, index) => (
                  <div
                    key={`${rating.ratingId}-${rating.bookingId}-${index}`}
                    className="review-card"
                  >
                    <div className="review-card__avatar">
                      <span className="avatar-text">U{rating.bookingId}</span>
                    </div>
                    <div className="review-card__content">
                      <div className="review-card__header">
                        <div className="reviewer-info">
                          <span className="reviewer-name">
                            Người dùng #{rating.bookingId}
                          </span>
                          <span className="review-time">
                            • Booking #{rating.bookingId}
                          </span>
                        </div>
                        <div className="review-stars">
                          {[...Array(5)].map((_, starIndex) => (
                            <span
                              key={starIndex}
                              className={`star ${
                                starIndex < rating.stars ? "filled" : ""
                              }`}
                            >
                              ★
                            </span>
                          ))}
                        </div>
                      </div>
                      <p className="review-text">
                        {rating.comment || "Không có bình luận"}
                      </p>
                    </div>
                  </div>
                ))}
              </div>

              <div className="reviews-bottom">
                <button
                  className="btn-view-all"
                  onClick={() => setReviewsModalOpen(true)}
                >
                  <span>
                    Xem tất cả đánh giá (
                    {(() => {
                      const uniqueRatings = facilityData.ratings.filter(
                        (rating, index, self) =>
                          index ===
                          self.findIndex(
                            (r) =>
                              `${r.ratingId}-${r.bookingId}` ===
                              `${rating.ratingId}-${rating.bookingId}`
                          )
                      );
                      return uniqueRatings.length;
                    })()}
                    )
                  </span>
                  <span className="btn-arrow">→</span>
                </button>
              </div>
            </>
          ) : (
            <div className="empty-state">
              <div className="empty-icon">⭐</div>
              <p>Chưa có đánh giá nào cho cơ sở này</p>
              <button className="btn-write-review">
                <span className="btn-icon">📝</span>
                <span>Viết đánh giá đầu tiên</span>
              </button>
            </div>
          )}
        </div>
      </section>

      {/* BookingModal với callback để chuyển sang BookingDetail */}
      {modalOpen && (
        <BookingModal
          open={modalOpen}
          onClose={() => setModalOpen(false)}
          timeSlots={timeSlots}
          selectedDate={selectedDate}
          facilityData={facilityData}
          selectedCategory={selectedCategory}
          onProceedToDetail={handleProceedToBookingDetail}
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
          userId={userId}
          onBookingSuccess={handleBookingSuccess} // ✅ Success callback
        />
      )}
    </div>
  );
};

export default FacilityDetails;
