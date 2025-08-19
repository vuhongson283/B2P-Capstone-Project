using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface IMerchantPaymentRepository
    {
        Task<IEnumerable<MerchantPayment>> GetAllAsync();
        Task<MerchantPayment?> GetByIdAsync(int id);
        Task<MerchantPayment> AddAsync(MerchantPayment entity);
        Task UpdateAsync(MerchantPayment entity);
        Task DeleteAsync(MerchantPayment entity);
        Task<IEnumerable<MerchantPayment>> GetByUserIdAsync(int userId);

    }
}
