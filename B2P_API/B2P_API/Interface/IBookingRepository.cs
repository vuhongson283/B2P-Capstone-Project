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
        Task<Dictionary<int, TimeSlot>> GetTimeSlotsByIdsAsync(IEnumerable<int> timeSlotIds);
        Task<Dictionary<int, Court>> GetCourtsByIdsAsync(IEnumerable<int> courtIds);
        Task AddBookingAsync(Booking booking);
        Task AddBookingDetailsAsync(IEnumerable<BookingDetail> details);
        Task<Dictionary<int, TimeSpan>> GetSlotStartTimesByIdsAsync(IEnumerable<int> slotIds);
        Task<Dictionary<int, Court>> GetCourtsWithCategoryAsync();
        Task<Dictionary<int, TimeSlot>> GetTimeSlotsAsync();
        Task<Booking?> GetBookingWithDetailsByIdAsync(int bookingId);
        Task<Booking?> GetBookingWithDetailsAsync(int bookingId);
        Task<bool> SaveAsync(); 



    }

}
