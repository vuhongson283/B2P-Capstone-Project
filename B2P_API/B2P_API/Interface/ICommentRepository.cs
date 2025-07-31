using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.DTOs;
namespace B2P_API.Interface
{
    public interface ICommentRepository
    {
        Task<Comment> AddAsync(Comment comment);
        Task<Comment?> GetByIdAsync(int id);
        Task<Comment?> UpdateAsync(Comment comment);
        Task DeleteAsync(Comment comment);

        Task<bool> BlogExists(int blogId);
        Task<bool> UserExists(int userId);

        Task<List<Comment>> GetByUserIdAsync(int userId, CommentQueryParameters queryParams);
        Task<int> CountByUserIdAsync(int userId, string? keyword);

        Task<List<Comment>> GetAllAsync(CommentQueryParameters queryParams);
        Task<int> CountAllAsync(string? keyword);
    }
}
