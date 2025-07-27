using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Mozilla;
using System.Data;
using System.Globalization;

namespace B2P_API.Repository
{
    public class CourtRepository : ICourtRepository
    {
        private readonly SportBookingDbContext _context;

        public CourtRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<CourtDTO>> GetAllCourts(CourtRequestDTO req)
        {
            var query = _context.Courts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                query = query.Where(c => c.CourtName.Contains(req.Search));
            }

            if (req.Status.HasValue)
            {
                query = query.Where(c => c.StatusId == req.Status.Value);
            }

            if (req.CategoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == req.CategoryId.Value);
            }

            query = query.Where(c => c.FacilityId == req.FacilityId);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)req.PageSize);

            var data = await query
                .Skip((req.PageNumber - 1) * req.PageSize)
                .Take(req.PageSize)
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
                CurrentPage = req.PageNumber,
                ItemsPerPage = req.PageSize,
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

            if (court.StatusId.HasValue)
                existCourt.StatusId = court.StatusId;

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

        public bool CheckCourtOwner(int userId, int courtId)
        {
            var court = _context.Courts
                .Include(c => c.Facility)
                .Where(c => c.Facility.UserId == userId)
                .FirstOrDefault(c => c.CourtId == courtId);
            if (court == null)
            {
                return false;
            }
            return true;
        }

    }
}
