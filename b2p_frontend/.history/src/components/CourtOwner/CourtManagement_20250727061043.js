import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getAllCourts } from '../../services/apiService';
import './CourtManagement.scss';

const CourtManagement = () => {
  const { facilityId } = useParams(); // Lấy facilityId từ URL
  const [courts, setCourts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
    totalItems: 0
  });

  // Fetch data từ API với facilityId từ URL
  const fetchCourts = async () => {
    setLoading(true);
    try {
      const response = await getAllCourts({
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        facilityId: parseInt(facilityId) // Chuyển sang number
      });

      if (response.data && response.data.success) {
        setCourts(response.data.data.items);
        setPagination(prev => ({
          ...prev,
          totalItems: response.data.data.totalItems
        }));
      }
    } catch (error) {
      console.error('Lỗi khi lấy danh sách sân:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (facilityId) {
      fetchCourts();
    }
  }, [facilityId, pagination.pageNumber]);

  // Hàm xử lý thay đổi trạng thái
  const handleToggleStatus = (courtId) => {
    // Gọi API cập nhật trạng thái ở đây
    console.log('Thay đổi trạng thái sân:', courtId);
  };

  // Hàm format giá tiền
  const formatPrice = (price) => {
    return new Intl.NumberFormat('vi-VN').format(price) + 'đ';
  };

  return (
    <div className="court-management-container">
      <h1>Quản Lý Sân</h1>
      
      <div className="management-notes">
        <p><strong>Quản Lý G5S</strong></p>
        <p><strong>Quản Lý Sân</strong></p>
      </div>

      <div className="court-table-container">
        {loading ? (
          <div className="loading">Đang tải dữ liệu...</div>
        ) : (
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
                      <img src={court.imageUrl} alt={court.courtName} className="court-image" />
                    ) : (
                      <div className="no-image">No Image</div>
                    )}
                  </td>
                  <td>{court.courtName}</td>
                  <td>{court.categoryName || 'Cầu Lông'}</td>
                  <td>{formatPrice(court.pricePerHour || 80000)}</td>
                  <td>{court.description || 'Sân mới, cầu free'}</td>
                  <td>
                    <span className={`status ${court.status === 1 ? 'active' : 'inactive'}`}>
                      {court.status === 1 ? 'Đang mở' : 'Đóng cửa'}
                    </span>
                  </td>
                  <td>
                    <button 
                      className={`toggle-btn ${court.status === 1 ? 'active' : ''}`}
                      onClick={() => handleToggleStatus(court.courtId)}
                    >
                      {court.status === 1 ? '✅' : '❌'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Phân trang */}
      <div className="pagination">
        <button
          disabled={pagination.pageNumber === 1}
          onClick={() => setPagination(prev => ({...prev, pageNumber: prev.pageNumber - 1}))}
        >
          Trang trước
        </button>
        <span>Trang {pagination.pageNumber}</span>
        <button
          disabled={courts.length < pagination.pageSize}
          onClick={() => setPagination(prev => ({...prev, pageNumber: prev.pageNumber + 1}))}
        >
          Trang sau
        </button>
      </div>
    </div>
  );
};

export default CourtManagement;