using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;


namespace B2P_API.Repository
{
    public class RatingRepository : IRatingRepository
    {
        private readonly SportBookingDbContext _context;

        public RatingRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Rating>> GetAllAsync() =>
            await _context.Ratings.ToListAsync();

        public async Task<Rating?> GetByIdAsync(int id) =>
            await _context.Ratings.FindAsync(id);

        public async Task AddAsync(Rating rating)
        {
            await _context.Ratings.AddAsync(rating);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Rating rating)
        {
            _context.Ratings.Update(rating);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Rating rating)
        {
            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Ratings.AnyAsync(r => r.RatingId == id);
    }

}
