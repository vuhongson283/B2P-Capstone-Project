import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  getAllCourts, 
  addNewCourt, 
  updateCourt, 
  deleteCourt, 
  getCourtDetail,
  getAllCourtCategories 
} from '../../services/apiService';
import { Form, InputGroup, Button, Modal } from 'react-bootstrap';
import './CourtManagement.scss';

const CourtManagement = () => {
  const { facilityId } = useParams();
  const navigate = useNavigate();
  const [courts, setCourts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 5,
    totalItems: 0,
    totalPages: 0
  });
  const [currentFacility, setCurrentFacility] = useState(null);
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
        
        setCurrentFacility({
          id: facilityId,
          name: `Cơ sở ${facilityId}`,
          location: "Địa chỉ cơ sở",
          contact: "0123456789"
        });
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

  // Handle add new court
  const handleAddCourt = async () => {
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
        alert(response.message || 'Court added successfully!');
      } else {
        setError(response.message || 'Failed to add court');
      }
    } catch (error) {
      console.error('Error adding court:', error);
      setError('Error adding court: ' + (error.response?.message || error.message));
    }
  };

  // Handle edit court
  const handleEdit = (court) => {
    setEditCourt({
        courtId: court.courtId,
        statusId: court.status || court.statusId, // Sửa thành giá trị status thực tế từ API
        courtName: court.courtName,
        categoryId: court.categoryId,
        pricePerHour: court.pricePerHour
    });
    setShowEditModal(true);
    };

  const handleEditInputChange = (e) => {
    const { name, value } = e.target;
    setEditCourt(prev => ({ ...prev, [name]: value }));
  };
  
  const handleUpdateCourt = async () => {
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
      const response = await updateCourt(courtData);
      if (response.success) {
        setShowEditModal(false);
        fetchCourts();
        alert(response.message || 'Cập nhật sân thành công!');
      } else {
        alert(response.message || 'Cập nhật sân thất bại');
      }
    } catch (error) {
      console.error('Error updating court:', error);
      alert('Lỗi khi cập nhật sân: ' + (error.response?.data?.message || error.message));
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setNewCourt(prev => ({ ...prev, [name]: value }));
  };

  const handleSearch = (e) => {
    e.preventDefault();
    fetchCourts();
  };

  const handlePageChange = (newPage) => {
    setPagination(prev => ({ ...prev, pageNumber: newPage }));
  };

  const handleDeleteCourt = async (courtId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa sân này không?')) {
      try {
        const response = await deleteCourt(courtId);
        
        if (response.success) {
          fetchCourts();
          alert(response.message || 'Xóa sân thành công!');
        } else {
          alert(response.message || 'Không thể xóa sân');
        }
      } catch (error) {
        console.error('Error deleting court:', error);
        alert(error.response?.message || 'Lỗi khi xóa sân');
      }
    }
  };

  // Add this handler function
  const handleViewDetail = async (courtId) => {
    try {
      const response = await getCourtDetail(courtId);
      if (response.success) {
        setCourtDetail(response.data);
        setShowDetailModal(true);
      } else {
        alert('Không thể tải thông tin sân');
      }
    } catch (error) {
      console.error('Error fetching court detail:', error);
      alert('Lỗi khi tải thông tin sân');
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
        <h1>Quản Lý Sân - {currentFacility?.name || 'Đang tải...'}</h1>
        <div className="facility-info">
          <span><i className="fas fa-map-marker-alt"></i> {currentFacility?.location}</span>
          <span><i className="fas fa-phone"></i> {currentFacility?.contact}</span>
        </div>
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
                  <th>Hành Động</th>
                </tr>
              </thead>
              <tbody>
                {courts.map(court => (
                  <tr key={court.courtId}>
                    <td>{court.courtId}</td>
                    <td>{court.courtName}</td>
                    <td>{court.categoryName || 'Chưa phân loại'}</td>
                    <td>
                      <span className={`status ${court.statusName === 'Active' ? 'active' : 'inactive'}`}>
                        {court.statusName === 'Active' ? 'Hoạt động' : 'Không hoạt động'}
                      </span>
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
                          onClick={() => handleDeleteCourt(court.courtId)}
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
      <Modal show={showAddModal} onHide={() => setShowAddModal(false)}>
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
                required
              />
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
                required
                disabled={categoryLoading}
              >
                <option value="">Chọn thể loại sân</option>
                {categories.map(category => (
                  <option key={category.categoryId} value={category.categoryId}>
                    {category.categoryName}
                  </option>
                ))}
              </Form.Select>
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
                required
              />
            </Form.Group>
          </Form>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowAddModal(false)}>
            <i className="fas fa-times me-2"></i>
            Hủy
          </Button>
          <Button variant="primary" onClick={handleAddCourt}>
            <i className="fas fa-check me-2"></i>
            Thêm mới
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Edit Court Modal */}
      <Modal show={showEditModal} onHide={() => setShowEditModal(false)}>
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
                value={editCourt.statusId || '1'} // Add fallback to '1'
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
                required
              />
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
                required
                disabled={categoryLoading}
              >
                <option value="">Chọn thể loại sân</option>
                {categories.map(category => (
                  <option key={category.categoryId} value={category.categoryId}>
                    {category.categoryName}
                  </option>
                ))}
              </Form.Select>
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
                required
              />
            </Form.Group>
          </Form>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowEditModal(false)}>
            <i className="fas fa-times me-2"></i>
            Hủy
          </Button>
          <Button variant="primary" onClick={handleUpdateCourt}>
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
                <label><i className="fas fa-map-marker-alt"></i>Địa chỉ</label>
                <span>{courtDetail.location}</span>
              </div>
              <div className="detail-item">
                <label><i className="fas fa-phone"></i>Liên hệ</label>
                <span>{courtDetail.contact}</span>
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
    </div>
  );
};

export default CourtManagement;