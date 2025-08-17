import Form from "react-bootstrap/Form";
import "./SearchField.scss";
import { getAllCourtCategories } from "../../services/apiService";
import { useState, useEffect, useRef } from "react";
import { useDispatch } from "react-redux";
import { setSearchFacility } from "../../store/action/searchFacilityAction";
import { useNavigate } from "react-router-dom";

const SearchField = (props) => {
  const dispatch = useDispatch();
  const navigate = useNavigate();

  // State cho danh sách loại sân, loại sân được chọn, và ô tìm kiếm
  const [listCourtCategories, setListCourtCategories] = useState([]);
  const [selectedCategory, setSelectedCategory] = useState("");
  const [searchText, setSearchText] = useState("");

  // State cho tỉnh/thành phố và quận/huyện
  const [provinces, setProvinces] = useState([]);
  const [districts, setDistricts] = useState([]);
  const [selectedProvince, setSelectedProvince] = useState("");
  const [selectedDistrict, setSelectedDistrict] = useState("");

  // State cho filter dropdown
  const [provinceFilter, setProvinceFilter] = useState("");
  const [districtFilter, setDistrictFilter] = useState("");
  const [showProvinceDropdown, setShowProvinceDropdown] = useState(false);
  const [showDistrictDropdown, setShowDistrictDropdown] = useState(false);

  // Refs cho click outside
  const provinceDropdownRef = useRef(null);
  const districtDropdownRef = useRef(null);

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

      // Set giá trị mặc định là phần tử đầu tiên
      if (categories && categories.length > 0) {
        setSelectedCategory(categories[0].categoryId);
      }
    } catch (error) {
      console.error("Error fetching court categories:", error);
    }
  };

  // Lấy danh sách tỉnh/thành phố
  const fetchProvinces = async () => {
    try {
      const response = await fetch("https://provinces.open-api.vn/api/p/");
      const data = await response.json();
      setProvinces(data);
    } catch (error) {
      console.error("Error fetching provinces:", error);
    }
  };

  // Lấy danh sách quận/huyện theo tỉnh được chọn
  const fetchDistricts = async (provinceName) => {
    if (!provinceName) return;

    try {
      // Tìm province code từ name
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

      // Không auto chọn quận/huyện đầu tiên nữa
      setSelectedDistrict("");
      setDistrictFilter("");
    } catch (error) {
      console.error("Error fetching districts:", error);
    }
  };

  useEffect(() => {
    fetchCourtCategories();
    fetchProvinces();
  }, []);

  // Fetch districts khi selectedProvince thay đổi
  useEffect(() => {
    if (selectedProvince && provinces.length > 0) {
      fetchDistricts(selectedProvince);
    } else {
      setDistricts([]);
      setSelectedDistrict("");
      setDistrictFilter("");
    }
  }, [selectedProvince, provinces]);

  // Handle click outside để đóng dropdown
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
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  const handleSearchChange = (event) => {
    setSearchText(event.target.value);
  };

  const handleCategoryChange = (event) => {
    setSelectedCategory(event.target.value);
  };

  // Filter provinces based on search text
  const filteredProvinces = provinces.filter((province) =>
    province.name.toLowerCase().includes(provinceFilter.toLowerCase())
  );

  // Filter districts based on search text
  const filteredDistricts = districts.filter((district) =>
    district.name.toLowerCase().includes(districtFilter.toLowerCase())
  );

  // Handle province selection
  const handleProvinceSelect = (provinceName) => {
    setSelectedProvince(provinceName);
    setProvinceFilter(provinceName);
    setShowProvinceDropdown(false);

    // Reset district selection when province changes
    setSelectedDistrict("");
    setDistrictFilter("");
  };

  // Handle district selection
  const handleDistrictSelect = (districtName) => {
    setSelectedDistrict(districtName);
    setDistrictFilter(districtName);
    setShowDistrictDropdown(false);
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

  // Handle province filter change
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

  // Handle district filter change
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

  const handleSearch = () => {
    const searchParams = {
      searchText,
      categoryId: [parseInt(selectedCategory)], // 🎯 Đảm bảo là integer trong array
      province: selectedProvince,
      district: selectedDistrict,
      timestamp: Date.now(), // 🎯 Thêm timestamp để force refresh
    };

    console.log("=== SEARCHFIELD DISPATCHING ===");
    console.log("selectedCategory (raw):", selectedCategory);
    console.log("selectedCategory (parsed):", parseInt(selectedCategory));
    console.log("searchParams:", searchParams);

    dispatch(setSearchFacility(searchParams));

    // Gọi API tìm kiếm hoặc truyền data lên component cha
    navigate("/search");
  };

  return (
    <div className="search-field">
      <h3 className="title">Book2Play - Đặt sân ngay </h3>

      {/* 🎯 Moved main filter fields up */}
      <div className="search-category">
        <div className="d-flex">
          <i className="fas fa-futbol me-1"></i>
          <Form.Select
            aria-label="Select court category"
            value={selectedCategory}
            onChange={handleCategoryChange}
          >
            {listCourtCategories.map((category) => (
              <option
                key={"categories" + category.categoryId}
                value={category.categoryId}
              >
                {category.categoryName}
              </option>
            ))}
          </Form.Select>
        </div>

        {/* Custom Province Dropdown */}
        <div className="d-flex custom-dropdown" ref={provinceDropdownRef}>
          <i className="fas fa-city me-1"></i>
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
              {/* 🎯 Clear button for province */}
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
                        selectedProvince === province.name ? "selected" : ""
                      }`}
                      onClick={() => handleProvinceSelect(province.name)}
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

        {/* Custom District Dropdown */}
        <div className="d-flex custom-dropdown" ref={districtDropdownRef}>
          <i className="fas fa-map-marker-alt me-1"></i>
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
              {/* 🎯 Clear button for district */}
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
                        selectedDistrict === district.name ? "selected" : ""
                      }`}
                      onClick={() => handleDistrictSelect(district.name)}
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

      {/* 🎯 Moved search input down and made it less prominent */}
      <div className="search-input secondary">
        <Form.Control
          value={searchText}
          onChange={handleSearchChange}
          size="sm"
          type="text"
          placeholder="Tìm theo cơ sở..."
          className="secondary-input"
        />
      </div>

      <button className="btn-search btn btn-success" onClick={handleSearch}>
        Tìm kiếm
      </button>
    </div>
  );
};

export default SearchField;
