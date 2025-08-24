import React, { useState, useEffect } from "react";
import "./AccountTable.scss";
import {
  Table,
  Input,
  Modal,
  message,
  Tooltip,
  Typography,
  Select,
  Button,
  Form,
  Upload,
} from "antd";
import {
  SearchOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
  LockOutlined,
  UnlockOutlined,
  EditOutlined,
  PlusOutlined,
  UploadOutlined,
} from "@ant-design/icons";
import {
  getSliderList,
  getSliderById,
  deleteSlider,
  deactivateSlider,
  activateSlider,
  createSlider,
  updateSlider,
  uploadslideImage,
  updateSlideImage,
} from "../../services/apiService";
import altImg from "../../assets/images/sports-tools.jpg";

const { Text } = Typography;
const { Option } = Select;

const STATUS_MAP = {
  Active: 1,
  Inactive: 2,
};

const ModalContent = ({ title, content, type, tagLabel }) => (
  <div className="custom-modal-content">
    <div className="modal-title-row">
      <ExclamationCircleOutlined className="modal-icon" />
      <span className="modal-title">{title}</span>
    </div>
    <div className="modal-body">
      <p>{content}</p>
      {tagLabel && (
        <div className={`modal-tag-box ${type}`}>
          {type === "delete" && <DeleteOutlined />}
          {type === "deactivate" && <LockOutlined />}
          {type === "activate" && <UnlockOutlined />}
          <span>{tagLabel}</span>
        </div>
      )}
      <div className="modal-warning-box">
        <ExclamationCircleOutlined />
        <span>Hành động này không thể hoàn tác!</span>
      </div>
    </div>
  </div>
);

const SliderForm = ({ initialValues, onSubmit, loading }) => {
  const [form] = Form.useForm();
  const [fileList, setFileList] = useState([]);
  const [submitting, setSubmitting] = useState(false);
  const [hasNewImage, setHasNewImage] = useState(false); // Track if user selected new image

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

  useEffect(() => {
    form.setFieldsValue(initialValues);
    setHasNewImage(false); // Reset flag when initialValues change

    if (initialValues?.imageUrl) {
      setFileList([
        {
          uid: "-1",
          name: "existing-image.jpg",
          status: "done",
          url: convertGoogleDriveUrl(initialValues.imageUrl),
          isExisting: true, // Mark as existing image
        },
      ]);
    } else {
      setFileList([]);
    }
  }, [initialValues]);

  const handleChange = ({ fileList: newFileList }) => {
    setFileList(newFileList);

    // Check if user selected a new file
    const hasNewFile = newFileList.some(
      (file) => file.originFileObj && !file.isExisting
    );
    setHasNewImage(hasNewFile);

    console.log("File list changed:", newFileList);
    console.log("Has new image:", hasNewFile);
  };

  const handleSubmit = async (values) => {
    setSubmitting(true);
    const isEdit = !!initialValues?.slideId;

    try {
      // 1. Chuẩn bị payload cho slider
      const sliderPayload = {
        slideDescription: values.slideDescription,
        slideUrl: values.slideUrl,
      };

      // 2. Tạo hoặc cập nhật slider
      let sliderResponse;
      let sliderId;
      if (isEdit) {
        sliderResponse = await updateSlider(
          initialValues.slideId,
          sliderPayload
        );
        sliderId = initialValues.slideId;
      } else {
        sliderResponse = await createSlider(sliderPayload);
        sliderId = sliderResponse?.data;
      }

      if (![200, 201].includes(sliderResponse?.status)) {
        throw new Error(`Slider API trả về status ${sliderResponse?.status}`);
      }

      // 3. Xử lý ảnh (nếu có)
      let imageSuccess = null;

      if (hasNewImage) {
        const newFile = fileList.find((f) => f.originFileObj && !f.isExisting);

        if (newFile) {
          try {
            let imageResponse;
            const imageId = isEdit ? initialValues?.imageId : null;
            console.log("👉 Image ID dùng để xử lý:", imageId);

            if (isEdit && imageId) {
              imageResponse = await updateSlideImage(
                imageId,
                newFile.originFileObj
              );
            } else {
              imageResponse = await uploadslideImage(
                newFile.originFileObj,
                sliderId
              );
            }

            // ❌ Bỏ kiểm tra status vì nó undefined
            console.log("Upload/update ảnh thành công:", imageResponse);
            imageSuccess = true;
          } catch (imgErr) {
            console.error("Image error:", imgErr);
            imageSuccess = false;
          }
        } else {
          console.log(
            "hasNewImage nhưng không tìm thấy file để upload → không lỗi"
          );
          imageSuccess = true;
        }
      }
      // 4. Hiển thị thông báo tổng kết
      if (imageSuccess === false) {
        message.warning(
          `${
            isEdit ? "Cập nhật" : "Tạo"
          } slider thành công, nhưng lỗi khi xử lý ảnh`
        );
      } else {
        message.success(
          `${isEdit ? "Cập nhật" : "Tạo"} slider${
            hasNewImage ? " và ảnh" : ""
          } thành công`
        );
      }

      // 5. Reset và callback
      form.resetFields();
      setFileList([]);
      setHasNewImage(false);
      onSubmit(sliderResponse.data);
    } catch (err) {
      console.error("Submit error:", err);
      const detail = err.response?.data?.message || err.message;
      message.error(
        `${isEdit ? "Cập nhật" : "Tạo"} slider thất bại: ${detail}`
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Form layout="vertical" form={form} onFinish={handleSubmit}>
      <Form.Item
        name="slideDescription"
        label="Tiêu đề"
        rules={[{ required: true, message: "Vui lòng nhập tiêu đề" }]}
      >
        <Input />
      </Form.Item>

      <Form.Item
        name="slideUrl"
        label="Url"
        rules={[{ required: true, message: "Vui lòng nhập URL" }]}
      >
        <Input />
      </Form.Item>

      <Form.Item name="imageUrl" label="Ảnh">
        <Upload
          listType="picture"
          beforeUpload={() => false}
          fileList={fileList}
          onChange={handleChange}
          showUploadList={{ showPreviewIcon: false }}
          maxCount={1}
        >
          <Button icon={<UploadOutlined />}>
            {fileList.length > 0 ? "Thay đổi ảnh" : "Chọn ảnh"}
          </Button>
        </Upload>
        {hasNewImage && (
          <div style={{ marginTop: 8, color: "#1890ff", fontSize: 12 }}>
            ✓ Đã chọn ảnh mới
          </div>
        )}
      </Form.Item>

      <Form.Item>
        <Button
          type="primary"
          htmlType="submit"
          loading={submitting || loading}
          disabled={submitting || loading}
        >
          {submitting ? "Đang xử lý..." : "Lưu"}
        </Button>
      </Form.Item>
    </Form>
  );
};

const SliderManagement = () => {
  useEffect(() => {
    document.title = "Quản lý Slider - B2P";
  }, []);
  const [sliders, setSliders] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState(null);
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 5,
    total: 0,
    showSizeChanger: true,
  });
  const [formModalVisible, setFormModalVisible] = useState(false);
  const [editingSlider, setEditingSlider] = useState(null);

  const fetchSliders = async () => {
    try {
      setLoading(true);
      const requestData = {
        search: searchText,
        pageNumber: pagination.current,
        pageSize: pagination.pageSize,
        statusId: STATUS_MAP[statusFilter] || null,
      };
      const response = await getSliderList(requestData);
      if (response?.data) {
        setSliders(response.data.items || []);
        setPagination((prev) => ({
          ...prev,
          total: response.data.totalItems || 0,
        }));
      }
    } catch (error) {
      message.error("Có lỗi xảy ra khi tải danh sách slider");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSliders();
  }, [searchText, pagination.current, pagination.pageSize, statusFilter]);

  const handleSearch = (value) => {
    setSearchText(value);
    setPagination((prev) => ({ ...prev, current: 1 }));
  };

  const handleStatusFilterChange = (value) => {
    setStatusFilter(value);
    setPagination((prev) => ({ ...prev, current: 1 }));
  };

  const handleTableChange = (newPagination) => {
    setPagination({
      ...pagination,
      current: newPagination.current,
      pageSize: newPagination.pageSize,
    });
  };

  const handleToggleStatus = (record) => {
    const isActive = record.statusName === "Active";
    Modal.confirm({
      icon: null,
      centered: true,
      content: (
        <ModalContent
          title={`Xác nhận ${isActive ? "vô hiệu hóa" : "kích hoạt"} slider`}
          content={`Bạn có chắc chắn muốn ${
            isActive ? "vô hiệu hóa" : "kích hoạt"
          } slider này không?`}
          type={isActive ? "deactivate" : "activate"}
          tagLabel={record.slideDescription}
        />
      ),
      okText: isActive ? "Xác nhận vô hiệu hóa" : "Xác nhận kích hoạt",
      cancelText: "Hủy bỏ",
      onOk: async () => {
        try {
          setLoading(true);
          const res = isActive
            ? await deactivateSlider(record.slideId)
            : await activateSlider(record.slideId);
          if (res?.status === 200) {
            message.success(
              `${isActive ? "Vô hiệu hóa" : "Kích hoạt"} thành công`
            );
            fetchSliders();
          }
        } catch (e) {
          message.error("Lỗi khi cập nhật trạng thái slider");
        } finally {
          setLoading(false);
        }
      },
    });
  };

  const handleDelete = (record) => {
    if (record.statusName === "Active") {
      Modal.warning({
        centered: true,
        title: "Không thể xóa slider",
        content: (
          <>
            <p>
              Slider <strong>{record.slideDescription}</strong> hiện đang hoạt
              động.
            </p>
            <p>Vui lòng vô hiệu hóa trước khi thực hiện thao tác xóa.</p>
          </>
        ),
        okText: "Đã hiểu",
      });
      return;
    }

    Modal.confirm({
      icon: null,
      centered: true,
      content: (
        <ModalContent
          title="Xác nhận xóa slider"
          content="Bạn có chắc chắn muốn xóa slider này không?"
          type="delete"
          tagLabel={record.slideDescription}
        />
      ),
      okText: "Xác nhận xóa",
      cancelText: "Hủy bỏ",
      onOk: async () => {
        try {
          const res = await deleteSlider(record.slideId);
          if (res?.status === 200) {
            message.success("Xóa thành công");
            fetchSliders();
          }
        } catch (e) {
          message.error("Lỗi khi xóa slider");
        }
      },
    });
  };

  const openFormModal = async (slider = null) => {
    if (slider) {
      try {
        // Gọi API để lấy thông tin đầy đủ của slider
        const response = await getSliderById(slider.slideId);
        if (response?.status === 200 && response.data) {
          const sliderDetails = response.data;
          setEditingSlider({
            ...sliderDetails,
            // Thêm imageId nếu có trong response
            imageId:
              sliderDetails.imageId || sliderDetails.images?.[0]?.imageId,
          });
        } else {
          // Fallback nếu API fail
          setEditingSlider(slider);
        }
      } catch (error) {
        console.error("Error fetching slider details:", error);
        message.error("Có lỗi khi tải thông tin slider");
        setEditingSlider(slider);
      }
    } else {
      setEditingSlider(null);
    }
    setFormModalVisible(true);
  };

  const handleFormSubmit = async (data) => {
    setFormModalVisible(false);
    setEditingSlider(null);
    // Force refresh danh sách sau khi create/update thành công
    await fetchSliders();
  };

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

  const columns = [
    {
      title: "STT",
      key: "index",
      render: (_, __, index) => (
        <strong style={{ color: "#52c41a" }}>
          {(pagination.current - 1) * pagination.pageSize + index + 1}
        </strong>
      ),
      align: "center",
    },
    {
      title: "Mã slider",
      dataIndex: "slideId",
      key: "slideId",
      align: "center",
    },
    {
      title: "Tiêu đề",
      dataIndex: "slideDescription",
      key: "slideDescription",
      align: "center",
    },
    {
      title: "URL",
      dataIndex: "slideUrl",
      key: "slideUrl",
      align: "center",
    },
    {
      title: "Ảnh",
      dataIndex: "imageUrl",
      key: "imageUrl",
      align: "center",
      render: (text) => {
        const imageSrc = convertGoogleDriveUrl(text);
        return imageSrc ? (
          <img
            src={imageSrc}
            alt="slider"
            style={{
              width: 160,
              height: 100,
              objectFit: "cover",
              borderRadius: 6,
            }}
            onError={(e) => {
              e.target.onerror = null;
              e.target.src = altImg;
            }}
          />
        ) : (
          <img
            src={altImg}
            alt="no-img"
            style={{ width: 160, height: 100, opacity: 0.6, borderRadius: 6 }}
          />
        );
      },
    },

    {
      title: "Trạng thái",
      key: "status",
      align: "center",
      render: (_, record) => (
        <div className="status-icon" onClick={() => handleToggleStatus(record)}>
          {record.statusName === "Active" ? (
            <Tooltip title="Hoạt động">
              <UnlockOutlined className="anticon-unlock" />
            </Tooltip>
          ) : (
            <Tooltip title="Không hoạt động">
              <LockOutlined className="anticon-lock" />
            </Tooltip>
          )}
        </div>
      ),
    },
    {
      title: "Hành động",
      key: "actions",
      align: "center",
      render: (_, record) => (
        <div className="action-buttons">
          <Tooltip title="Sửa">
            <EditOutlined
              className="edit-icon"
              onClick={() => openFormModal(record)}
            />
          </Tooltip>
          <Tooltip title="Xóa">
            <DeleteOutlined
              className="delete-icon"
              onClick={() => handleDelete(record)}
            />
          </Tooltip>
        </div>
      ),
    },
  ];

  return (
    <div className="account-table-wrapper">
      <div className="account-table-card">
        <div className="page-header">
          <h1>Quản lý slider</h1>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => openFormModal()}
          >
            Thêm slider mới
          </Button>
          <Text type="secondary">
            {searchText ? (
              <>
                Tìm thấy <Text strong>{pagination.total}</Text> kết quả cho "
                {searchText}"
              </>
            ) : (
              <>
                Tổng cộng <Text strong>{pagination.total}</Text> slider
              </>
            )}
          </Text>
        </div>

        <div className="table-filters">
          <Input.Search
            placeholder="Tìm kiếm slider"
            prefix={<SearchOutlined />}
            className="search-input"
            allowClear
            enterButton={<SearchOutlined />}
            value={searchText}
            onChange={(e) => handleSearch(e.target.value)}
            onSearch={handleSearch}
          />
          <div className="filter-section">
            <Select
              placeholder="Lọc trạng thái"
              allowClear
              onChange={handleStatusFilterChange}
              value={statusFilter}
            >
              <Option value={null}>Tất cả</Option>
              <Option value="Active">Active</Option>
              <Option value="Inactive">Inactive</Option>
            </Select>
          </div>
        </div>

        <Table
          columns={columns}
          dataSource={sliders}
          loading={loading}
          pagination={{
            ...pagination,
            pageSizeOptions: ["5", "10", "20"],
          }}
          onChange={handleTableChange}
          rowKey="slideId"
          locale={{
            emptyText: searchText
              ? "Không tìm thấy slider phù hợp"
              : "Không có dữ liệu slider",
          }}
        />

        <Modal
          open={formModalVisible}
          title={editingSlider ? "Chỉnh sửa slider" : "Thêm slider mới"}
          footer={null}
          onCancel={() => {
            setFormModalVisible(false);
            setEditingSlider(null);
          }}
          destroyOnClose
        >
          <SliderForm
            initialValues={
              editingSlider || {
                slideDescription: "",
                slideUrl: "",
                imageUrl: "",
              }
            }
            onSubmit={handleFormSubmit}
            loading={loading}
          />
        </Modal>
      </div>
    </div>
  );
};

export default SliderManagement;
