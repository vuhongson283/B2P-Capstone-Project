using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Response;

namespace B2P_API.Interface
{
    public interface IFacilityService
    {
        Task<ApiResponse<PagedResponse<FacilityWithCourtCountDto>>> GetFacilitiesByUserAsync(int userId, string? facilityName = null, int? statusId = null);
    }
}
