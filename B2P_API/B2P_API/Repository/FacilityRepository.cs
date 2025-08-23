using B2P_API.DTOs.FacilityDTO;
using B2P_API.DTOs.RatingDTO;
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

            var openTime = facility.TimeSlots.Any() ? facility.TimeSlots.Min(t => t.StartTime) : null;
            var closeTime = facility.TimeSlots.Any() ? facility.TimeSlots.Max(t => t.EndTime) : null;
            var categories = await _context.Courts
    .Where(c => c.FacilityId == facilityId)
    .GroupBy(c => c.CategoryId)
    .Select(g => new CategoryDto
    {
        CategoryId = g.Key.Value,
        CategoryName = _context.CourtCategories
            .Where(cat => cat.CategoryId == g.Key)
            .Select(cat => cat.CategoryName)
            .FirstOrDefault(),
        PricePerHour = g.Select(c => c.PricePerHour).FirstOrDefault()
    })
    .ToListAsync();



            // Lấy danh sách rating
            var ratings = await (from r in _context.Ratings
                                 join b in _context.Bookings on r.BookingId equals b.BookingId
                                 join bd in _context.BookingDetails on b.BookingId equals bd.BookingId
                                 join c in _context.Courts on bd.CourtId equals c.CourtId
                                 where c.FacilityId == facilityId
                                 select new RatingDto
                                 {
                                     RatingId = r.RatingId,
                                     Stars = r.Stars ?? 0,
                                     Comment = r.Comment,
                                     BookingId = r.BookingId ?? 0
                                 }).ToListAsync();

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
                Categories = categories,
                Ratings = ratings
            };
        }


    }
}
