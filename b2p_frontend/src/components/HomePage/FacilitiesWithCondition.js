import Form from "react-bootstrap/Form";
import Button from "react-bootstrap/Button";
import Card from "react-bootstrap/Card";
import React, { useEffect, useState, useRef } from "react";
import {
  getAllCourtCategories,
  getAllFacilitiesByPlayer,
} from "../../services/apiService";
import { useSelector, useDispatch } from "react-redux";
import ReactPaginate from "react-paginate";
import { useNavigate, useLocation } from "react-router-dom";
import { setSearchFacility } from "../../store/action/searchFacilityAction";
import "./FacilitiesWithCondition.scss";
import altImg from "../../assets/images/sports-tools.jpg";

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

// Function to format price
const formatPrice = (price) => {
  if (!price || price === 0) return "0";
  return parseInt(price).toLocaleString("vi-VN");
};

const FacilitiesWithCondition = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const searchFacility = useSelector((state) => state.searchFacility);
  const dispatch = useDispatch();

  // State management
  const [facilities, setFacilities] = useState([]);
  const [loading, setLoading] = useState(false);
  const [listCourtCategories, setListCourtCategories] = useState([]);
  const [apiStatus, setApiStatus] = useState(null);

  // Filter states
  const [searchText, setSearchText] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("");
  const [sortOrder, setSortOrder] = useState("3");
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);

  // State for selected province and district
  const [selectedProvince, setSelectedProvince] = useState(
    searchFacility.province || ""
  );
  const [selectedDistrict, setSelectedDistrict] = useState(
    searchFacility.district || ""
  );
  const [provinces, setProvinces] = useState([]);
  const [districts, setDistricts] = useState([]);

  const pageSize = 6;

  // Ref to track initial search
  const initialSearchDone = useRef(false);

  // Fetch court categories
  const fetchCourtCategories = async () => {
    try {
      const response = await getAllCourtCategories(
        "",
        1,
        Number.MAX_SAFE_INTEGER
      );
      const categories = response.data.items;
      setListCourtCategories(categories);

      console.log("=== COURT CATEGORIES LOADED ===");
      console.log("Categories:", categories);

      // Set default selected category to first item when categories are loaded
      if (categories.length > 0 && !selectedCategory) {
        const firstCategoryId = categories[0].categoryId.toString();
        setSelectedCategory(firstCategoryId);
        console.log("Set default category to:", firstCategoryId);
      }
    } catch (error) {
      console.error("Error fetching court categories:", error);
    }
  };

  // Fetch provinces
  const fetchProvinces = async () => {
    try {
      const response = await fetch("https://provinces.open-api.vn/api/p/");
      const data = await response.json();
      setProvinces(data);
    } catch (error) {
      console.error("Error fetching provinces:", error);
    }
  };

  // Fetch districts based on selected province
  const fetchDistricts = async (provinceName) => {
    if (!provinceName) return;

    try {
      const selectedProvinceObj = provinces.find(
        (p) => p.name === provinceName
      );
      if (!selectedProvinceObj) return;

      const response = await fetch(
        `https://provinces.open-api.vn/api/p/${selectedProvinceObj.code}?depth=2`
      );
      const data = await response.json();
      const districtList = data.districts || [];
      setDistricts(districtList);

      if (districtList.length > 0) {
        setSelectedDistrict(districtList[0].name);
      } else {
        setSelectedDistrict("");
      }
    } catch (error) {
      console.error("Error fetching districts:", error);
    }
  };

  // UseEffect to fetch data
  useEffect(() => {
    fetchCourtCategories();
    fetchProvinces();
  }, []);

  useEffect(() => {
    if (selectedProvince) {
      fetchDistricts(selectedProvince);
    } else {
      setDistricts([]);
      setSelectedDistrict("");
    }
  }, [selectedProvince]);

  // 🎯 Handle URL parameter changes (for forced refresh from header)
  useEffect(() => {
    const urlParams = new URLSearchParams(location.search);
    const categoryFromUrl = urlParams.get("category");
    const timestampFromUrl = urlParams.get("t");

    console.log("=== URL PARAMS CHANGED ===");
    console.log("Category from URL:", categoryFromUrl);
    console.log("Timestamp from URL:", timestampFromUrl);

    if (categoryFromUrl && listCourtCategories.length > 0) {
      console.log("Setting category from URL:", categoryFromUrl);
      setSelectedCategory(categoryFromUrl);

      // Update Redux state to match URL
      dispatch(
        setSearchFacility({
          searchText: "",
          categoryId: parseInt(categoryFromUrl),
          province: "",
          district: "",
          timestamp: timestampFromUrl ? parseInt(timestampFromUrl) : Date.now(),
        })
      );

      // Perform search with new category
      const requestBody = {
        name: "",
        type: [parseInt(categoryFromUrl)],
        city: "",
        ward: "",
        order: parseInt(sortOrder),
      };

      console.log("Searching from URL params:", requestBody);
      fetchFacilities(1, requestBody);

      // Clean up URL params after processing
      navigate("/search", { replace: true });
    }
  }, [location.search, listCourtCategories, sortOrder, dispatch, navigate]);

  // 🎯 Handle Redux search facility state changes
  useEffect(() => {
    if (searchFacility && listCourtCategories.length > 0) {
      console.log("=== REDUX STATE CHANGED ===");
      console.log("searchFacility:", searchFacility);

      // Update local states from searchFacility
      setSearchText(searchFacility.searchText || "");

      // Handle category selection from Redux state
      if (searchFacility.categoryId) {
        let categoryToSet;
        if (Array.isArray(searchFacility.categoryId)) {
          categoryToSet = searchFacility.categoryId[0]?.toString() || "";
        } else {
          categoryToSet = searchFacility.categoryId.toString();
        }

        console.log("Setting category from Redux:", categoryToSet);
        setSelectedCategory(categoryToSet);
      } else {
        if (listCourtCategories.length > 0) {
          const firstCategoryId = listCourtCategories[0].categoryId.toString();
          setSelectedCategory(firstCategoryId);
          console.log("No category from Redux, using first:", firstCategoryId);
        }
      }

      setSelectedProvince(searchFacility.province || "");
      setSelectedDistrict(searchFacility.district || "");

      // 🎯 Check for timestamp to force search (from header navigation)
      if (searchFacility.timestamp) {
        console.log(
          "Timestamp detected, forcing search:",
          searchFacility.timestamp
        );

        const categoryToSearch = searchFacility.categoryId
          ? Array.isArray(searchFacility.categoryId)
            ? searchFacility.categoryId[0]
            : searchFacility.categoryId
          : listCourtCategories.length > 0
          ? listCourtCategories[0].categoryId
          : null;

        const requestBody = {
          name: searchFacility.searchText || "",
          type: categoryToSearch ? [parseInt(categoryToSearch)] : [],
          city: searchFacility.province || "",
          ward: searchFacility.district || "",
          order: parseInt(sortOrder),
        };

        console.log("Force search request body:", requestBody);
        fetchFacilities(1, requestBody);

        // Clear timestamp to prevent infinite loops
        dispatch(
          setSearchFacility({
            ...searchFacility,
            timestamp: null,
          })
        );
      } else if (!initialSearchDone.current) {
        // Only perform initial search if no timestamp
        const categoryToSearch = searchFacility.categoryId
          ? Array.isArray(searchFacility.categoryId)
            ? searchFacility.categoryId[0]
            : searchFacility.categoryId
          : listCourtCategories.length > 0
          ? listCourtCategories[0].categoryId
          : null;

        console.log("Initial search with category:", categoryToSearch);

        const requestBody = {
          name: searchFacility.searchText || "",
          type: categoryToSearch ? [parseInt(categoryToSearch)] : [],
          city: searchFacility.province || "",
          ward: searchFacility.district || "",
          order: parseInt(sortOrder),
        };

        console.log("Initial search request body:", requestBody);
        fetchFacilities(1, requestBody);
        initialSearchDone.current = true;
      }
    }
  }, [searchFacility, listCourtCategories, sortOrder, dispatch]);

  // Handle search
  const handleSearch = () => {
    const categoryId = selectedCategory ? parseInt(selectedCategory) : null;

    console.log("=== HANDLING SEARCH ===");
    console.log("Selected category:", categoryId);

    // Update searchParams with latest values
    const searchParams = {
      searchText: searchText,
      categoryId: categoryId,
      province: selectedProvince,
      district: selectedDistrict,
    };

    console.log("Search params:", searchParams);

    // Update Redux state with new search params
    dispatch(setSearchFacility(searchParams));

    // Create request body for API call
    const requestBody = {
      name: searchText,
      type: categoryId ? [categoryId] : [],
      city: selectedProvince,
      ward: selectedDistrict,
      order: parseInt(sortOrder),
    };

    console.log("Search request body:", requestBody);

    // Call API with request body
    fetchFacilities(1, requestBody);
  };

  // Fetch facilities based on filters
  const fetchFacilities = async (page = 1, customRequestBody = null) => {
    setLoading(true);
    setApiStatus(null);
    try {
      const categoryId = selectedCategory ? parseInt(selectedCategory) : null;

      const requestBody = customRequestBody || {
        name: searchText,
        type: categoryId ? [categoryId] : [],
        city: selectedProvince,
        ward: selectedDistrict,
        order: parseInt(sortOrder),
      };

      console.log("=== FETCHING FACILITIES ===");
      console.log("Page:", page, "Request body:", requestBody);

      const response = await getAllFacilitiesByPlayer(
        page,
        pageSize,
        requestBody
      );
      const status = response.status;
      setApiStatus(status);
      console.log("API Response:", status, response.data);
      if (status === 200) {
        setFacilities(response.data.items || []);
        setTotalPages(response.data.totalPages || 1);
        setTotalItems(response.data.totalItems || 0);
        setCurrentPage(page);
      } else {
        setFacilities([]);
        setTotalPages(1);
        setTotalItems(0);
        setCurrentPage(1);
      }
    } catch (error) {
      console.error("Error fetching facilities:", error);
      setApiStatus(404);
      setFacilities([]);
      setTotalPages(1);
      setTotalItems(0);
      setCurrentPage(1);
    } finally {
      setLoading(false);
    }
  };

  // Handle reset filters - set to first category
  const handleReset = () => {
    console.log("=== RESETTING FILTERS ===");

    setSearchText("");
    setSelectedProvince("");
    setSelectedDistrict("");
    setSortOrder("3");
    setCurrentPage(1);

    // Set selectedCategory to first category
    const firstCategoryId =
      listCourtCategories.length > 0 ? listCourtCategories[0].categoryId : "";

    console.log("Reset to first category:", firstCategoryId);
    setSelectedCategory(firstCategoryId.toString());

    // Reset Redux state with first category
    dispatch(
      setSearchFacility({
        searchText: "",
        categoryId: firstCategoryId,
        province: "",
        district: "",
      })
    );

    // Call API with first category as default
    const requestBody = {
      name: "",
      type: firstCategoryId ? [firstCategoryId] : [],
      city: "",
      ward: "",
      order: 3,
    };

    console.log("Reset request body:", requestBody);
    fetchFacilities(1, requestBody);
  };

  // Handle pagination
  const handlePageChange = (selectedPage) => {
    const page = selectedPage.selected + 1;
    fetchFacilities(page);
  };

  // Get category name by ID for debugging
  const getCategoryNameById = (categoryId) => {
    const category = listCourtCategories.find(
      (cat) => cat.categoryId === parseInt(categoryId)
    );
    return category ? category.categoryName : "Unknown";
  };

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

              {/* Tỉnh/Thành phố */}
              <div className="filter-section">
                <h5 className="filter-title">Tỉnh/Thành phố</h5>
                <Form.Select
                  value={selectedProvince}
                  onChange={(e) => setSelectedProvince(e.target.value)}
                >
                  <option value="">Chọn tỉnh/thành phố</option>
                  {provinces.map((province) => (
                    <option key={province.code} value={province.name}>
                      {province.name}
                    </option>
                  ))}
                </Form.Select>
              </div>

              {/* Quận/Huyện */}
              <div className="filter-section">
                <h5 className="filter-title">Quận/Huyện</h5>
                <Form.Select
                  value={selectedDistrict}
                  onChange={(e) => setSelectedDistrict(e.target.value)}
                  disabled={!selectedProvince || districts.length === 0}
                >
                  <option value="">Chọn quận/huyện</option>
                  {districts.map((district) => (
                    <option key={district.code} value={district.name}>
                      {district.name}
                    </option>
                  ))}
                </Form.Select>
              </div>

              {/* Loại sân */}
              <div className="filter-section">
                <h5 className="filter-title">Loại sân</h5>
                <Form.Select
                  value={selectedCategory}
                  onChange={(e) => {
                    console.log("Category changed to:", e.target.value);
                    setSelectedCategory(e.target.value);
                  }}
                >
                  {listCourtCategories.map((category) => (
                    <option
                      key={category.categoryId}
                      value={category.categoryId}
                    >
                      {category.categoryName}
                    </option>
                  ))}
                </Form.Select>
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
              ) : facilities.length === 0 ? (
                <div className="error-container text-center">
                  <i className="fas fa-exclamation-triangle fa-3x mb-3"></i>
                  <h5>Không tìm thấy kết quả</h5>
                  <p className="text-muted">
                    Không tìm thấy cơ sở nào phù hợp với tiêu chí tìm kiếm của
                    bạn. Vui lòng thử lại với các tiêu chí khác.
                  </p>
                </div>
              ) : (
                <div className="facilities-grid">
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
                          </div>
                        </div>

                        <Card.Body className="facility-body">
                          <Card.Title>{facility.facilityName}</Card.Title>
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
                          <Button
                            className="detail-btn"
                            variant="primary"
                            onClick={() =>
                              navigate(`/facility-details/${facility.facilityId}`)
                            }
                          >
                            <i className="fas fa-eye me-2"></i>
                            Xem chi tiết
                          </Button>
                        </Card.Body>
                      </Card>
                    </div>
                  ))}
                </div>
              )}

              {facilities.length > 0 && totalPages > 1 && (
                <div className="pagination-container">
                  <ReactPaginate
                    nextLabel="Tiếp >"
                    onPageChange={handlePageChange}
                    pageRangeDisplayed={3}
                    marginPagesDisplayed={2}
                    pageCount={totalPages}
                    previousLabel="< Trước"
                    pageClassName="page-item"
                    pageLinkClassName="page-link"
                    previousClassName="page-item"
                    previousLinkClassName="page-link"
                    nextClassName="page-item"
                    nextLinkClassName="page-link"
                    breakLabel="..."
                    breakClassName="page-item"
                    breakLinkClassName="page-link"
                    containerClassName="pagination"
                    activeClassName="active"
                    forcePage={currentPage - 1}
                    renderOnZeroPageCount={null}
                  />
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FacilitiesWithCondition;
