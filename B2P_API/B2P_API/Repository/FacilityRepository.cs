using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    public class FacilityRepository : IFacilityRepository
    {
        private readonly SportBookingDbContext _context;
        public FacilityRepository(SportBookingDbContext context)
        {
            _context = context;
        }
        public async Task<List<Facility>?> SearchFacilities()
        {
            return await _context.Facilities.Include(f => f.Status).Include(f => f.Images).
                Include(f => f.Courts).Include(x=>x.TimeSlots)
                .ToListAsync();
        }
    }
}
