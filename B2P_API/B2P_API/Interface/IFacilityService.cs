using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Interface
{
    public interface IFacilityService
    {
        Task<ApiResponse<PagedResponse<FacilityWithCourtCountDto>>> GetFacilitiesByUserAsync(int userId, string? facilityName = null, int? statusId = null, int currentPage = 1, int itemsPerPage = 3);
        Task<ApiResponse<Facility>> CreateFacility(CreateFacilityRequest request);
        Task<ApiResponse<Facility>> UpdateFacility(UpdateFacilityRequest request, int facilityId);
        Task<ApiResponse<Facility>> DeleteFacility( int facilityId);
        Task<ApiResponse<Facility>> GetFacilityById(int facilityId);
        
    }
}
