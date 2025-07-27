import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAllCourts } from '../../services/apiService';
import './CourtManagement.scss';

const CourtManagement = () => {
  const { facilityId } = useParams();
  const navigate = useNavigate();
  const [courts, setCourts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null); // Thêm state để track error
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
    totalItems: 0
  });
  const [currentFacility, setCurrentFacility] = useState(null);

  // Fetch data từ API với debug chi tiết
  const fetchCourts = async () => {
    console.log('=== DEBUG fetchCourts ===');
    console.log('facilityId từ useParams:', facilityId);
    console.log('typeof facilityId:', typeof facilityId);
    console.log('parseInt(facilityId):', parseInt(facilityId));
    console.log('pagination:', pagination);
    
    if (!facilityId) {
      console.log('Không có facilityId, return early');
      return;
    }
    
    setLoading(true);
    setError(null); // Reset error state
    
    try {
      const requestParams = {
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        facilityId: parseInt(facilityId)
      };
      
      console.log('Request parameters:', requestParams);
      console.log('Gọi getAllCourts với params:', requestParams);
      
      const response = await getAllCourts(requestParams);
      
      console.log('Raw response:', response);
      console.log('response.data:', response.data);
      console.log('response.status:', response.status);
      
      // Kiểm tra cấu trúc response
      if (response && response.data) {
        console.log('response.data.success:', response.data.success);
        console.log('response.data.data:', response.data.data);
        
        if (response.data.success) {
          console.log('API call thành công');
          console.log('items:', response.data.data.items);
          console.log('totalItems:', response.data.data.totalItems);
          
          setCourts(response.data.data.items || []);
          setPagination(prev => ({
            ...prev,
            totalItems: response.data.data.totalItems || 0,
            totalPages: Math.ceil((response.data.data.totalItems || 0) / pagination.pageSize)
          }));
          
          setCurrentFacility({
            id: facilityId,
            name: `Cơ sở ${facilityId}`,
            location: "Địa chỉ cơ sở",
            contact: "0123456789"
          });
        } else {
          console.log('API trả về success = false');
          console.log('Error message:', response.data.message);
          setError(`API Error: ${response.data.message || 'Unknown error'}`);
        }
      } else {
        console.log('Response không có data');
        setError('Invalid response structure');
      }
    } catch (error) {
      console.error('=== CATCH ERROR ===');
      console.error('Error type:', typeof error);
      console.error('Error message:', error.message);
      console.error('Error stack:', error.stack);
      console.error('Full error object:', error);
      
      // Kiểm tra các loại error phổ biến
      if (error.response) {
        console.error('HTTP Error Response:');
        console.error('Status:', error.response.status);
        console.error('Status Text:', error.response.statusText);
        console.error('Response Data:', error.response.data);
        console.error('Response Headers:', error.response.headers);
        setError(`HTTP ${error.response.status}: ${error.response.statusText}`);
      } else if (error.request) {
        console.error('Network Error - No response received:');
        console.error('Request:', error.request);
        setError('Network Error: No response from server');
      } else {
        console.error('Other Error:', error.message);
        setError(`Error: ${error.message}`);
      }
    } finally {
      setLoading(false);
      console.log('=== END fetchCourts ===');
    }
  };

  useEffect(() => {
    console.log('useEffect triggered with facilityId:', facilityId);
    fetchCourts();
  }, [facilityId, pagination.pageNumber]);

  // Hàm xử lý thay đổi trạng thái
  const handleToggleStatus = async (courtId) => {
    try {
      console.log('Thay đổi trạng thái sân:', courtId);
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
        {/* Debug info */}
        <div style={{fontSize: '12px', color: '#666', marginTop: '10px'}}>
          Debug: facilityId = {facilityId} (type: {typeof facilityId})
        </div>
      </div>

      <div className="management-actions">
        <button 
          className="btn-add-court"
          onClick={() => navigate(`facilities/${facilityId}/courts/add`)}
        >
          <i className="fas fa-plus"></i> Thêm sân mới
        </button>
      </div>

      <div className="court-table-container">
        {/* Hiển thị error nếu có */}
        {error && (
          <div className="error-message" style={{
            backgroundColor: '#fee', 
            color: '#c00', 
            padding: '10px', 
            marginBottom: '10px',
            border: '1px solid #fcc'
          }}>
            <strong>Lỗi:</strong> {error}
            <br />
            <button onClick={fetchCourts} style={{marginTop: '5px'}}>
              Thử lại
            </button>
          </div>
        )}
        
        {loading ? (
          <div className="loading">
            <i className="fas fa-spinner fa-spin"></i> Đang tải dữ liệu...
          </div>
        ) : courts.length === 0 ? (
          <div className="no-data">
            Không có sân nào trong cơ sở này
            <br />
            <small>Facility ID: {facilityId}</small>
          </div>
        ) : (
          <>
            <div style={{marginBottom: '10px', fontSize: '14px', color: '#666'}}>
              Tìm thấy {courts.length} sân
            </div>
            
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
                          onClick={() => navigate(`facilities/${facilityId}/courts/${court.courtId}`)}
                        />
                      ) : (
                        <div 
                          className="no-image"
                          onClick={() => navigate(`facilities/${facilityId}/courts/${court.courtId}`)}
                        >
                          <i className="fas fa-camera"></i>
                        </div>
                      )}
                    </td>
                    <td>
                      <span 
                        className="court-name"
                        onClick={() => navigate(`facilities/${facilityId}/courts/${court.courtId}`)}
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
                          onClick={() => navigate(`facilities/${facilityId}/courts/${court.courtId}/edit`)}
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