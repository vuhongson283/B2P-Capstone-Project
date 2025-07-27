import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAllCourts } from '../../services/apiService';
import { Form, InputGroup, Dropdown, Button } from 'react-bootstrap';
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
    { id: 1, name: 'Sân đơn' },
    { id: 2, name: 'Sân đôi' },
    // Thêm các categories khác
  ]);

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
    } catch (error) {
      console.error("Error fetching courts:", error);
      setError(error.message || 'Không thể tải dữ liệu sân');
      setCourts([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCourts();
  }, [facilityId, pagination.pageNumber]);

  // Hàm xử lý thay đổi trạng thái
  const handleToggleStatus = async (courtId) => {
    try {
      // TODO: Gọi API cập nhật trạng thái ở đây
      console.log('Thay đổi trạng thái sân:', courtId);
      // Sau khi cập nhật thành công, refetch data
      await fetchCourts();
    } catch (error) {
      console.error('Lỗi khi cập nhật trạng thái:', error);
      setError('Không thể cập nhật trạng thái sân. Vui lòng thử lại.');
    }
  };

  // Hàm format giá tiền
  const formatPrice = (price) => {
    if (!price || price === 0) return "0";
    return parseInt(price).toLocaleString("vi-VN");
  };

  // Hàm xử lý phân trang
  const handlePageChange = (newPage) => {
    setPagination(prev => ({...prev, pageNumber: newPage}));
  };

  // Hàm xử lý tìm kiếm
  const handleSearch = (e) => {
    e.preventDefault();
    fetchCourts();
  };

  const renderTableHeader = () => (
    <thead>
      <tr>
        <th>ID</th>
        <th>Ảnh Sân</th>
        <th>Tên Sân</th>
        <th>Loại Sân</th>
        <th>Giá/Giờ</th>
        <th>Mô Tả</th>
        <th>Trạng Thái</th>
        <th>Hành Động</th>
      </tr>
    </thead>
  );

  const renderTableRow = (court) => (
    <tr key={court.courtId}>
      <td>{court.courtId}</td>
      <td>
        {court.imageUrl ? (
          <img 
            src={court.imageUrl} 
            alt={court.courtName} 
            className="court-image"
            onClick={() => navigate(`${court.courtId}`)}
          />
        ) : (
          <div 
            className="no-image"
            onClick={() => navigate(`${court.courtId}`)}
          >
            <i className="fas fa-camera"></i>
          </div>
        )}
      </td>
      <td>
        <span 
          className="court-name"
          onClick={() => navigate(`${court.courtId}`)}
        >
          {court.courtName}
        </span>
      </td>
      <td>{court.categoryName || 'Chưa phân loại'}</td>
      <td>{formatPrice(court.pricePerHour)}đ</td>
      <td>
        <div className="description" title={court.description}>
          {court.description || 'Chưa có mô tả'}
        </div>
      </td>
      <td>
        <span className={`status ${court.status === 1 ? 'active' : 'inactive'}`}>
          {court.status === 1 ? 'Đang mở' : 'Đóng cửa'}
        </span>
      </td>
      <td>
        <div className="action-buttons">
          <button 
            className={`toggle-btn ${court.status === 1 ? 'active' : ''}`}
            onClick={() => handleToggleStatus(court.courtId)}
            title={court.status === 1 ? 'Đóng sân' : 'Mở sân'}
          >
            <i className={`fas fa-power-off`}></i>
          </button>
          <button 
            className="view-btn"
            onClick={() => navigate(`${court.courtId}`)}
            title="Xem chi tiết"
          >
            <i className="fas fa-eye"></i>
          </button>
          <button 
            className="edit-btn"
            onClick={() => navigate(`${court.courtId}/edit`)}
            title="Chỉnh sửa"
          >
            <i className="fas fa-edit"></i>
          </button>
        </div>
      </td>
    </tr>
  );

  const renderPagination = () => (
    pagination.totalPages > 1 && (
      <div className="pagination">
        <button
          disabled={pagination.pageNumber === 1}
          onClick={() => handlePageChange(pagination.pageNumber - 1)}
          className="page-btn"
        >
          <i className="fas fa-chevron-left"></i> Trước
        </button>
        
        <div className="page-numbers">
          {/* Hiển thị các số trang */}
          {Array.from({length: Math.min(5, pagination.totalPages)}, (_, i) => {
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
              <button
                key={pageNum}
                className={`page-number ${pagination.pageNumber === pageNum ? 'active' : ''}`}
                onClick={() => handlePageChange(pageNum)}
              >
                {pageNum}
              </button>
            );
          })}
        </div>
        
        <button
          disabled={pagination.pageNumber >= pagination.totalPages}
          onClick={() => handlePageChange(pagination.pageNumber + 1)}
          className="page-btn"
        >
          Sau <i className="fas fa-chevron-right"></i>
        </button>
      </div>
    )
  );

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
      {/* Header với thông tin cơ sở */}
      <div className="facility-header">
        <h1>Quản Lý Sân - {currentFacility?.name || 'Đang tải...'}</h1>
        <div className="facility-info">
          <span><i className="fas fa-map-marker-alt"></i> {currentFacility?.location}</span>
          <span><i className="fas fa-phone"></i> {currentFacility?.contact}</span>
        </div>
      </div>

      <div className="management-actions">
        <button 
          className="btn-add-court"
          onClick={() => navigate(`add`)}
        >
          <i className="fas fa-plus"></i> Thêm sân mới
        </button>
      </div>

      <div className="court-table-container">
        {courts.length === 0 ? (
          <div className="no-data">
            <i className="fas fa-court-alt"></i>
            <p>Cơ sở này chưa có sân nào</p>
            <p>Hãy thêm sân mới để bắt đầu quản lý</p>
            <button 
              className="btn-add-court"
              onClick={() => navigate(`add`)}
            >
              <i className="fas fa-plus"></i> Thêm sân đầu tiên
            </button>
          </div>
        ) : (
          <>
            <div className="table-info">
              <span>Tìm thấy {courts.length} sân (Tổng: {pagination.totalItems})</span>
              <span>Trang {pagination.pageNumber}/{pagination.totalPages}</span>
            </div>
            
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
    </div>
  );
};

export default CourtManagement;