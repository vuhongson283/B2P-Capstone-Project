using B2P_API.DTOs.BookingDTOs;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace B2P_API.Repository
{
    public class BookingRepository
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



    }
    // Class trung gian để xử lý xếp sân
    public class CourtAvailability
    {
        public int CourtId { get; set; }
        public string CourtName { get; set; }
        public HashSet<int> UnavailableSlotIds { get; set; } = new();
    }
}
