
using B2P_API.DTOs.BookingDTOs;
using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.DTOs.ReportDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace B2P_API.Services
{

    public class ReportService
    {
        private readonly IReportRepository _repository;
        private readonly IExcelExportService _excelExportService;


        public ReportService( IReportRepository repository, IExcelExportService excelExportService)
        {
            _repository = repository;
            _excelExportService = excelExportService;
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
            try
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
            catch (Exception ex)
            {
                return new ApiResponse<TotalReportDTO>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi trong quá trình lấy báo cáo: " + ex.Message,
                    Status = 500,
                    Data = null
                };
            }
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

        public async Task<ApiResponse<byte[]>> ExportReportToExcel(
            int userId, DateTime? startDate, DateTime? endDate, int? facilityId, int pageNumber = 1, int pageSize = 100)
        {
            var reportResponse = await GetReport(userId, startDate, endDate, facilityId, pageNumber, pageSize);

            if (!reportResponse.Success || reportResponse.Data == null || !reportResponse.Data.Items.Any())
            {
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = "Không có dữ liệu để xuất ra file Excel.",
                    Status = 200,
                    Data = null
                };
            }

            // Sửa thông tin phân trang cho export Excel
            var reportData = reportResponse.Data;
            reportData.CurrentPage = 1;
            reportData.TotalPages = 1;
            reportData.ItemsPerPage = reportData.Items.Count(); // Số bản ghi thực tế được export

            return await _excelExportService.ExportToExcelAsync(reportData, "Báo cáo đặt sân");
        }


        public async Task<ApiResponse<byte[]>> ExportAdminReportToExcel(int? year = null, int? month = null)
        {
            var adminReportResponse = await GetAdminReportPaged(year, month);
            if (!adminReportResponse.Success || adminReportResponse.Data == null)
            {
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = "Không có dữ liệu để xuất ra file Excel.",
                    Status = 200
                };
            }

            var report = adminReportResponse.Data;

            // Check từng danh sách con
            if (report.MonthlyStats == null &&
                (report.TopFacilities == null || !report.TopFacilities.Any()) &&
                (report.PopularCourtCategories == null || !report.PopularCourtCategories.Any()))
            {
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = "Không có dữ liệu để xuất ra file Excel.",
                    Status = 200
                };
            }

            using var package = new ExcelPackage();

            // Sheet 1: Thống kê tổng
            if (report.MonthlyStats != null)
            {
                var statsPaged = new PagedResponse<MonthlyStatsDTO>
                {
                    Items = new List<MonthlyStatsDTO> { report.MonthlyStats },
                    TotalItems = 1,
                    CurrentPage = 1,
                    TotalPages = 1,
                    ItemsPerPage = 1
                };
                var statsResult = await _excelExportService.ExportToExcelAsync(statsPaged, "Tổng quan");
                using var tempStats = new ExcelPackage(new System.IO.MemoryStream(statsResult.Data));
                package.Workbook.Worksheets.Add("Tổng quan", tempStats.Workbook.Worksheets[0]);
            }

            // Sheet 2: Cơ sở hàng đầu
            if (report.TopFacilities != null && report.TopFacilities.Any())
            {
                var topFacilitiesPaged = new PagedResponse<FacilityStatDTO>
                {
                    Items = report.TopFacilities,
                    TotalItems = report.TopFacilities.Count,
                    CurrentPage = 1,
                    TotalPages = 1,
                    ItemsPerPage = report.TopFacilities.Count
                };
                var facilitiesResult = await _excelExportService.ExportToExcelAsync(topFacilitiesPaged, "Top Cơ Sở");
                using var tempFac = new ExcelPackage(new System.IO.MemoryStream(facilitiesResult.Data));
                package.Workbook.Worksheets.Add("Top Cơ Sở", tempFac.Workbook.Worksheets[0]);
            }

            // Sheet 3: Loại sân phổ biến
            if (report.PopularCourtCategories != null && report.PopularCourtCategories.Any())
            {
                var categoryPaged = new PagedResponse<CourtCategoryStatDTO>
                {
                    Items = report.PopularCourtCategories,
                    TotalItems = report.PopularCourtCategories.Count,
                    CurrentPage = 1,
                    TotalPages = 1,
                    ItemsPerPage = report.PopularCourtCategories.Count
                };
                var categoryResult = await _excelExportService.ExportToExcelAsync(categoryPaged, "Loại sân");
                using var tempCat = new ExcelPackage(new System.IO.MemoryStream(categoryResult.Data));
                package.Workbook.Worksheets.Add("Loại sân", tempCat.Workbook.Worksheets[0]);
            }

            return new ApiResponse<byte[]>
            {
                Success = true,
                Message = "Xuất báo cáo quản trị thành công!",
                Status = 200,
                Data = package.GetAsByteArray()
            };
        }

        public string FormatDateRange(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
                return $"From_{startDate.Value:yyyy-MM-dd}_To_{endDate.Value:yyyy-MM-dd}";
            if (startDate.HasValue)
                return $"From_{startDate.Value:yyyy-MM-dd}";
            if (endDate.HasValue)
                return $"To_{endDate.Value:yyyy-MM-dd}";
            return DateTime.Now.ToString("yyyy-MM-dd");
        }



    }
}
