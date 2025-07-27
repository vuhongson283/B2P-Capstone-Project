import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAllCourts, addNewCourt } from '../../services/apiService'; // Thêm hàm addNewCourt
import { Form, InputGroup, Dropdown, Button, Modal } from 'react-bootstrap';
import './CourtManagement.scss';

const CourtManagement = () => {
  const { facilityId } = useParams();
  const navigate = useNavigate();
  const [courts, setCourts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 0
  });
  const [currentFacility, setCurrentFacility] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [selectedStatus, setSelectedStatus] = useState(null);
  const [categories] = useState([
    { id: 1, name: 'Sân bóng đá mini' },
    { id: 2, name: 'Sân bóng rổ' },
    { id: 3, name: 'Sân cầu lông' },
    { id: 4, name: 'Sân tennis' },
    { id: 5, name: 'Sân bóng chuyền' },
    { id: 6, name: 'Sân futsal' },
    { id: 7, name: 'Sân bóng bàn' },
    { id: 8, name: 'Sân bóng ném' },
    { id: 9, name: 'Sân đa năng' },
    { id: 10, name: 'Sân tập gym' }
  ]);

  // State cho modal thêm mới
  const [showAddModal, setShowAddModal] = useState(false);
  const [newCourt, setNewCourt] = useState({
    courtName: '',
    categoryId: '',
    pricePerHour: ''
  });

  // Fetch data từ API
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
      
      if (response && response.data) {
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
      setCourts([]);
    } finally {
      setLoading(false);
    }
  };

  // Hàm xử lý thêm sân mới
  const handleAddCourt = async () => {
    try {
      const courtData = {
        facilityId: parseInt(facilityId),
        courtName: newCourt.courtName,
        categoryId: parseInt(newCourt.categoryId),
        pricePerHour: parseInt(newCourt.pricePerHour)
      };
      
      const response = await addNewCourt(courtData);
      
      if (response && response.success) {
        setShowAddModal(false);
        setNewCourt({
          courtName: '',
          categoryId: '',
          pricePerHour: ''
        });
        fetchCourts(); // Refresh danh sách sau khi thêm thành công
      } else {
        setError('Thêm sân thất bại. Vui lòng thử lại.');
      }
    } catch (error) {
      console.error('Error adding court:', error);
      setError('Có lỗi xảy ra khi thêm sân mới.');
    }
  };

  // Hàm xử lý thay đổi input trong modal
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setNewCourt(prev => ({
      ...prev,
      [name]: value
    }));
  };

  useEffect(() => {
    fetchCourts();
  }, [facilityId, pagination.pageNumber, selectedCategory, selectedStatus]);

  // ... (giữ nguyên các hàm khác như formatPrice, handlePageChange, handleSearch, renderTableHeader, renderTableRow, renderPagination)

  // Modal thêm sân mới
  const renderAddCourtModal = () => (
    <Modal show={showAddModal} onHide={() => setShowAddModal(false)}>
      <Modal.Header closeButton>
        <Modal.Title>Thêm Sân Mới</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Form>
          <Form.Group className="mb-3">
            <Form.Label>Tên sân <span className="text-danger">*</span></Form.Label>
            <Form.Control
              type="text"
              name="courtName"
              value={newCourt.courtName}
              onChange={handleInputChange}
              placeholder="Nhập tên sân"
              required
            />
          </Form.Group>

          <Form.Group className="mb-3">
            <Form.Label>Loại sân <span className="text-danger">*</span></Form.Label>
            <Form.Select
              name="categoryId"
              value={newCourt.categoryId}
              onChange={handleInputChange}
              required
            >
              <option value="">Chọn loại sân</option>
              {categories.map(category => (
                <option key={category.id} value={category.id}>{category.name}</option>
              ))}
            </Form.Select>
          </Form.Group>

          <Form.Group className="mb-3">
            <Form.Label>Giá mỗi giờ (VND) <span className="text-danger">*</span></Form.Label>
            <Form.Control
              type="number"
              name="pricePerHour"
              value={newCourt.pricePerHour}
              onChange={handleInputChange}
              min="0"
              placeholder="Nhập giá"
              required
            />
          </Form.Group>
        </Form>
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={() => setShowAddModal(false)}>
          Hủy
        </Button>
        <Button variant="primary" onClick={handleAddCourt}>
          Thêm mới
        </Button>
      </Modal.Footer>
    </Modal>
  );

  // ... (giữ nguyên phần render loading, error)

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
          >
            <option value="">Tất cả loại sân</option>
            {categories.map(category => (
              <option key={category.id} value={category.id}>
                {category.name}
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

        {/* Thay đổi nút thêm sân để mở modal */}
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
              {renderTableHeader()}
              <tbody>
                {courts.map(renderTableRow)}
              </tbody>
            </table>
            {renderPagination()}
          </>
        )}
      </div>

      {/* Thêm modal vào đây */}
      {renderAddCourtModal()}
    </div>
  );
};

export default CourtManagement;