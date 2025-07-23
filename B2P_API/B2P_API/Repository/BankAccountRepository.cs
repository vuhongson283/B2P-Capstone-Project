using B2P_API.Interface;
using B2P_API.Models;
using Google.Apis.Drive.v3.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;

namespace B2P_API.Repository
{
    public class BankAccountRepository : IBankAccountRepository
    {   
        private readonly SportBookingDbContext _context;
        public BankAccountRepository(SportBookingDbContext context)
        {
            _context = context;
        }
        public async Task<bool> AddBankAccountAsync(BankAccount bankAccount)
        {

                _context.BankAccounts.Add(bankAccount);
                await _context.SaveChangesAsync();
                return true;

        }

        public async Task<bool> AddBankTypeAsync(BankType bankType)
        {

                _context.BankTypes.Add(bankType);
                await _context.SaveChangesAsync();
                return true;
        }

        public async Task<bool> DeleteBankTypeAsync(int bankTypeId)
        {
                var bankType = await _context.BankTypes.FindAsync(bankTypeId);
                if (bankType == null)
                {
                    return false;
                }
                _context.BankTypes.Remove(bankType);
                await _context.SaveChangesAsync();
                return true;
        }

        public async Task<List<BankType>?> GetAllBankTypeAysnc()
        {
            return await _context.BankTypes.ToListAsync();
        }

        public async Task<BankAccount?> GetBankAccountsByUserIdAsync(int userId)
        {
            return await _context.BankAccounts.FirstOrDefaultAsync(x=> x.UserId == userId);
        }

        public async Task<BankType?> GetBankTypeByIdAsync(int? id)
        {
            return await _context.BankTypes.FirstOrDefaultAsync(x => x.BankTypeId == id);
        }

        public async Task<bool> UpdateBankAccountAsync(BankAccount bankAccount)
        {

                _context.BankAccounts.Update(bankAccount);
                await _context.SaveChangesAsync();
                return true;  
        }

        public async Task<bool> UpdateBankTypeAsync(BankType bankType)
        {

                _context.BankTypes.Update(bankType);
                await _context.SaveChangesAsync();
                return true;
        }
    }
}
