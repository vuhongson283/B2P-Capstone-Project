using B2P_API.Models;
using System;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    // Repositories/PaymentRepository.cs
    public class PaymentRepository
    {
        private readonly SportBookingDbContext _context;

        public PaymentRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> AddAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment?> GetByIdAsync(int paymentId)
        {
            return await _context.Payments
                .Include(p => p.Booking)
                .Include(p => p.Status)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }
    }

}
