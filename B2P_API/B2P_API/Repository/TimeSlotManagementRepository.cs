using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;
using Twilio.Rest.Messaging.V1.Service;

namespace B2P_API.Repository
{
    public class TimeSlotManagementRepository : ITimeSlotManagementRepository
    {
        private readonly SportBookingDbContext _context;

        public TimeSlotManagementRepository (SportBookingDbContext context)
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
            if (data != null)
            {
                return  false;
            }
            _context.TimeSlots.Remove(data);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<List<TimeSlot>> GetByFacilityIdAsync(int facilityId)
        {
            throw new NotImplementedException();
        }

        public async Task<TimeSlot> GetByIdAsync(int id)
        {
            return await _context.TimeSlots
                .Include(x => x.Facility)
                .Include(x => x.BookingDetails)
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.TimeSlotId == id);
        }

        public Task<TimeSlot> UpdateAsync(TimeSlot timeSlot)
        {
            throw new NotImplementedException();
        }

        

    }
}
