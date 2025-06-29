using B2P_API.DTOs.FacilityDTOs;
using B2P_API.DTOs.TimeslotDTO;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Interface
{
    public interface ITimeSlotManagementService
    {
        Task<ApiResponse<PagedResponse<TimeSlot>>> GetTimeslotByFacilityIdAsync(int facilityId, int? statusId = null, int pageNumber = 1, int pageSize = 10);
        Task<ApiResponse<TimeSlot>> CreateNewTimeSlot(CreateTimeslotRequestDTO request);
        Task<ApiResponse<TimeSlot>> UpdateTimeSlot(CreateTimeslotRequestDTO request, int timeslotId);
        Task<ApiResponse<TimeSlot>> DeleteTimeSlot(int timeslotId);
    }
}
