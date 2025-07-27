using B2P_API.DTOs.FacilityDTO;
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

        public async Task<FacilityDetailsDto?> GetFacilityDetails(int facilityId)
        {
            var facility = await _context.Facilities
                .Where(f => f.FacilityId == facilityId)
                .Include(f => f.Images)
                .Include(f => f.TimeSlots)
                .FirstOrDefaultAsync();

            if (facility == null) return null;

            // Lấy open - close time từ TimeSlots
            var openTime = facility.TimeSlots.Any() ? facility.TimeSlots.Min(t => t.StartTime) : null;
            var closeTime = facility.TimeSlots.Any() ? facility.TimeSlots.Max(t => t.EndTime) : null;

            // Lấy danh sách các category mà facility này có qua các court
            var categories = await _context.Courts
                .Where(c => c.FacilityId == facilityId)
                .Select(c => c.CategoryId)
                .Distinct()
                .Join(_context.CourtCategories,
                      cid => cid,
                      cat => cat.CategoryId,
                      (cid, cat) => new CategoryDto
                      {
                          CategoryId = cat.CategoryId,
                          CategoryName = cat.CategoryName
                      })
                .ToListAsync();

            return new FacilityDetailsDto
            {
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                Location = facility.Location,
                Contact = facility.Contact,
                OpenTime = openTime,
                CloseTime = closeTime,
                Images = facility.Images.Select(img => new FImageDto
                {
                    ImageId = img.ImageId,
                    ImageUrl = img.ImageUrl,
                    Caption = img.Caption,
                    Order = img.Order
                }).ToList(),
                Categories = categories
            };
        }

    }
}
