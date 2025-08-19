using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repositories
{
    public class CommissionPaymentHistoryRepository : ICommissionPaymentHistoryRepository
    {
        private readonly SportBookingDbContext _context;

        public CommissionPaymentHistoryRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CommissionPaymentHistory>> GetAllAsync()
        {
            return await _context.CommissionPaymentHistories.ToListAsync();
        }

        public async Task<IEnumerable<CommissionPaymentHistory>> GetByUserIdAsync(int userId)
        {
            return await _context.CommissionPaymentHistories
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<CommissionPaymentHistory?> GetByIdAsync(int id)
        {
            return await _context.CommissionPaymentHistories.FindAsync(id);
        }

        public async Task AddAsync(CommissionPaymentHistory entity)
        {
            await _context.CommissionPaymentHistories.AddAsync(entity);
        }

        public async Task UpdateAsync(CommissionPaymentHistory entity)
        {
            _context.CommissionPaymentHistories.Update(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
