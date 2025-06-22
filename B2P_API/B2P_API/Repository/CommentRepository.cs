using B2P_API.Models;
using System;
using Microsoft.EntityFrameworkCore;


namespace B2P_API.Repository
{
    public class CommentRepository
    {
        private readonly SportBookingDbContext _context;

        public CommentRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Comment> AddAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<bool> BlogExists(int blogId)
        {
            return await _context.Blogs.AnyAsync(b => b.BlogId == blogId);
        }

        public async Task<bool> UserExists(int userId)
        {
            return await _context.Users.AnyAsync(u => u.UserId == userId);
        }

        public async Task<Comment?> GetByIdAsync(int commentId)
        {
            return await _context.Comments.FirstOrDefaultAsync(c => c.CommentId == commentId);
        }

        public async Task<Comment> UpdateAsync(Comment comment)
        {
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task DeleteAsync(Comment comment)
        {
            // Xóa các comment con (reply) trước
            var replies = await _context.Comments
                .Where(c => c.ParentCommentId == comment.CommentId)
                .ToListAsync();

            if (replies.Any())
            {
                _context.Comments.RemoveRange(replies);
            }

            // Sau đó xóa comment cha
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }


        public async Task<(List<Comment>, int)> GetByUserIdAsync(int userId, int page, int size, string sortBy, string sortDir)
        {
            var query = _context.Comments
                .Include(c => c.Blog)
                .Where(c => c.UserId == userId);

            // Tổng số comment
            var totalItems = await query.CountAsync();

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "updatedat" => sortDir == "desc" ? query.OrderByDescending(c => c.UpdatedAt) : query.OrderBy(c => c.UpdatedAt),
                _ => sortDir == "desc" ? query.OrderByDescending(c => c.PostAt) : query.OrderBy(c => c.PostAt)
            };

            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, totalItems);
        }

        public async Task<(List<Comment>, int)> GetAllAsync(
    int page, int size, string sortBy, string sortDir,
    int? userId, int? blogId, bool? hasParent)
        {
            var query = _context.Comments
                .Include(c => c.Blog)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(c => c.UserId == userId.Value);

            if (blogId.HasValue)
                query = query.Where(c => c.BlogId == blogId.Value);

            if (hasParent.HasValue)
            {
                if (hasParent.Value)
                    query = query.Where(c => c.ParentCommentId != null);
                else
                    query = query.Where(c => c.ParentCommentId == null);
            }

            var totalItems = await query.CountAsync();

            query = sortBy.ToLower() switch
            {
                "updatedat" => sortDir == "asc" ? query.OrderBy(c => c.UpdatedAt) : query.OrderByDescending(c => c.UpdatedAt),
                _ => sortDir == "asc" ? query.OrderBy(c => c.PostAt) : query.OrderByDescending(c => c.PostAt)
            };

            var result = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (result, totalItems);
        }

    }

}
