using B2P_API.DTOs.ReportDTO;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    public class ReportRepository
    {
        private readonly SportBookingDbContext _context;

        public ReportRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReportDTO>> GetReport(int userId, DateTime? startDate, DateTime? endDate, int? facilityId)
        {
            return await _context.Facilities
            .Where(f => f.UserId == userId && (!facilityId.HasValue || f.FacilityId == facilityId.Value))
            .SelectMany(f => f.Courts)
            .SelectMany(c => c.BookingDetails)
            .Where(bd => (!startDate.HasValue || bd.CreateAt.Date >= startDate.Value.Date) &&
                         (!endDate.HasValue || bd.CreateAt.Date <= endDate.Value.Date))
            .Select(bd => new ReportDTO
            {
                FacilityId = bd.Court.Facility.FacilityId,
                FacilityName = bd.Court.Facility.FacilityName,
                CourtId = bd.Court.CourtId,
                CourtName = bd.Court.CourtName,
                CourtCategory = bd.Court.Category != null ? bd.Court.Category.CategoryName : null,
                PricePerHour = bd.Court.PricePerHour,
                BookingId = bd.Booking.BookingId,
                CustomerName = bd.Booking.User != null ? bd.Booking.User.FullName : null,
                CustomerEmail = bd.Booking.User != null ? bd.Booking.User.Email : null,
                CustomerPhone = bd.Booking.User != null ? bd.Booking.User.Phone : null,
                StartTime = bd.TimeSlot != null ? bd.TimeSlot.StartTime : null,
                EndTime = bd.TimeSlot != null ? bd.TimeSlot.EndTime : null,
                CheckInDate = bd.CheckInDate,
                TotalPrice = bd.Booking.TotalPrice,
                BookingTime = bd.Booking.CreateAt,
                BookingStatus = bd.Booking.Status != null ? bd.Booking.Status.StatusName : null,
                PaymentId = bd.Booking.Payments.FirstOrDefault() != null ? bd.Booking.Payments.FirstOrDefault().PaymentId : 0,
                PaymentAmount = bd.Booking.Payments.FirstOrDefault() != null ? bd.Booking.Payments.FirstOrDefault().Amount : null,
                PaymentTime = bd.Booking.Payments.FirstOrDefault() != null ? bd.Booking.Payments.FirstOrDefault().TimeStamp : null,
                PaymentStatus = bd.Booking.Payments.FirstOrDefault() != null &&
                              bd.Booking.Payments.FirstOrDefault().Status != null ?
                              bd.Booking.Payments.FirstOrDefault().Status.StatusName : null
            })
            .ToListAsync();
        }
    }
}
