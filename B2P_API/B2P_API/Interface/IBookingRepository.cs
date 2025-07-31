using B2P_API.DTOs.BookingDTOs;
using B2P_API.Models;
using B2P_API.Repository;

namespace B2P_API.Interface
{
    public interface IBookingRepository
    {
        Task<List<CourtAvailability>> GetCourtAvailabilityAsync(int facilityId, int categoryId, DateTime checkInDate, List<int> timeSlotIds);
        Task<int> CountByUserIdAsync(int? userId, int? statusId);
        Task<List<Booking>> GetByUserIdAsync(int? userId, BookingQueryParameters query);
        Booking GetById(int id);
        Task<List<TimeSlotAvailability>> GetAvailableCourtCountPerSlotAsync(int facilityId, int categoryId, DateTime checkInDate);
    }

}
