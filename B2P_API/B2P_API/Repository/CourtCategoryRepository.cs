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
        public async Task<bool> AddCourtCategoryAsync(CourtCategory courtCategory)
        {

                _context.CourtCategories.Add(courtCategory);
                await _context.SaveChangesAsync();
                return true;

        }


        public async Task<bool> DeleteCourtCategoryAsync(int? id)
        {
                var courtCategory = await _context.CourtCategories.FindAsync(id);
                if (courtCategory == null)
                {
                    return false;
                }
                _context.CourtCategories.Remove(courtCategory);
                await _context.SaveChangesAsync();
                return true;
        }

        public async Task<List<CourtCategory>?> GetAllCourtCategoriesAsync()
        {
            return await _context.CourtCategories.ToListAsync();
        }

        public async Task<CourtCategory?> GetCourtCategoryByIdAsync(int? id)
        {
            return await _context.CourtCategories.FirstOrDefaultAsync(x=>x.CategoryId == id);
        }

        public async Task<bool> UpdateCourtCategoryAsync(CourtCategory courtCategory)
        {

                _context.CourtCategories.Update(courtCategory);
                await _context.SaveChangesAsync();
                return true;
        }
    }
}
