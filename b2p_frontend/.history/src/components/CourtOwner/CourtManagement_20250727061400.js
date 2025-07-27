import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAllCourts } from '../../services/apiService';
import './CourtManagement.scss';

const CourtManagement = () => {
  const { facilityId } = useParams(); // Lấy facilityId từ URL
  const navigate = useNavigate();
  const [courts, setCourts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
    totalItems: 0
  });
  const [currentFacility, setCurrentFacility] = useState(null);

  // Fetch data từ API
  const fetchCourts = async () => {
    if (!facilityId) return;
    
    setLoading(true);
    try {
      const response = await getAllCourts({
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        facilityId: parseInt(facilityId) // Sử dụng facilityId từ URL
      });

      if (response.data?.success) {
        setCourts(response.data.data.items);
        setPagination(prev => ({
          ...prev,
          totalItems: response.data.data.totalItems,
          totalPages: Math.ceil(response.data.data.totalItems / pagination.pageSize)
        }));
        
        // Mock data for facility info - bạn nên thay bằng API call thực tế
        setCurrentFacility({
          id: facilityId,
          name: `Cơ sở ${facilityId}`,
          location: "Địa chỉ cơ sở",
          contact: "0123456789"
        });
      }
    } catch (error) {
      console.error('Lỗi khi lấy danh sách sân:', error);
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
      // Gọi API cập nhật trạng thái ở đây
      console.log('Thay đổi trạng thái sân:', courtId);
      // Sau khi cập nhật thành công, refetch data
      await fetchCourts();
    } catch (error) {
      console.error('Lỗi khi cập nhật trạng thái:', error);
    }
  };

  // Hàm format giá tiền
  const formatPrice = (price) => {
    return new Intl.NumberFormat('vi-VN').format(price) + 'đ';
  };

  if (!facilityId) {
    return (
      <div className="court-management-container">
        <div className="error-message">Vui lòng chọn cơ sở để xem danh sách sân</div>
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
          onClick={() => navigate(`/court-owner/facilities/${facilityId}/courts/add`)}
        >
          <i className="fas fa-plus"></i> Thêm sân mới
        </button>
      </div>

      <div className="court-table-container">
        {loading ? (
          <div className="loading">
            <i className="fas fa-spinner fa-spin"></i> Đang tải dữ liệu...
          </div>
        ) : courts.length === 0 ? (
          <div className="no-data">Không có sân nào trong cơ sở này</div>
        ) : (
          <>
            <table className="court-table">
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
              <tbody>
                {courts.map((court) => (
                  <tr key={court.courtId}>
                    <td>{court.courtId}</td>
                    <td>
                      {court.imageUrl ? (
                        <img 
                          src={court.imageUrl} 
                          alt={court.courtName} 
                          className="court-image"
                          onClick={() => navigate(`/court-owner/facilities/${facilityId}/courts/${court.courtId}`)}
                        />
                      ) : (
                        <div 
                          className="no-image"
                          onClick={() => navigate(`/court-owner/facilities/${facilityId}/courts/${court.courtId}`)}
                        >
                          <i className="fas fa-camera"></i>
                        </div>
                      )}
                    </td>
                    <td>
                      <span 
                        className="court-name"
                        onClick={() => navigate(`/court-owner/facilities/${facilityId}/courts/${court.courtId}`)}
                      >
                        {court.courtName}
                      </span>
                    </td>
                    <td>{court.categoryName || 'Cầu Lông'}</td>
                    <td>{formatPrice(court.pricePerHour || 80000)}</td>
                    <td>{court.description || 'Sân mới, cầu free'}</td>
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
                          <i className={`fas fa-power-off ${court.status === 1 ? 'active' : ''}`}></i>
                        </button>
                        <button 
                          className="edit-btn"
                          onClick={() => navigate(`/court-owner/facilities/${facilityId}/courts/${court.courtId}/edit`)}
                          title="Chỉnh sửa"
                        >
                          <i className="fas fa-edit"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Phân trang */}
            {pagination.totalItems > pagination.pageSize && (
              <div className="pagination">
                <button
                  disabled={pagination.pageNumber === 1}
                  onClick={() => setPagination(prev => ({...prev, pageNumber: prev.pageNumber - 1}))}
                >
                  <i className="fas fa-chevron-left"></i> Trước
                </button>
                <span>Trang {pagination.pageNumber}/{pagination.totalPages}</span>
                <button
                  disabled={pagination.pageNumber >= pagination.totalPages}
                  onClick={() => setPagination(prev => ({...prev, pageNumber: prev.pageNumber + 1}))}
                >
                  Sau <i className="fas fa-chevron-right"></i>
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default CourtManagement;