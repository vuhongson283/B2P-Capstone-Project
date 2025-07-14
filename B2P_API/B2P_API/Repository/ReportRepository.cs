using B2P_API.DTOs.ReportDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly SportBookingDbContext _context;

        public ReportRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<ReportDTO>> GetReport(int pageNumber, int pageSize,
            int userId, DateTime? startDate, DateTime? endDate, int? facilityId)
        {
            var query = _context.Bookings
                .Where(b => b.BookingDetails.Any(bd =>
                    bd.Court.Facility.UserId == userId &&
                    (!facilityId.HasValue || bd.Court.FacilityId == facilityId.Value) &&
                    (!startDate.HasValue || b.CreateAt.Date >= startDate.Value.Date) &&
                    (!endDate.HasValue || b.CreateAt.Date <= endDate.Value.Date)))
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new ReportDTO
                {
                    BookingId = b.BookingId,
                    CustomerName = b.User != null ? b.User.FullName : null,
                    CustomerEmail = b.User != null ? b.User.Email : null,
                    CustomerPhone = b.User != null ? b.User.Phone : null,
                    TotalPrice = b.TotalPrice,
                    BookingTime = b.CreateAt,
                    BookingStatus = b.Status != null ? b.Status.StatusDescription : null,
                    PaymentId = b.Payments.FirstOrDefault() != null ? b.Payments.FirstOrDefault().PaymentId : 0,
                    PaymentAmount = b.Payments.FirstOrDefault() != null ? b.Payments.FirstOrDefault().Amount : null,
                    PaymentTime = b.Payments.FirstOrDefault() != null ? b.Payments.FirstOrDefault().TimeStamp : null,
                    PaymentStatus = b.Payments.FirstOrDefault() != null &&
                                  b.Payments.FirstOrDefault().Status != null ?
                                  b.Payments.FirstOrDefault().Status.StatusDescription : null,
                    CourtCount = b.BookingDetails.Count,
                    CourtCategories = string.Join(", ", b.BookingDetails.Select(bd => bd.Court.Category.CategoryName)),
                    TimeSlotCount = b.BookingDetails.Count,
                    CheckInDate = b.BookingDetails.OrderBy(bd => bd.CheckInDate)
                             .Select(bd => bd.CheckInDate.ToString("dd/MM/yyyy"))
                             .FirstOrDefault()
                })
                .ToListAsync();

            return new PagedResponse<ReportDTO>
            {
                CurrentPage = pageNumber,
                ItemsPerPage = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = data.Any() ? data : null
            };
        }

        public async Task<TotalReportDTO> GetTotalReport(int userId, DateTime? startDate, DateTime? endDate)
        {
            var totalFacility = _context.Facilities.Count(f => f.UserId == userId);

            // Query cơ bản cho booking
            var bookingsQuery = _context.Bookings
                .Where(b => b.BookingDetails.Any(bd =>
                    bd.Court.Facility.UserId == userId));

            // Áp dụng filter thời gian nếu có
            if (startDate.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.CreateAt.Date >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.CreateAt.Date <= endDate.Value.Date);
            }

            var totalBooking = await bookingsQuery.CountAsync();

            var totalCourt = await _context.Courts.CountAsync(c => c.Facility.UserId == userId);

            // Lấy tổng doanh thu (TotalPrice từ các booking)
            var totalCost = await bookingsQuery.SumAsync(b => b.TotalPrice);

            return new TotalReportDTO
            {
                TotalFacility = totalFacility,
                TotalBooking = totalBooking,
                TotalCourt = totalCourt,
                TotalCost = totalCost
            };
        }
    }
}
