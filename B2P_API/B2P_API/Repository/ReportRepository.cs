using B2P_API.DTOs.BookingDTOs;
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

        public async Task<bool> HasAnyBookings(int userId, int? facilityId)
        {
            return await _context.BookingDetails
                .AnyAsync(bd => bd.Court.Facility.UserId == userId &&
                                (!facilityId.HasValue || bd.Court.FacilityId == facilityId.Value));
        }

        public async Task<AdminReportDTO> GetAdminReport(int? year, int? month)
        {
            // Xác định tháng cần lấy (mặc định là tháng gần nhất)
            DateTime reportDate;
            if (year.HasValue && month.HasValue)
            {
                reportDate = new DateTime(year.Value, month.Value, 1);
            }
            else
            {
                // Lấy tháng gần nhất có dữ liệu
                var latestBooking = await _context.Bookings
                    .OrderByDescending(b => b.CreateAt)
                    .FirstOrDefaultAsync();

                reportDate = latestBooking?.CreateAt.Date ?? DateTime.Now;
                reportDate = new DateTime(reportDate.Year, reportDate.Month, 1);
            }

            DateTime startDate = reportDate;
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            // 1. Thống kê tổng quan tháng
            var monthlyStats = await _context.Bookings
                .Where(b => b.CreateAt >= startDate && b.CreateAt <= endDate)
                .GroupBy(b => 1) // Group all records
                .Select(g => new MonthlyStatsDTO
                {
                    TotalBooking = g.Count(),
                    TotalRevenue = g.Sum(b => b.TotalPrice),
                    AverageRevenuePerBooking = g.Average(b => b.TotalPrice),
                    CompletedBookings = g.Count(b => b.StatusId == 3), // StatusId 3 = Completed
                    CancelledBookings = g.Count(b => b.StatusId == 4), // StatusId 4 = Cancelled
                    TotalFacilities = _context.Facilities.Count(),
                    TotalCourts = _context.Courts.Count(),
                    ActiveUsers = _context.Users.Count(u => u.StatusId == 1)
                })
                .FirstOrDefaultAsync() ?? new MonthlyStatsDTO();

            // 2. Top facility trong tháng
            var topFacilities = await _context.Facilities
                .Select(f => new FacilityStatDTO
                {
                    FacilityId = f.FacilityId,
                    FacilityName = f.FacilityName,
                    TotalBooking = f.Courts
                        .SelectMany(c => c.BookingDetails)
                        .Count(bd => bd.Booking.CreateAt >= startDate && bd.Booking.CreateAt <= endDate),
                    TotalRevenue = f.Courts
                        .SelectMany(c => c.BookingDetails)
                        .Where(bd => bd.Booking.CreateAt >= startDate && bd.Booking.CreateAt <= endDate)
                        .Sum(bd => bd.Booking.TotalPrice)
                })
                .Where(f => f.TotalBooking > 0)
                .OrderByDescending(f => f.TotalBooking)
                .Take(5)
                .ToListAsync();

            // 3. Thống kê loại sân phổ biến
            var popularCourtCategories = await _context.Courts
                .Select(c => new CourtCategoryStatDTO
                {
                    CategoryName = c.Category.CategoryName,
                    TotalBooking = c.BookingDetails
                        .Count(bd => bd.Booking.CreateAt >= startDate && bd.Booking.CreateAt <= endDate)
                })
                .Where(c => c.TotalBooking > 0)
                .OrderByDescending(c => c.TotalBooking)
                .Take(3)
                .ToListAsync();

            // 4. Tổng hợp dữ liệu
            var report = new AdminReportDTO
            {
                Year = reportDate.Year,
                Month = reportDate.Month,
                StartDate = startDate,
                EndDate = endDate,
                MonthlyStats = monthlyStats,
                TopFacilities = topFacilities,
                PopularCourtCategories = popularCourtCategories
            };

            return report;
        }
    }
}
