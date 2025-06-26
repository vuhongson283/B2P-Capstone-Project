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
    }

    // Class trung gian để xử lý xếp sân
    public class CourtAvailability
    {
        public int CourtId { get; set; }
        public string CourtName { get; set; }
        public HashSet<int> UnavailableSlotIds { get; set; } = new();
    }
}
