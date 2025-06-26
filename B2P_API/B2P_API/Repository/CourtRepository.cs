using B2P_API.DTOs;
using B2P_API.Models;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace B2P_API.Repository
{
    public class CourtRepository
    {
        private readonly SportBookingDbContext _context;

        public CourtRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<CourtDTO>> GetAllCourts(int pageNumber, int pageSize, 
             int facilityId, string? search, int? status, int? categoryId)
        {
            var query = _context.Courts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.CourtName.Contains(search));
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.StatusId == status.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            query = query.Where(c => c.FacilityId == facilityId);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CourtDTO
                {
                    CourtId = c.CourtId,
                    CourtName = c.CourtName,
                    CategoryName = c.Category != null ? c.Category.CategoryName : null,
                    StatusName = c.Status != null ? c.Status.StatusName : null,
                })
                .ToListAsync();

            return new PagedResponse<CourtDTO>
            {
                CurrentPage = pageNumber,
                ItemsPerPage = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = data.Any() ? data : null
            };
        }

        public async Task<CourtDetailDTO> GetCourtDetail(int courtId)
        {
            return await _context.Courts
            .Include(c => c.Category)
            .Include(c => c.Facility)
            .Include(c => c.Status)
            .Where(c => c.CourtId == courtId)
            .Select(c => new CourtDetailDTO
            {
                CourtId = c.CourtId,
                CourtName = c.CourtName,
                PricePerHour = c.PricePerHour,
                StatusId = c.StatusId,
                StatusName = c.Status != null ? c.Status.StatusName : null,
                StatusDescription = c.Status != null ? c.Status.StatusDescription : null,
                CategoryId = c.CategoryId,
                CategoryName = c.Category != null ? c.Category.CategoryName : null,
                FacilityId = c.FacilityId,
                FacilityName = c.Facility != null ? c.Facility.FacilityName : null,
                Location = c.Facility != null ? c.Facility.Location : null,
                Contact = c.Facility != null? c.Facility.Contact : null
            })
            .FirstOrDefaultAsync();
        }

        public async Task<Court> CreateCourt(CreateCourt court)
        {
            var newCourt = new Court()
            {
                FacilityId = court.FacilityId,
                CourtName = court.CourtName,
                StatusId = 1,
                CategoryId = court.CategoryId,
                PricePerHour = court.PricePerHour
            };
            _context.Courts.Add(newCourt);
            await _context.SaveChangesAsync();
            return newCourt;
        }

        public async Task<bool> UpdateCourt(UpdateCourtRequest court)
        {
            var existCourt = _context.Courts.FirstOrDefault(c => c.CourtId == court.CourtId);
            if(existCourt == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(court.CourtName))
                existCourt.CourtName = court.CourtName;

            if (court.CategoryId.HasValue)
                existCourt.CategoryId = court.CategoryId;

            if (court.PricePerHour.HasValue)
                existCourt.PricePerHour = court.PricePerHour;

            _context.Courts.Update(existCourt);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCourt(int courtId)
        {
            var existCourt = _context.Courts.FirstOrDefault(c => c.CourtId == courtId);
            if( existCourt == null)
            {
                return false;
            }
            _context.Courts.Remove(existCourt);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LockCourt(int courtId, int statusId)
        {
            var existCourt = _context.Courts.FirstOrDefault(c => c.CourtId == courtId);
            if (existCourt == null)
            {
                return false;
            }

            existCourt.StatusId = statusId;

            _context.Courts.Update(existCourt);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
