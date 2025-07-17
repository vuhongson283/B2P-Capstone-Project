using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;
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
