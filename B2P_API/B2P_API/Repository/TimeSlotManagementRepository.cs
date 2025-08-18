using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    public class TimeSlotManagementRepository : ITimeSlotManagementRepository
    {
        private readonly SportBookingDbContext _context;

        public TimeSlotManagementRepository(SportBookingDbContext context)
        {
            _context = context;
        }
        public async Task<TimeSlot> CreateAsync(TimeSlot timeSlot)
        {
            _context.TimeSlots.Add(timeSlot);
            await _context.SaveChangesAsync();
            return timeSlot;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var data = await GetByIdAsync(id);
            if (data == null)
            {
                return false;
            }
            _context.TimeSlots.Remove(data);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TimeSlot>> GetByFacilityIdAsync(int facilityId)
        {
            return await _context.TimeSlots
                .Where(x => x.FacilityId == facilityId)
                .Include(x => x.Status)
                .ToListAsync();
        }

        public async Task<TimeSlot?> GetByIdAsync(int id)
        {
            return await _context.TimeSlots
                .Include(x => x.Facility)
                .Include(x => x.BookingDetails)
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.TimeSlotId == id);
        }

        public async Task<TimeSlot?> UpdateAsync(TimeSlot timeSlot)
        {
            try
            {
                _context.TimeSlots.Update(timeSlot);
                await _context.SaveChangesAsync();
                return timeSlot;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> HasAnyActiveOrFutureBookingsAsync(int timeSlotId)
        {
            var today = DateTime.Today;

            var hasBookings = await _context.BookingDetails
                .AnyAsync(bd => bd.TimeSlotId == timeSlotId &&
                               (
                                   bd.StatusId == 1 || // Active booking (điều chỉnh StatusId theo system của bạn)
                                   (bd.CheckInDate >= today && bd.StatusId != 3) // Future booking not cancelled
                               ));

            return hasBookings;
        }



    }
}
