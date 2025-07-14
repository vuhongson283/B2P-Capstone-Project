using B2P_API.DTOs.ReportDTO;
using B2P_API.Response;

namespace B2P_API.Interface
{
    public interface IReportRepository
    {
        Task<PagedResponse<ReportDTO>> GetReport(int pageNumber, int pageSize,
            int userId, DateTime? startDate, DateTime? endDate, int? facilityId);
        Task<TotalReportDTO> GetTotalReport(int userId, DateTime? startDate, DateTime? endDate);
    }
}
