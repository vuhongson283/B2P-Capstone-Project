import Form from "react-bootstrap/Form";
import "./SearchField.scss";
import { getAllCourtCategories } from "../../services/apiService";
import { useState, useEffect } from "react";

const SearchField = (props) => {
  const [listCourtCategories, setListCourtCategories] = useState([]);
  const [selectedCategory, setSelectedCategory] = useState("");
  const [searchText, setSearchText] = useState("");

  // State cho tỉnh/thành phố và quận/huyện
  const [provinces, setProvinces] = useState([]);
  const [districts, setDistricts] = useState([]);
  const [selectedProvince, setSelectedProvince] = useState("");
  const [selectedDistrict, setSelectedDistrict] = useState("");

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

      // Set giá trị mặc định là tỉnh đầu tiên - sử dụng name thay vì code
      if (data && data.length > 0) {
        setSelectedProvince(data[0].name);
      }
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

      // Set giá trị mặc định là quận/huyện đầu tiên - sử dụng name
      if (districtList.length > 0) {
        setSelectedDistrict(districtList[0].name);
      } else {
        setSelectedDistrict("");
      }
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
    }
  }, [selectedProvince, provinces]); // Thêm provinces vào dependency

  const handleSearchChange = (event) => {
    setSearchText(event.target.value);
  };

  const handleCategoryChange = (event) => {
    setSelectedCategory(event.target.value);
  };

  const handleProvinceChange = (event) => {
    setSelectedProvince(event.target.value);
  };

  const handleDistrictChange = (event) => {
    setSelectedDistrict(event.target.value);
  };

  // Hàm xử lý tìm kiếm
  const handleSearch = () => {
    const searchParams = {
      searchText,
      categoryId: selectedCategory,
      province: selectedProvince,
      district: selectedDistrict,
    };

    console.log("Search params:", searchParams);

    // Gọi API tìm kiếm hoặc truyền data lên component cha
    if (props.onSearch) {
      props.onSearch(searchParams);
    }
  };

  return (
    <div className="search-field">
      <h3 className="title">Book2Play - Đặt sân ngay </h3>
      <div className="search-input">
        <Form.Control
          value={searchText}
          onChange={handleSearchChange}
          size="md"
          type="text"
          placeholder="🔎 Nhập tên sân..."
        />
        <br />
      </div>
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

        <div className="d-flex">
          <i className="fas fa-city me-1"></i>
          <Form.Select
            aria-label="Select province"
            value={selectedProvince}
            onChange={handleProvinceChange}
          >
            {provinces.map((province) => (
              <option key={province.code} value={province.name}>
                {province.name}
              </option>
            ))}
          </Form.Select>
        </div>

        <div className="d-flex">
          <i className="fas fa-map-marker-alt me-1"></i>
          <Form.Select
            aria-label="Select district"
            value={selectedDistrict}
            onChange={handleDistrictChange}
            disabled={!selectedProvince || districts.length === 0}
          >
            {districts.map((district) => (
              <option key={district.code} value={district.name}>
                {district.name}
              </option>
            ))}
          </Form.Select>
        </div>
      </div>
      <button className="btn-search btn btn-success" onClick={handleSearch}>
        Tìm kiếm
      </button>
    </div>
  );
};

export default SearchField;
