using B2P_API.Models;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace B2P_API.Interface
{
    public interface IBankAccountRepository
    {
        Task<bool> AddBankAccountAsync(BankAccount bankAccount);
        Task<bool> UpdateBankAccountAsync(BankAccount bankAccount);
        Task<BankAccount?> GetBankAccountsByUserIdAsync(int userId);
        Task<bool> AddBankTypeAsync(BankType bankType);
        Task<bool> UpdateBankTypeAsync(BankType bankType);
        Task<List<BankType>?> GetAllBankTypeAysnc();
        Task<BankType?> GetBankTypeByIdAsync(int? id);
        Task<bool> DeleteBankTypeAsync(int bankTypeId);
    }
}
