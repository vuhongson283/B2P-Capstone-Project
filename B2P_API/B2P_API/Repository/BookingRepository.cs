using B2P_API.DTOs.BookingDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace B2P_API.Repository
{
    public class BookingRepository : IBookingRepository
    {
        private readonly SportBookingDbContext _context;

        public BookingRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách sân (court) thuộc facility & category,
        /// kèm theo thông tin slot nào đã được đặt vào ngày cụ thể.
        /// </summary>
        public async Task<List<CourtAvailability>> GetCourtAvailabilityAsync(
            int facilityId,
            int categoryId,
            DateTime checkInDate,
            List<int> timeSlotIds)
        {
            // 1. Lấy danh sách sân thuộc facility & category
            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId && c.CategoryId == categoryId && c.StatusId == 1)
                .OrderBy(c => c.CourtId)
                .ToListAsync();

            var courtIds = courts.Select(c => c.CourtId).ToList();

            // 2. Lấy các slot đã được đặt trong các sân đó
            var bookedDetails = await _context.BookingDetails
                .Where(bd =>
                    courtIds.Contains(bd.CourtId) &&
                    timeSlotIds.Contains(bd.TimeSlotId) &&
                    bd.CheckInDate.Date == checkInDate.Date &&
                    bd.StatusId != 3 // Trạng thái khác "Đã hủy"
                )
                .ToListAsync();

            // 3. Trả về danh sách CourtAvailability (custom class)
            var result = new List<CourtAvailability>();

            foreach (var court in courts)
            {
                var unavailableSlots = bookedDetails
                    .Where(b => b.CourtId == court.CourtId)
                    .Select(b => b.TimeSlotId)
                    .ToHashSet();

                result.Add(new CourtAvailability
                {
                    CourtId = court.CourtId,
                    CourtName = court.CourtName,
                    UnavailableSlotIds = unavailableSlots
                });
            }

            return result;
        }

        public async Task<int> CountByUserIdAsync(int? userId, int? statusId)
        {
            var query = _context.Bookings.AsQueryable();

            if (userId.HasValue)
                query = query.Where(b => b.UserId == userId.Value);

            if (statusId.HasValue)
                query = query.Where(b => b.StatusId == statusId.Value);

            return await query.CountAsync();
        }

        public async Task<List<Booking>> GetByUserIdAsync(int? userId, BookingQueryParameters query)
        {
            var bookings = _context.Bookings
                .Include(b => b.Status)
                .Include(b => b.Ratings)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Court).ThenInclude(c => c.Category)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.TimeSlot)
                .AsQueryable();
            if (query.FacilityId.HasValue)
            {
                bookings = bookings.Where(b =>
                    b.BookingDetails.Any(d => d.Court.FacilityId == query.FacilityId.Value));
            }
            if (userId.HasValue)
                bookings = bookings.Where(b => b.UserId == userId.Value);

            if (query.StatusId.HasValue)
                bookings = bookings.Where(b => b.StatusId == query.StatusId.Value);

            switch (query.SortBy?.ToLower())
            {
                case "checkindate":
                    bookings = query.SortDirection == "asc"
                        ? bookings.OrderBy(b => b.BookingDetails.Min(d => d.CheckInDate))
                        : bookings.OrderByDescending(b => b.BookingDetails.Min(d => d.CheckInDate));
                    break;
                case "createdate":
                    bookings = query.SortDirection == "asc"
                        ? bookings.OrderBy(b => b.CreateAt)
                        : bookings.OrderByDescending(b => b.CreateAt);
                    break;
            }

            return await bookings
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }


        public Booking? GetById(int id)
        {
            return _context.Bookings
                .Include(b => b.Status)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Court)
                        .ThenInclude(c => c.Category)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.TimeSlot)
                .FirstOrDefault(b => b.BookingId == id);
        }

        public async Task<List<TimeSlotAvailability>> GetAvailableCourtCountPerSlotAsync(
    int facilityId,
    int categoryId,
    DateTime checkInDate)
        {
            // 1. Lấy danh sách sân đang hoạt động thuộc Facility và Category
            var courtIds = await _context.Courts
                .Where(c => c.FacilityId == facilityId && c.CategoryId == categoryId && c.StatusId == 1)
                .Select(c => c.CourtId)
                .ToListAsync();

            if (!courtIds.Any()) return new List<TimeSlotAvailability>();

            // 2. Lấy danh sách TimeSlot thuộc Facility này
            var allTimeSlots = await _context.TimeSlots
                .Where(ts => ts.FacilityId == facilityId)
                .OrderBy(ts => ts.StartTime)
                .Select(ts => new
                {
                    ts.TimeSlotId,
                    ts.StartTime,
                    ts.EndTime
                })
                .ToListAsync();

            // 3. Lấy số sân đã được đặt trong mỗi TimeSlot vào ngày check-in (trừ booking đã hủy)
            var bookedDetails = await _context.BookingDetails
                .Where(bd =>
                    courtIds.Contains(bd.CourtId) &&
                    bd.CheckInDate.Date == checkInDate.Date &&
                    bd.StatusId != 7
                )
                .GroupBy(bd => bd.TimeSlotId)
                .Select(g => new
                {
                    TimeSlotId = g.Key,
                    BookedCourtIds = g.Select(x => x.CourtId).Distinct()
                })
                .ToListAsync();

            // 4. Tính toán số sân còn trống cho từng slot
            var result = new List<TimeSlotAvailability>();

            foreach (var slot in allTimeSlots)
            {
                var booked = bookedDetails.FirstOrDefault(b => b.TimeSlotId == slot.TimeSlotId);
                int bookedCount = booked?.BookedCourtIds.Count() ?? 0;

                result.Add(new TimeSlotAvailability
                {
                    TimeSlotId = slot.TimeSlotId,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    AvailableCourtCount = courtIds.Count - bookedCount
                });
            }

            return result;
        }

        public async Task<Dictionary<int, TimeSlot>> GetTimeSlotsByIdsAsync(IEnumerable<int> timeSlotIds)
        {
            return await _context.TimeSlots
                .Where(ts => timeSlotIds.Contains(ts.TimeSlotId))
                .ToDictionaryAsync(ts => ts.TimeSlotId);
        }

        public async Task<Dictionary<int, Court>> GetCourtsByIdsAsync(IEnumerable<int> courtIds)
        {
            return await _context.Courts
                .Where(c => courtIds.Contains(c.CourtId))
                .ToDictionaryAsync(c => c.CourtId);
        }

        public async Task AddBookingAsync(Booking booking)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
        }

        public async Task AddBookingDetailsAsync(IEnumerable<BookingDetail> details)
        {
            _context.BookingDetails.AddRange(details);
            await _context.SaveChangesAsync();
        }
        public async Task<Dictionary<int, TimeSpan>> GetSlotStartTimesByIdsAsync(IEnumerable<int> slotIds)
        {
            return await _context.TimeSlots
                .Where(ts => slotIds.Contains(ts.TimeSlotId) && ts.StartTime.HasValue)
                .ToDictionaryAsync(
                    ts => ts.TimeSlotId,
                    ts => ts.StartTime.Value.ToTimeSpan()
                );
        }

        public async Task<Dictionary<int, Court>> GetCourtsWithCategoryAsync()
        {
            return await _context.Courts
                .Include(c => c.Category)
                .ToDictionaryAsync(c => c.CourtId);
        }

        public async Task<Dictionary<int, TimeSlot>> GetTimeSlotsAsync()
        {
            return await _context.TimeSlots
                .ToDictionaryAsync(s => s.TimeSlotId);
        }

        public async Task<Booking?> GetBookingWithDetailsByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Status)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Court)
                        .ThenInclude(c => c.Category)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.TimeSlot)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }
        public async Task<Booking?> GetBookingWithDetailsAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }





    }
    // Class trung gian để xử lý xếp sân
    public class CourtAvailability
    {
        public int CourtId { get; set; }
        public string CourtName { get; set; }
        public HashSet<int> UnavailableSlotIds { get; set; } = new();
    }
    public class TimeSlotAvailability
    {
        public int TimeSlotId { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        public int AvailableCourtCount { get; set; }
    }



}
