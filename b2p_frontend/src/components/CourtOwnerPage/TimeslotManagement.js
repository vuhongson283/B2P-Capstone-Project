import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { getFacilitiesByCourtOwnerId, getTimeslotsByFacilityId, createTimeslot, deleteTimeslot, updateTimeslot } from '../../services/apiService';
import {
  Table,
  Button,
  Modal,
  Form,
  TimePicker,
  Select,
  Space,
  Alert,
  Popconfirm,
  message,
  Card,
  Typography,
  Row,
  Col,
  Statistic,
  Tag,
  Tooltip,
  Switch,
  InputNumber,
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
  InfoCircleOutlined,
  PercentageOutlined,
  QuestionCircleOutlined,
  FilterOutlined
} from '@ant-design/icons';
import dayjs from 'dayjs';
import './TimeslotManagement.scss';
import { set } from 'nprogress';
const { Text } = Typography;
const { Option } = Select;

// ✅ Updated status mapping
const STATUS_CONFIG = {
  1: { name: 'Active', description: 'Hoạt động', color: 'success' },
  2: { name: 'Inactive', description: 'Tạm dừng', color: 'default' }
};

const TimeslotManagement = () => {
  // States
  const [facilities, setFacilities] = useState([]);
  const [selectedFacility, setSelectedFacility] = useState(null);
  const [selectedStatus, setSelectedStatus] = useState(null); // ✅ Status filter state
  const [timeslots, setTimeslots] = useState([]);
  const [loading, setLoading] = useState(false);
  const [facilitiesLoading, setFacilitiesLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingTimeslot, setEditingTimeslot] = useState(null);
  const [form] = Form.useForm();
  const { facilityId } = useParams();
  const navigate = useNavigate();
  const location = useLocation();

  // Statistics
  const [stats, setStats] = useState({
    totalSlots: 0,
    activeSlots: 0,
    avgDiscount: 0
  });

  // ✅ Get Court Owner ID from localStorage
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

  // Load facilities on component mount
  useEffect(() => {
    loadFacilities();
  }, []);

  // ✅ Handle URL params after facilities loaded
  useEffect(() => {
    if (facilityId && facilities.length > 0) {
      const facilityIdNum = parseInt(facilityId);
      const facilityExists = facilities.some(f => f.facilityId === facilityIdNum);

      if (facilityExists) {
        setSelectedFacility(facilityIdNum);

        // Get status from URL
        const searchParams = new URLSearchParams(location.search);
        const status = searchParams.get('status');
        if (status) {
          setSelectedStatus(parseInt(status));
        }
      } else {
        // Redirect to first facility if URL facility doesn't exist
        if (facilities.length > 0) {
          navigate(`/court-owner/facility/time-slots/${facilities[0].facilityId}`, { replace: true });
        }
      }
    }
  }, [facilityId, facilities, location.search, navigate]);

  // ✅ Load timeslots when facility OR status filter changes
  useEffect(() => {
    if (selectedFacility) {
      loadTimeslots(selectedFacility, selectedStatus); // Pass status filter
    } else {
      setTimeslots([]);
    }
  }, [selectedFacility, selectedStatus]); // ✅ Add selectedStatus dependency

  // Calculate stats when timeslots change
  useEffect(() => {
    calculateStats();
  }, [timeslots]);

  // Load facilities from API
  const loadFacilities = async () => {
    setFacilitiesLoading(true);
    try {
      const courtOwnerId = getCourtOwnerId();
      console.log('Loading facilities for court owner:', courtOwnerId);

      const response = await getFacilitiesByCourtOwnerId(
        courtOwnerId,
        "", // facilityName - empty to get all
        1,  // statusId - 1 for active only
        1,  // currentPage
        100 // itemsPerPage - high number to get all
      );

      console.log('Facilities API response:', response.data);

      if (response.data && response.data.items && Array.isArray(response.data.items)) {
        const facilitiesData = response.data.items;

        // Map to simple format for dropdown
        const mappedFacilities = facilitiesData.map((facility) => ({
          facilityId: facility.facilityId,
          facilityName: facility.facilityName,
          status: facility.status?.statusName || 'Active'
        }));

        console.log('Mapped facilities:', mappedFacilities);
        setFacilities(mappedFacilities);

        // ✅ Auto select from URL or first facility
        if (mappedFacilities.length > 0) {
          if (facilityId) {
            const facilityIdNum = parseInt(facilityId);
            const facilityExists = mappedFacilities.some(f => f.facilityId === facilityIdNum);
            if (!facilityExists) {
              navigate(`/court-owner/facility/time-slots/${mappedFacilities[0].facilityId}`, { replace: true });
            }
          } else {
            navigate(`/court-owner/facility/time-slots/${mappedFacilities[0].facilityId}`, { replace: true });
          }
        }
      } else {
        console.log('No facilities found in response');
        message.info('Không tìm thấy cơ sở nào');
        setFacilities([]);
      }
    } catch (error) {
      console.error('Error loading facilities:', error);
      message.error('Có lỗi xảy ra khi tải danh sách cơ sở');
      setFacilities([]);
    } finally {
      setFacilitiesLoading(false);
    }
  };

  // ✅ Updated loadTimeslots to accept statusId parameter
  const loadTimeslots = async (facilityId, statusId = null, pageNumber = 1, pageSize = 100) => {
    setLoading(true);
    try {
      console.log('Loading timeslots for facility:', facilityId, 'status:', statusId);

      const response = await getTimeslotsByFacilityId(facilityId, statusId, pageNumber, pageSize);
      console.log('Timeslots API response:', response.data);

      if (response.data && response.data.items && Array.isArray(response.data.items)) {
        const timeslotsData = response.data.items;
        console.log('Timeslots data:', timeslotsData);
        setTimeslots(timeslotsData);
      } else {
        console.log('No timeslots found in response');
        setTimeslots([]);
      }
    } catch (error) {
      console.error('Error loading timeslots:', error);
      if (error.response?.status === 404) {
        setTimeslots([]);
        message.info('Chưa có khung giờ nào cho cơ sở này');
      } else {
        message.error('Có lỗi xảy ra khi tải danh sách khung giờ');
        setTimeslots([]);
      }
    } finally {
      setLoading(false);
    }
  };

  const calculateStats = () => {
    const totalSlots = timeslots.length;
    const activeSlots = timeslots.filter(slot => slot.statusId === 1).length;
    const slotsWithDiscount = timeslots.filter(slot => slot.discount !== null && slot.discount > 0);
    const avgDiscount = slotsWithDiscount.length > 0
      ? slotsWithDiscount.reduce((sum, slot) => sum + slot.discount, 0) / slotsWithDiscount.length
      : 0;

    setStats({
      totalSlots,
      activeSlots,
      avgDiscount
    });
  };

  const handleFacilityChange = (facilityId) => {
    console.log('Facility changed to:', facilityId);
    setSelectedFacility(facilityId);
    setTimeslots([]);

    // ✅ Update URL with correct nested route
    const params = new URLSearchParams(location.search);
    if (selectedStatus !== null) {
      params.set('status', selectedStatus);
    }
    navigate(`/court-owner/facility/time-slots/${facilityId}${params.toString() ? '?' + params.toString() : ''}`, { replace: true });
  };

  // ✅ Handle status filter change
  const handleStatusFilterChange = (statusId) => {
    console.log('Status filter changed to:', statusId);
    setSelectedStatus(statusId);

    // ✅ Update URL with correct nested route
    const params = new URLSearchParams();
    if (statusId !== null) {
      params.set('status', statusId);
    }
    navigate(`/court-owner/facility/time-slots/${selectedFacility}${params.toString() ? '?' + params.toString() : ''}`, { replace: true });
  };

  const handleAddTimeslot = () => {
    setEditingTimeslot(null);
    form.resetFields();
    // Set default status to Active (1)
    form.setFieldsValue({ statusId: 1 });
    setModalVisible(true);
  };

  const handleEditTimeslot = (record) => {
    console.log('🔄 Editing timeslot:', record);

    setEditingTimeslot(record);

    // Set form values với đúng format
    form.setFieldsValue({
      statusId: record.statusId,
      discount: record.discount || 0,
      startTime: dayjs(record.startTime, 'HH:mm:ss'),
      endTime: dayjs(record.endTime, 'HH:mm:ss')
    });

    setModalVisible(true);
  };

  const handleDeleteTimeslot = async (timeSlotId) => {
    try {
      console.log('🗑️ Deleting timeslot:', timeSlotId);

      const response = await deleteTimeslot(timeSlotId);
      console.log('✅ Delete response:', response);

      if (response.status === 200 || response.status === 204) {
        message.success('🗑️ Xóa khung giờ thành công!');
        await loadTimeslots(selectedFacility, selectedStatus); // ✅ Pass status filter
      } else {
        message.warning('⚠️ Phản hồi không xác định từ server');
        await loadTimeslots(selectedFacility, selectedStatus);
      }

    } catch (error) {
      console.error('💥 Error deleting timeslot:', error);

      if (error.response?.data) {
        const errorData = error.response.data;
        console.log('🚨 Delete error response:', errorData);

        if (error.response.status === 404) {
          message.error('❌ Khung giờ không tồn tại hoặc đã bị xóa');
        } else if (error.response.status === 400) {
          message.error(`❌ ${errorData.message || 'Dữ liệu không hợp lệ'}`);
        } else if (error.response.status === 409) {
          message.error('❌ Không thể xóa khung giờ đang được sử dụng');
        } else if (errorData.message) {
          message.error(`❌ ${errorData.message}`);
        } else {
          message.error('❌ Có lỗi xảy ra từ server khi xóa khung giờ');
        }
      } else if (error.request) {
        message.error('❌ Không thể kết nối đến server');
      } else {
        message.error('❌ Có lỗi xảy ra khi xóa khung giờ');
      }

      await loadTimeslots(selectedFacility, selectedStatus);
    }
  };

  const handleToggleStatus = async (timeSlotId, currentStatusId) => {
    try {
      console.log('🔄 Toggling status for timeslot:', timeSlotId, 'current status:', currentStatusId);

      const newStatusId = currentStatusId === 1 ? 2 : 1;
      const currentTimeslot = timeslots.find(slot => slot.timeSlotId === timeSlotId);

      if (!currentTimeslot) {
        message.error('❌ Không tìm thấy thông tin khung giờ');
        return;
      }

      const updateData = {
        startTime: currentTimeslot.startTime,
        endTime: currentTimeslot.endTime,
        statusId: newStatusId,
        discount: currentTimeslot.discount || 0
      };

      console.log('📤 Update payload:', updateData);

      const response = await updateTimeslot(timeSlotId, updateData);
      console.log('✅ Update response:', response);

      if (response.data && (response.status === 200 || response.status === 201)) {
        const statusText = newStatusId === 1 ? 'Kích hoạt' : 'Tạm dừng';
        message.success(`✅ ${statusText} khung giờ thành công!`);

        setTimeslots(prev =>
          prev.map(slot =>
            slot.timeSlotId === timeSlotId
              ? { ...slot, statusId: newStatusId }
              : slot
          )
        );

      } else {
        message.warning('⚠️ Phản hồi không xác định từ server');
        await loadTimeslots(selectedFacility, selectedStatus);
      }

    } catch (error) {
      console.error('💥 Error toggling timeslot status:', error);

      if (error.response?.data) {
        const errorData = error.response.data;
        console.log('🚨 Update error response:', errorData);

        if (error.response.status === 404) {
          message.error('❌ Khung giờ không tồn tại');
        } else if (error.response.status === 400) {
          message.error(`❌ ${errorData.message || 'Dữ liệu không hợp lệ'}`);
        } else if (error.response.status === 409) {
          message.error(`❌ ${errorData.message || 'Khung giờ bị trùng với TimeSlot khác'}`);
        } else if (errorData.message) {
          message.error(`❌ ${errorData.message}`);
        } else {
          message.error('❌ Có lỗi xảy ra từ server khi cập nhật');
        }
      } else if (error.request) {
        message.error('❌ Không thể kết nối đến server');
      } else {
        message.error('❌ Có lỗi xảy ra khi cập nhật trạng thái');
      }

      await loadTimeslots(selectedFacility, selectedStatus);
    }
  };

  const handleModalSubmit = async (values) => {
    try {
      console.log('Form values:', values);

      const startTime = values.startTime;
      const endTime = values.endTime;

      if (!startTime || !endTime) {
        message.error('⚠️ Vui lòng chọn đầy đủ giờ bắt đầu và kết thúc!');
        return;
      }

      if (endTime.isBefore(startTime) || endTime.isSame(startTime)) {
        message.error('⚠️ Giờ kết thúc phải sau giờ bắt đầu!');
        return;
      }

      const requestData = {
        statusId: values.statusId,
        startTime: startTime.format('HH:mm:ss'),
        endTime: endTime.format('HH:mm:ss'),
        discount: values.discount || 0
      };

      console.log('📝 Request data:', requestData);

      if (editingTimeslot) {
        console.log('🔄 Updating timeslot:', editingTimeslot.timeSlotId);

        const response = await updateTimeslot(editingTimeslot.timeSlotId, requestData);

        console.log('✅ Update response:', response);
        console.log('✅ Update response.status:', response.status);
        console.log('✅ Update response.data:', response.data);

        if (response.status === 200 || response.status === 201) {
          console.log('✅ SUCCESS: Update HTTP status indicates success');
          message.success(`✅ Cập nhật khung giờ ${startTime.format('HH:mm')} - ${endTime.format('HH:mm')} thành công!`);

          setTimeslots(prev =>
            prev.map(slot =>
              slot.timeSlotId === editingTimeslot.timeSlotId
                ? {
                  ...slot,
                  statusId: requestData.statusId,
                  startTime: requestData.startTime,
                  endTime: requestData.endTime,
                  discount: requestData.discount
                }
                : slot
            )
          );

        } else if (response.status === 409) {
          console.log('⚠️ CONFLICT: Update returned 409');

          Modal.warning({
            title: 'Khung giờ bị trùng',
            content: 'Khung giờ bị trùng với TimeSlot đã tồn tại. Vui lòng chọn thời gian khác.',
            okText: 'Đã hiểu',
            zIndex: 9999,
          });

          return;

        } else if (response.data && response.data.success) {
          console.log('✅ SUCCESS: Update response.data indicates success');
          message.success(`✅ Cập nhật khung giờ ${startTime.format('HH:mm')} - ${endTime.format('HH:mm')} thành công!`);

          setTimeslots(prev =>
            prev.map(slot =>
              slot.timeSlotId === editingTimeslot.timeSlotId
                ? {
                  ...slot,
                  statusId: requestData.statusId,
                  startTime: requestData.startTime,
                  endTime: requestData.endTime,
                  discount: requestData.discount
                }
                : slot
            )
          );

        } else {
          console.log('❓ Unknown update response format:', response.data);
          console.log('❓ Response status:', response.status);
          message.warning('⚠️ Phản hồi không xác định từ server - check console');
          await loadTimeslots(selectedFacility, selectedStatus);
        }

      } else {
        const createRequest = {
          facilityId: selectedFacility,
          ...requestData
        };

        console.log('📝 Create request data:', createRequest);

        const response = await createTimeslot(createRequest);

        console.log('🔍 Full response:', response);

        if (response.data && response.data.timeSlotId) {
          console.log('✅ SUCCESS: TimeSlot created with ID:', response.data.timeSlotId);
          message.success(`✅ Thêm khung giờ ${startTime.format('HH:mm')} - ${endTime.format('HH:mm')} thành công!`);
          await loadTimeslots(selectedFacility, selectedStatus); // ✅ Pass status filter
        } else if (response.status === 200 || response.status === 201) {
          console.log('✅ SUCCESS: HTTP status indicates success');
          message.success(`✅ Thêm khung giờ ${startTime.format('HH:mm')} - ${endTime.format('HH:mm')} thành công!`);
          await loadTimeslots(selectedFacility, selectedStatus);
        } else {
          console.log('❓ Unknown response format:', response.data);
          message.warning('⚠️ Phản hồi không xác định - check console');
          await loadTimeslots(selectedFacility, selectedStatus);
        }
      }

      setModalVisible(false);
      form.resetFields();
      setEditingTimeslot(null);

    } catch (error) {
      console.error('💥 Error submitting timeslot:', error);

      if (error.response?.status === 409) {
        const errorMessage = error.response.data?.message || 'Khung giờ bị trùng với TimeSlot đã tồn tại';
        message.warning({
          content: `⚠️ ${errorMessage}`,
          duration: 5,
        });
        console.log('🔄 Keeping modal open for user to adjust time...');
        return;
      }

      if (error.response?.status === 400) {
        const errorMessage = error.response.data?.message || 'Dữ liệu không hợp lệ';
        message.error(`❌ ${errorMessage}`);
      }
      else if (error.response?.data?.message) {
        message.error(`❌ ${error.response.data.message}`);
      }
      else if (error.response) {
        message.error(`❌ Lỗi server (${error.response.status})`);
      }
      else if (error.request) {
        message.error('❌ Không thể kết nối đến server');
      }
      else {
        message.error(`❌ Lỗi: ${error.message}`);
      }

      setModalVisible(false);
      form.resetFields();
      setEditingTimeslot(null);
    }
  };

  const formatTime = (timeString) => {
    // Convert "HH:mm:ss" to "HH:mm"
    return timeString ? timeString.substring(0, 5) : '';
  };

  const calculateDuration = (startTime, endTime) => {
    const start = dayjs(startTime, 'HH:mm:ss');
    const end = dayjs(endTime, 'HH:mm:ss');
    return end.diff(start, 'minute');
  };

  // Helper function to get status info
  const getStatusInfo = (statusId) => {
    return STATUS_CONFIG[statusId] || { name: 'Unknown', description: 'Không xác định', color: 'default' };
  };

  // ✅ Get filtered status text for display
  const getStatusFilterText = () => {
    if (selectedStatus === null) return 'Tất cả trạng thái';
    return STATUS_CONFIG[selectedStatus]?.description || 'Không xác định';
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
      width: 180,
      render: (record) => (
        <div className="time-range">
          <ClockCircleOutlined className="time-icon" />
          <span className="time-text">
            {formatTime(record.startTime)} - {formatTime(record.endTime)}
          </span>
          <div className="duration">
            ({calculateDuration(record.startTime, record.endTime)} phút)
          </div>
        </div>
      )
    },
    {
      title: 'Giảm giá (%)',
      dataIndex: 'discount',
      width: 120,
      render: (discount) => (
        <span className="discount-text">
          {discount ? (
            <Tag color="orange" icon={<PercentageOutlined />}>
              {discount}%
            </Tag>
          ) : (
            <Tag color="default">Không có</Tag>
          )}
        </span>
      )
    },
    {
      title: 'Trạng thái',
      key: 'status',
      width: 140,
      render: (record) => {
        const statusInfo = getStatusInfo(record.statusId);
        return (
          <Switch
            checked={record.statusId === 1}
            onChange={() => handleToggleStatus(record.timeSlotId, record.statusId)}
            checkedChildren={<PlayCircleOutlined />}
            unCheckedChildren={<PauseCircleOutlined />}
            className={`status-switch ${record.statusId === 1 ? 'active' : 'inactive'}`}
          />
        );
      }
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
              onConfirm={() => handleDeleteTimeslot(record.timeSlotId)}
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

      {/* ✅ Updated Facility Selection with Status Filter */}
      <Card className="facility-selection-card">
        <Row gutter={[24, 16]} align="middle">
          <Col xs={24} sm={12} md={6}>
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
                loading={facilitiesLoading}
                notFoundContent={facilitiesLoading ? "Đang tải..." : "Không có cơ sở nào"}
              >
                {facilities.map(facility => (
                  <Option key={facility.facilityId} value={facility.facilityId}>
                    <div className="facility-option">
                      <span className="facility-name">{facility.facilityName}</span>
                      <Tag
                        color={facility.status === 'Active' ? 'success' : 'default'}
                        className="facility-status"
                      >
                        {facility.status === 'Active' ? 'Hoạt động' : 'Tạm dừng'}
                      </Tag>
                    </div>
                  </Option>
                ))}
              </Select>
            </div>
          </Col>

          {/* ✅ Status Filter Dropdown */}
          <Col xs={24} sm={12} md={6}>
            <div className="selection-group">
              <label className="selection-label">
                <FilterOutlined className="label-icon" />
                Lọc trạng thái
              </label>
              <Select
                placeholder="Tất cả trạng thái"
                value={selectedStatus}
                onChange={handleStatusFilterChange}
                className="status-filter-select"
                size="large"
                allowClear
                clearIcon={<div>×</div>}
              >
                <Option value={null}>Tất cả trạng thái</Option>
                {Object.entries(STATUS_CONFIG).map(([id, config]) => (
                  <Option key={id} value={parseInt(id)}>
                    <Space>
                      <Tag color={config.color}>
                        {config.description}
                      </Tag>
                    </Space>
                  </Option>
                ))}
              </Select>
            </div>
          </Col>

          <Col xs={24} sm={12} md={12}>
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
              {/* ✅ Show current filter */}
              {selectedStatus !== null && (
                <Tag color={STATUS_CONFIG[selectedStatus]?.color} style={{ marginLeft: 8 }}>
                  {getStatusFilterText()}
                </Tag>
              )}
            </h3>
          </div>
          <Row gutter={[24, 16]}>
            <Col xs={24} sm={8}>
              <Statistic
                title="Tổng khung giờ"
                value={stats.totalSlots}
                prefix={<ClockCircleOutlined />}
                valueStyle={{ color: '#1890ff' }}
                className="stat-item"
              />
            </Col>
            <Col xs={24} sm={8}>
              <Statistic
                title="Đang hoạt động"
                value={stats.activeSlots}
                prefix={<PlayCircleOutlined />}
                valueStyle={{ color: '#52c41a' }}
                className="stat-item"
              />
            </Col>
            <Col xs={24} sm={8}>
              <Statistic
                title="Giảm giá trung bình"
                value={stats.avgDiscount}
                suffix="%"
                precision={1}
                prefix={<PercentageOutlined />}
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
              <Space>
                <Tag color="blue" className="count-tag">
                  {timeslots.length} khung giờ
                </Tag>
                {/* ✅ Show active filter */}
                {selectedStatus !== null && (
                  <Tag color={STATUS_CONFIG[selectedStatus]?.color}>
                    Lọc: {getStatusFilterText()}
                  </Tag>
                )}
              </Space>
            )}
          </div>

          <Table
            columns={columns}
            dataSource={timeslots}
            rowKey="timeSlotId"
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
                  <p>
                    {selectedStatus !== null
                      ? `Không có khung giờ nào với trạng thái "${getStatusFilterText()}"`
                      : 'Chưa có khung giờ nào'
                    }
                  </p>
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
      {!selectedFacility && !facilitiesLoading && (
        <Card className="no-selection-card">
          <div className="no-selection">
            <CalendarOutlined className="no-selection-icon" />
            <h3>Chưa chọn cơ sở</h3>
            <p>Vui lòng chọn một cơ sở để quản lý khung giờ</p>
          </div>
        </Card>
      )}

      {/* Add/Edit Modal - Same as before */}
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
          setEditingTimeslot(null);
        }}
        footer={null}
        width={650}
        className="timeslot-modal"
        destroyOnClose={true}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleModalSubmit}
          className="timeslot-form"
          initialValues={{
            statusId: 1,
            discount: 0
          }}
        >
          <Alert
            message="💡 Hướng dẫn chọn giờ"
            description={
              <Space direction="vertical" size={4}>
                <Text>• <strong>Click chuột:</strong> Click vào ô input hoặc icon đồng hồ để mở bảng chọn giờ</Text>
                <Text>• <strong>Nhập thủ công:</strong> Gõ trực tiếp <code>08:00</code> vào ô input</Text>
                <Text>• <strong>Bước nhảy:</strong> 30 phút (08:00, 08:30, 09:00, 09:30...)</Text>
                <Text>• <strong>Ví dụ hợp lệ:</strong> <Tag color="green">08:00 → 10:00</Tag> <Tag color="green">14:30 → 16:00</Tag></Text>
              </Space>
            }
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
            closable
          />

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="startTime"
                label={
                  <Space>
                    <ClockCircleOutlined style={{ color: '#52c41a' }} />
                    <span>Giờ bắt đầu</span>
                  </Space>
                }
                rules={[{ required: true, message: "⚠️ Chọn giờ bắt đầu!" }]}
              >
                <TimePicker
                  format="HH:mm"
                  placeholder="🕐 Ví dụ: 08:00"
                  minuteStep={30}
                  hourStep={1}
                  showNow={false}
                  showSecond={false}
                  size="large"
                  style={{ width: '100%' }}
                  allowClear={true}
                  suffixIcon={<ClockCircleOutlined />}
                  inputReadOnly={false}
                  popupClassName="custom-time-picker"
                  onChange={(time, timeString) => {
                    console.log('Start time selected:', timeString);
                    if (time) {
                      setTimeout(() => {
                        const endTimeInput = document.querySelector('[data-field="endTime"] input');
                        if (endTimeInput) {
                          endTimeInput.focus();
                        }
                      }, 200);
                    }
                  }}
                  onOpenChange={(open) => {
                    console.log('Start time picker open:', open);
                  }}
                />
              </Form.Item>
            </Col>

            <Col span={12}>
              <Form.Item
                name="endTime"
                label={
                  <Space>
                    <ClockCircleOutlined style={{ color: '#f5222d' }} />
                    <span>Giờ kết thúc</span>
                  </Space>
                }
                rules={[
                  { required: true, message: "⚠️ Chọn giờ kết thúc!" },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      const startTime = getFieldValue('startTime');
                      if (!value || !startTime) {
                        return Promise.resolve();
                      }

                      if (value.isAfter(startTime)) {
                        return Promise.resolve();
                      }

                      return Promise.reject(new Error('⚠️ Giờ kết thúc phải sau giờ bắt đầu!'));
                    },
                  }),
                ]}
                data-field="endTime"
              >
                <TimePicker
                  format="HH:mm"
                  placeholder="🕙 Ví dụ: 10:00"
                  minuteStep={30}
                  hourStep={1}
                  showNow={false}
                  showSecond={false}
                  size="large"
                  style={{ width: '100%' }}
                  allowClear={true}
                  suffixIcon={<ClockCircleOutlined />}
                  inputReadOnly={false}
                  popupClassName="custom-time-picker"
                  onChange={(time, timeString) => {
                    console.log('End time selected:', timeString);
                  }}
                  onOpenChange={(open) => {
                    console.log('End time picker open:', open);
                  }}
                />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            label={
              <Space>
                <span>Trạng thái</span>
                <Tooltip title="Chọn trạng thái hoạt động của khung giờ">
                  <QuestionCircleOutlined style={{ color: '#1890ff' }} />
                </Tooltip>
              </Space>
            }
            name="statusId"
            rules={[
              { required: true, message: '⚠️ Vui lòng chọn trạng thái' }
            ]}
          >
            <Select
              placeholder="Chọn trạng thái..."
              size="large"
              className="status-select"
              showSearch={false}
              getPopupContainer={(trigger) => trigger.parentElement}
            >
              {Object.entries(STATUS_CONFIG).map(([id, config]) => (
                <Option key={id} value={parseInt(id)}>
                  <Space>
                    <Tag color={config.color}>
                      {config.description}
                    </Tag>
                  </Space>
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            label={
              <Space>
                <span>Giảm giá (tùy chọn)</span>
                <Tooltip title="Phần trăm giảm giá cho khung giờ này">
                  <QuestionCircleOutlined style={{ color: '#1890ff' }} />
                </Tooltip>
              </Space>
            }
            name="discount"
            rules={[
              { type: 'number', min: 0, max: 100, message: '⚠️ Giảm giá phải từ 0% đến 100%' }
            ]}
          >
            <InputNumber
              placeholder="Nhập % giảm giá (0-100)..."
              size="large"
              min={0}
              max={100}
              step={5}
              precision={1}
              className="discount-input"
              style={{ width: '100%' }}
              addonAfter="%"
              formatter={value => value ? `${value}` : ''}
              parser={value => value ? value.replace('%', '') : ''}
            />
          </Form.Item>

          <Form.Item className="form-actions" style={{ marginBottom: 0, marginTop: 24 }}>
            <Space size="middle" style={{ width: '100%', justifyContent: 'flex-end' }}>
              <Button
                onClick={() => {
                  setModalVisible(false);
                  form.resetFields();
                }}
                size="large"
                className="cancel-btn"
              >
                Hủy bỏ
              </Button>
              <Button
                type="primary"
                htmlType="submit"
                size="large"
                className="submit-btn"
                loading={loading}
                icon={editingTimeslot ? <EditOutlined /> : <PlusOutlined />}
              >
                {editingTimeslot ? 'Cập nhật khung giờ' : 'Thêm khung giờ'}
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default TimeslotManagement;