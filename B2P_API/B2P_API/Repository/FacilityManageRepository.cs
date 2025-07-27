using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Reflection.Metadata;

namespace B2P_API.Repository
{
    public class FacilityManageRepository : IFacilityManageRepository
    {
        private readonly SportBookingDbContext _context;

        public FacilityManageRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Facility> CreateFacilityAsync(Facility facility)
        {
            _context.Facilities.Add(facility);
            await _context.SaveChangesAsync();
            return facility;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var facility = await GetByIdAsync(id);
            if (facility == null)
                return false;

            _context.Facilities.Remove(facility);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCascadeAsync(int facilityId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Lấy tất cả court IDs trong facility
                var courtIds = await _context.Courts
                    .Where(c => c.FacilityId == facilityId)
                    .Select(c => c.CourtId)
                    .ToListAsync();

                if (courtIds.Any())
                {
                    // 2. Xóa BookingDetails liên quan đến courts
                    var bookingDetailIds = await _context.BookingDetails
                        .Where(bd => courtIds.Contains(bd.CourtId))
                        .Select(bd => bd.BookingDetailId)
                        .ToListAsync();

                    if (bookingDetailIds.Any())
                    {
                        var bookingDetails = await _context.BookingDetails
                            .Where(bd => bookingDetailIds.Contains(bd.BookingDetailId))
                            .ToListAsync();

                        _context.BookingDetails.RemoveRange(bookingDetails);
                    }

                    // 3. Lấy các booking IDs từ booking details đã xóa
                    var bookingIds = await _context.BookingDetails
                        .Where(bd => courtIds.Contains(bd.CourtId))
                        .Select(bd => bd.BookingId)
                        .Distinct()
                        .ToListAsync();

                    // 4. Xóa Payments liên quan đến bookings
                    if (bookingIds.Any())
                    {
                        var payments = await _context.Payments
                            .Where(p => bookingIds.Contains(p.BookingId.Value))
                            .ToListAsync();

                        if (payments.Any())
                            _context.Payments.RemoveRange(payments);

                        // 5. Xóa Ratings liên quan đến bookings
                        var ratings = await _context.Ratings
                            .Where(r => bookingIds.Contains(r.BookingId.Value))
                            .ToListAsync();

                        if (ratings.Any())
                            _context.Ratings.RemoveRange(ratings);

                        // 6. Xóa Bookings không còn BookingDetails
                        var orphanBookings = await _context.Bookings
                            .Where(b => bookingIds.Contains(b.BookingId))
                            .ToListAsync();

                        if (orphanBookings.Any())
                            _context.Bookings.RemoveRange(orphanBookings);
                    }

                    // 7. Xóa Courts trong facility
                    var courts = await _context.Courts
                        .Where(c => courtIds.Contains(c.CourtId))
                        .ToListAsync();

                    _context.Courts.RemoveRange(courts);
                }

                // 8. Xóa TimeSlots liên quan đến facility
                var timeSlots = await _context.TimeSlots
                    .Where(ts => ts.FacilityId == facilityId)
                    .ToListAsync();

                if (timeSlots.Any())
                    _context.TimeSlots.RemoveRange(timeSlots);

                // 9. Xóa Images liên quan đến facility
                var images = await _context.Images
                    .Where(img => img.FacilityId == facilityId) // Điều chỉnh theo cách bạn lưu entity type
                    .ToListAsync();

                if (images.Any())
                    _context.Images.RemoveRange(images);

                // 10. Cuối cùng xóa Facility
                var facility = await _context.Facilities.FindAsync(facilityId);
                if (facility != null)
                    _context.Facilities.Remove(facility);

                // Save changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log error
                Console.WriteLine($"Error in DeleteFacilityCascade: {ex.Message}");
                return false;
            }
        }


        public async Task<List<Facility>> GetAllAsync()
        {
            return await _context.Facilities
                .Include(f => f.Status)
                .Include(f => f.User)
                .Include(f => f.Images)
                .ToListAsync();
        }

        public async Task<Facility?> GetByIdAsync(int id)
        {
            return await _context.Facilities
                .Include(f => f.Status)
                .Include(f => f.User)
                .Include(f => f.Images)
                .Include(f => f.Courts)
                .Include(f => f.TimeSlots)
                .FirstOrDefaultAsync(f => f.FacilityId == id);
        }

        public async Task<List<Facility>> GetByUserIdAsync(int userId)
        {
            return await _context.Facilities
                .Where(f => f.UserId == userId)
                .Include(f => f.Status)
                .Include(f => f.Images)
                .Include(f => f.Courts)
                .ToListAsync();
        }

        public async Task<bool> HasActiveBookingsAsync(int facilityId)
        {
            try
            {
                // Lấy tất cả court trong facility
                var courts = await _context.Courts
                    .Where(c => c.FacilityId == facilityId)
                    .Select(c => c.CourtId)
                    .ToListAsync();

                if (!courts.Any())
                    return false;

                // Kiểm tra có booking detail nào với court trong facility và có status active không
                var activeStatusIds = new List<int> { 1, 8, 3 }; // Điều chỉnh theo status active trong hệ thống

                var hasActiveBookings = await _context.BookingDetails
                    .Include(bd => bd.Booking)
                    .AnyAsync(bd => courts.Contains(bd.CourtId) &&
                                   activeStatusIds.Contains(bd.StatusId) &&
                                   bd.CheckInDate >= DateTime.Now.Date); // Chỉ kiểm tra booking từ hôm nay trở đi

                return hasActiveBookings;
            }
            catch (Exception)
            {
                return true; // Nếu có lỗi, an toàn hơn là không cho phép xóa
            }
        }

        public async Task<Facility?> UpdateAsync(Facility facility)
        {
            try
            {
                _context.Facilities.Update(facility);
                await _context.SaveChangesAsync();
                return facility;
            }
            catch
            {
                return null;
            }
        }

        
    }
}
