import React, { useState, useEffect } from 'react';
import { 
  Table, 
  Button, 
  Modal, 
  Form, 
  TimePicker, 
  Select, 
  Input, 
  Space, 
  Popconfirm, 
  message, 
  Card, 
  Row, 
  Col, 
  Statistic, 
  Tag, 
  Tooltip,
  Switch
} from 'antd';
import {
  ClockCircleOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SettingOutlined,
  CalendarOutlined,
  PlayCircleOutlined,
  PauseCircleOutlined,
  InfoCircleOutlined
} from '@ant-design/icons';
import dayjs from 'dayjs';
import './TimeslotManagement.scss';

const { Option } = Select;
const { TextArea } = Input;

// Mock data - replace with actual API calls
const mockFacilities = [
  { facilityId: 1, facilityName: 'Sân Bóng Đá Thiên Trường', status: 'active' },
  { facilityId: 2, facilityName: 'Sân Tennis Center', status: 'active' },
  { facilityId: 3, facilityName: 'Sân Cầu Lông VIP', status: 'inactive' }
];

const mockTimeslots = [
  {
    id: 1,
    facilityId: 1,
    startTime: '06:00',
    endTime: '07:30',
    duration: 90,
    price: 200000,
    isActive: true,
    description: 'Khung giờ sáng sớm',
    createdAt: '2025-01-01'
  },
  {
    id: 2,
    facilityId: 1,
    startTime: '07:30',
    endTime: '09:00',
    duration: 90,
    price: 250000,
    isActive: true,
    description: 'Khung giờ vàng buổi sáng',
    createdAt: '2025-01-01'
  },
  {
    id: 3,
    facilityId: 1,
    startTime: '18:00',
    endTime: '19:30',
    duration: 90,
    price: 300000,
    isActive: true,
    description: 'Khung giờ vàng buổi chiều',
    createdAt: '2025-01-01'
  }
];

const TimeslotManagement = () => {
  // States
  const [facilities, setFacilities] = useState(mockFacilities);
  const [selectedFacility, setSelectedFacility] = useState(null);
  const [timeslots, setTimeslots] = useState([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingTimeslot, setEditingTimeslot] = useState(null);
  const [form] = Form.useForm();

  // Statistics
  const [stats, setStats] = useState({
    totalSlots: 0,
    activeSlots: 0,
    totalRevenue: 0,
    avgPrice: 0
  });

  // Load timeslots when facility is selected
  useEffect(() => {
    if (selectedFacility) {
      loadTimeslots(selectedFacility);
    }
  }, [selectedFacility]);

  // Calculate stats when timeslots change
  useEffect(() => {
    calculateStats();
  }, [timeslots]);

  const loadTimeslots = async (facilityId) => {
    setLoading(true);
    try {
      // TODO: Replace with actual API call
      const facilityTimeslots = mockTimeslots.filter(slot => slot.facilityId === facilityId);
      setTimeslots(facilityTimeslots);
    } catch (error) {
      message.error('Không thể tải danh sách khung giờ');
    } finally {
      setLoading(false);
    }
  };

  const calculateStats = () => {
    const totalSlots = timeslots.length;
    const activeSlots = timeslots.filter(slot => slot.isActive).length;
    const totalRevenue = timeslots.reduce((sum, slot) => sum + (slot.isActive ? slot.price : 0), 0);
    const avgPrice = activeSlots > 0 ? totalRevenue / activeSlots : 0;

    setStats({
      totalSlots,
      activeSlots,
      totalRevenue,
      avgPrice
    });
  };

  const handleFacilityChange = (facilityId) => {
    setSelectedFacility(facilityId);
    setTimeslots([]);
  };

  const handleAddTimeslot = () => {
    setEditingTimeslot(null);
    form.resetFields();
    setModalVisible(true);
  };

  const handleEditTimeslot = (record) => {
    setEditingTimeslot(record);
    form.setFieldsValue({
      ...record,
      timeRange: [dayjs(record.startTime, 'HH:mm'), dayjs(record.endTime, 'HH:mm')]
    });
    setModalVisible(true);
  };

  const handleDeleteTimeslot = async (id) => {
    try {
      // TODO: Replace with actual API call
      setTimeslots(prev => prev.filter(slot => slot.id !== id));
      message.success('Xóa khung giờ thành công');
    } catch (error) {
      message.error('Không thể xóa khung giờ');
    }
  };

  const handleToggleStatus = async (id, currentStatus) => {
    try {
      // TODO: Replace with actual API call
      setTimeslots(prev => 
        prev.map(slot => 
          slot.id === id ? { ...slot, isActive: !currentStatus } : slot
        )
      );
      message.success(`${!currentStatus ? 'Kích hoạt' : 'Tạm dừng'} khung giờ thành công`);
    } catch (error) {
      message.error('Không thể thay đổi trạng thái khung giờ');
    }
  };

  const handleModalSubmit = async (values) => {
    try {
      const [startTime, endTime] = values.timeRange;
      const duration = endTime.diff(startTime, 'minute');
      
      const timeslotData = {
        facilityId: selectedFacility,
        startTime: startTime.format('HH:mm'),
        endTime: endTime.format('HH:mm'),
        duration,
        price: values.price,
        description: values.description,
        isActive: true
      };

      if (editingTimeslot) {
        // Update existing timeslot
        setTimeslots(prev => 
          prev.map(slot => 
            slot.id === editingTimeslot.id 
              ? { ...slot, ...timeslotData }
              : slot
          )
        );
        message.success('Cập nhật khung giờ thành công');
      } else {
        // Add new timeslot
        const newTimeslot = {
          ...timeslotData,
          id: Date.now(),
          createdAt: new Date().toISOString().split('T')[0]
        };
        setTimeslots(prev => [...prev, newTimeslot]);
        message.success('Thêm khung giờ thành công');
      }

      setModalVisible(false);
      form.resetFields();
    } catch (error) {
      message.error('Có lỗi xảy ra khi lưu khung giờ');
    }
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      minimumFractionDigits: 0
    }).format(amount);
  };

  const columns = [
    {
      title: 'STT',
      key: 'index',
      width: 60,
      render: (_, __, index) => (
        <span className="row-index">{index + 1}</span>
      )
    },
    {
      title: 'Khung giờ',
      key: 'timeRange',
      width: 150,
      render: (record) => (
        <div className="time-range">
          <ClockCircleOutlined className="time-icon" />
          <span className="time-text">
            {record.startTime} - {record.endTime}
          </span>
          <div className="duration">
            ({record.duration} phút)
          </div>
        </div>
      )
    },
    {
      title: 'Giá tiền',
      dataIndex: 'price',
      width: 120,
      render: (price) => (
        <span className="price-text">
          {formatCurrency(price)}
        </span>
      )
    },
    {
      title: 'Mô tả',
      dataIndex: 'description',
      ellipsis: true,
      render: (text) => (
        <Tooltip title={text}>
          <span className="description-text">{text || 'Không có mô tả'}</span>
        </Tooltip>
      )
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 120,
      render: (isActive, record) => (
        <Switch
          checked={isActive}
          onChange={(checked) => handleToggleStatus(record.id, isActive)}
          checkedChildren={<PlayCircleOutlined />}
          unCheckedChildren={<PauseCircleOutlined />}
          className={`status-switch ${isActive ? 'active' : 'inactive'}`}
        />
      )
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 120,
      render: (record) => (
        <Space size="small">
          <Tooltip title="Chỉnh sửa">
            <Button
              type="text"
              icon={<EditOutlined />}
              onClick={() => handleEditTimeslot(record)}
              className="action-btn edit-btn"
            />
          </Tooltip>
          <Tooltip title="Xóa">
            <Popconfirm
              title="Xác nhận xóa"
              description="Bạn có chắc chắn muốn xóa khung giờ này?"
              onConfirm={() => handleDeleteTimeslot(record.id)}
              okText="Xóa"
              cancelText="Hủy"
              okButtonProps={{ danger: true }}
            >
              <Button
                type="text"
                icon={<DeleteOutlined />}
                className="action-btn delete-btn"
              />
            </Popconfirm>
          </Tooltip>
        </Space>
      )
    }
  ];

  const selectedFacilityName = facilities.find(f => f.facilityId === selectedFacility)?.facilityName;

  return (
    <div className="timeslot-management">
      {/* Header */}
      <div className="page-header">
        <div className="header-content">
          <div className="header-title">
            <SettingOutlined className="header-icon" />
            <h1>Quản lý khung giờ</h1>
          </div>
          <p className="header-subtitle">
            Thiết lập và quản lý các khung giờ hoạt động cho từng cơ sở
          </p>
        </div>
      </div>

      {/* Facility Selection */}
      <Card className="facility-selection-card">
        <Row gutter={[24, 16]} align="middle">
          <Col xs={24} sm={12} md={8}>
            <div className="selection-group">
              <label className="selection-label">
                <CalendarOutlined className="label-icon" />
                Chọn cơ sở
              </label>
              <Select
                placeholder="Vui lòng chọn cơ sở..."
                value={selectedFacility}
                onChange={handleFacilityChange}
                className="facility-select"
                size="large"
              >
                {facilities.map(facility => (
                  <Option key={facility.facilityId} value={facility.facilityId}>
                    <div className="facility-option">
                      <span className="facility-name">{facility.facilityName}</span>
                      <Tag 
                        color={facility.status === 'active' ? 'success' : 'default'}
                        className="facility-status"
                      >
                        {facility.status === 'active' ? 'Hoạt động' : 'Tạm dừng'}
                      </Tag>
                    </div>
                  </Option>
                ))}
              </Select>
            </div>
          </Col>
          <Col xs={24} sm={12} md={16}>
            {selectedFacility && (
              <div className="action-group">
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  onClick={handleAddTimeslot}
                  size="large"
                  className="add-btn"
                >
                  Thêm khung giờ mới
                </Button>
              </div>
            )}
          </Col>
        </Row>
      </Card>

      {/* Statistics */}
      {selectedFacility && (
        <Card className="statistics-card">
          <div className="stats-header">
            <h3>
              <InfoCircleOutlined className="stats-icon" />
              Thống kê khung giờ - {selectedFacilityName}
            </h3>
          </div>
          <Row gutter={[24, 16]}>
            <Col xs={12} sm={6}>
              <Statistic
                title="Tổng khung giờ"
                value={stats.totalSlots}
                prefix={<ClockCircleOutlined />}
                valueStyle={{ color: '#1890ff' }}
                className="stat-item"
              />
            </Col>
            <Col xs={12} sm={6}>
              <Statistic
                title="Đang hoạt động"
                value={stats.activeSlots}
                prefix={<PlayCircleOutlined />}
                valueStyle={{ color: '#52c41a' }}
                className="stat-item"
              />
            </Col>
            <Col xs={12} sm={6}>
              <Statistic
                title="Tổng doanh thu/ngày"
                value={stats.totalRevenue}
                formatter={(value) => formatCurrency(value)}
                valueStyle={{ color: '#f5222d' }}
                className="stat-item"
              />
            </Col>
            <Col xs={12} sm={6}>
              <Statistic
                title="Giá trung bình"
                value={stats.avgPrice}
                formatter={(value) => formatCurrency(value)}
                valueStyle={{ color: '#722ed1' }}
                className="stat-item"
              />
            </Col>
          </Row>
        </Card>
      )}

      {/* Timeslots Table */}
      {selectedFacility && (
        <Card className="table-card">
          <div className="table-header">
            <h3>Danh sách khung giờ</h3>
            {timeslots.length > 0 && (
              <Tag color="blue" className="count-tag">
                {timeslots.length} khung giờ
              </Tag>
            )}
          </div>
          
          <Table
            columns={columns}
            dataSource={timeslots}
            rowKey="id"
            loading={loading}
            pagination={{
              pageSize: 10,
              showSizeChanger: true,
              showQuickJumper: true,
              showTotal: (total, range) => 
                `${range[0]}-${range[1]} của ${total} khung giờ`,
              className: 'custom-pagination'
            }}
            locale={{
              emptyText: (
                <div className="empty-state">
                  <ClockCircleOutlined className="empty-icon" />
                  <p>Chưa có khung giờ nào</p>
                  <Button 
                    type="primary" 
                    icon={<PlusOutlined />}
                    onClick={handleAddTimeslot}
                  >
                    Thêm khung giờ đầu tiên
                  </Button>
                </div>
              )
            }}
            className="timeslots-table"
          />
        </Card>
      )}

      {/* No Facility Selected */}
      {!selectedFacility && (
        <Card className="no-selection-card">
          <div className="no-selection">
            <CalendarOutlined className="no-selection-icon" />
            <h3>Chưa chọn cơ sở</h3>
            <p>Vui lòng chọn một cơ sở để quản lý khung giờ</p>
          </div>
        </Card>
      )}

      {/* Add/Edit Modal */}
      <Modal
        title={
          <div className="modal-title">
            <ClockCircleOutlined className="modal-icon" />
            {editingTimeslot ? 'Chỉnh sửa khung giờ' : 'Thêm khung giờ mới'}
          </div>
        }
        open={modalVisible}
        onCancel={() => {
          setModalVisible(false);
          form.resetFields();
        }}
        footer={null}
        width={600}
        className="timeslot-modal"
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleModalSubmit}
          className="timeslot-form"
        >
          <Form.Item
            label="Khung giờ"
            name="timeRange"
            rules={[
              { required: true, message: 'Vui lòng chọn khung giờ' },
              {
                validator: (_, value) => {
                  if (value && value[0] && value[1]) {
                    if (value[1].isBefore(value[0])) {
                      return Promise.reject('Giờ kết thúc phải sau giờ bắt đầu');
                    }
                    if (value[1].diff(value[0], 'minute') < 30) {
                      return Promise.reject('Khung giờ phải ít nhất 30 phút');
                    }
                  }
                  return Promise.resolve();
                }
              }
            ]}
          >
            <TimePicker.RangePicker
              format="HH:mm"
              placeholder={['Giờ bắt đầu', 'Giờ kết thúc']}
              size="large"
              className="time-picker"
            />
          </Form.Item>

          <Form.Item
            label="Giá tiền (VND)"
            name="price"
            rules={[
              { required: true, message: 'Vui lòng nhập giá tiền' },
              { type: 'number', min: 1000, message: 'Giá tiền phải ít nhất 1,000 VND' }
            ]}
          >
            <Input
              type="number"
              placeholder="Nhập giá tiền..."
              size="large"
              addonAfter="VND"
              className="price-input"
            />
          </Form.Item>

          <Form.Item
            label="Mô tả"
            name="description"
            rules={[
              { max: 200, message: 'Mô tả không được quá 200 ký tự' }
            ]}
          >
            <TextArea
              placeholder="Nhập mô tả cho khung giờ này..."
              rows={3}
              showCount
              maxLength={200}
              className="description-input"
            />
          </Form.Item>

          <Form.Item className="form-actions">
            <Space size="middle">
              <Button
                onClick={() => {
                  setModalVisible(false);
                  form.resetFields();
                }}
                size="large"
              >
                Hủy bỏ
              </Button>
              <Button 
                type="primary" 
                htmlType="submit"
                size="large"
                className="submit-btn"
              >
                {editingTimeslot ? 'Cập nhật' : 'Thêm mới'}
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default TimeslotManagement;