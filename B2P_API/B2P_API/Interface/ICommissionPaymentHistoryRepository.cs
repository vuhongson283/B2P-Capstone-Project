using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface ICommissionPaymentHistoryRepository
    {
        Task<IEnumerable<CommissionPaymentHistory>> GetAllAsync();
        Task<IEnumerable<CommissionPaymentHistory>> GetByUserIdAsync(int userId);
        Task<CommissionPaymentHistory?> GetByIdAsync(int id);
        Task AddAsync(CommissionPaymentHistory entity);
        Task UpdateAsync(CommissionPaymentHistory entity);
        Task SaveChangesAsync();
    }
}
