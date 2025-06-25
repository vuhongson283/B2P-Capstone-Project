using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    public class FacilityRepository : IFacilityRepositoryForUser
    {
        private readonly SportBookingDbContext _context;
        public FacilityRepository(SportBookingDbContext context)
        {
            _context = context;
        }
        public async Task<List<Facility>?> GetAllFacilitiesByPlayer()
        {
            return await _context.Facilities.Include(f => f.Status).Include(f => f.Images).
                Include(f => f.Courts).ThenInclude(f => f.BookingDetails).ThenInclude(x=>x.Booking).ThenInclude(f=>f.Ratings).
                Include(x=>x.TimeSlots)
                .ToListAsync();
        }
    }
}
