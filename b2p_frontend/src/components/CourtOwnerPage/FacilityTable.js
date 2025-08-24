import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import {
  Table,
  Button,
  Modal,
  Form,
  Input,
  message,
  Tooltip,
  Typography,
  Select,
  Upload,
  Popconfirm,
  Image as AntdImage,
  Card,
  Row,
  Col,
  Tag,
} from "antd";
import {
  SearchOutlined,
  EditOutlined,
  PlusOutlined,
  UploadOutlined,
  DeleteOutlined,
  EyeOutlined,
} from "@ant-design/icons";
import {
  getFacilitiesByCourtOwnerId,
  createFacility,
  uploadFacilityImages,
  deleteFacilityImage,
  getFacilityById,
  updateFacility,
  deleteFacility,
} from "../../services/apiService";
import "./FacilityTable.scss";

const { Text } = Typography;

// THAY ĐỔI 1: Sửa convertGoogleDriveUrl - thêm extract file ID từ /d/ format
const convertGoogleDriveUrl = (originalUrl) => {
  if (!originalUrl) return "/src/assets/images/default.jpg";
  if (originalUrl.includes("thumbnail")) return originalUrl;
  if (originalUrl.includes("/assets/")) return originalUrl;

  // THÊM: Extract từ format /d/FILE_ID/
  let fileId = originalUrl.match(/\/d\/([a-zA-Z0-9_-]+)/)?.[1];

  // Nếu không có thì thử format ?id=
  if (!fileId) {
    fileId = originalUrl.match(/[?&]id=([a-zA-Z0-9_-]+)/)?.[1];
  }

  if (fileId) {
    // THAY ĐỔI: Dùng googleusercontent.com thay vì drive.google.com
    return `https://lh3.googleusercontent.com/d/${fileId}=w300-h200-c`;
  }

  return "/src/assets/images/default.jpg";
};

const cleanAddressForDisplay = (address) => {
  if (!address) return "";
  return address.replace(/\$\$/g, "");
};
const handleImageError = (e, originalSrc, retryCount = 0) => {
  const img = e.target;

  if (retryCount >= 4) {
    // Tăng lên 4 để có thêm 1 lần thử
    img.src = "/src/assets/images/default.jpg";
    return;
  }

  const fileId =
    originalSrc.match(/\/d\/([a-zA-Z0-9_-]+)/)?.[1] ||
    originalSrc.match(/[?&]id=([a-zA-Z0-9_-]+)/)?.[1];

  if (!fileId) {
    img.src = "/src/assets/images/default.jpg";
    return;
  }

  // THAY ĐỔI: Thêm googleusercontent.com vào đầu list
  const formats = [
    `https://lh3.googleusercontent.com/d/${fileId}=w300-h200-c`, // THÊM format này
    `https://drive.google.com/uc?export=view&id=${fileId}`,
    `https://drive.google.com/thumbnail?id=${fileId}&sz=w300-h200`,
    `https://docs.google.com/uc?export=download&id=${fileId}`,
  ];

  if (formats[retryCount]) {
    setTimeout(() => {
      img.src = formats[retryCount];
      img.onerror = (e2) => handleImageError(e2, originalSrc, retryCount + 1);
    }, 500);
  } else {
    img.src = "/src/assets/images/default.jpg";
  }
};
const FacilityTable = () => {
  useEffect(() => {
    document.title = "Thông tin cơ sở - B2P";
  }, []);
  const navigate = useNavigate();
  const { Option } = Select;
  const { userId, isLoggedIn, isLoading: authLoading } = useAuth();

  // ✅ FIX: Tạo hàm getCourtOwnerId không dependency vào state
  const getCourtOwnerId = useCallback(() => {
    console.log(
      "🔍 Getting court owner ID - isLoggedIn:",
      isLoggedIn,
      "userId:",
      userId
    );

    if (isLoggedIn && userId) {
      return userId;
    }

    // ✅ Không có fallback - return null khi chưa đăng nhập
    console.warn("⚠️ Court owner not logged in");
    return null;
  }, [isLoggedIn, userId]);
  // ✅ STATES CHO ĐỊA CHỈ API
  const [provinces, setProvinces] = useState([]);
  const [districts, setDistricts] = useState([]);

  // State cho modal thêm mới
  const [selectedProvince, setSelectedProvince] = useState("");
  const [selectedDistrict, setSelectedDistrict] = useState("");

  // State cho modal chỉnh sửa
  const [editSelectedProvince, setEditSelectedProvince] = useState("");
  const [editSelectedDistrict, setEditSelectedDistrict] = useState("");
  const [forceUpdate, setForceUpdate] = useState(0);

  // ... existing states ...
  const [facilityImages, setFacilityImages] = useState([]);
  const [uploadFileList, setUploadFileList] = useState([]);
  const [modalVisible, setModalVisible] = useState(false);
  const [facilities, setFacilities] = useState([]);
  const [loading, setLoading] = useState(false);
  const [editModalVisible, setEditModalVisible] = useState(false);
  const [editLoading, setEditLoading] = useState(false);
  const [editingFacility, setEditingFacility] = useState(null);
  const [editForm] = Form.useForm();
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState(null);
  const [hasInitialized, setHasInitialized] = useState(false);
  const [submitLoading, setSubmitLoading] = useState(false);
  const [form] = Form.useForm();
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [selectedImages, setSelectedImages] = useState([]);
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 3,
    total: 0,
  });

  // ✅ FIX: Tối ưu fetchFacilities với dependencies rõ ràng
  const fetchFacilities = useCallback(
    async (page = 1, pageSize = 3, searchQuery = "", status = null) => {
      try {
        setLoading(true);

        // ✅ FIX: Kiểm tra auth loading trước
        if (authLoading) {
          console.log("⏳ Auth is still loading, skipping fetch...");
          return;
        }

        // ✅ FIX: Lấy courtOwnerId từ callback
        const courtOwnerId = getCourtOwnerId();

        if (!courtOwnerId) {
          console.error("❌ No valid court owner ID, user not authenticated");
          message.error(
            "Người dùng chưa đăng nhập hoặc không có quyền truy cập"
          );
          setFacilities([]);
          return;
        }

        console.log("🚀 Fetching facilities for courtOwnerId:", courtOwnerId, {
          page,
          pageSize,
          searchQuery,
          status,
        });

        const response = await getFacilitiesByCourtOwnerId(
          courtOwnerId,
          searchQuery,
          status,
          page,
          pageSize
        );

        let success, payload;
        if (response?.data?.success !== undefined) {
          success = response.data.success;
          payload = response.data.data;
        } else if (response?.success !== undefined) {
          success = response.success;
          payload = response.data;
        } else if (response?.data) {
          success = true;
          payload = response.data;
        } else {
          success = false;
          payload = null;
        }

        if (success && payload && payload.items) {
          const { items, totalItems, currentPage, itemsPerPage } = payload;

          const mappedFacilities = items.map((facility) => ({
            key: facility.facilityId,
            id: facility.facilityId,
            name: facility.facilityName,
            address: cleanAddressForDisplay(facility.location),
            courtCount: facility.courtCount,
            status: facility.status,
            image:
              facility.images?.length > 0
                ? convertGoogleDriveUrl(facility.images[0].imageUrl)
                : "https://placehold.co/300x200?text=No+Image",
          }));

          setFacilities(mappedFacilities);
          setTimeout(() => setForceUpdate((prev) => prev + 1), 100);
          setPagination((prev) => ({
            ...prev,
            current: currentPage,
            pageSize: itemsPerPage,
            total: totalItems,
          }));

          console.log(
            "✅ Facilities loaded successfully:",
            mappedFacilities.length,
            "items"
          );
        } else {
          console.log("❌ No facilities data or failed response");
          setFacilities([]);
        }
      } catch (error) {
        console.error("💥 Error fetching facilities:", error);
        setFacilities([]);
      } finally {
        setLoading(false);
      }
    },
    [authLoading, getCourtOwnerId]
  );

  // ✅ FIX: useEffect chính để fetch dữ liệu khi auth sẵn sàng
  useEffect(() => {
    console.log("🔄 Auth state changed:", {
      authLoading,
      isLoggedIn,
      userId,
      hasInitialized,
    });

    // Chỉ fetch khi:
    // 1. Auth không loading
    // 2. User đã login
    // 3. Có userId
    // 4. Chưa initialize
    if (!authLoading && isLoggedIn && userId && !hasInitialized) {
      console.log("🚀 Conditions met, fetching facilities...");
      fetchFacilities(1, 3, "", null);
      setHasInitialized(true);
    }
  }, [authLoading, isLoggedIn, userId, hasInitialized, fetchFacilities]);

  // ✅ FIX: Reset hasInitialized khi user thay đổi
  useEffect(() => {
    if (!authLoading && (!isLoggedIn || !userId)) {
      console.log("🔄 User logged out or changed, resetting...");
      setHasInitialized(false);
      setFacilities([]);
    }
  }, [authLoading, isLoggedIn, userId]);

  // ✅ FETCH PROVINCES
  const fetchProvinces = async () => {
    try {
      console.log("🌍 Fetching provinces...");
      const response = await fetch("https://provinces.open-api.vn/api/p/");
      const data = await response.json();
      console.log("✅ Provinces loaded:", data.length, "provinces");
      setProvinces(data);
    } catch (error) {
      console.error("💥 Error fetching provinces:", error);
      message.error("Không thể tải danh sách tỉnh/thành phố");
    }
  };

  // ✅ FETCH DISTRICTS
  const fetchDistricts = async (provinceName) => {
    if (!provinceName) {
      setDistricts([]);
      return;
    }

    try {
      console.log("🏘️ Fetching districts for:", provinceName);
      const selectedProvinceObj = provinces.find(
        (p) => p.name === provinceName
      );
      if (!selectedProvinceObj) {
        console.error("❌ Province not found:", provinceName);
        return;
      }

      const response = await fetch(
        `https://provinces.open-api.vn/api/p/${selectedProvinceObj.code}?depth=2`
      );
      const data = await response.json();
      const districtList = data.districts || [];
      console.log("✅ Districts loaded:", districtList.length, "districts");
      setDistricts(districtList);
    } catch (error) {
      console.error("💥 Error fetching districts:", error);
      message.error("Không thể tải danh sách quận/huyện");
    }
  };

  // ✅ PARSE ĐỊA CHỈ (chỉ có phần chi tiết)
  const parseAddress = (fullAddress) => {
    if (!fullAddress) {
      return { detail: "", district: "", province: "" };
    }

    console.log("🔍 Parsing address:", fullAddress);

    // Tách theo dấu phẩy
    const parts = fullAddress.split(", ");
    console.log("🔍 Address parts:", parts);

    if (parts.length >= 3) {
      // Format đầy đủ: "chi tiết, $$huyện, tỉnh"
      const district = parts[1].trim().replace(/^\$\$/, "");
      const result = {
        detail: parts[0].trim(),
        district: district,
        province: parts[2].trim(),
      };
      console.log("✅ Parsed result (3 parts):", result);
      return result;
    } else if (parts.length === 2) {
      // Format: "chi tiết, tỉnh"
      const result = {
        detail: parts[0].trim(),
        district: "",
        province: parts[1].trim(),
      };
      console.log("✅ Parsed result (2 parts):", result);
      return result;
    } else {
      // ✅ CHỈ CÓ ĐỊA CHỈ CHI TIẾT (database hiện tại)
      const result = {
        detail: fullAddress.trim(),
        district: "",
        province: "",
      };
      console.log("✅ Parsed result (detail only):", result);
      return result;
    }
  };

  // ✅ BUILD ĐỊA CHỈ
  const buildAddress = (detail, district, province) => {
    const parts = [];

    if (detail && detail.trim()) {
      parts.push(detail.trim());
    }

    if (district && district.trim()) {
      parts.push("$$" + district.trim());
    }

    if (province && province.trim()) {
      parts.push(province.trim());
    }

    const result = parts.join(", ");
    console.log("🏗️ Built address:", result);
    return result;
  };

  useEffect(() => {
    console.log("🔄 Provinces state changed:", provinces.length);
    if (provinces.length > 0) {
      console.log("✅ Provinces available:", provinces.slice(0, 3));
    }
  }, [provinces]);

  // ✅ LOAD PROVINCES KHI COMPONENT MOUNT
  useEffect(() => {
    fetchProvinces();
  }, []);

  // ✅ HANDLE PROVINCE CHANGE - MODAL THÊM MỚI
  useEffect(() => {
    if (selectedProvince && provinces.length > 0) {
      fetchDistricts(selectedProvince);
      setSelectedDistrict(""); // Reset district
    }
  }, [selectedProvince, provinces]);

  // ✅ HANDLE PROVINCE CHANGE - MODAL CHỈNH SỬA
  useEffect(() => {
    if (editSelectedProvince && provinces.length > 0) {
      fetchDistricts(editSelectedProvince);
      setEditSelectedDistrict(""); // Reset district
    }
  }, [editSelectedProvince, provinces]);

  // ✅ FETCH FACILITY BY ID
  const fetchFacilityById = async (facilityId) => {
    try {
      setEditLoading(true);
      console.log("🔍 Fetching facility by ID:", facilityId);

      const response = await getFacilityById(facilityId);
      console.log("📡 Get facility by ID response:", response);

      let success, data;
      if (response?.data?.success !== undefined) {
        success = response.data.success;
        data = response.data.data;
      } else if (response?.success !== undefined) {
        success = response.success;
        data = response.data;
      } else {
        success = false;
        data = null;
      }

      if (success && data) {
        console.log("✅ Facility data loaded:", data);
        console.log("📍 Original address from DB:", data.location);

        setEditingFacility(data);

        // ✅ PARSE ĐỊA CHỈ
        const addressParts = parseAddress(data.location);
        console.log("📍 Parsed address parts:", addressParts);

        // Set địa chỉ cho modal edit
        setEditSelectedProvince(addressParts.province);

        // Load districts nếu có province
        if (addressParts.province) {
          await fetchDistricts(addressParts.province);
          setTimeout(() => {
            setEditSelectedDistrict(addressParts.district);
            console.log("📍 Set district to:", addressParts.district);
          }, 500);
        }

        // Load images
        const convertedImages = (data.images || []).map((image) => ({
          ...image,
          imageUrl: convertGoogleDriveUrl(image.imageUrl),
        }));
        setFacilityImages(convertedImages);
        setUploadFileList([]);
        console.log(
          "🖼️ Facility images loaded:",
          convertedImages.length,
          "images"
        );

        // Set form values
        editForm.setFieldsValue({
          facilityName: data.facilityName,
          detailAddress: addressParts.detail,
          contact: data.contact,
          statusId: data.statusId,
        });

        setEditModalVisible(true);
      } else {
        message.error("Không thể tải thông tin cơ sở");
      }
    } catch (error) {
      console.error("💥 Error fetching facility:", error);
      message.error(
        `Lỗi khi tải thông tin cơ sở: ${
          error.response?.data?.message || error.message
        }`
      );
    } finally {
      setEditLoading(false);
    }
  };

  // ✅ HANDLE DELETE IMAGE
  const handleDeleteImage = async (imageId, imageName) => {
    Modal.confirm({
      title: "Xác nhận xóa ảnh",
      content: `Bạn có chắc chắn muốn xóa ảnh "${imageName || "này"}"?`,
      okText: "Xóa",
      cancelText: "Hủy",
      okType: "danger",
      onOk: async () => {
        try {
          console.log("🗑️ Deleting image ID:", imageId);
          const response = await deleteFacilityImage(imageId);
          console.log("✅ Delete image response:", response);

          let success = false;
          if (response?.message === "Image deleted successfully") {
            success = true;
          } else if (response?.data?.message === "Image deleted successfully") {
            success = true;
          } else if (response?.success === true) {
            success = true;
          } else if (
            !response?.message?.includes("not found") &&
            !response?.message?.includes("failed")
          ) {
            success = true;
          }

          if (success) {
            setFacilityImages((prev) =>
              prev.filter((img) => img.imageId !== imageId)
            );
            message.success("Xóa ảnh thành công");
          } else {
            message.error(
              response?.message || response?.data?.message || "Xóa ảnh thất bại"
            );
          }
        } catch (error) {
          console.error("💥 Error deleting image:", error);
          message.error(
            `Xóa ảnh thất bại: ${
              error.response?.data?.message || error.message
            }`
          );
        }
      },
    });
  };

  // ✅ HANDLE PREVIEW IMAGE
  const handlePreviewImage = (imageUrl, caption) => {
    let previewUrl = imageUrl;
    if (imageUrl.includes("thumbnail")) {
      previewUrl = imageUrl.replace("thumbnail", "uc");
    }

    Modal.info({
      title: caption || "Xem ảnh",
      content: (
        <div style={{ textAlign: "center" }}>
          <img
            src={previewUrl}
            alt="Preview"
            style={{
              maxWidth: "100%",
              maxHeight: "500px",
              objectFit: "contain",
              borderRadius: "8px",
            }}
            onError={(e) => {
              e.target.src = imageUrl;
            }}
          />
        </div>
      ),
      width: 700,
      okText: "Đóng",
    });
  };

  // ✅ HANDLE UPLOAD CHANGE
  const handleUploadChange = ({ fileList }) => {
    const validFiles = fileList.filter((file) => {
      const isImage = file.type?.startsWith("image/");
      if (!isImage && file.originFileObj) {
        message.error(`${file.name} không phải là file ảnh hợp lệ`);
        return false;
      }
      const isValidSize = !file.size || file.size / 1024 / 1024 < 5;
      if (!isValidSize) {
        message.error(`${file.name} vượt quá 5MB`);
        return false;
      }
      return true;
    });

    const filesWithPreview = validFiles.map((file) => {
      if (!file.url && !file.preview && file.originFileObj) {
        file.preview = URL.createObjectURL(file.originFileObj);
      }
      return file;
    });

    setUploadFileList(filesWithPreview);
    console.log(
      "📁 Upload file list changed:",
      filesWithPreview.length,
      "files"
    );
  };

  // ✅ HANDLE IMAGE CHANGE (cho modal thêm mới)
  const handleImageChange = (info) => {
    const { fileList } = info;
    const validFiles = fileList.filter((file) => {
      const isImage = file.type?.startsWith("image/");
      if (!isImage && file.originFileObj) {
        message.error(`${file.name} không phải là file ảnh hợp lệ`);
        return false;
      }
      const isValidSize = !file.size || file.size / 1024 / 1024 < 5;
      if (!isValidSize) {
        message.error(`${file.name} vượt quá 5MB`);
        return false;
      }
      return true;
    });

    const filesWithPreview = validFiles.map((file) => {
      if (!file.url && !file.preview && file.originFileObj) {
        file.preview = URL.createObjectURL(file.originFileObj);
      }
      return file;
    });

    setSelectedImages(filesWithPreview);
  };

  // ✅ UPLOAD IMAGES
  const uploadImages = async (facilityId, imageFiles) => {
    if (!imageFiles || imageFiles.length === 0) {
      console.log("📸 No images to upload");
      return { success: true, message: "No images to upload" };
    }

    try {
      console.log(
        `📸 Uploading ${imageFiles.length} images for facility ${facilityId}`
      );
      const formData = new FormData();

      imageFiles.forEach((fileObj) => {
        const file = fileObj.originFileObj || fileObj;
        formData.append("files", file);
      });

      formData.append("entityId", facilityId.toString());
      formData.append("caption", "Facility image");

      console.log("📤 FormData prepared:", {
        files: imageFiles.length,
        entityId: facilityId,
      });

      const uploadResponse = await uploadFacilityImages(formData);
      console.log("✅ Upload response:", uploadResponse);

      let responseData;
      if (uploadResponse?.data) {
        responseData = uploadResponse.data;
      } else {
        responseData = uploadResponse;
      }

      const isValidResponse =
        Array.isArray(responseData) &&
        responseData.length > 0 &&
        responseData[0].imageId;

      if (isValidResponse) {
        console.log(
          `✅ Upload successful: ${imageFiles.length} images uploaded`
        );
        return { success: true, data: responseData };
      } else {
        throw new Error("Upload response không hợp lệ");
      }
    } catch (error) {
      console.error("💥 Error uploading images:", error);
      const errorMessage =
        error.response?.data?.message || error.message || "Upload ảnh thất bại";
      message.error(`Upload ảnh thất bại: ${errorMessage}`);
      return { success: false, error: errorMessage };
    }
  };

  // ✅ HANDLE CREATE FACILITY
  const handleCreateFacility = async (values) => {
    try {
      setSubmitLoading(true);
      const courtOwnerId = getCourtOwnerId();

      if (!courtOwnerId) {
        message.error("Không thể xác định người dùng");
        return;
      }

      console.log("📝 Form values:", values);

      // Validate địa chỉ
      if (!selectedProvince || !selectedDistrict || !values.detailAddress) {
        message.error("Vui lòng chọn đầy đủ thông tin địa chỉ");
        return;
      }

      // Build địa chỉ
      const fullAddress = buildAddress(
        values.detailAddress,
        selectedDistrict,
        selectedProvince
      );

      console.log("🏠 Built address:", fullAddress);

      // Validate giờ
      if (values.closeHour <= values.openHour) {
        message.error("Giờ đóng cửa phải sau giờ mở cửa");
        return;
      }

      // Tạo cơ sở
      const facilityData = {
        facilityName: values.facilityName.trim(),
        location: fullAddress,
        contact: values.contact.trim(),
        userId: courtOwnerId,
        statusId: values.statusId,
        openHour: values.openHour,
        closeHour: values.closeHour,
        slotDuration: values.slotDuration,
      };

      console.log("🚀 Step 1: Creating facility...", facilityData);

      const createResponse = await createFacility(facilityData);
      console.log("✅ Create facility response:", createResponse);

      const success = createResponse?.data?.success || createResponse?.success;
      const message_text =
        createResponse?.data?.message || createResponse?.message;

      if (!success) {
        message.error(message_text || "Tạo cơ sở thất bại");
        return;
      }

      // Lấy facilityId
      const facilityId =
        createResponse?.data?.data?.facilityId ||
        createResponse?.data?.facilityId ||
        createResponse?.facilityId;

      console.log("🆔 Created facility ID:", facilityId);

      if (!facilityId) {
        message.error("Không thể lấy ID cơ sở vừa tạo");
        return;
      }

      // Upload ảnh
      let uploadResult = { success: true };
      if (selectedImages && selectedImages.length > 0) {
        console.log("🚀 Step 2: Uploading images...");
        uploadResult = await uploadImages(facilityId, selectedImages);
      }

      // Kết quả
      if (uploadResult.success) {
        message.success(
          selectedImages.length > 0
            ? `Tạo cơ sở và upload ${selectedImages.length} ảnh thành công!`
            : "Tạo cơ sở thành công!"
        );
      } else {
        message.warning("Tạo cơ sở thành công nhưng upload ảnh thất bại");
      }

      // Reset và refresh
      handleModalClose();
      await fetchFacilities(1, pagination.pageSize, searchText, statusFilter);
      setPagination((prev) => ({ ...prev, current: 1 }));
    } catch (error) {
      console.error("💥 Error in create facility flow:", error);
      message.error(
        `Tạo cơ sở thất bại: ${error.response?.data?.message || error.message}`
      );
    } finally {
      setSubmitLoading(false);
    }
  };

  // ✅ HANDLE UPDATE FACILITY
  const handleUpdateFacility = async (values) => {
    try {
      setEditLoading(true);
      console.log("📝 Updating facility with values:", values);

      if (!editingFacility?.facilityId) {
        message.error("Không tìm thấy ID cơ sở");
        return;
      }

      // Validate địa chỉ
      if (
        !editSelectedProvince ||
        !editSelectedDistrict ||
        !values.detailAddress
      ) {
        message.error("Vui lòng chọn đầy đủ thông tin địa chỉ");
        return;
      }

      // Build địa chỉ
      const fullAddress = buildAddress(
        values.detailAddress,
        editSelectedDistrict,
        editSelectedProvince
      );

      // Update cơ sở
      const updateData = {
        facilityName: values.facilityName.trim(),
        location: fullAddress,
        contact: values.contact.trim(),
        statusId: values.statusId,
      };

      console.log("🚀 Step 1: Updating facility data...", updateData);
      const response = await updateFacility(
        editingFacility.facilityId,
        updateData
      );
      console.log("✅ Update facility response:", response);

      let success, message_text;
      if (response?.data?.success !== undefined) {
        success = response.data.success;
        message_text = response.data.message;
      } else if (response?.success !== undefined) {
        success = response.success;
        message_text = response.message;
      } else {
        success = true;
        message_text = "Cập nhật thành công";
      }

      if (!success) {
        message.error(message_text || "Cập nhật cơ sở thất bại");
        return;
      }

      // Upload ảnh mới
      let uploadResult = { success: true };
      if (uploadFileList && uploadFileList.length > 0) {
        console.log("🚀 Step 2: Uploading new images...");
        uploadResult = await uploadImages(
          editingFacility.facilityId,
          uploadFileList
        );
      }

      if (uploadResult.success) {
        const successMsg =
          uploadFileList.length > 0
            ? `Cập nhật cơ sở và thêm ${uploadFileList.length} ảnh mới thành công!`
            : "Cập nhật cơ sở thành công!";
        message.success(successMsg);
      } else {
        message.warning("Cập nhật cơ sở thành công nhưng upload ảnh thất bại");
      }

      // Close modal và refresh
      handleEditModalClose();
      await fetchFacilities(
        pagination.current,
        pagination.pageSize,
        searchText,
        statusFilter
      );
    } catch (error) {
      console.error("💥 Error updating facility:", error);
      message.error(
        `Cập nhật cơ sở thất bại: ${
          error.response?.data?.message || error.message
        }`
      );
    } finally {
      setEditLoading(false);
    }
  };

  // ✅ HANDLE MODAL CLOSE
  const handleModalClose = () => {
    selectedImages.forEach((file) => {
      if (file.preview && file.preview.startsWith("blob:")) {
        URL.revokeObjectURL(file.preview);
      }
    });

    setSelectedProvince("");
    setSelectedDistrict("");
    setModalVisible(false);
    form.resetFields();
    setSelectedImages([]);
  };

  const handleEditModalClose = () => {
    uploadFileList.forEach((file) => {
      if (file.preview && file.preview.startsWith("blob:")) {
        URL.revokeObjectURL(file.preview);
      }
    });

    setEditSelectedProvince("");
    setEditSelectedDistrict("");
    setEditModalVisible(false);
    setEditingFacility(null);
    setFacilityImages([]);
    setUploadFileList([]);
    editForm.resetFields();
  };

  const handleSearch = async (value) => {
    const searchValue = value || "";
    setSearchText(searchValue);
    const newPagination = { ...pagination, current: 1 };
    setPagination(newPagination);
    await fetchFacilities(1, pagination.pageSize, searchValue, statusFilter);
  };

  const handleTableChange = (newPagination) => {
    setPagination(newPagination);
    fetchFacilities(
      newPagination.current,
      newPagination.pageSize,
      searchText,
      statusFilter
    );
  };

  const handleEdit = (record) => {
    console.log("✏️ Edit facility:", record);
    fetchFacilityById(record.id);
  };

  const handleManageCourts = (record) => {
    console.log("Manage courts for facility:", record);
    const facilityId = record.facilityId || record.id; // Tùy thuộc vào tên field
    navigate(`/court-owner/facilities/${facilityId}/courts`);
  };

  const handleDelete = async (record) => {
    try {
      setDeleteLoading(true);
      const response = await deleteFacility(record.id);
      const { success, message: apiMessage } = response;

      if (success) {
        message.success(
          apiMessage || `Đã xóa cơ sở "${record.name}" thành công!`
        );
        await fetchFacilities(
          pagination.current,
          pagination.pageSize,
          searchText,
          statusFilter
        );

        if (facilities.length === 1 && pagination.current > 1) {
          setPagination((prev) => ({ ...prev, current: prev.current - 1 }));
          await fetchFacilities(
            pagination.current - 1,
            pagination.pageSize,
            searchText,
            statusFilter
          );
        }
      } else {
        message.error(apiMessage || "Xóa cơ sở thất bại");
      }
    } catch (error) {
      console.error("💥 Error deleting facility:", error);
      message.error(
        `Xóa cơ sở thất bại: ${error.message || "Lỗi không xác định"}`
      );
    } finally {
      setDeleteLoading(false);
    }
  };

  const handleStatusFilter = (value) => {
    let parsedValue;
    if (value === undefined || value === "" || value === "all") {
      parsedValue = null;
    } else {
      parsedValue = parseInt(value);
    }

    setStatusFilter(parsedValue);
    setPagination((prev) => ({ ...prev, current: 1 }));
    fetchFacilities(1, pagination.pageSize, searchText, parsedValue);
  };

  const columns = [
    {
      title: "STT",
      key: "index",
      width: 60,
      align: "center",
      render: (_, __, index) => (
        <strong style={{ color: "#52c41a" }}>
          {(pagination.current - 1) * pagination.pageSize + index + 1}
        </strong>
      ),
    },
    {
      title: "Ảnh cơ sở",
      key: "image",
      width: 120,
      align: "center",
      render: (_, record) => (
        <div className="facility-image">
          <img
            src={convertGoogleDriveUrl(record.image)}
            alt=""
            referrerPolicy="no-referrer"
            crossOrigin="anonymous"
            onError={(e) => handleImageError(e, record.image)}
            style={{
              maxWidth: "100%",
              height: "auto",
              display: "block", // THÊM để tránh broken image icon
            }}
          />
        </div>
      ),
    },
    {
      title: "Tên cơ sở",
      dataIndex: "name",
      key: "name",
      align: "center",
    },
    {
      title: "Địa chỉ",
      dataIndex: "address",
      key: "address",
      align: "center",
    },
    {
      title: "Số sân",
      dataIndex: "courtCount",
      key: "courtCount",
      width: 80,
      align: "center",
    },
    {
      title: "Trạng thái",
      dataIndex: "status",
      key: "status",
      width: 120,
      align: "center",
      render: (status) => {
        const statusText = status?.statusDescription || "Không xác định";
        const statusId = status?.statusId;
        let color = "default";
        if (statusId === 1) {
          color = "success";
        } else if (statusId === 2) {
          color = "error";
        }
        return (
          <Tag color={color} style={{ fontWeight: "500" }}>
            {statusText}
          </Tag>
        );
      },
    },
    {
      title: "Hành động",
      key: "actions",
      width: 200,
      align: "center",
      render: (_, record) => (
        <div className="action-buttons">
          <Tooltip title="Chỉnh sửa">
            <EditOutlined
              className="edit-icon"
              onClick={() => handleEdit(record)}
            />
          </Tooltip>
          <Popconfirm
            title="Xóa cơ sở"
            description={
              <div>
                <p>
                  Bạn có chắc chắn muốn xóa cơ sở{" "}
                  <strong>"{record.name}"</strong>?
                </p>
                <p
                  style={{
                    color: "#ff4d4f",
                    fontSize: "12px",
                    margin: "4px 0 0 0",
                  }}
                >
                  ⚠️ Thao tác này sẽ xóa tất cả dữ liệu liên quan và không thể
                  hoàn tác!
                </p>
              </div>
            }
            onConfirm={() => handleDelete(record)}
            okText="Xóa"
            cancelText="Hủy"
            okType="danger"
            placement="topRight"
            okButtonProps={{
              loading: deleteLoading,
              danger: true,
            }}
          >
            <Tooltip title="Xóa cơ sở">
              <DeleteOutlined
                className="delete-icon"
                style={{
                  color: deleteLoading ? "#ccc" : "#ff4d4f",
                  fontSize: "16px",
                  cursor: deleteLoading ? "not-allowed" : "pointer",
                  marginLeft: "12px",
                  transition: "all 0.3s ease",
                }}
              />
            </Tooltip>
          </Popconfirm>
          <Button
            type="primary"
            className="manage-courts-btn"
            onClick={() => handleManageCourts(record)}
          >
            Quản lý sân
          </Button>
        </div>
      ),
    },
  ];

  return (
    <>
      <div className="facility-table-wrapper">
        <div className="facility-table-card">
          <div className="page-header">
            <h1>Quản lý cơ sở</h1>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setModalVisible(true)}
            >
              Thêm mới cơ sở
            </Button>
          </div>

          <div className="table-filters">
            <Input.Search
              placeholder="Tìm kiếm theo tên cơ sở"
              prefix={<SearchOutlined />}
              className="search-input"
              allowClear
              enterButton={<SearchOutlined />}
              value={searchText}
              onChange={(e) => handleSearch(e.target.value)}
              onSearch={handleSearch}
            />
            <Select
              placeholder="Trạng thái"
              style={{ width: 160 }}
              allowClear
              onChange={handleStatusFilter}
              value={statusFilter === null ? undefined : statusFilter}
            >
              <Option value={1}>Hoạt động</Option>
              <Option value={2}>Bị khóa</Option>
            </Select>
          </div>

          <Table
            key={forceUpdate}
            columns={columns}
            dataSource={facilities}
            loading={loading}
            pagination={{
              ...pagination,
              pageSizeOptions: ["3", "5", "10", "20"],
              showSizeChanger: true,
              showQuickJumper: true,
              showTotal: (total, range) =>
                `${range[0]}-${range[1]} của ${total} mục`,
            }}
            onChange={handleTableChange}
            rowKey="id"
            className="facility-table"
            locale={{
              emptyText: searchText
                ? "Không tìm thấy kết quả phù hợp"
                : "Không có dữ liệu cơ sở",
            }}
          />
        </div>
      </div>

      {/* ✅ MODAL THÊM CƠ SỞ */}
      <Modal
        title="Thêm cơ sở mới"
        open={modalVisible}
        onCancel={handleModalClose}
        footer={null}
        width={700}
        centered
        destroyOnClose
        styles={{
          body: {
            maxHeight: "80vh",
            minHeight: "600px",
            overflow: "auto",
            padding: "24px",
          },
        }}
      >
        <Form
          form={form}
          layout="vertical"
          className="facility-form"
          onFinish={handleCreateFacility}
        >
          <Form.Item
            label="Tên cơ sở"
            name="facilityName"
            rules={[
              { required: true, message: "Vui lòng nhập tên cơ sở" },
              { min: 2, message: "Tên cơ sở phải có ít nhất 2 ký tự" },
            ]}
          >
            <Input placeholder="Nhập tên cơ sở..." />
          </Form.Item>

          {/* ✅ CÁC SELECT ĐỊA CHỈ API */}
          <div style={{ display: "flex", gap: "16px" }}>
            <Form.Item label="Tỉnh/Thành phố" style={{ flex: 1 }}>
              <Select
                getPopupContainer={(trigger) => trigger.parentElement}
                placeholder={`Chọn tỉnh/thành phố (${provinces.length} tỉnh)`}
                value={selectedProvince}
                onChange={(value) => {
                  console.log("🎯 Province selected:", value);
                  setSelectedProvince(value);
                }}
                showSearch
                optionFilterProp="children"
                loading={provinces.length === 0}
                notFoundContent={
                  provinces.length === 0 ? "Đang tải..." : "Không tìm thấy"
                }
                key={provinces.length}
              >
                {provinces.length > 0 ? (
                  provinces.map((province) => (
                    <Option key={province.code} value={province.name}>
                      {province.name}
                    </Option>
                  ))
                ) : (
                  <Option disabled value="">
                    Đang tải tỉnh thành...
                  </Option>
                )}
              </Select>
            </Form.Item>

            <Form.Item label="Quận/Huyện" style={{ flex: 1 }}>
              <Select
                getPopupContainer={(trigger) => trigger.parentElement}
                placeholder="Chọn quận/huyện"
                value={selectedDistrict}
                onChange={setSelectedDistrict}
                disabled={!selectedProvince || districts.length === 0}
                showSearch
                optionFilterProp="children"
                loading={selectedProvince && districts.length === 0}
              >
                {districts.map((district) => (
                  <Option key={district.code} value={district.name}>
                    {district.name}
                  </Option>
                ))}
              </Select>
            </Form.Item>
          </div>

          <Form.Item
            label="Địa chỉ chi tiết"
            name="detailAddress"
            rules={[
              { required: true, message: "Vui lòng nhập địa chỉ chi tiết" },
              { min: 5, message: "Địa chỉ chi tiết phải có ít nhất 5 ký tự" },
            ]}
          >
            <Input placeholder="Số nhà, tên đường..." />
          </Form.Item>

          {(form.getFieldValue("detailAddress") ||
            selectedDistrict ||
            selectedProvince) && (
            <div
              style={{
                padding: "12px",
                background: "#f0f2f5",
                borderRadius: "6px",
                marginBottom: "16px",
                border: "1px solid #d9d9d9",
              }}
            >
              <div
                style={{
                  fontWeight: "bold",
                  marginBottom: "4px",
                  color: "#1890ff",
                }}
              >
                📍 Địa chỉ đầy đủ:
              </div>
              <div style={{ color: "#333" }}>
                {cleanAddressForDisplay(
                  buildAddress(
                    form.getFieldValue("detailAddress") || "",
                    selectedDistrict,
                    selectedProvince
                  )
                )}
              </div>
            </div>
          )}

          <Form.Item
            label="Số điện thoại liên hệ"
            name="contact"
            rules={[
              { required: true, message: "Vui lòng nhập số điện thoại" },
              {
                pattern: /(84|0[3|5|7|8|9])+([0-9]{8})\b/,
                message: "Số điện thoại không hợp lệ",
              },
            ]}
          >
            <Input placeholder="Nhập số điện thoại (VD: 0987654321)" />
          </Form.Item>

          <div style={{ display: "flex", gap: "16px" }}>
            <Form.Item
              label="Giờ mở cửa"
              name="openHour"
              rules={[{ required: true, message: "Vui lòng chọn giờ mở cửa" }]}
              style={{ flex: 1 }}
            >
              <Select
                placeholder="Chọn giờ mở cửa"
                getPopupContainer={(trigger) => trigger.parentElement}
              >
                {Array.from({ length: 24 }, (_, i) => (
                  <Option key={i} value={i}>
                    {i.toString().padStart(2, "0")}:00
                  </Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item
              label="Giờ đóng cửa"
              name="closeHour"
              rules={[
                { required: true, message: "Vui lòng chọn giờ đóng cửa" },
              ]}
              style={{ flex: 1 }}
            >
              <Select
                placeholder="Chọn giờ đóng cửa"
                getPopupContainer={(trigger) => trigger.parentElement}
              >
                {Array.from({ length: 24 }, (_, i) => (
                  <Option key={i} value={i}>
                    {i.toString().padStart(2, "0")}:00
                  </Option>
                ))}
              </Select>
            </Form.Item>
          </div>

          <Form.Item
            label="Thời lượng mỗi slot (phút)"
            name="slotDuration"
            rules={[
              { required: true, message: "Vui lòng chọn thời lượng slot" },
            ]}
          >
            <Select
              placeholder="Chọn thời lượng mỗi slot"
              getPopupContainer={(trigger) => trigger.parentElement}
            >
              <Option value={30}>30 phút</Option>
              <Option value={45}>45 phút</Option>
              <Option value={60}>60 phút</Option>
              <Option value={90}>90 phút</Option>
              <Option value={120}>120 phút</Option>
            </Select>
          </Form.Item>

          <Form.Item
            label="Trạng thái"
            name="statusId"
            rules={[{ required: true, message: "Vui lòng chọn trạng thái" }]}
          >
            <Select
              placeholder="Chọn trạng thái"
              getPopupContainer={(trigger) => trigger.parentElement}
            >
              <Option value={1}>🟢 Hoạt động</Option>
              <Option value={2}>🔴 Bị khóa</Option>
            </Select>
          </Form.Item>

          {/* ✅ UPLOAD ẢNH */}
          <Form.Item label="Ảnh cơ sở">
            <Upload
              listType="picture-card"
              fileList={selectedImages}
              onChange={handleImageChange}
              beforeUpload={() => false}
              multiple
              accept="image/*"
              maxCount={10}
              onPreview={(file) => {
                const previewUrl =
                  file.url ||
                  file.preview ||
                  URL.createObjectURL(file.originFileObj);
                handlePreviewImage(previewUrl, file.name);
              }}
              onRemove={(file) => {
                if (file.preview && file.preview.startsWith("blob:")) {
                  URL.revokeObjectURL(file.preview);
                }
              }}
            >
              {selectedImages.length < 10 && (
                <div>
                  <PlusOutlined />
                  <div style={{ marginTop: 8 }}>Tải ảnh lên</div>
                </div>
              )}
            </Upload>
            <div style={{ marginTop: 8, color: "#666", fontSize: "12px" }}>
              * Chọn tối đa 10 ảnh, mỗi ảnh không quá 5MB. Click vào ảnh để xem
              trước.
            </div>
          </Form.Item>

          <Form.Item>
            <div
              style={{
                display: "flex",
                gap: "12px",
                justifyContent: "flex-end",
              }}
            >
              <Button onClick={handleModalClose}>Hủy</Button>
              <Button type="primary" htmlType="submit" loading={submitLoading}>
                {selectedImages.length > 0
                  ? `Thêm cơ sở + ${selectedImages.length} ảnh`
                  : "Thêm mới cơ sở"}
              </Button>
            </div>
          </Form.Item>
        </Form>
      </Modal>

      {/* ✅ MODAL CHỈNH SỬA CƠ SỞ */}
      <Modal
        title={`Chỉnh sửa cơ sở: ${editingFacility?.facilityName || ""}`}
        open={editModalVisible}
        onCancel={handleEditModalClose}
        footer={null}
        width={1000}
        centered
        destroyOnClose
        styles={{
          body: {
            maxHeight: "80vh",
            minHeight: "600px",
            overflow: "auto",
            padding: "24px",
          },
        }}
      >
        <Form
          form={editForm}
          layout="vertical"
          className="facility-form"
          onFinish={handleUpdateFacility}
        >
          <Form.Item
            label="Tên cơ sở"
            name="facilityName"
            rules={[
              { required: true, message: "Vui lòng nhập tên cơ sở" },
              { min: 2, message: "Tên cơ sở phải có ít nhất 2 ký tự" },
            ]}
          >
            <Input placeholder="Nhập tên cơ sở..." />
          </Form.Item>

          {/* ✅ CÁC SELECT ĐỊA CHỈ API - EDIT */}
          <div style={{ display: "flex", gap: "16px" }}>
            <Form.Item label="Tỉnh/Thành phố" style={{ flex: 1 }}>
              <Select
                getPopupContainer={(trigger) => trigger.parentElement}
                placeholder="Chọn tỉnh/thành phố"
                value={editSelectedProvince}
                onChange={setEditSelectedProvince}
                showSearch
                optionFilterProp="children"
                loading={provinces.length === 0}
              >
                {provinces.map((province) => (
                  <Option key={province.code} value={province.name}>
                    {province.name}
                  </Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item label="Quận/Huyện" style={{ flex: 1 }}>
              <Select
                placeholder="Chọn quận/huyện"
                value={editSelectedDistrict}
                onChange={setEditSelectedDistrict}
                disabled={!editSelectedProvince || districts.length === 0}
                showSearch
                getPopupContainer={(trigger) => trigger.parentElement}
                optionFilterProp="children"
                loading={editSelectedProvince && districts.length === 0}
              >
                {districts.map((district) => (
                  <Option key={district.code} value={district.name}>
                    {district.name}
                  </Option>
                ))}
              </Select>
            </Form.Item>
          </div>

          <Form.Item
            label="Địa chỉ chi tiết"
            name="detailAddress"
            rules={[
              { required: true, message: "Vui lòng nhập địa chỉ chi tiết" },
              { min: 5, message: "Địa chỉ chi tiết phải có ít nhất 5 ký tự" },
            ]}
          >
            <Input placeholder="Số nhà, tên đường..." />
          </Form.Item>

          {/* ✅ PREVIEW ĐỊA CHỈ - Modal chỉnh sửa */}
          {(editForm.getFieldValue("detailAddress") ||
            editSelectedDistrict ||
            editSelectedProvince) && (
            <div
              style={{
                padding: "12px",
                background: "#f0f2f5",
                borderRadius: "6px",
                marginBottom: "16px",
                border: "1px solid #d9d9d9",
              }}
            >
              <div
                style={{
                  fontWeight: "bold",
                  marginBottom: "4px",
                  color: "#1890ff",
                }}
              >
                📍 Địa chỉ đầy đủ:
              </div>
              <div style={{ color: "#333" }}>
                {cleanAddressForDisplay(
                  buildAddress(
                    editForm.getFieldValue("detailAddress") || "",
                    editSelectedDistrict,
                    editSelectedProvince
                  )
                )}
              </div>
            </div>
          )}

          <Form.Item
            label="Số điện thoại liên hệ"
            name="contact"
            rules={[
              { required: true, message: "Vui lòng nhập số điện thoại" },
              {
                pattern: /(84|0[3|5|7|8|9])+([0-9]{8})\b/,
                message: "Số điện thoại không hợp lệ",
              },
            ]}
          >
            <Input placeholder="Nhập số điện thoại (VD: 0987654321)" />
          </Form.Item>

          <Form.Item
            label="Trạng thái"
            name="statusId"
            rules={[{ required: true, message: "Vui lòng chọn trạng thái" }]}
          >
            <Select
              placeholder="Chọn trạng thái"
              getPopupContainer={(trigger) => trigger.parentElement}
            >
              <Option value={1}>🟢 Hoạt động</Option>
              <Option value={2}>🔴 Bị khóa</Option>
            </Select>
          </Form.Item>

          {/* ✅ QUẢN LÝ ẢNH */}
          <Form.Item label="Ảnh cơ sở">
            <div className="facility-images-manager">
              {/* Hiển thị ảnh hiện tại */}
              {facilityImages.length > 0 ? (
                <div className="current-images">
                  <h4 style={{ marginBottom: 16, color: "#1890ff" }}>
                    Ảnh hiện tại ({facilityImages.length}):
                  </h4>
                  <Row gutter={[16, 16]}>
                    {facilityImages.map((image) => (
                      <Col span={6} key={image.imageId}>
                        <Card
                          hoverable
                          cover={
                            <div style={{ height: 120, overflow: "hidden" }}>
                              <img
                                src={image.imageUrl}
                                alt={image.caption || "Facility image"}
                                style={{
                                  width: "100%",
                                  height: "100%",
                                  objectFit: "cover",
                                  cursor: "pointer",
                                }}
                                onClick={() =>
                                  handlePreviewImage(
                                    image.imageUrl,
                                    image.caption
                                  )
                                }
                                onError={(e) => {
                                  e.target.src =
                                    "https://placehold.co/300x200?text=Error+Loading";
                                }}
                              />
                            </div>
                          }
                          actions={[
                            <EyeOutlined
                              key="view"
                              onClick={() =>
                                handlePreviewImage(
                                  image.imageUrl,
                                  image.caption
                                )
                              }
                              style={{ color: "#1890ff" }}
                              title="Xem ảnh"
                            />,
                            <DeleteOutlined
                              key="delete"
                              onClick={() =>
                                handleDeleteImage(image.imageId, image.caption)
                              }
                              style={{ color: "#ff4d4f" }}
                              title="Xóa ảnh"
                            />,
                          ]}
                          size="small"
                        >
                          <Card.Meta
                            description={
                              <div
                                style={{
                                  fontSize: "12px",
                                  textAlign: "center",
                                }}
                              >
                                {image.caption || "Không có mô tả"}
                              </div>
                            }
                          />
                        </Card>
                      </Col>
                    ))}
                  </Row>
                </div>
              ) : (
                <div
                  style={{
                    textAlign: "center",
                    padding: "20px",
                    background: "#fafafa",
                    borderRadius: "6px",
                    border: "1px dashed #d9d9d9",
                  }}
                >
                  <p style={{ color: "#999", margin: 0 }}>
                    Cơ sở này chưa có ảnh nào
                  </p>
                </div>
              )}

              {/* Upload ảnh mới */}
              <div className="upload-new-images" style={{ marginTop: 20 }}>
                <h4 style={{ marginBottom: 12, color: "#52c41a" }}>
                  Thêm ảnh mới:
                </h4>
                <Upload
                  listType="picture-card"
                  fileList={uploadFileList}
                  onChange={handleUploadChange}
                  beforeUpload={() => false}
                  multiple
                  accept="image/*"
                  maxCount={8}
                  onPreview={(file) => {
                    const previewUrl =
                      file.url ||
                      file.preview ||
                      URL.createObjectURL(file.originFileObj);
                    handlePreviewImage(previewUrl, file.name);
                  }}
                >
                  {uploadFileList.length >= 8 ? null : (
                    <div>
                      <UploadOutlined />
                      <div style={{ marginTop: 8 }}>Upload</div>
                    </div>
                  )}
                </Upload>
                <p style={{ color: "#999", fontSize: "12px", marginTop: 8 }}>
                  * Chọn tối đa 8 ảnh mới để thêm vào cơ sở
                </p>
              </div>
            </div>
          </Form.Item>

          <Form.Item>
            <div
              style={{
                display: "flex",
                gap: "12px",
                justifyContent: "flex-end",
              }}
            >
              <Button onClick={handleEditModalClose}>Hủy</Button>
              <Button type="primary" htmlType="submit" loading={editLoading}>
                Cập nhật cơ sở
              </Button>
            </div>
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
};

export default FacilityTable;
