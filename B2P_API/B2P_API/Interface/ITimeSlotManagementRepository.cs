using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface ITimeSlotManagementRepository
    {
        Task<TimeSlot> CreateAsync(TimeSlot timeSlot);
        Task<TimeSlot> UpdateAsync(TimeSlot timeSlot);
        Task<bool> DeleteAsync(int id);
        Task<List<TimeSlot>> GetByFacilityIdAsync(int facilityId);
        Task<TimeSlot> GetByIdAsync(int id);
    }
}
