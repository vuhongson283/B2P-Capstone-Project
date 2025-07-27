import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getAllCourts, addNewCourt } from '../../services/apiService';
import { Form, InputGroup, Button, Modal } from 'react-bootstrap';
import './CourtManagement.scss';

const CourtManagement = () => {
  const { facilityId } = useParams();
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

  // Modal state
  const [showAddModal, setShowAddModal] = useState(false);
  const [newCourt, setNewCourt] = useState({
    courtName: '',
    categoryId: '',
    pricePerHour: ''
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
      
      if (response.data?.success) {
        setShowAddModal(false);
        setNewCourt({ courtName: '', categoryId: '', pricePerHour: '' });
        fetchCourts();
        alert(response.data.message || 'Court added successfully!');
      } else {
        setError(response.data?.message || 'Failed to add court');
      }
    } catch (error) {
      console.error('Error adding court:', error);
      setError('Error adding court: ' + (error.response?.data?.message || error.message));
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

  useEffect(() => {
    fetchCourts();
  }, [facilityId, pagination.pageNumber, selectedCategory, selectedStatus]);

  const formatPrice = (price) => {
    return price ? parseInt(price).toLocaleString("vi-VN") : "0";
  };

  if (loading) {
    return (
      <div className="court-management-container">
        <div className="loading-spinner">
          <i className="fas fa-spinner fa-spin"></i> Loading...
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
        <h1>Court Management - {currentFacility?.name || 'Loading...'}</h1>
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
                placeholder="Search courts..."
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
            <option value="">All categories</option>
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
            <option value="">All statuses</option>
            <option value="1">Active</option>
            <option value="2">Inactive</option>
          </Form.Select>
        </div>

        <Button 
          variant="success"
          className="btn-add-court"
          onClick={() => setShowAddModal(true)}
        >
          <i className="fas fa-plus"></i> Add New Court
        </Button>
      </div>

      <div className="court-table-container">
        {courts.length === 0 ? (
          <div className="no-data-message">
            <i className="fas fa-search"></i>
            <p>No courts found matching your criteria</p>
            <p>Please try different search parameters</p>
          </div>
        ) : (
          <>
            <table className="court-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Court Name</th>
                  <th>Category</th>
                  <th>Price/Hour</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {courts.map(court => (
                  <tr key={court.courtId}>
                    <td>{court.courtId}</td>
                    <td>{court.courtName}</td>
                    <td>{court.categoryName || 'Uncategorized'}</td>
                    <td>{formatPrice(court.pricePerHour)}đ</td>
                    <td>
                      <span className={`status ${court.status === 1 ? 'active' : 'inactive'}`}>
                        {court.status === 1 ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td>
                      {/* Add action buttons here */}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {/* Add pagination here if needed */}
          </>
        )}
      </div>

      {/* Add Court Modal */}
      <Modal show={showAddModal} onHide={() => setShowAddModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Thêm sân mới</Modal.Title>
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
                placeholder="Enter court name"
                required
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Thể loại <span className="text-danger">*</span></Form.Label>
              <Form.Select
                name="categoryId"
                value={newCourt.categoryId}
                onChange={handleInputChange}
                required
              >
                <option value="">Chọn thể loại sân</option>
                {categories.map(category => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </Form.Select>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Giá/Giờ (VND) <span className="text-danger">*</span></Form.Label>
              <Form.Control
                type="number"
                name="pricePerHour"
                value={newCourt.pricePerHour}
                onChange={handleInputChange}
                min="0"
                placeholder="Enter price"
                required
              />
            </Form.Group>
          </Form>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowAddModal(false)}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleAddCourt}>
            Add Court
          </Button>
        </Modal.Footer>
      </Modal>
    </div>
  );
};

export default CourtManagement;