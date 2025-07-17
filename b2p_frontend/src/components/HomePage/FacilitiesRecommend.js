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
  const requestBody = {
    name: "",
    type: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
    city: "",
    ward: "",
    order: 3,
  };

  const fetchRecommendedFacilities = async () => {
    try {
      const response = await getAllFacilitiesByPlayer(1, 12, requestBody);
      if (response && response.data.items.length > 0) {
        setFacilities(response.data.items);
      }
    } catch (error) {
      console.error("Error fetching recommended facilities:", error);
    }
  };

  useEffect(() => {
    fetchRecommendedFacilities();
  }, []);

  return (
    <div className="facilities-recommend">
      <h3 className="title">Các cơ sở có lượt đánh giá cao</h3>
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
                />
                <div className="image-overlay">
                  <div className="rating-badge">
                    <i className="fas fa-star"></i>
                    {facility.averageRating || "N/A"}
                  </div>
                </div>
              </div>

              <Card.Body className="facility-body">
                <Card.Title className="facility-title">
                  {facility.facilityName}
                </Card.Title>

                <div className="facility-info">
                  <div className="info-item">
                    <i className="fas fa-map-marker-alt"></i>
                    <span>{facility.location}</span>
                  </div>

                  <div className="info-item">
                    <i className="fas fa-clock"></i>
                    <span>{facility.openTime}</span>
                  </div>

                  <div className="info-item">
                    <i className="fas fa-money-bill-wave"></i>
                    <span>
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
                    Xem chi tiết
                  </Button>
                </div>
              </Card.Body>
            </Card>
          </div>
        ))}
      </div>
    </div>
  );
};

export default FacilitiesRecommend;
