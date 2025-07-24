import Button from "react-bootstrap/Button";
import Card from "react-bootstrap/Card";
import { useEffect, useState } from "react";
import { getAllFacilitiesByPlayer } from "../../services/apiService";
import "./FacilitiesRecommend.scss";
import altImg from "../../assets/images/sports-tools.jpg";
import { useNavigate } from "react-router-dom";

const convertGoogleDriveUrl = (url) => {
  if (!url) return "";

  if (url.includes("drive.google.com")) {
    const fileIdMatch =
      url.match(/\/d\/([a-zA-Z0-9-_]+)/) ||
      url.match(/[?&]id=([a-zA-Z0-9-_]+)/);
    if (fileIdMatch) {
      const fileId = fileIdMatch[1];
      return `https://drive.google.com/thumbnail?id=${fileId}&sz=w1000`;
    }
  }

  return url;
};

// Hàm format giá tiền
const formatPrice = (price) => {
  if (!price || price === 0) return "0";
  return parseInt(price).toLocaleString("vi-VN");
};

const FacilitiesRecommend = (props) => {
  const navigate = useNavigate();

  const [facilities, setFacilities] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const requestBody = {
    name: "",
    type: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
    city: "",
    ward: "",
    order: 3,
  };

  const fetchRecommendedFacilities = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getAllFacilitiesByPlayer(1, 12, requestBody);
      if (response && response.data.items.length > 0) {
        setFacilities(response.data.items);
      } else {
        setFacilities([]);
      }
    } catch (error) {
      console.error("Error fetching recommended facilities:", error);
      setError("Không thể tải danh sách cơ sở đề xuất");
      setFacilities([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecommendedFacilities();
  }, []);

  // Loading state
  if (loading) {
    return (
      <div className="facilities-recommend">
        <h3 className="title">Các cơ sở có lượt đánh giá cao</h3>
        <div className="loading-container">
          <div className="loading-content">
            <div className="spinner-border text-success" role="status">
              <span className="visually-hidden">Đang tải...</span>
            </div>
            <p className="loading-text">Đang tải danh sách cơ sở...</p>
          </div>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="facilities-recommend">
        <h3 className="title">Các cơ sở có lượt đánh giá cao</h3>
        <div className="error-container">
          <div className="error-content">
            <i className="fas fa-exclamation-triangle fa-3x"></i>
            <h5>Có lỗi xảy ra</h5>
            <p>{error}</p>
            <Button
              variant="success"
              onClick={fetchRecommendedFacilities}
              className="retry-btn"
            >
              <i className="fas fa-redo me-2"></i>
              Thử lại
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // Empty state
  if (facilities.length === 0) {
    return (
      <div className="facilities-recommend">
        <h3 className="title">Các cơ sở có lượt đánh giá cao</h3>
        <div className="empty-container">
          <div className="empty-content">
            <i className="fas fa-search fa-3x"></i>
            <h5>Không có cơ sở nào</h5>
            <p>Hiện tại chưa có cơ sở nào để đề xuất.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="facilities-recommend">
      <div className="section-header">
        <h3 className="title">Các cơ sở có lượt đánh giá cao</h3>
      </div>

      <div className="main-content">
        {facilities.map((facility) => (
          <div key={facility.facilityId} className="facility-item">
            <Card className="facility-card">
              <div className="card-image-container">
                <Card.Img
                  variant="top"
                  src={
                    facility.firstImage
                      ? convertGoogleDriveUrl(facility.firstImage)
                      : altImg
                  }
                  alt={facility.facilityName}
                  className="facility-image"
                  onError={(e) => {
                    e.target.onerror = null;
                    e.target.src = altImg;
                  }}
                />
                <div className="image-overlay">
                  <div className="rating-badge">
                    <i className="fas fa-star"></i>
                    {facility.averageRating || "N/A"}
                  </div>
                  {facility.isNew && (
                    <div className="new-badge">
                      <i className="fas fa-bolt"></i>
                      Mới
                    </div>
                  )}
                </div>
              </div>

              <Card.Body className="facility-body">
                <Card.Title className="facility-title">
                  {facility.facilityName}
                </Card.Title>

                <div className="facility-info">
                  <div className="info-item">
                    <i className="fas fa-map-marker-alt"></i>
                    <span className="info-text">{facility.location}</span>
                  </div>

                  <div className="info-item">
                    <i className="fas fa-clock"></i>
                    <span className="info-text">{facility.openTime}</span>
                  </div>

                  <div className="info-item">
                    <i className="fas fa-money-bill-wave"></i>
                    <span className="info-text price-range">
                      {formatPrice(facility.minPrice)}đ -{" "}
                      {formatPrice(facility.maxPrice)}đ/giờ
                    </span>
                  </div>
                </div>

                <div className="facility-actions">
                  <Button
                    className="detail-btn"
                    variant="primary"
                    onClick={() => navigate(`/facility/${facility.facilityId}`)}
                  >
                    <i className="fas fa-eye me-2"></i>
                    <span className="btn-text">Xem chi tiết</span>
                  </Button>
                </div>
              </Card.Body>
            </Card>
          </div>
        ))}
      </div>

      {/* Show more button if there are more facilities */}
      {facilities.length >= 12 && (
        <div className="show-more-container">
          <Button
            variant="outline-success"
            size="lg"
            onClick={() => navigate("/search")}
            className="show-more-btn"
          >
            <i className="fas fa-plus me-2"></i>
            Xem thêm cơ sở
          </Button>
        </div>
      )}
    </div>
  );
};

export default FacilitiesRecommend;
