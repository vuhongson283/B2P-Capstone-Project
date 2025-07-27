import React, { useState, useEffect, useCallback } from "react";
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
  Col
} from "antd";
import {
  SearchOutlined,
  EditOutlined,
  PlusOutlined,
  UploadOutlined,
  DeleteOutlined,
  EyeOutlined
} from "@ant-design/icons";
import {
  getFacilitiesByCourtOwnerId, createFacility, uploadFacilityImages,
  deleteFacilityImage, getFacilityById, updateFacility, deleteFacility
} from "../../services/apiService";
import "./FacilityTable.scss";

const { Text } = Typography;
const convertGoogleDriveUrl = (originalUrl) => {
  if (!originalUrl) return "https://placehold.co/300x200?text=No+Image";
  if (originalUrl.includes('thumbnail')) return originalUrl;
  const match = originalUrl.match(/id=([^&]+)/);
  if (match) {
    const id = match[1];
    return `https://drive.google.com/thumbnail?id=${id}`;
  }
  return originalUrl;
};
const FacilityTable = () => {
  
  const { Option } = Select;
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

  const getCourtOwnerId = () => {
    const courtOwnerData = localStorage.getItem("courtOwner");
    if (courtOwnerData) {
      try {
        const parsed = JSON.parse(courtOwnerData);
        return parsed.id;
      } catch (error) {
        console.error("Error parsing courtOwner data:", error);
      }
    }
    return 8; // fallback ID
  };
  // Fetch facility by ID for editing
  const fetchFacilityById = async (facilityId) => {
    try {
      setEditLoading(true);
      console.log("🔍 Fetching facility by ID:", facilityId);

      const response = await getFacilityById(facilityId);
      console.log("📡 Get facility by ID response:", response);

      let success, data;

      // Handle different response structures
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
        setEditingFacility(data);
        const convertedImages = (data.images || []).map(image => ({
          ...image,
          imageUrl: convertGoogleDriveUrl(image.imageUrl) // Dùng hàm convert đã có
        }));
        setFacilityImages(convertedImages);
        setUploadFileList([]); // Reset upload list
        console.log("🖼️ Facility images loaded:", convertedImages.length, "images");

        // Populate form with facility data
        editForm.setFieldsValue({
          facilityName: data.facilityName,
          location: data.location,
          contact: data.contact,
          statusId: data.statusId,
        });

        setEditModalVisible(true);
      } else {
        message.error("Không thể tải thông tin cơ sở");
      }
    } catch (error) {
      console.error("💥 Error fetching facility:", error);
      message.error(`Lỗi khi tải thông tin cơ sở: ${error.response?.data?.message || error.message}`);
    } finally {
      setEditLoading(false);
    }
  };
  // Sửa lại hàm handleDeleteImage
  const handleDeleteImage = async (imageId, imageName) => {
    Modal.confirm({
      title: 'Xác nhận xóa ảnh',
      content: `Bạn có chắc chắn muốn xóa ảnh "${imageName || 'này'}"?`,
      okText: 'Xóa',
      cancelText: 'Hủy',
      okType: 'danger',
      onOk: async () => {
        try {
          console.log("🗑️ Deleting image ID:", imageId);
          const response = await deleteFacilityImage(imageId);
          console.log("✅ Delete image response:", response);

          // Xử lý response theo cấu trúc API thực tế
          let success = false;

          // Kiểm tra nhiều cấu trúc response có thể
          if (response?.message === "Image deleted successfully") {
            success = true;
          } else if (response?.data?.message === "Image deleted successfully") {
            success = true;
          } else if (response?.success === true) {
            success = true;
          } else if (!response?.message?.includes("not found") && !response?.message?.includes("failed")) {
            success = true; // Assume success if no explicit error
          }

          if (success) {
            setFacilityImages(prev => prev.filter(img => img.imageId !== imageId));
            message.success('Xóa ảnh thành công');
          } else {
            message.error(response?.message || response?.data?.message || 'Xóa ảnh thất bại');
          }
        } catch (error) {
          console.error('💥 Error deleting image:', error);

          if (error.response?.status === 404) {
            message.error('Không tìm thấy ảnh để xóa');
          } else if (error.response?.status === 500) {
            message.error('Lỗi máy chủ khi xóa ảnh');
          } else {
            message.error(`Xóa ảnh thất bại: ${error.response?.data?.message || error.message}`);
          }
        }
      },
    });
  };

  // Cập nhật hàm handlePreviewImage
  const handlePreviewImage = (imageUrl, caption) => {
    // Xử lý Google Drive URL hoặc blob URL
    let previewUrl = imageUrl;

    if (imageUrl.includes('thumbnail')) {
      previewUrl = imageUrl.replace('thumbnail', 'uc');
    }
    // Blob URL (từ file upload) giữ nguyên

    Modal.info({
      title: caption || 'Xem ảnh',
      content: (
        <div style={{ textAlign: 'center' }}>
          <img
            src={previewUrl}
            alt="Preview"
            style={{
              maxWidth: '100%',
              maxHeight: '500px',
              objectFit: 'contain',
              borderRadius: '8px'
            }}
            onError={(e) => {
              // Fallback về URL gốc nếu convert failed
              e.target.src = imageUrl;
            }}
          />
        </div>
      ),
      width: 700,
      okText: 'Đóng',
    });
  };
  // Hàm xử lý upload ảnh (chuẩn bị cho tương lai)
  // Cập nhật hàm handleUploadChange
  const handleUploadChange = ({ fileList }) => {
    // Validate và tạo preview URL
    const validFiles = fileList.filter(file => {
      const isImage = file.type?.startsWith('image/');
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

    // Tạo preview URL cho file mới
    const filesWithPreview = validFiles.map(file => {
      if (!file.url && !file.preview && file.originFileObj) {
        file.preview = URL.createObjectURL(file.originFileObj);
      }
      return file;
    });

    setUploadFileList(filesWithPreview);
    console.log("📁 Upload file list changed:", filesWithPreview.length, "files");
  };
  // Update facility
  // Cập nhật hàm handleUpdateFacility
  const handleUpdateFacility = async (values) => {
    try {
      setEditLoading(true);
      console.log("📝 Updating facility with values:", values);

      if (!editingFacility?.facilityId) {
        message.error("Không tìm thấy ID cơ sở");
        return;
      }

      // Bước 1: Update thông tin cơ sở
      const updateData = {
        facilityName: values.facilityName.trim(),
        location: values.location.trim(),
        contact: values.contact.trim(),
        statusId: values.statusId,
      };

      console.log("🚀 Step 1: Updating facility data...", updateData);
      const response = await updateFacility(editingFacility.facilityId, updateData);
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

      // Bước 2: Upload ảnh mới nếu có
      let uploadResult = { success: true };
      if (uploadFileList && uploadFileList.length > 0) {
        console.log("🚀 Step 2: Uploading new images...");
        uploadResult = await uploadImages(editingFacility.facilityId, uploadFileList);
      }

      // Thông báo kết quả
      if (uploadResult.success) {
        const successMsg = uploadFileList.length > 0
          ? `Cập nhật cơ sở và thêm ${uploadFileList.length} ảnh mới thành công!`
          : "Cập nhật cơ sở thành công!";
        message.success(successMsg);
      } else {
        message.warning("Cập nhật cơ sở thành công nhưng upload ảnh thất bại");
      }

      // Close modal và refresh
      handleEditModalClose();
      await fetchFacilities(pagination.current, pagination.pageSize, searchText, statusFilter);

    } catch (error) {
      console.error("💥 Error updating facility:", error);
      const errorMessage = error.response?.data?.message || error.message || "Đã có lỗi xảy ra";
      message.error(`Cập nhật cơ sở thất bại: ${errorMessage}`);
    } finally {
      setEditLoading(false);
    }
  };


  // Handle edit modal close

  const fetchFacilities = useCallback(async (page = 1, pageSize = 3, searchQuery = "", status = null) => {
    try {
      setLoading(true);
      const courtOwnerId = getCourtOwnerId();

      console.log("🔍 Calling API with params:", {
        courtOwnerId,
        searchQuery,
        status,
        page,
        pageSize
      });

      const response = await getFacilitiesByCourtOwnerId(
        courtOwnerId,
        searchQuery,
        status,
        page,
        pageSize
      );

      console.log("📡 Full API Response:", response);

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
        console.error("❌ Unknown response structure:", response);
        success = false;
        payload = null;
      }

      if (success && payload && payload.items) {
        const { items, totalItems, currentPage, itemsPerPage } = payload;

        const convertGoogleDriveUrl = (originalUrl) => {
          if (!originalUrl) return "https://placehold.co/300x200?text=No+Image";
          if (originalUrl.includes('thumbnail')) return originalUrl;
          const match = originalUrl.match(/id=([^&]+)/);
          if (match) {
            const id = match[1];
            return `https://drive.google.com/thumbnail?id=${id}`;
          }
          return originalUrl;
        };

        const mappedFacilities = items.map((facility) => ({
          key: facility.facilityId,
          id: facility.facilityId,
          name: facility.facilityName,
          address: facility.location,
          courtCount: facility.courtCount,
          status: facility.status,
          image:
            facility.images?.length > 0
              ? convertGoogleDriveUrl(facility.images[0].imageUrl)
              : "https://placehold.co/300x200?text=No+Image",
        }));

        setFacilities(mappedFacilities);
        setPagination(prev => ({
          ...prev,
          current: currentPage,
          pageSize: itemsPerPage,
          total: totalItems,
        }));
      } else {
        console.error("❌ API response invalid:", { success, payload });
        message.error(`Không thể tải danh sách cơ sở. Success: ${success}, Has items: ${payload?.items ? 'Yes' : 'No'}`);
        setFacilities([]);
      }
    } catch (error) {
      console.error("💥 Lỗi khi gọi API:", error);
      message.error(`Đã có lỗi xảy ra: ${error.response?.data?.message || error.message}`);
      setFacilities([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!hasInitialized) {
      fetchFacilities(1, 3, "", null);
      setHasInitialized(true);
    }
  }, [fetchFacilities, hasInitialized]);

  useEffect(() => {
    if (hasInitialized && (searchText !== "" || statusFilter !== null)) {
      fetchFacilities(1, pagination.pageSize, searchText, statusFilter);
    }
  }, [searchText, statusFilter, hasInitialized, pagination.pageSize, fetchFacilities]);

  const handleSearch = async (value) => {
    const searchValue = value || "";
    setSearchText(searchValue);

    const newPagination = {
      ...pagination,
      current: 1
    };
    setPagination(newPagination);

    // Gọi API ngay lập tức với giá trị mới
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
  };

  // Cập nhật hàm handleImageChange
  const handleImageChange = (info) => {
    const { fileList } = info;

    // Validate file types
    const validFiles = fileList.filter(file => {
      const isImage = file.type?.startsWith('image/');
      if (!isImage && file.originFileObj) {
        message.error(`${file.name} không phải là file ảnh hợp lệ`);
        return false;
      }
      return true;
    });

    // Validate file size (max 5MB per file)
    const validSizeFiles = validFiles.filter(file => {
      const isValidSize = !file.size || file.size / 1024 / 1024 < 5;
      if (!isValidSize) {
        message.error(`${file.name} vượt quá 5MB`);
        return false;
      }
      return true;
    });

    // Tạo preview URL cho các file mới
    const filesWithPreview = validSizeFiles.map(file => {
      if (!file.url && !file.preview && file.originFileObj) {
        file.preview = URL.createObjectURL(file.originFileObj);
      }
      return file;
    });

    setSelectedImages(filesWithPreview);
  };

  // Upload ảnh lên server
  const uploadImages = async (facilityId, imageFiles) => {
    if (!imageFiles || imageFiles.length === 0) {
      console.log("📸 No images to upload");
      return { success: true, message: "No images to upload" };
    }

    try {
      console.log(`📸 Uploading ${imageFiles.length} images for facility ${facilityId}`);

      // Tạo FormData
      const formData = new FormData();

      // Thêm files vào FormData
      imageFiles.forEach(fileObj => {
        const file = fileObj.originFileObj || fileObj;
        formData.append('files', file);
      });

      // Thêm entityId (facilityId)
      formData.append('entityId', facilityId.toString());

      // Thêm caption (optional)
      formData.append('caption', 'Facility image');

      console.log("📤 FormData prepared:", {
        files: imageFiles.length,
        entityId: facilityId
      });

      // Gọi API upload
      const uploadResponse = await uploadFacilityImages(formData);

      console.log("✅ Upload response:", uploadResponse);
      console.log("✅ Upload response.data:", uploadResponse.data);
      console.log("✅ Upload response.status:", uploadResponse.status);

      // Fix: Check the response structure correctly
      // The API returns the array directly, not wrapped in response.data
      let responseData;

      // Check if response has data property (axios wrapper)
      if (uploadResponse?.data) {
        responseData = uploadResponse.data;
      } else {
        // Direct response (the array itself)
        responseData = uploadResponse;
      }

      console.log("🔍 Processed response data:", responseData);

      // Check if responseData is an array with image data
      const isValidResponse = Array.isArray(responseData) &&
        responseData.length > 0 &&
        responseData[0].imageId;

      console.log("🔍 Response validation:", {
        isArray: Array.isArray(responseData),
        hasLength: responseData?.length > 0,
        hasImageId: responseData?.[0]?.imageId,
        isValidResponse
      });

      if (isValidResponse) {
        console.log(`✅ Upload successful: ${imageFiles.length} images uploaded`);
        return { success: true, data: responseData };
      } else {
        throw new Error("Upload response không hợp lệ - dữ liệu không đúng định dạng");
      }

    } catch (error) {
      console.error("💥 Error uploading images:", error);
      const errorMessage = error.response?.data?.message || error.message || "Upload ảnh thất bại";
      message.error(`Upload ảnh thất bại: ${errorMessage}`);
      return { success: false, error: errorMessage };
    }
  };

  const handleCreateFacility = async (values) => {
    try {
      setSubmitLoading(true);
      const courtOwnerId = getCourtOwnerId();

      console.log("📝 Form values:", values);

      // Validate giờ mở/đóng cửa
      if (values.closeHour <= values.openHour) {
        message.error("Giờ đóng cửa phải sau giờ mở cửa");
        return;
      }

      // Bước 1: Tạo cơ sở trước
      const facilityData = {
        facilityName: values.facilityName.trim(),
        location: values.location.trim(),
        contact: values.contact.trim(),
        userId: courtOwnerId,
        statusId: values.statusId,
        openHour: values.openHour,
        closeHour: values.closeHour,
        slotDuration: values.slotDuration
      };

      console.log("🚀 Step 1: Creating facility...", facilityData);

      const createResponse = await createFacility(facilityData);
      console.log("✅ Create facility response:", createResponse);

      const success = createResponse?.data?.success || createResponse?.success;
      const message_text = createResponse?.data?.message || createResponse?.message;

      if (!success) {
        message.error(message_text || "Tạo cơ sở thất bại");
        return;
      }

      // Lấy facilityId từ response
      const facilityId = createResponse?.data?.data?.facilityId ||
        createResponse?.data?.facilityId ||
        createResponse?.facilityId;

      console.log("🆔 Created facility ID:", facilityId);

      if (!facilityId) {
        message.error("Không thể lấy ID cơ sở vừa tạo");
        return;
      }

      // Bước 2: Upload ảnh nếu có
      let uploadResult = { success: true };
      if (selectedImages && selectedImages.length > 0) {
        console.log("🚀 Step 2: Uploading images...");
        uploadResult = await uploadImages(facilityId, selectedImages);
      }

      // Kết quả cuối cùng
      if (uploadResult.success) {
        message.success(
          selectedImages.length > 0
            ? `Tạo cơ sở và upload ${selectedImages.length} ảnh thành công!`
            : "Tạo cơ sở thành công!"
        );
      } else {
        message.warning("Tạo cơ sở thành công nhưng upload ảnh thất bại");
      }

      // Reset form và đóng modal
      setModalVisible(false);
      form.resetFields();
      setSelectedImages([]);

      // Refresh danh sách cơ sở
      await fetchFacilities(1, pagination.pageSize, searchText, statusFilter);
      setPagination(prev => ({ ...prev, current: 1 }));

    } catch (error) {
      console.error("💥 Error in create facility flow:", error);

      if (error.response?.status === 400) {
        const errorData = error.response.data;
        if (errorData.errors) {
          Object.keys(errorData.errors).forEach(field => {
            const fieldErrors = errorData.errors[field];
            message.error(`${field}: ${fieldErrors.join(', ')}`);
          });
        } else {
          message.error(errorData.message || "Dữ liệu không hợp lệ");
        }
      } else {
        const errorMessage = error.response?.data?.message || error.message || "Đã có lỗi xảy ra";
        message.error(`Tạo cơ sở thất bại: ${errorMessage}`);
      }
    } finally {
      setSubmitLoading(false);
    }
  };

  const handleModalClose = () => {
    // Cleanup blob URLs
    selectedImages.forEach(file => {
      if (file.preview && file.preview.startsWith('blob:')) {
        URL.revokeObjectURL(file.preview);
      }
    });

    setModalVisible(false);
    form.resetFields();
    setEditingFacility(null);
    setSelectedImages([]);
    setFacilityImages([]);
    setUploadFileList([]);
  };
  // Cập nhật hàm handleEditModalClose
  const handleEditModalClose = () => {
    // Cleanup blob URLs từ upload mới
    uploadFileList.forEach(file => {
      if (file.preview && file.preview.startsWith('blob:')) {
        URL.revokeObjectURL(file.preview);
      }
    });

    setEditModalVisible(false);
    setEditingFacility(null);
    setFacilityImages([]);
    setUploadFileList([]);
    editForm.resetFields();
  };
  const handleDelete = async (record) => {
    try {
      setDeleteLoading(true);
      console.log("🗑️ Deleting facility:", record);

      const response = await deleteFacility(record.id);
      console.log("✅ Delete facility response:", response);

      // Axios interceptor đã xử lý, response trực tiếp chứa success, message, status
      const { success, message: apiMessage, status } = response;

      console.log("🔍 Response data:", { success, apiMessage, status });

      if (success) {
        message.success(apiMessage || `Đã xóa cơ sở "${record.name}" thành công!`);

        // Refresh danh sách sau khi xóa
        await fetchFacilities(pagination.current, pagination.pageSize, searchText, statusFilter);

        // Nếu trang hiện tại không còn dữ liệu, chuyển về trang trước
        if (facilities.length === 1 && pagination.current > 1) {
          setPagination(prev => ({ ...prev, current: prev.current - 1 }));
          await fetchFacilities(pagination.current - 1, pagination.pageSize, searchText, statusFilter);
        }
      } else {
        // Xử lý các trường hợp lỗi khác nhau dựa trên status
        if (status === 400) {
          message.error(apiMessage || "Không thể xóa cơ sở này vì đang có booking hoạt động");
        } else if (status === 404) {
          message.error(apiMessage || "Không tìm thấy cơ sở cần xóa");
        } else if (status === 500) {
          message.error(apiMessage || "Lỗi máy chủ, vui lòng thử lại sau");
        } else {
          message.error(apiMessage || "Xóa cơ sở thất bại");
        }
      }

    } catch (error) {
      console.error("💥 Error deleting facility:", error);

      // Nếu có error object với cấu trúc tương tự
      if (error.success === false) {
        const { message: errorMessage, status } = error;

        if (status === 400) {
          message.error(errorMessage || "Không thể xóa cơ sở này vì đang có booking hoạt động");
        } else if (status === 404) {
          message.error(errorMessage || "Không tìm thấy cơ sở cần xóa");
        } else if (status === 500) {
          message.error(errorMessage || "Lỗi máy chủ, vui lòng thử lại sau");
        } else {
          message.error(errorMessage || "Xóa cơ sở thất bại");
        }
      } else {
        message.error(`Xóa cơ sở thất bại: ${error.message || 'Lỗi không xác định'}`);
      }
    } finally {
      setDeleteLoading(false);
    }
  };


  const handleStatusFilter = (value) => {
    console.log("🔧 Status filter changed:", value); // Debug log

    let parsedValue;
    if (value === undefined || value === "" || value === "all") {
      parsedValue = null;
    } else {
      parsedValue = parseInt(value);
    }

    console.log("🔧 Parsed status value:", parsedValue); // Debug log

    setStatusFilter(parsedValue);
    setPagination((prev) => ({
      ...prev,
      current: 1,
    }));

    // Force refresh data immediately khi filter thay đổi
    fetchFacilities(1, pagination.pageSize, searchText, parsedValue);
  };

  const columns = [
    {
      title: "STT",
      key: "index",
      width: 80,
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
      width: 200,
      align: "center",
      render: (_, record) => (
        <div className="facility-image">
          <img
            src={record.image}
            alt={record.name}
            onError={(e) => {
              e.target.onerror = null;
              e.target.src = "/assets/images/default.jpg";
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
      width: 120,
      align: "center",
    },
    {
      title: "Trạng thái",
      dataIndex: "status",
      key: "status",
      width: 120,
      align: "center",
      render: (status) => status?.statusDescription || "Không xác định",
    },
    {
      title: "Hành động",
      key: "actions",
      width: 250,
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
                <p>Bạn có chắc chắn muốn xóa cơ sở <strong>"{record.name}"</strong>?</p>
                <p style={{ color: '#ff4d4f', fontSize: '12px', margin: '4px 0 0 0' }}>
                  ⚠️ Thao tác này sẽ xóa tất cả dữ liệu liên quan và không thể hoàn tác!
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
              danger: true
            }}
          >
            <Tooltip title="Xóa cơ sở">
              <DeleteOutlined
                className="delete-icon"
                style={{
                  color: deleteLoading ? '#ccc' : '#ff4d4f',
                  fontSize: '16px',
                  cursor: deleteLoading ? 'not-allowed' : 'pointer',
                  marginLeft: '12px',
                  transition: 'all 0.3s ease'
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

      {/* Modal thêm cơ sở */}
      <Modal
        title="Thêm cơ sở mới"
        open={modalVisible}
        onCancel={handleModalClose}
        footer={null}
        width={600}
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
              { min: 2, message: "Tên cơ sở phải có ít nhất 2 ký tự" }
            ]}
          >
            <Input placeholder="Nhập tên cơ sở..." />
          </Form.Item>

          <Form.Item
            label="Địa chỉ"
            name="location"
            rules={[
              { required: true, message: "Vui lòng nhập địa chỉ" },
              { min: 5, message: "Địa chỉ phải có ít nhất 5 ký tự" }
            ]}
          >
            <Input.TextArea
              placeholder="Nhập địa chỉ cụ thể..."
              autoSize={{ minRows: 2, maxRows: 4 }}
            />
          </Form.Item>

          <Form.Item
            label="Số điện thoại liên hệ"
            name="contact"
            rules={[
              { required: true, message: "Vui lòng nhập số điện thoại" },
              { pattern: /(84|0[3|5|7|8|9])+([0-9]{8})\b/, message: "Số điện thoại không hợp lệ" }
            ]}
          >
            <Input placeholder="Nhập số điện thoại (VD: 0987654321)" />
          </Form.Item>

          <div style={{ display: 'flex', gap: '16px' }}>
            <Form.Item
              label="Giờ mở cửa"
              name="openHour"
              rules={[{ required: true, message: "Vui lòng chọn giờ mở cửa" }]}
              style={{ flex: 1 }}
            >
              <Select placeholder="Chọn giờ mở cửa">
                {Array.from({ length: 24 }, (_, i) => (
                  <Option key={i} value={i}>
                    {i.toString().padStart(2, '0')}:00
                  </Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item
              label="Giờ đóng cửa"
              name="closeHour"
              rules={[
                { required: true, message: "Vui lòng chọn giờ đóng cửa" }
              ]}
              style={{ flex: 1 }}
            >
              <Select placeholder="Chọn giờ đóng cửa">
                {Array.from({ length: 24 }, (_, i) => (
                  <Option key={i} value={i}>
                    {i.toString().padStart(2, '0')}:00
                  </Option>
                ))}
              </Select>
            </Form.Item>
          </div>

          <Form.Item
            label="Thời lượng mỗi slot (phút)"
            name="slotDuration"
            rules={[
              { required: true, message: "Vui lòng chọn thời lượng slot" }
            ]}
          >
            <Select placeholder="Chọn thời lượng mỗi slot">
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
            initialValue={1}
            rules={[{ required: true, message: "Vui lòng chọn trạng thái" }]}
          >
            <Select placeholder="Chọn trạng thái">
              <Option value={1}>Hoạt động</Option>
              <Option value={2}>Bị khóa</Option>
            </Select>
          </Form.Item>

          <Form.Item
            label="Ảnh cơ sở"
            name="images"
          >
            <Upload
              listType="picture-card"
              fileList={selectedImages}
              onChange={handleImageChange}
              beforeUpload={() => false} // Prevent auto upload
              multiple
              accept="image/*"
              maxCount={10}
              onPreview={(file) => {
                // Tạo URL preview cho file
                const previewUrl = file.url || file.preview || URL.createObjectURL(file.originFileObj);
                handlePreviewImage(previewUrl, file.name);
              }}
              onRemove={(file) => {
                // Cleanup URL object nếu có
                if (file.preview && file.preview.startsWith('blob:')) {
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
            <div style={{ marginTop: 8, color: '#666', fontSize: '12px' }}>
              * Chọn tối đa 10 ảnh, mỗi ảnh không quá 5MB. Click vào ảnh để xem trước.
            </div>
          </Form.Item>

          <Form.Item>
            <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
              <Button onClick={handleModalClose}>
                Hủy
              </Button>
              <Button
                type="primary"
                htmlType="submit"
                loading={submitLoading}
              >
                {selectedImages.length > 0
                  ? `Thêm cơ sở + ${selectedImages.length} ảnh`
                  : 'Thêm mới cơ sở'
                }
              </Button>
            </div>
          </Form.Item>
        </Form>
      </Modal>
      {/* Modal chỉnh sửa cơ sở */}
      <Modal
        title={`Chỉnh sửa cơ sở: ${editingFacility?.facilityName || ''}`}
        open={editModalVisible}
        onCancel={handleEditModalClose}
        footer={null}
        width={900} // **TĂNG WIDTH** để chứa ảnh
        confirmLoading={editLoading}
      >
        <Form
          form={editForm}
          layout="vertical"
          className="facility-form"
          onFinish={handleUpdateFacility}
        >
          {/* CÁC FORM ITEM HIỆN TẠI - GIỮ NGUYÊN */}
          <Form.Item
            label="Tên cơ sở"
            name="facilityName"
            rules={[
              { required: true, message: "Vui lòng nhập tên cơ sở" },
              { min: 2, message: "Tên cơ sở phải có ít nhất 2 ký tự" }
            ]}
          >
            <Input placeholder="Nhập tên cơ sở..." />
          </Form.Item>

          <Form.Item
            label="Địa chỉ"
            name="location"
            rules={[
              { required: true, message: "Vui lòng nhập địa chỉ" },
              { min: 5, message: "Địa chỉ phải có ít nhất 5 ký tự" }
            ]}
          >
            <Input.TextArea
              placeholder="Nhập địa chỉ cụ thể..."
              autoSize={{ minRows: 2, maxRows: 4 }}
            />
          </Form.Item>

          <Form.Item
            label="Số điện thoại liên hệ"
            name="contact"
            rules={[
              { required: true, message: "Vui lòng nhập số điện thoại" },
              { pattern: /(84|0[3|5|7|8|9])+([0-9]{8})\b/, message: "Số điện thoại không hợp lệ" }
            ]}
          >
            <Input placeholder="Nhập số điện thoại (VD: 0987654321)" />
          </Form.Item>

          <Form.Item
            label="Trạng thái"
            name="statusId"
            rules={[{ required: true, message: "Vui lòng chọn trạng thái" }]}
          >
            <Select placeholder="Chọn trạng thái">
              <Option value={1}>Hoạt động</Option>
              <Option value={2}>Bị khóa</Option>
            </Select>
          </Form.Item>

          {/* **THÊM PHẦN QUẢN LÝ ẢNH MỚI** */}
          <Form.Item label="Ảnh cơ sở">
            <div className="facility-images-manager">
              {/* Hiển thị ảnh hiện tại */}
              {facilityImages.length > 0 ? (
                <div className="current-images">
                  <h4 style={{ marginBottom: 16, color: '#1890ff' }}>
                    Ảnh hiện tại ({facilityImages.length}):
                  </h4>
                  <Row gutter={[16, 16]}>
                    {facilityImages.map((image) => (
                      <Col span={6} key={image.imageId}>
                        <Card
                          hoverable
                          cover={
                            <div style={{ height: 120, overflow: 'hidden' }}>
                              <img
                                src={image.imageUrl}
                                alt={image.caption || 'Facility image'}
                                style={{
                                  width: '100%',
                                  height: '100%',
                                  objectFit: 'cover',
                                  cursor: 'pointer'
                                }}
                                onClick={() => handlePreviewImage(image.imageUrl, image.caption)}
                                onError={(e) => {
                                  e.target.src = "https://placehold.co/300x200?text=Error+Loading";
                                }}
                              />
                            </div>
                          }
                          actions={[
                            <EyeOutlined
                              key="view"
                              onClick={() => handlePreviewImage(image.imageUrl, image.caption)}
                              style={{ color: '#1890ff' }}
                              title="Xem ảnh"
                            />,
                            <DeleteOutlined
                              key="delete"
                              onClick={() => handleDeleteImage(image.imageId, image.caption)}
                              style={{ color: '#ff4d4f' }}
                              title="Xóa ảnh"
                            />
                          ]}
                          size="small"
                        >
                          <Card.Meta
                            description={
                              <div style={{ fontSize: '12px', textAlign: 'center' }}>
                                {image.caption || 'Không có mô tả'}
                              </div>
                            }
                          />
                        </Card>
                      </Col>
                    ))}
                  </Row>
                </div>
              ) : (
                <div style={{
                  textAlign: 'center',
                  padding: '20px',
                  background: '#fafafa',
                  borderRadius: '6px',
                  border: '1px dashed #d9d9d9'
                }}>
                  <p style={{ color: '#999', margin: 0 }}>Cơ sở này chưa có ảnh nào</p>
                </div>
              )}

              {/* Upload ảnh mới */}
              <div className="upload-new-images" style={{ marginTop: 20 }}>
                <h4 style={{ marginBottom: 12, color: '#52c41a' }}>Thêm ảnh mới:</h4>
                <Upload
                  listType="picture-card"
                  fileList={uploadFileList}
                  onChange={handleUploadChange}
                  beforeUpload={() => false} // Prevent auto upload
                  multiple
                  accept="image/*"
                  maxCount={8}
                  onPreview={(file) => {
                    // Preview cho file upload mới
                    const previewUrl = file.url || file.preview || URL.createObjectURL(file.originFileObj);
                    handlePreviewImage(previewUrl, file.name);
                  }}
                // **BỎ DÒNG disabled** 
                >
                  {uploadFileList.length >= 8 ? null : (
                    <div>
                      <UploadOutlined />
                      <div style={{ marginTop: 8 }}>Upload</div>
                    </div>
                  )}
                </Upload>
                <p style={{ color: '#999', fontSize: '12px', marginTop: 8 }}>
                  * Chọn tối đa 8 ảnh mới để thêm vào cơ sở
                </p>
              </div>
            </div>
          </Form.Item>

          <Form.Item>
            <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
              <Button onClick={handleEditModalClose}>
                Hủy
              </Button>
              <Button
                type="primary"
                htmlType="submit"
                loading={editLoading}
              >
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