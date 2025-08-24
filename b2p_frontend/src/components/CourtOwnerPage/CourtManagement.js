import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom'; // ✅ THÊM useLocation
import { useAuth } from '../../contexts/AuthContext';
import { 
  getAllCourts, 
  addNewCourt, 
  updateCourt, 
  deleteCourt, 
  lockCourt,
  getCourtDetail,
  getAllCourtCategories 
} from '../../services/apiService';
import { Form, InputGroup, Button, Modal } from 'react-bootstrap';
import { LockOutlined, UnlockOutlined } from '@ant-design/icons';
import { Tooltip } from 'antd'; // Chỉ import Tooltip từ antd
import './CourtManagement.scss';


const CourtManagement = () => {
  const { userId } = useAuth();
  const { facilityId } = useParams();
  const navigate = useNavigate();
  const location = useLocation(); // ✅ THÊM useLocation
  
  const [courts, setCourts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 5,
    totalItems: 0,
    totalPages: 0
  });
  // ✅ CẬP NHẬT currentFacility STATE ĐỂ LƯU TRỮ TÊN FACILITY
  const [currentFacility, setCurrentFacility] = useState({
    id: facilityId,
    name: 'Đang tải...' // Default value
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [selectedStatus, setSelectedStatus] = useState(null);
  const [categories, setCategories] = useState([]); // Thay đổi từ const thành let
  const [categoryLoading, setCategoryLoading] = useState(true);

  // Modal state
  const [showAddModal, setShowAddModal] = useState(false);
  const [newCourt, setNewCourt] = useState({
    courtName: '',
    categoryId: '',
    pricePerHour: ''
  });

  const [showEditModal, setShowEditModal] = useState(false);
  const [editCourt, setEditCourt] = useState({
    courtId: '',
    statusId: '',
    courtName: '',
    categoryId: '',
    pricePerHour: ''
  });

  const [showDetailModal, setShowDetailModal] = useState(false);
  const [courtDetail, setCourtDetail] = useState(null);

  // Thêm state cho validation errors
  const [validationErrors, setValidationErrors] = useState({
    courtName: '',
    categoryId: '',
    pricePerHour: ''
  });

  // Thêm state mới cho edit validation
  const [editValidationErrors, setEditValidationErrors] = useState({
    courtName: '',
    categoryId: '',
    pricePerHour: ''
  });

  // Thêm state để track loading state cho từng sân
  const [loadingCourtIds, setLoadingCourtIds] = useState([]);

  // Thêm state cho modal thông báo
  const [showNotificationModal, setShowNotificationModal] = useState(false);
  const [notificationData, setNotificationData] = useState({
    type: 'success', // 'success', 'error', 'warning', 'info'
    title: '',
    message: '',
    icon: 'fa-check-circle'
  });

  // Fetch courts data
  const fetchCourts = async () => {
    if (!facilityId) {
      setError('Facility ID is required');
      return;
    }
    
    setLoading(true);
    setError(null);
    
    try {
      const requestParams = {
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        facilityId: facilityId,
        search: searchTerm || undefined,
        status: selectedStatus,
        categoryId: selectedCategory
      };
      
      const response = await getAllCourts(requestParams);
      
      if (response?.data) {
        setCourts(response.data.items || []);
        setPagination(prev => ({
          ...prev,
          pageNumber: response.data.currentPage || prev.pageNumber,
          totalItems: response.data.totalItems || 0,
          totalPages: response.data.totalPages || 0
        }));
        
        // ✅ CHỈ CẬP NHẬT ID, KHÔNG GHI ĐÈ NAME
        setCurrentFacility(prev => ({
          ...prev,
          id: facilityId
        }));
      } else {
        setCourts([]);
      }
    } catch (error) {
      console.error("Error fetching courts:", error);
      setError("Failed to fetch courts data");
      setCourts([]);
    } finally {
      setLoading(false);
    }
  };

  // ✅ HÀM HIỂN THỊ NOTIFICATION
  const showNotification = (type, title, message) => {
    const icons = {
      success: 'fa-check-circle',
      error: 'fa-exclamation-circle',
      warning: 'fa-exclamation-triangle',
      info: 'fa-info-circle'
    };

    setNotificationData({
      type,
      title,
      message,
      icon: icons[type] || icons.info
    });
    setShowNotificationModal(true);

    // ✅ TỰ ĐỘNG ĐÓNG SAU 3 GIÂY NẾU LÀ SUCCESS
    if (type === 'success') {
      setTimeout(() => {
        setShowNotificationModal(false);
      }, 3000);
    }
  };

  // Handle add new court
  const handleAddCourt = async () => {
    // Reset validation errors
    setValidationErrors({
      courtName: '',
      categoryId: '',
      pricePerHour: ''
    });

    // Validate form inputs
    let isValid = true;
    if (!newCourt.courtName) {
      setValidationErrors(prev => ({ ...prev, courtName: 'Tên sân là bắt buộc' }));
      isValid = false;
    }
    if (!newCourt.categoryId) {
      setValidationErrors(prev => ({ ...prev, categoryId: 'Thể loại sân là bắt buộc' }));
      isValid = false;
    }
    if (!newCourt.pricePerHour) {
      setValidationErrors(prev => ({ ...prev, pricePerHour: 'Giá sân là bắt buộc' }));
      isValid = false;
    }

    if (!isValid) return; // Ngừng thực hiện nếu có lỗi validation

    try {
      const courtData = {
        facilityId: parseInt(facilityId),
        courtName: newCourt.courtName,
        categoryId: parseInt(newCourt.categoryId),
        pricePerHour: parseInt(newCourt.pricePerHour)
      };
      
      const response = await addNewCourt(courtData);
      
      if (response.success) {
        setShowAddModal(false);
        setNewCourt({ courtName: '', categoryId: '', pricePerHour: '' });
        fetchCourts();
        // ✅ THAY ALERT BẰNG NOTIFICATION
        showNotification(
          'success',
          'Thêm sân thành công!',
          `Sân "${newCourt.courtName}" đã được thêm vào hệ thống.`
        );
      } else {
        showNotification(
          'error',
          'Thêm sân thất bại!',
          response.message || 'Không thể thêm sân. Vui lòng thử lại.'
        );
      }
    } catch (error) {
      console.error('Error adding court:', error);
      showNotification(
        'error',
        'Có lỗi xảy ra!',
        'Lỗi khi thêm sân: ' + (error.response?.message || error.message)
      );
    }
  };

  // Handle edit court
  const handleEdit = (court) => {
    setEditCourt({
      courtId: court.courtId,
      // Chuyển đổi trạng thái từ 'Active' thành giá trị tương ứng
      statusId: court.status.statusName === 'Active' ? '1' : '2',
      courtName: court.courtName,
      categoryId: court.categoryId,
      pricePerHour: court.pricePerHour
    });
    setShowEditModal(true);
    };

  const handleEditInputChange = (e) => {
    const { name, value } = e.target;
    setEditCourt(prev => ({ ...prev, [name]: value }));
    
    // Validate từng trường của form edit
    switch (name) {
      case 'courtName':
        if (!value.trim()) {
          setEditValidationErrors(prev => ({
            ...prev,
            courtName: 'Tên sân không được để trống'
          }));
        } else {
          setEditValidationErrors(prev => ({ ...prev, courtName: '' }));
        }
        break;
        
      case 'categoryId':
        if (!value) {
          setEditValidationErrors(prev => ({
            ...prev,
            categoryId: 'Vui lòng chọn loại sân'
          }));
        } else {
          setEditValidationErrors(prev => ({ ...prev, categoryId: '' }));
        }
        break;
        
      case 'pricePerHour':
        if (!value) {
          setEditValidationErrors(prev => ({
            ...prev,
            pricePerHour: 'Giá sân không được để trống'
          }));
        } else if (parseInt(value) <= 0) {
          setEditValidationErrors(prev => ({
            ...prev,
            pricePerHour: 'Giá sân phải lớn hơn 0'
          }));
        } else {
          setEditValidationErrors(prev => ({ ...prev, pricePerHour: '' }));
        }
        break;
        
      default:
        break;
    }
  };
  
  const handleUpdateCourt = async () => {
    // Reset edit validation errors
    setEditValidationErrors({
      courtName: '',
      categoryId: '',
      pricePerHour: ''
    });

    // Validate form inputs
    let isValid = true;
    if (!editCourt.courtName) {
      setEditValidationErrors(prev => ({ ...prev, courtName: 'Tên sân là bắt buộc' }));
      isValid = false;
    }
    if (!editCourt.categoryId) {
      setEditValidationErrors(prev => ({ ...prev, categoryId: 'Thể loại sân là bắt buộc' }));
      isValid = false;
    }
    if (!editCourt.pricePerHour) {
      setEditValidationErrors(prev => ({ ...prev, pricePerHour: 'Giá sân là bắt buộc' }));
      isValid = false;
    }

    if (!isValid) return;

    try {
        console.log('Edit court data before submit:', editCourt);
      const courtData = {
        courtId: parseInt(editCourt.courtId),
        status: isNaN(parseInt(editCourt.statusId)) ? 1 : parseInt(editCourt.statusId),
        courtName: editCourt.courtName,
        categoryId: parseInt(editCourt.categoryId),
        pricePerHour: parseInt(editCourt.pricePerHour)
      };
      console.log('Court data sending to API:', courtData);
      const response = await updateCourt(courtData, userId);
      if (response.success) {
        setShowEditModal(false);
        fetchCourts();
        // ✅ THAY ALERT BẰNG NOTIFICATION
        showNotification(
          'success',
          'Cập nhật sân thành công!',
          `Thông tin sân "${editCourt.courtName}" đã được cập nhật.`
        );
      } else {
        showNotification(
          'error',
          'Cập nhật sân thất bại!',
          response.message || 'Không thể cập nhật sân. Vui lòng thử lại.'
        );
      }
    } catch (error) {
      console.error('Error updating court:', error);
      showNotification(
        'error',
        'Có lỗi xảy ra!',
        'Lỗi khi cập nhật sân: ' + (error.response?.data?.message || error.message)
      );
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setNewCourt(prev => ({ ...prev, [name]: value }));
    
    switch (name) {
      case 'courtName':
        if (!value.trim()) {
          setValidationErrors(prev => ({
            ...prev,
            courtName: 'Tên sân không được để trống'
          }));
        } else {
          setValidationErrors(prev => ({ ...prev, courtName: '' }));
        }
        break;
        
      case 'categoryId':
        if (!value) {
          setValidationErrors(prev => ({
            ...prev,
            categoryId: 'Vui lòng chọn loại sân'
          }));
        } else {
          setValidationErrors(prev => ({ ...prev, categoryId: '' }));
        }
        break;
        
      case 'pricePerHour':
        if (!value) {
          setValidationErrors(prev => ({
            ...prev,
            pricePerHour: 'Giá sân không được để trống'
          }));
        } else if (parseInt(value) <= 0) {
          setValidationErrors(prev => ({
            ...prev,
            pricePerHour: 'Giá sân phải lớn hơn 0'
          }));
        } else {
          setValidationErrors(prev => ({ ...prev, pricePerHour: '' }));
        }
        break;
        
      default:
        break;
    }
  };

  const handleSearch = (e) => {
    e.preventDefault();
    fetchCourts();
  };

  const handlePageChange = (newPage) => {
    setPagination(prev => ({ ...prev, pageNumber: newPage }));
  };

  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [courtToDelete, setCourtToDelete] = useState(null);

  // ✅ CẬP NHẬT HÀM DELETE COURT VỚI CONFIRMATION MODAL
  const handleDeleteCourt = (court) => {
    setCourtToDelete(court);
    setShowDeleteModal(true);
  };

  const confirmDeleteCourt = async () => {
    if (!courtToDelete) return;

    try {
      const response = await deleteCourt(courtToDelete.courtId, userId);

      if (response.success) {
        setShowDeleteModal(false);
        setCourtToDelete(null);
        fetchCourts();
        // ✅ THAY ALERT BẰNG NOTIFICATION
        showNotification(
          'success',
          'Xóa sân thành công!',
          `Sân "${courtToDelete.courtName}" đã được xóa khỏi hệ thống.`
        );
      } else {
        showNotification(
          'error',
          'Xóa sân thất bại!',
          response.message || 'Không thể xóa sân. Vui lòng thử lại.'
        );
      }
    } catch (error) {
      console.error('Error deleting court:', error);
      showNotification(
        'error',
        'Có lỗi xảy ra!',
        'Lỗi khi xóa sân: ' + (error.response?.message || error.message)
      );
    }
  };

  // Thêm hàm xử lý thay đổi pageSize
  const handlePageSizeChange = (e) => {
    const newPageSize = parseInt(e.target.value);
    setPagination(prev => ({
      ...prev,
      pageSize: newPageSize,
      pageNumber: 1 // Reset về trang 1 khi đổi pageSize
    }));
  };

  // Thêm useEffect để fetch categories
  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const response = await getAllCourtCategories('', 1, 100); // Lấy tất cả categories
        if (response.data && response.data.items) {
          setCategories(response.data.items.map(cat => ({
            categoryId: cat.categoryId,
            categoryName: cat.categoryName
          })));
        }
      } catch (error) {
        console.error('Error fetching categories:', error);
      } finally {
        setCategoryLoading(false);
      }
    };

    fetchCategories();
  }, []);

  useEffect(() => {
    fetchCourts();
  }, [facilityId, pagination.pageNumber, pagination.pageSize, selectedCategory, selectedStatus]);

  // Thêm vào hàm đóng modal
  const handleCloseEditModal = () => {
    setShowEditModal(false);
    // Reset form data
    setEditCourt({
      courtId: '',
      statusId: '',
      courtName: '',
      categoryId: '',
      pricePerHour: ''
    });
    // Reset validation errors
    setEditValidationErrors({
      courtName: '',
      categoryId: '',
      pricePerHour: ''
    });
  };

  // Thêm handler để xử lý khóa/mở khóa
  const handleLockToggle = async (courtId, currentStatus) => {
    try {
      setLoadingCourtIds(prev => [...prev, courtId]);
      
      const newStatus = currentStatus === 'Active' ? 2 : 1;
      const response = await lockCourt(courtId, newStatus, userId);
      
      if (response?.status === 200) {
        fetchCourts();
        // ✅ THAY ALERT BẰNG NOTIFICATION
        const action = currentStatus === 'Active' ? 'Khóa' : 'Mở khóa';
        const court = courts.find(c => c.courtId === courtId);
        showNotification(
          'success',
          `${action} sân thành công!`,
          `Sân "${court?.courtName || 'N/A'}" đã được ${action.toLowerCase()}.`
        );
      } else {
        showNotification(
          'error',
          'Cập nhật trạng thái thất bại!',
          'Không thể cập nhật trạng thái sân. Vui lòng thử lại.'
        );
      }
    } catch (error) {
      console.error('Error toggling court status:', error);
      showNotification(
        'error',
        'Có lỗi xảy ra!',
        'Lỗi khi cập nhật trạng thái sân. Vui lòng thử lại.'
      );
    } finally {
      setLoadingCourtIds(prev => prev.filter(id => id !== courtId));
    }
  };

  // Thêm hàm reset form
  const handleCloseAddModal = () => {
    setShowAddModal(false);
    // Reset form data
    setNewCourt({
      courtName: '',
      categoryId: '',
      pricePerHour: ''
    });
    // Reset validation errors
    setValidationErrors({
      courtName: '',
      categoryId: '',
      pricePerHour: ''
    });
  };

  // ✅ THÊM HÀM handleViewDetail VÀO COMPONENT
  const handleViewDetail = async (courtId) => {
    try {
      setShowDetailModal(true);
      setCourtDetail(null); // Reset data cũ
      
      // ✅ HIỂN THỊ LOADING TRONG MODAL
      const loadingData = {
        courtId: courtId,
        courtName: 'Đang tải...',
        pricePerHour: 0,
        status: { statusName: 'Đang tải...' },
        category: { categoryName: 'Đang tải...' },
        facility: { 
          facilityName: 'Đang tải...', 
          location: 'Đang tải...', 
          contact: 'Đang tải...' 
        }
      };
      setCourtDetail(loadingData);
      
      // ✅ GỌI API ĐỂ LẤY CHI TIẾT SÂN
      const response = await getCourtDetail(courtId);
      
      if (response && response.success && response.data) {
        setCourtDetail(response.data);
      } else {
        // ✅ HIỂN THỊ LỖI NẾU KHÔNG TẢI ĐƯỢC
        showNotification(
          'error',
          'Không thể tải thông tin!',
          'Không thể tải chi tiết sân. Vui lòng thử lại.'
        );
        setShowDetailModal(false);
      }
    } catch (error) {
      console.error('Error fetching court detail:', error);
      showNotification(
        'error',
        'Có lỗi xảy ra!',
        'Lỗi khi tải chi tiết sân: ' + (error.response?.message || error.message)
      );
      setShowDetailModal(false);
    }
  };

  // ✅ THÊM useEffect ĐỂ LẤY facilityName TỪ URL QUERY
  useEffect(() => {
    const searchParams = new URLSearchParams(location.search);
    const facilityName = searchParams.get('name');
    
    if (facilityName) {
      setCurrentFacility({
        id: facilityId,
        name: decodeURIComponent(facilityName)
      });
    } else {
      // ✅ NẾU KHÔNG CÓ NAME TRONG URL, CÓ THỂ GỌI API ĐỂ LẤY
      setCurrentFacility({
        id: facilityId,
        name: `Cơ sở ${facilityId}` // Fallback
      });
    }
  }, [facilityId, location.search]);

  if (loading) {
    return (
      <div className="court-management-container">
        <div className="loading-spinner">
          <i className="fas fa-spinner fa-spin"></i> Đang tải dữ liệu...
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="court-management-container">
        <div className="error-message">
          <i className="fas fa-exclamation-circle"></i> {error}
        </div>
      </div>
    );
  }

  return (
    <div className="court-management-container">
      <div className="facility-header">
        <h1><i className="fas fa-building me-2"></i> Quản Lý Sân - {currentFacility?.name || 'Đang tải...'}</h1>
      </div>

      <div className="management-controls">
        <div className="filters">
          <Form className="search-form" onSubmit={handleSearch}>
            <InputGroup>
              <Form.Control
                type="text"
                placeholder="Tìm kiếm sân..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
              <Button variant="outline-secondary" type="submit">
                <i className="fas fa-search"></i>
              </Button>
            </InputGroup>
          </Form>

          <Form.Select 
            value={selectedCategory || ''} 
            onChange={(e) => setSelectedCategory(e.target.value || null)}
            disabled={categoryLoading}
          >
            <option value="">Tất cả loại sân</option>
            {categories.map(category => (
              <option key={category.categoryId} value={category.categoryId}>
                {category.categoryName}
              </option>
            ))}
          </Form.Select>

          <Form.Select
            value={selectedStatus || ''} 
            onChange={(e) => setSelectedStatus(e.target.value || null)}
          >
            <option value="">Tất cả trạng thái</option>
            <option value="1">Hoạt động</option>
            <option value="2">Không hoạt động</option>
          </Form.Select>
        </div>

        <Button 
          variant="success"
          className="btn-add-court"
          onClick={() => setShowAddModal(true)}
        >
          <i className="fas fa-plus"></i> Thêm sân mới
        </Button>
      </div>

      <div className="court-table-container">
        {courts.length === 0 ? (
          <div className="no-data-message">
            <i className="fas fa-search"></i>
            <p>Không tìm thấy sân phù hợp với điều kiện tìm kiếm</p>
            <p>Vui lòng thử lại với điều kiện khác</p>
          </div>
        ) : (
          <>
            <table className="court-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Tên Sân</th>
                  <th>Loại Sân</th>
                  <th>Trạng Thái</th>
                  <th>Khóa/Mở khóa</th>
                  <th>Hành Động</th>
                </tr>
              </thead>
              <tbody>
                {courts.map(court => (
                  <tr key={court.courtId}>
                    <td>{court.courtId}</td>
                    <td>{court.courtName}</td>
                    <td>{court.category.categoryName || 'Chưa phân loại'}</td>
                    <td>
                      <span className={`status ${court.status.statusName === 'Active' ? 'active' : 'inactive'}`}>
                        {court.status.statusName === 'Active' ? 'Hoạt động' : 'Không hoạt động'}
                      </span>
                    </td>
                    <td>
                      <div 
                        className={`status-icon ${loadingCourtIds.includes(court.courtId) ? 'disabled' : ''}`}
                        onClick={() => !loadingCourtIds.includes(court.courtId) && handleLockToggle(court.courtId, court.status.statusName)}
                      >
                        <div className="icon-tooltip">
                          {court.status.statusName === 'Active' ? (
                            <i className="fas fa-unlock" />
                          ) : (
                            <i className="fas fa-lock" />
                          )}
                          <span className="tooltip-text">
                            {court.status.statusName === 'Active' ? 'Khóa sân' : 'Mở khóa sân'}
                          </span>
                        </div>
                        {loadingCourtIds.includes(court.courtId) && (
                          <i className="fas fa-spinner fa-spin ms-2"></i>
                        )}
                      </div>
                    </td>
                    <td>
                      <div className="action-buttons">
                        <Button 
                          variant="info" 
                          size="sm"
                          className="me-2"
                          onClick={() => handleViewDetail(court.courtId)}
                          title="Xem chi tiết"
                        >
                          <i className="fas fa-eye"></i>
                        </Button>
                        <Button 
                          variant="warning" 
                          size="sm"
                          className="me-2"
                          onClick={() => handleEdit(court)}
                          title="Chỉnh sửa"
                        >
                          <i className="fas fa-edit"></i>
                        </Button>
                        <Button 
                          variant="danger" 
                          size="sm"
                          onClick={() => handleDeleteCourt(court)} // ✅ THAY ĐỔI Ở ĐÂY
                          title="Xóa sân"
                        >
                          <i className="fas fa-trash"></i>
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {courts.length > 0 && (
              <div className="pagination-container">
                <div className="pagination">
                  <Button
                    variant="outline-primary"
                    className="btn-prev"
                    disabled={pagination.pageNumber === 1}
                    onClick={() => handlePageChange(pagination.pageNumber - 1)}
                  >
                    <i className="fas fa-chevron-left me-1"></i> Trước
                  </Button>
                  
                  <div className="page-numbers">
                    {Array.from({ length: Math.min(5, pagination.totalPages) }, (_, i) => {
                      let pageNum;
                      if (pagination.totalPages <= 5) {
                        pageNum = i + 1;
                      } else if (pagination.pageNumber <= 3) {
                        pageNum = i + 1;
                      } else if (pagination.pageNumber >= pagination.totalPages - 2) {
                        pageNum = pagination.totalPages - 4 + i;
                      } else {
                        pageNum = pagination.pageNumber - 2 + i;
                      }
                      
                      return (
                        <Button
                          key={pageNum}
                          variant={pagination.pageNumber === pageNum ? 'primary' : 'outline-primary'}
                          onClick={() => handlePageChange(pageNum)}
                        >
                          {pageNum}
                        </Button>
                      );
                    })}
                  </div>
                  
                  <Button
                    variant="outline-primary"
                    className="btn-next"
                    disabled={pagination.pageNumber >= pagination.totalPages}
                    onClick={() => handlePageChange(pagination.pageNumber + 1)}
                  >
                    Sau <i className="fas fa-chevron-right ms-1"></i>
                  </Button>
                </div>

                <div className="page-size-selector">
                  <span>Hiển thị:</span>
                  <Form.Select 
                    value={pagination.pageSize} 
                    onChange={handlePageSizeChange}
                    className="page-size-select"
                  >
                    <option value="3">3 / page</option>
                    <option value="5">5 / page</option>
                    <option value="10">10 / page</option>
                  </Form.Select>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Add Court Modal */}
      <Modal show={showAddModal} onHide={handleCloseAddModal}>
        <Modal.Header closeButton>
          <Modal.Title>
            <i className="fas fa-plus-circle me-2"></i>
            Thêm sân mới
          </Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Form>
            <Form.Group className="mb-4">
              <Form.Label>
                <i className="fas fa-signature me-2"></i>
                Tên sân <span className="text-danger">*</span>
              </Form.Label>
              <Form.Control
                type="text"
                name="courtName"
                value={newCourt.courtName}
                onChange={handleInputChange}
                placeholder="Nhập tên sân"
                isInvalid={!!validationErrors.courtName}
              />
              <Form.Control.Feedback type="invalid">
                {validationErrors.courtName}
              </Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>
                <i className="fas fa-th-large me-2"></i>
                Thể loại <span className="text-danger">*</span>
              </Form.Label>
              <Form.Select
                name="categoryId"
                value={newCourt.categoryId}
                onChange={handleInputChange}
                disabled={categoryLoading}
                isInvalid={!!validationErrors.categoryId}
              >
                <option value="">Chọn thể loại sân</option>
                {categories.map(category => (
                  <option key={category.categoryId} value={category.categoryId}>
                    {category.categoryName}
                  </option>
                ))}
              </Form.Select>
              <Form.Control.Feedback type="invalid">
                {validationErrors.categoryId}
              </Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>
                <i className="fas fa-money-bill me-2"></i>
                Giá/Giờ (VND) <span className="text-danger">*</span>
              </Form.Label>
              <Form.Control
                type="number"
                name="pricePerHour"
                value={newCourt.pricePerHour}
                onChange={handleInputChange}
                min="0"
                placeholder="Nhập giá sân"
                isInvalid={!!validationErrors.pricePerHour}
              />
              <Form.Control.Feedback type="invalid">
                {validationErrors.pricePerHour}
              </Form.Control.Feedback>
            </Form.Group>
          </Form>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={handleCloseAddModal}>
            <i className="fas fa-times me-2"></i>
            Hủy
          </Button>
          <Button 
            variant="primary" 
            onClick={handleAddCourt}
            disabled={Object.values(validationErrors).some(error => error !== '')}
          >
            <i className="fas fa-check me-2"></i>
            Thêm mới
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Edit Court Modal */}
      <Modal show={showEditModal} onHide={handleCloseEditModal}>
        <Modal.Header closeButton>
          <Modal.Title>
            <i className="fas fa-edit me-2"></i>
            Chỉnh sửa sân
          </Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Form>
            <Form.Group className="mb-4">
              <Form.Label>
                <i className="fas fa-hashtag me-2"></i>
                ID Sân
              </Form.Label>
              <Form.Control
                type="text"
                value={editCourt.courtId}
                disabled
                readOnly
              />
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>
                <i className="fas fa-toggle-on me-2"></i>
                Trạng thái <span className="text-danger">*</span>
              </Form.Label>
              <Form.Select
                name="statusId"
                value={editCourt.statusId}
                onChange={handleEditInputChange}
                required
              >
                <option value="1">Hoạt động</option>
                <option value="2">Không hoạt động</option>
              </Form.Select>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>
                <i className="fas fa-signature me-2"></i>
                Tên sân <span className="text-danger">*</span>
              </Form.Label>
              <Form.Control
                type="text"
                name="courtName"
                value={editCourt.courtName}
                onChange={handleEditInputChange}
                placeholder="Nhập tên sân"
                isInvalid={!!editValidationErrors.courtName}
              />
              <Form.Control.Feedback type="invalid">
                {editValidationErrors.courtName}
              </Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>
                <i className="fas fa-th-large me-2"></i>
                Thể loại <span className="text-danger">*</span>
              </Form.Label>
              <Form.Select
                name="categoryId"
                value={editCourt.categoryId}
                onChange={handleEditInputChange}
                isInvalid={!!editValidationErrors.categoryId}
                disabled={categoryLoading}
              >
                <option value="">Chọn thể loại sân</option>
                {categories.map(category => (
                  <option key={category.categoryId} value={category.categoryId}>
                    {category.categoryName}
                  </option>
                ))}
              </Form.Select>
              <Form.Control.Feedback type="invalid">
                {editValidationErrors.categoryId}
              </Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>
                <i className="fas fa-money-bill me-2"></i>
                Giá/Giờ (VND) <span className="text-danger">*</span>
              </Form.Label>
              <Form.Control
                type="number"
                name="pricePerHour"
                value={editCourt.pricePerHour}
                onChange={handleEditInputChange}
                min="0"
                placeholder="Nhập giá sân"
                isInvalid={!!editValidationErrors.pricePerHour}
              />
              <Form.Control.Feedback type="invalid">
                {editValidationErrors.pricePerHour}
              </Form.Control.Feedback>
            </Form.Group>
          </Form>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={handleCloseEditModal}>
            <i className="fas fa-times me-2"></i>
            Hủy
          </Button>
          <Button 
            variant="primary" 
            onClick={handleUpdateCourt}
            disabled={Object.values(editValidationErrors).some(error => error !== '')}
          >
            <i className="fas fa-save me-2"></i>
            Lưu thay đổi
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Court Detail Modal */}
      <Modal show={showDetailModal} onHide={() => setShowDetailModal(false)} size="lg">
        <Modal.Header closeButton className="detail-header">
          <Modal.Title>
            <i className="fas fa-info-circle"></i>
            Chi tiết sân
          </Modal.Title>
        </Modal.Header>
        <Modal.Body className="detail-body">
          {courtDetail && (
            <div className="court-detail">
              <div className="detail-item">
                <label><i className="fas fa-hashtag"></i>ID Sân</label>
                <span>{courtDetail.courtId}</span>
              </div>
              <div className="detail-item">
                <label><i className="fas fa-signature"></i>Tên sân</label>
                <span>{courtDetail.courtName}</span>
              </div>
              <div className="detail-item">
                <label><i className="fas fa-money-bill"></i>Giá/Giờ</label>
                <span className="price">{courtDetail.pricePerHour?.toLocaleString('vi-VN')} VNĐ</span>
              </div>
              <div className="detail-item">
                <label><i className="fas fa-toggle-on"></i>Trạng thái</label>
                <span className={`status ${courtDetail.status.statusName === 'Active' ? 'active' : 'inactive'}`}>
                  {courtDetail.status.statusName === 'Active' ? 'Hoạt động' : 'Không hoạt động'}
                </span>
              </div>
              <div className="detail-item">
                <label><i className="fas fa-th-large"></i>Loại sân</label>
                <span>{courtDetail.category.categoryName}</span>
              </div>
              <div className="detail-item">
                <label><i className="fas fa-building"></i>Cơ sở</label>
                <span>{courtDetail.facility.facilityName}</span>
              </div>
              <div className="detail-item">
                <label><i className="fas fa-map-marker-alt"></i>Địa chỉ</label>
                <span>{courtDetail.facility.location}</span>
              </div>
              <div className="detail-item">
                <label><i className="fas fa-phone"></i>Liên hệ</label>
                <span>{courtDetail.facility.contact}</span>
              </div>
            </div>
          )}
        </Modal.Body>
        <Modal.Footer className="detail-footer">
          <Button variant="secondary" onClick={() => setShowDetailModal(false)} className="btn-close">
            <i className="fas fa-times"></i>
            Đóng
          </Button>
        </Modal.Footer>
      </Modal>

      {/* ✅ NOTIFICATION MODAL - THAY THẾ ALERT */}
      <Modal 
        show={showNotificationModal} 
        onHide={() => setShowNotificationModal(false)}
        centered
        className="notification-modal"
      >
        <Modal.Header closeButton className={`notification-header ${notificationData.type}`}>
          <Modal.Title className="notification-title">
            <i className={`fas ${notificationData.icon} me-3`}></i>
            {notificationData.title}
          </Modal.Title>
        </Modal.Header>
        <Modal.Body className="notification-body">
          <div className="notification-content">
            <div className={`notification-icon ${notificationData.type}`}>
              <i className={`fas ${notificationData.icon}`}></i>
            </div>
            <div className="notification-message">
              {notificationData.message}
            </div>
          </div>
        </Modal.Body>
        <Modal.Footer className="notification-footer">
          <Button 
            variant={notificationData.type === 'success' ? 'success' : 'primary'}
            onClick={() => setShowNotificationModal(false)}
            className="notification-btn"
          >
            <i className="fas fa-check me-2"></i>
            Đã hiểu
          </Button>
        </Modal.Footer>
      </Modal>

      {/* ✅ DELETE CONFIRMATION MODAL */}
      <Modal 
        show={showDeleteModal} 
        onHide={() => setShowDeleteModal(false)}
        centered
        className="delete-confirmation-modal"
      >
        <Modal.Header closeButton className="delete-header">
          <Modal.Title className="delete-title">
            <i className="fas fa-exclamation-triangle me-3"></i>
            Xác nhận xóa sân
          </Modal.Title>
        </Modal.Header>
        <Modal.Body className="delete-body">
          <div className="delete-content">
            <div className="delete-icon">
              <i className="fas fa-trash-alt"></i>
            </div>
            <div className="delete-message">
              <h5>Bạn có chắc chắn muốn xóa sân này?</h5>
              {courtToDelete && (
                <div className="court-info">
                  <p><strong>Tên sân:</strong> {courtToDelete.courtName}</p>
                  <p><strong>Loại sân:</strong> {courtToDelete.category?.categoryName}</p>
                  <p><strong>Giá:</strong> {courtToDelete.pricePerHour?.toLocaleString('vi-VN')} VNĐ/giờ</p>
                </div>
              )}
              <div className="warning-note">
                <i className="fas fa-exclamation-circle me-2"></i>
                <span>Hành động này không thể hoàn tác!</span>
              </div>
            </div>
          </div>
        </Modal.Body>
        <Modal.Footer className="delete-footer">
          <Button 
            variant="secondary" 
            onClick={() => setShowDeleteModal(false)}
            className="cancel-btn"
          >
            <i className="fas fa-times me-2"></i>
            Hủy bỏ
          </Button>
          <Button 
            variant="danger" 
            onClick={confirmDeleteCourt}
            className="confirm-delete-btn"
          >
            <i className="fas fa-trash me-2"></i>
            Xóa sân
          </Button>
        </Modal.Footer>
      </Modal>
    </div>
  );
};

export default CourtManagement;