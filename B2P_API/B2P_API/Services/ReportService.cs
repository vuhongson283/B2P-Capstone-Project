
using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.DTOs.ReportDTO;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Services
{

    public class ReportService
    {
        private readonly SportBookingDbContext _context;
        private readonly ReportRepository _repository;

        public ReportService(SportBookingDbContext context, ReportRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<ApiResponse<List<ReportDTO>>> GetReport(int userId, DateTime? startDate, DateTime? endDate, int? facilityId)
        {
            // Kiểm tra xem user có booking nào không
            var hasAnyBookings = await _context.BookingDetails
                .AnyAsync(bd => bd.Court.Facility.UserId == userId &&
                               (!facilityId.HasValue || bd.Court.FacilityId == facilityId.Value));

            if (!hasAnyBookings)
            {
                return new ApiResponse<List<ReportDTO>>
                {
                    Success = false,
                    Message = "Cơ sở của bạn chưa có booking nào",
                    Status = 200,
                    Data = new List<ReportDTO>()
                };
            }

            var report = await _repository.GetReport(userId, startDate, endDate, facilityId);

            if(report == null || !report.Any())
            {
                return new ApiResponse<List<ReportDTO>>
                {
                    Success = false,
                    Message = "Không có dữ liệu trong khoảng thời gian đã chọn",
                    Status = 200,
                    Data = report
                };
            }

            return new ApiResponse<List<ReportDTO>>
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
    }
}
