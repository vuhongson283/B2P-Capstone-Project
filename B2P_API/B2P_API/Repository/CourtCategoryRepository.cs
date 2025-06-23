using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    public class CourtCategoryRepository : ICourtCategoryRepository
    {
        private readonly SportBookingDbContext _context;
        public CourtCategoryRepository(SportBookingDbContext context)
        {
            _context = context;
        }
        public Task<bool> AddCourtCategoryAsync(CourtCategory courtCategory)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteCourtCategoryAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<CourtCategory>?> GetAllCourtCategoriesAsync()
        {
            return await _context.CourtCategories.ToListAsync();
        }

        public Task<CourtCategory?> GetCourtCategoryByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateCourtCategoryAsync(CourtCategory courtCategory)
        {
            throw new NotImplementedException();
        }
    }
}
