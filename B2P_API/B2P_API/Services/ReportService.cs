
using B2P_API.DTOs.BookingDTOs;
using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.DTOs.ReportDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Services
{

    public class ReportService
    {
        private readonly IReportRepository _repository;

        public ReportService( IReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<PagedResponse<ReportDTO>>> GetReport(
            int userId, DateTime? startDate, DateTime? endDate, int? facilityId, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0) pageNumber = 1;

            // Kiểm tra xem user có booking nào không
            var hasAnyBookings = await _repository.HasAnyBookings(userId, facilityId);

            if (!hasAnyBookings)
            {
                return new ApiResponse<PagedResponse<ReportDTO>>
                {
                    Success = false,
                    Message = "Cơ sở của bạn chưa có booking nào",
                    Status = 200
                };
            }

            var report = await _repository.GetReport(pageNumber, pageSize, userId, startDate, endDate, facilityId);

            if(report == null)
            {
                return new ApiResponse<PagedResponse<ReportDTO>>
                {
                    Success = false,
                    Message = "Không có dữ liệu trong khoảng thời gian đã chọn",
                    Status = 200
                };
            }

            return new ApiResponse<PagedResponse<ReportDTO>>
            {
                Success = true,
                Message = "Lấy dữ liệu báo cáo thành công!",
                Status = 200,
                Data = report
            };
        }

        public async Task<ApiResponse<TotalReportDTO>> GetTotalReport(int userId, DateTime? startDate, DateTime? endDate)
        {
            var report = await _repository.GetTotalReport(userId, startDate, endDate);

            if (report == null)
            {
                return new ApiResponse<TotalReportDTO>
                {
                    Success = false,
                    Message = "Không có dữ liệu trong khoảng thời gian đã chọn",
                    Status = 200,
                    Data = report
                };
            }

            return new ApiResponse<TotalReportDTO>
            {
                Success = true,
                Message = "Lấy dữ liệu báo cáo thành công!",
                Status = 200,
                Data = report
            };
        }

        public async Task<ApiResponse<AdminReportDTO>> GetAdminReportPaged(int? year = null, int? month = null)
        {
            try
            {
                var report = await _repository.GetAdminReport(year, month);

                if (report == null)
                {
                    return new ApiResponse<AdminReportDTO>
                    {
                        Success = false,
                        Message = "Không có dữ liệu trong khoảng thời gian đã chọn",
                        Status = 200,
                        Data = null
                    };
                }

                return new ApiResponse<AdminReportDTO>
                {
                    Success = true,
                    Message = "Lấy dữ liệu báo cáo thành công!",
                    Status = 200,
                    Data = report
                };
            }

            catch (Exception ex)
            {
                return new ApiResponse<AdminReportDTO>
                {
                    Success = false,
                    Message = $"Đã xảy ra lỗi: {ex.Message}",
                    Status = 500,
                    Data = null
                };
            }
        }
    }
}
