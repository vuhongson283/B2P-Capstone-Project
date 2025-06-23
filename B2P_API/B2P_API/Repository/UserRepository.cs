using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly SportBookingDbContext _context;
        public UserRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<User?> CheckPhoneExistedByUserId(int userId, string phone)
        {
            return await _context.Users
                .Where(u => u.UserId != userId && u.Phone == phone)
                .FirstOrDefaultAsync();
        }

        public async Task<Image?> GetImageByUserId(int userId)
        {
            return await _context.Images
                .Where(i => i.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x=>x.Email.Equals(email));
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.BankAccount).ThenInclude(u => u.BankType)
                .Include(u => u.Status).Include(u=>u.Images)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> UpdateAvatar(Image image)
        {
            _context.Images.Update(image);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
