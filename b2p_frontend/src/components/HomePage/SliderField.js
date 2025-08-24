import { useEffect, useState } from "react";
import Carousel from "react-bootstrap/Carousel";
import { getAllActiveSliders } from "../../services/apiService";
import "./SliderField.scss";
import { useNavigate } from "react-router-dom";
import altImg from "../../assets/images/sports-tools.jpg";
import SearchField from "./SearchField";

const SliderField = (props) => {
  useEffect(() => {
    document.title = "Trang chủ - B2P";
  }, []);
  const [index, setIndex] = useState(0);
  const [sliders, setSliders] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  const handleSelect = (selectedIndex) => {
    setIndex(selectedIndex);
  };

  // Hàm chuyển đổi Google Drive URL
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

  const fetchSliders = async () => {
    try {
      setLoading(true);
      const response = await getAllActiveSliders(1, 10);
      console.log("Sliders:", response.data.items);

      // Chuyển đổi URL cho tất cả sliders
      const processedSliders =
        response.data.items?.map((slider) => ({
          ...slider,
          imageUrl: convertGoogleDriveUrl(slider.imageUrl),
        })) || [];

      setSliders(processedSliders);
    } catch (error) {
      console.error("Error fetching sliders:", error);
      setSliders([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSliders();
  }, []);

  if (loading) {
    return (
      <div className="slider-field">
        <div className="slider-loading">
          <div className="loading-content">
            <div className="spinner-border text-success" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
            <p className="loading-text">Đang tải slider...</p>
          </div>
        </div>
      </div>
    );
  }

  if (!sliders || sliders.length === 0) {
    return (
      <div className="slider-field">
        <div className="slider-empty">
          <div className="empty-content">
            <i className="fas fa-images fa-3x"></i>
            <p>Không có slider nào để hiển thị</p>
          </div>
        </div>
        <SearchField />
      </div>
    );
  }

  return (
    <div className="slider-field">
      <Carousel
        activeIndex={index}
        onSelect={handleSelect}
        interval={5000}
        pause="hover"
        fade
        controls={true}
        indicators={false}
      >
        {sliders.map((slider, idx) => (
          <Carousel.Item key={slider.slideUrl + "ABC" || idx}>
            <div className="carousel-image-container">
              <img
                className="carousel-image"
                src={slider.imageUrl}
                alt={slider.title || `Slider ${idx + 1}`}
                onError={(e) => {
                  console.log("Image load error:", slider.imageUrl);
                  e.target.src = altImg;
                }}
                onLoad={() => {
                  console.log("Image loaded successfully:", slider.imageUrl);
                }}
                onClick={() => {
                  if (slider.slideUrl) {
                    navigate(slider.slideUrl);
                  }
                }}
              />
              <div className="carousel-overlay"></div>

              {/* Optional: Add caption if slider has title/description */}
              {(slider.title || slider.description) && (
                <Carousel.Caption className="custom-caption">
                  <div className="caption-content">
                    {slider.title && (
                      <h3 className="caption-title">{slider.title}</h3>
                    )}
                    {slider.description && (
                      <p className="caption-description">
                        {slider.description}
                      </p>
                    )}
                  </div>
                </Carousel.Caption>
              )}
            </div>
          </Carousel.Item>
        ))}
      </Carousel>

      {/* Search Field overlay */}
      <SearchField />

      {/* 🎯 REMOVED: Custom indicators section completely removed */}

      {/* Decorative elements */}
      <div className="slider-decorations">
        <div className="decoration-left"></div>
        <div className="decoration-right"></div>
      </div>
    </div>
  );
};

export default SliderField;
