import Form from "react-bootstrap/Form";
import "./FacilitiesWithCondition.scss";
import Button from "react-bootstrap/Button";
import Card from "react-bootstrap/Card";
import { useEffect, useState } from "react";
import {
  getAllFacilitiesByPlayer,
  getAllCourtCategories,
} from "../../services/apiService";
import altImg from "../../assets/images/sports-tools.jpg";
import { useNavigate } from "react-router-dom";
import ReactPaginate from "react-paginate";

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

const formatPrice = (price) => {
  if (!price || price === 0) return "0";
  return parseInt(price).toLocaleString("vi-VN");
};

const FacilitiesWithCondition = (props) => {
  const navigate = useNavigate();

  // State management
  const [facilities, setFacilities] = useState([]);
  const [loading, setLoading] = useState(false);
  const [listCourtCategories, setListCourtCategories] = useState([]);
  const [apiStatus, setApiStatus] = useState(null);

  // Filter states
  const [searchText, setSearchText] = useState("");
  const [selectedCategories, setSelectedCategories] = useState([]);
  const [sortOrder, setSortOrder] = useState("3");
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);

  const pageSize = 6;

  // Fetch court categories
  const fetchCourtCategories = async () => {
    try {
      const response = await getAllCourtCategories(
        "",
        1,
        Number.MAX_SAFE_INTEGER
      );
      console.log("Court Categories:", response.data);
      const categories = response.data.items;
      setListCourtCategories(categories);
    } catch (error) {
      console.error("Error fetching court categories:", error);
    }
  };

  // Fetch facilities - SIMPLIFIED STATUS HANDLING
  const fetchFacilities = async (page = 1) => {
    setLoading(true);
    setApiStatus(null);

    try {
      const requestBody = {
        name: searchText,
        type: [0],
        city: "",
        ward: "",
        order: parseInt(sortOrder),
      };

      if (selectedCategories.length > 0) {
        requestBody.type = selectedCategories;
      }

      console.log("Request body:", requestBody);

      const response = await getAllFacilitiesByPlayer(
        page,
        pageSize,
        requestBody
      );

      const status = response.data.status;
      setApiStatus(status);

      console.log("API Response status:", status);

      if (status === 200) {
        // Status 200 - hiển thị kết quả
        if (response && response.data) {
          setFacilities(response.data.items || []);
          setTotalPages(response.data.totalPages || 1);
          setTotalItems(response.data.totalItems || 0);
          setCurrentPage(page);
        }
      } else if (status === 404) {
        // Status 404 - không tìm thấy
        setFacilities([]);
        setTotalPages(1);
        setTotalItems(0);
        setCurrentPage(1);
      }
    } catch (error) {
      console.error("Error fetching facilities:", error);
      // Xử lý lỗi như status 404
      setApiStatus(404);
      setFacilities([]);
      setTotalPages(1);
      setTotalItems(0);
      setCurrentPage(1);
    } finally {
      setLoading(false);
    }
  };

  // Handle category checkbox change
  const handleCategoryChange = (categoryId, isChecked) => {
    console.log(
      `Category ${categoryId} ${isChecked ? "checked" : "unchecked"}`
    );

    let newCategories;
    if (isChecked) {
      newCategories = [...selectedCategories, categoryId];
    } else {
      newCategories = selectedCategories.filter((id) => id !== categoryId);
    }

    console.log("New categories will be:", newCategories);
    setSelectedCategories(newCategories);
  };

  // Handle search
  const handleSearch = () => {
    console.log("Searching with categories:", selectedCategories);
    setCurrentPage(1);
    fetchFacilities(1);
  };

  // Handle reset filters
  const handleReset = () => {
    setSearchText("");
    setSelectedCategories([0]);
    setSortOrder("3");
    setCurrentPage(1);
  };

  // Handle pagination
  const handlePageChange = (selectedPage) => {
    const page = selectedPage.selected + 1;
    fetchFacilities(page);
  };

  // Initial load
  useEffect(() => {
    fetchCourtCategories();
    fetchFacilities();
  }, []);

  return (
    <div className="facilities-with-condition">
      <div className="container-fluid">
        <div className="row">
          {/* Sidebar tìm kiếm */}
          <div className="col-lg-3 col-md-4 col-12">
            <div className="search-sidebar">
              <div className="sidebar-header">
                <h4 className="sidebar-title">
                  <i className="fas fa-filter me-2"></i>
                  Bộ lọc tìm kiếm
                </h4>
              </div>

              <div className="filter-section">
                <h5 className="filter-title">Tìm kiếm</h5>
                <Form.Control
                  className="search-input"
                  type="text"
                  placeholder="🔍 Nhập tên sân..."
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                />
              </div>

              <div className="filter-section">
                <h5 className="filter-title">Loại sân</h5>
                <div className="checkbox-group">
                  {listCourtCategories.map((category) => (
                    <Form.Check
                      key={category.categoryId}
                      type="checkbox"
                      id={`category-${category.categoryId}`}
                      label={category.categoryName}
                      className="filter-checkbox"
                      checked={selectedCategories.includes(category.categoryId)}
                      onChange={(e) =>
                        handleCategoryChange(
                          category.categoryId,
                          e.target.checked
                        )
                      }
                    />
                  ))}
                </div>
              </div>

              <div className="filter-section">
                <h5 className="filter-title">Sắp xếp</h5>
                <Form.Select
                  className="sort-select"
                  value={sortOrder}
                  onChange={(e) => setSortOrder(e.target.value)}
                >
                  <option value="1">Giá cao đến thấp</option>
                  <option value="2">Giá thấp đến cao</option>
                  <option value="3">Đánh giá cao nhất</option>
                </Form.Select>
              </div>

              <Button
                className="search-btn"
                variant="success"
                size="lg"
                onClick={handleSearch}
                disabled={loading}
              >
                <i className="fas fa-search me-2"></i>
                {loading ? "Đang tìm..." : "Tìm kiếm"}
              </Button>

              <Button
                className="reset-btn"
                variant="outline-secondary"
                size="sm"
                onClick={handleReset}
                disabled={loading}
              >
                <i className="fas fa-undo me-2"></i>
                Đặt lại
              </Button>
            </div>
          </div>

          {/* Kết quả tìm kiếm */}
          <div className="col-lg-9 col-md-8 col-12">
            <div className="search-results">
              <div className="results-header">
                <h4 className="results-title">
                  <i className="fas fa-list me-2"></i>
                  Kết quả tìm kiếm
                </h4>
                <div className="results-info">
                  <span className="results-count">
                    {apiStatus === 200 ? `Tìm thấy ${totalItems} cơ sở` : ""}
                  </span>
                </div>
              </div>

              {loading ? (
                <div className="loading-container">
                  <div className="text-center py-5">
                    <div className="spinner-border text-success" role="status">
                      <span className="visually-hidden">Đang tải...</span>
                    </div>
                    <p className="mt-2">Đang tải dữ liệu...</p>
                  </div>
                </div>
              ) : (
                <>
                  <div className="facilities-grid">
                    {apiStatus === 200 && facilities.length > 0 ? (
                      // Hiển thị kết quả khi status 200 và có data
                      facilities.map((facility) => (
                        <div
                          key={facility.facilityId}
                          className="facility-item"
                        >
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
                                  onClick={() =>
                                    navigate(`/facility/${facility.facilityId}`)
                                  }
                                >
                                  <i className="fas fa-eye me-2"></i>
                                  Xem chi tiết
                                </Button>
                              </div>
                            </Card.Body>
                          </Card>
                        </div>
                      ))
                    ) : apiStatus === 404 ||
                      (apiStatus === 200 && facilities.length === 0) ? (
                      // Hiển thị thông báo đơn giản khi status 404 hoặc không có kết quả
                      <div className="no-results">
                        <div className="text-center py-5">
                          <i className="fas fa-search fa-3x text-muted mb-3"></i>
                          <h5>Không tìm thấy cơ sở nào</h5>
                          <p className="text-muted">
                            Vui lòng thử lại với từ khóa khác hoặc điều chỉnh bộ
                            lọc
                          </p>
                        </div>
                      </div>
                    ) : null}
                  </div>

                  {/* Pagination chỉ hiển thị khi status 200 và có nhiều trang */}
                  {apiStatus === 200 && totalPages > 1 && (
                    <div className="pagination-container">
                      <ReactPaginate
                        nextLabel={
                          <>
                            Tiếp theo{" "}
                            <i className="fas fa-chevron-right ms-1"></i>
                          </>
                        }
                        onPageChange={handlePageChange}
                        pageRangeDisplayed={3}
                        marginPagesDisplayed={2}
                        pageCount={totalPages}
                        previousLabel={
                          <>
                            <i className="fas fa-chevron-left me-1"></i> Trước
                          </>
                        }
                        pageClassName="page-item"
                        pageLinkClassName="page-link"
                        previousClassName="page-item"
                        previousLinkClassName="page-link"
                        nextClassName="page-item"
                        nextLinkClassName="page-link"
                        breakLabel="..."
                        breakClassName="page-item"
                        breakLinkClassName="page-link"
                        containerClassName="pagination justify-content-center"
                        activeClassName="active"
                        forcePage={currentPage - 1}
                        renderOnZeroPageCount={null}
                      />
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FacilitiesWithCondition;
