import React, { useState, useEffect } from "react";
import {
  Table,
  Input,
  Select,
  Modal,
  message,
  Button,
  Card,
  Row,
  Col,
  Space,
  Typography,
  Form,
  Tag,
  Tooltip,
} from "antd";
import {
  PlusOutlined,
  SearchOutlined,
  EditOutlined,
  DeleteOutlined,
  ReloadOutlined,
  TagOutlined,
  ExclamationCircleOutlined,
} from "@ant-design/icons";
import {
  getAllCourtCategories,
  addCourtCategory,
  updateCourtCategory,
  getCourtCategoryById,
  deleteCourtCategory,
} from "../../services/apiService";
import "./ManageCourtCategories.scss";

const { Title, Text } = Typography;
const { Option } = Select;
const { confirm } = Modal;

const ManageCourtCategories = () => {
  // State management
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [pageSize, setPageSize] = useState(10);

  // Modal states
  const [modalVisible, setModalVisible] = useState(false);
  const [modalType, setModalType] = useState(""); // 'add' | 'edit'
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

  // Form instance
  const [form] = Form.useForm();

  // Fetch categories on component mount and when dependencies change
  useEffect(() => {
    fetchCategories();
  }, [currentPage, pageSize, searchTerm]);

  // Fetch categories from API
  const fetchCategories = async () => {
    try {
      setLoading(true);
      console.log("🔍 Fetching categories with params:", {
        searchTerm: searchTerm || "",
        currentPage,
        pageSize,
      });

      const response = await getAllCourtCategories(
        searchTerm || "",
        currentPage,
        pageSize
      );

      console.log("📡 API Response:", response);

      if (response && response.success) {
        console.log("✅ Success - Categories data:", response.data);
        setCategories(response.data.items || []);
        setTotalItems(response.data.totalItems || 0);
      } else {
        // Chỉ hiển thị lỗi khi KHÔNG phải search
        if (!searchTerm) {
          message.error(
            response?.message || "Không thể tải danh sách thể loại sân"
          );
        }
        setCategories([]);
        setTotalItems(0);
      }
    } catch (error) {
      console.error("💥 Error fetching categories:", error);

      // Chỉ hiển thị lỗi khi KHÔNG phải search
      if (!searchTerm) {
        if (error.response?.data?.message) {
          message.error(error.response.data.message);
        } else if (error.message?.includes("timeout")) {
          message.error("Yêu cầu đã hết thời gian chờ. Vui lòng thử lại.");
        } else if (error.message?.includes("Network")) {
          message.error(
            "Lỗi kết nối mạng. Vui lòng kiểm tra kết nối internet."
          );
        } else {
          message.error("Có lỗi xảy ra khi tải danh sách thể loại sân");
        }
      }

      setCategories([]);
      setTotalItems(0);
    } finally {
      setLoading(false);
    }
  };

  // Handle search
  const handleSearch = (value) => {
    setSearchTerm(value);
    setCurrentPage(1);
  };

  // Handle page change
  const handleTableChange = (pagination) => {
    setCurrentPage(pagination.current);
    setPageSize(pagination.pageSize);
  };

  // Open modal for different actions
  const openModal = async (type, category = null) => {
    setModalType(type);
    setSelectedCategory(category);

    if (type === "add") {
      form.resetFields();
    } else if (type === "edit" && category) {
      try {
        setLoading(true);
        const response = await getCourtCategoryById(category.categoryId);

        if (response && response.success) {
          form.setFieldsValue({
            categoryName: response.data.categoryName || "",
          });
        } else {
          message.error(
            response?.message || "Không thể tải thông tin thể loại sân"
          );
          return;
        }
      } catch (error) {
        console.error("Error fetching category details:", error);
        message.error("Có lỗi xảy ra khi tải thông tin thể loại sân");
        return;
      } finally {
        setLoading(false);
      }
    }

    setModalVisible(true);
  };

  // Close modal
  const closeModal = () => {
    setModalVisible(false);
    setSelectedCategory(null);
    form.resetFields();
  };

  // Handle form submission
  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      setConfirmLoading(true);

      const submitData = {
        categoryName: values.categoryName.trim(),
      };

      let response;

      if (modalType === "add") {
        response = await addCourtCategory(submitData);
      } else if (modalType === "edit") {
        response = await updateCourtCategory({
          categoryId: selectedCategory.categoryId,
          ...submitData,
        });
      }

      if (response && response.success) {
        message.success(
          response.message ||
            (modalType === "add"
              ? "Thêm thể loại sân thành công!"
              : "Cập nhật thể loại sân thành công!")
        );
        closeModal();
        fetchCategories();
      } else {
        message.error(response?.message || "Có lỗi xảy ra, vui lòng thử lại");
      }
    } catch (error) {
      if (error.errorFields) {
        // Form validation errors
        return;
      }

      console.error("Error submitting form:", error);

      if (error.response?.data?.message) {
        message.error(error.response.data.message);
      } else if (error.message?.includes("timeout")) {
        message.error("Yêu cầu đã hết thời gian chờ. Vui lòng thử lại.");
      } else {
        message.error("Có lỗi xảy ra khi xử lý yêu cầu");
      }
    } finally {
      setConfirmLoading(false);
    }
  };

  // Handle delete with beautiful confirm modal
  const handleDelete = (category) => {
    confirm({
      title: (
        <div className="delete-confirm-title">
          <ExclamationCircleOutlined
            style={{ color: "#ff4d4f", marginRight: 8 }}
          />
          Xác nhận xóa thể loại sân
        </div>
      ),
      content: (
        <div className="delete-confirm-content">
          <p>Bạn có chắc chắn muốn xóa thể loại sân này không?</p>
          <div className="category-info">
            <Tag color="blue" className="category-tag">
              <TagOutlined style={{ marginRight: 4 }} />
              {category.categoryName}
            </Tag>
          </div>
          <div className="warning-text">
            <ExclamationCircleOutlined
              style={{ color: "#faad14", marginRight: 4 }}
            />
            Hành động này không thể hoàn tác!
          </div>
        </div>
      ),
      icon: null,
      okText: "Xác nhận xóa",
      cancelText: "Hủy bỏ",
      okType: "danger",
      okButtonProps: {
        icon: <DeleteOutlined />,
        size: "large",
        style: {
          borderRadius: "8px",
          fontWeight: "600",
          height: "40px",
          minWidth: "120px",
        },
      },
      cancelButtonProps: {
        size: "large",
        style: {
          borderRadius: "8px",
          fontWeight: "600",
          height: "40px",
          minWidth: "80px",
          borderColor: "#27ae60",
          color: "#27ae60",
        },
      },
      width: 500,
      centered: true,
      className: "custom-delete-modal",
      maskClosable: false,
      onOk: async () => {
        try {
          const response = await deleteCourtCategory(category.categoryId);

          if (response && response.success) {
            message.success({
              content: (
                <span>
                  <DeleteOutlined
                    style={{ color: "#52c41a", marginRight: 8 }}
                  />
                  {response.message || "Xóa thể loại sân thành công!"}
                </span>
              ),
              duration: 3,
            });

            // If current page becomes empty, go to previous page
            if (categories.length === 1 && currentPage > 1) {
              setCurrentPage(currentPage - 1);
            } else {
              fetchCategories();
            }
          } else {
            message.error({
              content: response?.message || "Không thể xóa thể loại sân",
              duration: 4,
            });
          }
        } catch (error) {
          console.error("Error deleting category:", error);

          if (error.response?.data?.message) {
            message.error({
              content: error.response.data.message,
              duration: 4,
            });
          } else if (error.message?.includes("timeout")) {
            message.error({
              content: "Yêu cầu đã hết thời gian chờ. Vui lòng thử lại.",
              duration: 4,
            });
          } else {
            message.error({
              content: "Có lỗi xảy ra khi xóa thể loại sân",
              duration: 4,
            });
          }
        }
      },
    });
  };

  // Define table columns
  const columns = [
    {
      title: "ID",
      dataIndex: "categoryId",
      key: "categoryId",
      width: 80,
      render: (id) => (
        <Tag color="blue" className="id-tag">
          #{id}
        </Tag>
      ),
    },
    {
      title: "Tên thể loại",
      dataIndex: "categoryName",
      key: "categoryName",
      render: (name) => (
        <Space>
          <TagOutlined style={{ color: "#27ae60" }} />
          <Text strong>{name}</Text>
        </Space>
      ),
    },
    {
      title: "Thao tác",
      key: "actions",
      width: 150,
      render: (_, record) => (
        <Space size="small">
          <Tooltip title="Chỉnh sửa">
            <Button
              type="default"
              size="small"
              icon={<EditOutlined />}
              onClick={() => openModal("edit", record)}
              className="edit-btn"
            />
          </Tooltip>

          <Tooltip title="Xóa">
            <Button
              type="primary"
              danger
              size="small"
              icon={<DeleteOutlined />}
              onClick={() => handleDelete(record)}
              className="delete-btn"
            />
          </Tooltip>
        </Space>
      ),
    },
  ];

  return (
    <div className="manage-court-categories-antd">
      <Card className="main-card">
        {/* Header */}
        <div className="page-header">
          <Row justify="space-between" align="middle">
            <Col>
              <Title level={3} className="page-title">
                <TagOutlined className="title-icon" />
                Quản lý thể loại sân
              </Title>
            </Col>
            <Col>
              <Button
                type="primary"
                size="large"
                icon={<PlusOutlined />}
                onClick={() => openModal("add")}
                className="add-button"
              >
                Thêm thể loại sân
              </Button>
            </Col>
          </Row>
        </div>

        {/* Search and Filters */}
        <div className="search-section">
          <Row gutter={[16, 16]} align="middle">
            <Col xs={24} sm={12} md={8}>
              <Input.Search
                placeholder="Tìm kiếm theo tên thể loại..."
                allowClear
                enterButton={<SearchOutlined />}
                size="large"
                value={searchTerm}
                onChange={(e) => handleSearch(e.target.value)}
                onSearch={handleSearch}
                className="search-input"
              />
            </Col>
            <Col xs={24} sm={24} md={12} className="stats-section">
              <Space>
                <Text type="secondary">
                  {searchTerm ? (
                    <>
                      Tìm thấy <Text strong>{totalItems}</Text> kết quả cho "
                      {searchTerm}"
                    </>
                  ) : (
                    <>
                      Tổng cộng <Text strong>{totalItems}</Text> thể loại sân
                    </>
                  )}
                </Text>
                <Button
                  icon={<ReloadOutlined />}
                  onClick={fetchCategories}
                  loading={loading}
                  title="Làm mới"
                />
              </Space>
            </Col>
          </Row>
        </div>

        {/* Table */}
        <Table
          columns={columns}
          dataSource={categories}
          loading={loading}
          rowKey="categoryId"
          pagination={{
            current: currentPage,
            pageSize: pageSize,
            total: totalItems,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total, range) =>
              `${range[0]}-${range[1]} của ${total} mục`,
            pageSizeOptions: ["5", "10", "20", "50"],
          }}
          onChange={handleTableChange}
          className="categories-table"
          locale={{
            emptyText: searchTerm
              ? "Không tìm thấy kết quả phù hợp"
              : "Chưa có thể loại sân nào",
          }}
        />
      </Card>

      {/* Modal for Add/Edit */}
      <Modal
        title={
          <Space>
            {modalType === "add" && <PlusOutlined />}
            {modalType === "edit" && <EditOutlined />}
            {modalType === "add" && "Thêm thể loại sân mới"}
            {modalType === "edit" && "Chỉnh sửa thể loại sân"}
          </Space>
        }
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={closeModal}
        confirmLoading={confirmLoading}
        okText={modalType === "add" ? "Thêm mới" : "Lưu thay đổi"}
        cancelText="Hủy"
        width={600}
        className="category-modal"
      >
        <Form form={form} layout="vertical" className="category-form">
          {modalType !== "add" && selectedCategory && (
            <Form.Item label="ID thể loại">
              <Input
                value={selectedCategory.categoryId}
                disabled
                prefix={<TagOutlined />}
                placeholder="ID thể loại"
              />
            </Form.Item>
          )}

          <Form.Item
            label="Tên thể loại"
            name="categoryName"
            rules={[
              { required: true, message: "Vui lòng nhập tên thể loại sân!" },
              { min: 2, message: "Tên thể loại phải có ít nhất 2 ký tự!" },
              {
                max: 100,
                message: "Tên thể loại không được vượt quá 100 ký tự!",
              },
              {
                whitespace: true,
                message: "Tên thể loại không được chỉ chứa khoảng trắng!",
              },
            ]}
          >
            <Input
              prefix={<TagOutlined />}
              placeholder="Nhập tên thể loại sân..."
              maxLength={100}
              showCount
              size="large"
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default ManageCourtCategories;
