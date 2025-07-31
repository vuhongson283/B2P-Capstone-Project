using B2P_API.DTOs;
using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface IBlogRepository
    {
        IQueryable<Blog> GetAllWithRelated();
        Task<Blog?> GetByIdAsync(int id);
        Task SaveAsync();
        Task<Blog> AddAsync(Blog blog);
        Task<Blog?> UpdateAsync(int id, Blog blogUpdate);
        Task DeleteAsync(Blog blog);
        Task<List<Blog>> GetAllAsync(BlogQueryParameters queryParams);
        Task<int> CountAsync(string? search);
        Task<List<Blog>> GetByUserIdAsync(int userId, BlogQueryParameters queryParams);
        Task<int> CountByUserIdAsync(int userId, string? search);
    }
}
