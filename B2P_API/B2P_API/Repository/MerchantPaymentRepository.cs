using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repositories
{
    public class MerchantPaymentRepository : IMerchantPaymentRepository
    {
        private readonly SportBookingDbContext _context;

        public MerchantPaymentRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MerchantPayment>> GetAllAsync()
        {
            return await _context.MerchantPayments.ToListAsync();
        }

        public async Task<MerchantPayment?> GetByIdAsync(int id)
        {
            return await _context.MerchantPayments.FindAsync(id);
        }

        public async Task<MerchantPayment> AddAsync(MerchantPayment entity)
        {
            _context.MerchantPayments.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(MerchantPayment entity)
        {
            _context.MerchantPayments.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(MerchantPayment entity)
        {
            _context.MerchantPayments.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<MerchantPayment>> GetByUserIdAsync(int userId)
        {
            return await _context.MerchantPayments
                .Include(mp => mp.PaymentMethod)
                .Include(mp => mp.Status)
                .Where(mp => mp.UserId == userId)
                .ToListAsync();
        }

    }
}
