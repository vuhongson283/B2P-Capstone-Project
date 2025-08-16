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
  // 🎯 Changed to array for multiple categories
  const [selectedCategories, setSelectedCategories] = useState([]);
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

  // 🎯 State cho filter dropdown
  const [provinceFilter, setProvinceFilter] = useState("");
  const [districtFilter, setDistrictFilter] = useState("");
  const [showProvinceDropdown, setShowProvinceDropdown] = useState(false);
  const [showDistrictDropdown, setShowDistrictDropdown] = useState(false);

  // 🎯 State cho category dropdown
  const [categoryFilter, setCategoryFilter] = useState("");
  const [showCategoryDropdown, setShowCategoryDropdown] = useState(false);

  // 🎯 Refs cho click outside
  const provinceDropdownRef = useRef(null);
  const districtDropdownRef = useRef(null);
  const categoryDropdownRef = useRef(null);

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

      // 🎯 Không set default category ở đây nữa, để useEffect khác handle
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

      // 🎯 Không auto chọn quận/huyện đầu tiên nữa
      setSelectedDistrict("");
      setDistrictFilter("");
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
      setDistrictFilter("");
    }
  }, [selectedProvince]);

  // 🎯 Sửa useEffect đồng bộ categories - FIX CHÍNH
  useEffect(() => {
    console.log("=== SYNCING CATEGORIES USEEFFECT ===");
    console.log("listCourtCategories.length:", listCourtCategories.length);
    console.log("searchFacility:", searchFacility);
    console.log("current selectedCategories:", selectedCategories);

    // Khi listCourtCategories load xong
    if (listCourtCategories.length > 0) {
      // Kiểm tra nếu có categoryId từ Redux
      if (searchFacility && searchFacility.categoryId) {
        let categoriesToSet;
        if (Array.isArray(searchFacility.categoryId)) {
          categoriesToSet = searchFacility.categoryId.map((id) => parseInt(id)); // 🎯 Ensure integers
        } else {
          categoriesToSet = [parseInt(searchFacility.categoryId)]; // 🎯 Ensure integer
        }

        console.log("Categories from Redux (parsed):", categoriesToSet);

        // 🎯 Validate tất cả categories có tồn tại trong listCourtCategories
        const validCategories = categoriesToSet.filter((categoryId) =>
          listCourtCategories.some((cat) => cat.categoryId === categoryId)
        );

        console.log("Valid categories after filter:", validCategories);

        // 🎯 Chỉ update nếu validCategories khác với selectedCategories hiện tại
        const currentCategoriesString = JSON.stringify(
          [...selectedCategories].sort()
        );
        const newCategoriesString = JSON.stringify([...validCategories].sort());

        console.log("Current categories string:", currentCategoriesString);
        console.log("New categories string:", newCategoriesString);

        if (
          validCategories.length > 0 &&
          currentCategoriesString !== newCategoriesString
        ) {
          console.log("🔄 UPDATING categories from Redux:", validCategories);
          setSelectedCategories(validCategories);
          return; // 🎯 Exit early to avoid setting default
        } else if (validCategories.length > 0) {
          console.log("✅ Categories already synced, no update needed");
          return; // 🎯 Exit early to avoid setting default
        } else {
          console.log("⚠️ No valid categories found from Redux");
        }
      }

      // 🎯 Chỉ set default nếu KHÔNG có categories nào được chọn VÀ không có từ Redux
      if (
        selectedCategories.length === 0 &&
        (!searchFacility || !searchFacility.categoryId)
      ) {
        console.log(
          "📝 No categories selected and none from Redux, using first category"
        );
        const firstCategoryId = listCourtCategories[0].categoryId;
        setSelectedCategories([firstCategoryId]);
      }
    }
  }, [listCourtCategories, searchFacility]); // 🎯 Removed selectedCategories from deps

  // 🎯 Handle click outside để đóng dropdown
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (
        provinceDropdownRef.current &&
        !provinceDropdownRef.current.contains(event.target)
      ) {
        setShowProvinceDropdown(false);
      }
      if (
        districtDropdownRef.current &&
        !districtDropdownRef.current.contains(event.target)
      ) {
        setShowDistrictDropdown(false);
      }
      if (
        categoryDropdownRef.current &&
        !categoryDropdownRef.current.contains(event.target)
      ) {
        setShowCategoryDropdown(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

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
      setSelectedCategories([parseInt(categoryFromUrl)]);

      // Update Redux state to match URL
      dispatch(
        setSearchFacility({
          searchText: "",
          categoryId: [parseInt(categoryFromUrl)],
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

  // 🎯 Sửa useEffect handle Redux state changes để không override categories
  useEffect(() => {
    if (searchFacility && listCourtCategories.length > 0) {
      console.log("=== REDUX STATE CHANGED USEEFFECT ===");
      console.log("searchFacility:", searchFacility);

      // Update local states from searchFacility (KHÔNG touch categories ở đây)
      setSearchText(searchFacility.searchText || "");
      setSelectedProvince(searchFacility.province || "");
      setSelectedDistrict(searchFacility.district || "");

      // 🎯 Sync filter states with selected values
      setProvinceFilter(searchFacility.province || "");
      setDistrictFilter(searchFacility.district || "");

      // 🎯 Check for timestamp to force search (from header navigation)
      if (searchFacility.timestamp) {
        console.log(
          "Timestamp detected, forcing search:",
          searchFacility.timestamp
        );

        // 🎯 Use categories from Redux instead of selectedCategories
        let categoriesToSearch = [];
        if (searchFacility.categoryId) {
          if (Array.isArray(searchFacility.categoryId)) {
            categoriesToSearch = searchFacility.categoryId.map((id) =>
              parseInt(id)
            );
          } else {
            categoriesToSearch = [parseInt(searchFacility.categoryId)];
          }
        } else if (selectedCategories.length > 0) {
          categoriesToSearch = selectedCategories;
        } else if (listCourtCategories.length > 0) {
          categoriesToSearch = [listCourtCategories[0].categoryId];
        }

        console.log("Categories to search (timestamp):", categoriesToSearch);

        const requestBody = {
          name: searchFacility.searchText || "",
          type: categoriesToSearch,
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
        let categoriesToSearch = [];
        if (searchFacility.categoryId) {
          if (Array.isArray(searchFacility.categoryId)) {
            categoriesToSearch = searchFacility.categoryId.map((id) =>
              parseInt(id)
            );
          } else {
            categoriesToSearch = [parseInt(searchFacility.categoryId)];
          }
        } else if (selectedCategories.length > 0) {
          categoriesToSearch = selectedCategories;
        } else if (listCourtCategories.length > 0) {
          categoriesToSearch = [listCourtCategories[0].categoryId];
        }

        console.log("Initial search with categories:", categoriesToSearch);

        const requestBody = {
          name: searchFacility.searchText || "",
          type: categoriesToSearch,
          city: searchFacility.province || "",
          ward: searchFacility.district || "",
          order: parseInt(sortOrder),
        };

        console.log("Initial search request body:", requestBody);
        fetchFacilities(1, requestBody);
        initialSearchDone.current = true;
      }
    }
  }, [searchFacility, listCourtCategories, sortOrder, dispatch]); // 🎯 Removed selectedCategories from deps

  // 🎯 Filter provinces based on search text
  const filteredProvinces = provinces.filter((province) =>
    province.name.toLowerCase().includes(provinceFilter.toLowerCase())
  );

  // 🎯 Filter districts based on search text
  const filteredDistricts = districts.filter((district) =>
    district.name.toLowerCase().includes(districtFilter.toLowerCase())
  );

  // 🎯 Filter categories based on search text and exclude selected ones
  const filteredCategories = listCourtCategories.filter(
    (category) =>
      category.categoryName
        .toLowerCase()
        .includes(categoryFilter.toLowerCase()) &&
      !selectedCategories.includes(category.categoryId)
  );

  // 🎯 Handle province selection
  const handleProvinceSelect = (provinceName) => {
    setSelectedProvince(provinceName);
    setProvinceFilter(provinceName);
    setShowProvinceDropdown(false);

    // Reset district selection when province changes
    setSelectedDistrict("");
    setDistrictFilter("");
  };

  // 🎯 Handle district selection
  const handleDistrictSelect = (districtName) => {
    setSelectedDistrict(districtName);
    setDistrictFilter(districtName);
    setShowDistrictDropdown(false);
  };

  // 🎯 Handle category selection
  const handleCategorySelect = (categoryId) => {
    if (!selectedCategories.includes(categoryId)) {
      setSelectedCategories([...selectedCategories, categoryId]);
    }
    setCategoryFilter("");
    setShowCategoryDropdown(false);
  };

  // 🎯 Remove category tag
  const removeCategoryTag = (categoryId) => {
    setSelectedCategories(selectedCategories.filter((id) => id !== categoryId));
  };

  // 🎯 Clear province selection
  const clearProvinceSelection = (e) => {
    e.stopPropagation();
    setSelectedProvince("");
    setProvinceFilter("");
    setSelectedDistrict("");
    setDistrictFilter("");
    setShowProvinceDropdown(false);
  };

  // 🎯 Clear district selection
  const clearDistrictSelection = (e) => {
    e.stopPropagation();
    setSelectedDistrict("");
    setDistrictFilter("");
    setShowDistrictDropdown(false);
  };

  // 🎯 Handle province filter change
  const handleProvinceFilterChange = (event) => {
    const value = event.target.value;
    setProvinceFilter(value);

    // If exact match found, select it
    const exactMatch = provinces.find(
      (p) => p.name.toLowerCase() === value.toLowerCase()
    );
    if (exactMatch && value !== selectedProvince) {
      setSelectedProvince(exactMatch.name);
      // Reset district when province changes
      setSelectedDistrict("");
      setDistrictFilter("");
    } else if (!exactMatch && selectedProvince) {
      // Clear selection if no exact match and something was previously selected
      setSelectedProvince("");
      setSelectedDistrict("");
      setDistrictFilter("");
    }
  };

  // 🎯 Handle district filter change
  const handleDistrictFilterChange = (event) => {
    const value = event.target.value;
    setDistrictFilter(value);

    // If exact match found, select it
    const exactMatch = districts.find(
      (d) => d.name.toLowerCase() === value.toLowerCase()
    );
    if (exactMatch) {
      setSelectedDistrict(exactMatch.name);
    } else if (!exactMatch && selectedDistrict) {
      // Clear selection if no exact match and something was previously selected
      setSelectedDistrict("");
    }
  };

  // 🎯 Handle category filter change
  const handleCategoryFilterChange = (event) => {
    setCategoryFilter(event.target.value);
  };

  // 🎯 Get category name by ID với better debugging
  const getCategoryNameById = (categoryId) => {
    console.log(
      `getCategoryNameById called with ID: ${categoryId} (type: ${typeof categoryId})`
    );
    console.log(
      "Available categories:",
      listCourtCategories.map((cat) => `${cat.categoryId}: ${cat.categoryName}`)
    );

    // Nếu listCourtCategories chưa load xong, return loading text thay vì "Unknown"
    if (listCourtCategories.length === 0) {
      return "Đang tải...";
    }

    // 🎯 Ensure categoryId is integer for comparison
    const categoryIdInt = parseInt(categoryId);
    const category = listCourtCategories.find(
      (cat) => cat.categoryId === categoryIdInt
    );

    const result = category ? category.categoryName : "Unknown";
    console.log(`Found category: ${result}`);
    return result;
  };

  // 🎯 Cập nhật hàm renderCategoryTags với better debugging
  const renderCategoryTags = () => {
    console.log("=== RENDERING CATEGORY TAGS ===");
    console.log("listCourtCategories.length:", listCourtCategories.length);
    console.log("selectedCategories:", selectedCategories);
    console.log("searchFacility.categoryId:", searchFacility?.categoryId);

    if (listCourtCategories.length === 0) {
      console.log("Categories not loaded yet, showing loading state");
      return (
        <div className="category-tags">
          <span className="category-tag loading">Đang tải...</span>
        </div>
      );
    }

    if (selectedCategories.length === 0) {
      console.log("No categories selected, showing nothing");
      return null;
    }

    console.log("Rendering", selectedCategories.length, "category tags");
    return (
      <div className="category-tags">
        {selectedCategories.map((categoryId) => {
          const categoryName = getCategoryNameById(categoryId);
          console.log(
            `Category ID ${categoryId} (type: ${typeof categoryId}) -> Name: ${categoryName}`
          );
          return (
            <span key={categoryId} className="category-tag">
              {categoryName}
              <button
                type="button"
                className="tag-remove"
                onClick={() => removeCategoryTag(categoryId)}
                title="Xóa"
                disabled={categoryName === "Đang tải..."}
              >
                <i className="fas fa-times"></i>
              </button>
            </span>
          );
        })}
      </div>
    );
  };

  // Handle search
  const handleSearch = () => {
    console.log("=== HANDLING SEARCH ===");
    console.log("Selected categories:", selectedCategories);

    // Update searchParams with latest values
    const searchParams = {
      searchText: searchText,
      categoryId: selectedCategories,
      province: selectedProvince,
      district: selectedDistrict,
    };

    console.log("Search params:", searchParams);

    // Update Redux state with new search params
    dispatch(setSearchFacility(searchParams));

    // Create request body for API call
    const requestBody = {
      name: searchText,
      type: selectedCategories,
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
      const requestBody = customRequestBody || {
        name: searchText,
        type: selectedCategories,
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
    setProvinceFilter("");
    setDistrictFilter("");
    setCategoryFilter("");
    setSortOrder("3");
    setCurrentPage(1);

    // Set selectedCategories to first category
    const firstCategoryId =
      listCourtCategories.length > 0 ? listCourtCategories[0].categoryId : null;

    console.log("Reset to first category:", firstCategoryId);
    setSelectedCategories(firstCategoryId ? [firstCategoryId] : []);

    // Reset Redux state with first category
    dispatch(
      setSearchFacility({
        searchText: "",
        categoryId: firstCategoryId ? [firstCategoryId] : [],
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

              {/* 🎯 Custom Province Dropdown */}
              <div className="filter-section">
                <h5 className="filter-title">Tỉnh/Thành phố</h5>
                <div className="custom-dropdown" ref={provinceDropdownRef}>
                  <div className="custom-select-wrapper">
                    <div className="input-with-clear">
                      <Form.Control
                        type="text"
                        placeholder="Chọn tỉnh/thành phố"
                        value={provinceFilter}
                        onChange={handleProvinceFilterChange}
                        onFocus={() => setShowProvinceDropdown(true)}
                        className="custom-select-input"
                      />
                      {selectedProvince && (
                        <button
                          type="button"
                          className="clear-button"
                          onClick={clearProvinceSelection}
                          title="Xóa lựa chọn"
                        >
                          <i className="fas fa-times"></i>
                        </button>
                      )}
                    </div>
                    {showProvinceDropdown && (
                      <div className="custom-dropdown-menu">
                        {filteredProvinces.length > 0 ? (
                          filteredProvinces.map((province) => (
                            <div
                              key={province.code}
                              className={`custom-dropdown-item ${
                                selectedProvince === province.name
                                  ? "selected"
                                  : ""
                              }`}
                              onClick={() =>
                                handleProvinceSelect(province.name)
                              }
                            >
                              {province.name}
                            </div>
                          ))
                        ) : (
                          <div className="custom-dropdown-item no-results">
                            Không tìm thấy tỉnh/thành phố
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* 🎯 Custom District Dropdown */}
              <div className="filter-section">
                <h5 className="filter-title">Quận/Huyện</h5>
                <div className="custom-dropdown" ref={districtDropdownRef}>
                  <div className="custom-select-wrapper">
                    <div className="input-with-clear">
                      <Form.Control
                        type="text"
                        placeholder="Chọn quận/huyện"
                        value={districtFilter}
                        onChange={handleDistrictFilterChange}
                        onFocus={() => setShowDistrictDropdown(true)}
                        disabled={!selectedProvince || districts.length === 0}
                        className="custom-select-input"
                      />
                      {selectedDistrict && (
                        <button
                          type="button"
                          className="clear-button"
                          onClick={clearDistrictSelection}
                          title="Xóa lựa chọn"
                        >
                          <i className="fas fa-times"></i>
                        </button>
                      )}
                    </div>
                    {showDistrictDropdown && selectedProvince && (
                      <div className="custom-dropdown-menu">
                        {filteredDistricts.length > 0 ? (
                          filteredDistricts.map((district) => (
                            <div
                              key={district.code}
                              className={`custom-dropdown-item ${
                                selectedDistrict === district.name
                                  ? "selected"
                                  : ""
                              }`}
                              onClick={() =>
                                handleDistrictSelect(district.name)
                              }
                            >
                              {district.name}
                            </div>
                          ))
                        ) : (
                          <div className="custom-dropdown-item no-results">
                            Không tìm thấy quận/huyện
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* 🎯 Custom Category Dropdown with Tags */}
              <div className="filter-section">
                <h5 className="filter-title">Loại sân</h5>

                {/* Selected Categories Tags - use render function */}
                {renderCategoryTags()}

                <div className="custom-dropdown" ref={categoryDropdownRef}>
                  <div className="custom-select-wrapper">
                    <Form.Control
                      type="text"
                      placeholder={
                        listCourtCategories.length > 0
                          ? "Thêm loại sân..."
                          : "Đang tải loại sân..."
                      }
                      value={categoryFilter}
                      onChange={handleCategoryFilterChange}
                      onFocus={() => setShowCategoryDropdown(true)}
                      className="custom-select-input"
                      disabled={listCourtCategories.length === 0}
                    />
                    {showCategoryDropdown && (
                      <div className="custom-dropdown-menu">
                        {listCourtCategories.length === 0 ? (
                          <div className="custom-dropdown-item no-results">
                            Đang tải danh sách loại sân...
                          </div>
                        ) : filteredCategories.length > 0 ? (
                          filteredCategories.map((category) => (
                            <div
                              key={category.categoryId}
                              className="custom-dropdown-item"
                              onClick={() =>
                                handleCategorySelect(category.categoryId)
                              }
                            >
                              {category.categoryName}
                            </div>
                          ))
                        ) : (
                          <div className="custom-dropdown-item no-results">
                            {selectedCategories.length ===
                            listCourtCategories.length
                              ? "Đã chọn tất cả loại sân"
                              : "Không tìm thấy loại sân"}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* 🎯 Sắp xếp */}
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

              {/* 🎯 Tìm kiếm (moved down) */}
              <div className="filter-section search-section">
                <h5 className="filter-title">Tìm kiếm</h5>
                <Form.Control
                  className="search-input secondary-search"
                  type="text"
                  placeholder="Tìm theo tên cơ sở"
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                />
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
                  <div className="error-content">
                    <div className="error-icon"></div>
                    <h5>Không tìm thấy kết quả</h5>
                    <p className="error-description">
                      Không tìm thấy cơ sở nào phù hợp với tiêu chí tìm kiếm của
                      bạn. Hãy thử điều chỉnh bộ lọc để có kết quả tốt hơn.
                    </p>
                  </div>
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
                              navigate(
                                `/facility-details/${facility.facilityId}`
                              )
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
