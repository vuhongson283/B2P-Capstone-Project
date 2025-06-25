using B2P_API.Models;
using System;
using Microsoft.EntityFrameworkCore;
using B2P_API.DTOs;


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


        public async Task<List<Comment>> GetByUserIdAsync(int userId, CommentQueryParameters queryParams)
        {
            var query = _context.Comments
                .Include(c => c.Blog)
                .Include(c => c.ParentComment)
                .Where(c => c.UserId == userId);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                query = query.Where(c =>
                    c.Content!.Contains(queryParams.Search) ||
                    c.Blog!.Title!.Contains(queryParams.Search));
            }

            // Sắp xếp
            query = queryParams.SortBy.ToLower() switch
            {
                "updatedat" => queryParams.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(c => c.UpdatedAt)
                    : query.OrderByDescending(c => c.UpdatedAt),

                _ => queryParams.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(c => c.PostAt)
                    : query.OrderByDescending(c => c.PostAt)
            };

            return await query
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();
        }

        public async Task<int> CountByUserIdAsync(int userId, string? keyword)
        {
            var query = _context.Comments.Where(c => c.UserId == userId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(c => c.Content!.Contains(keyword));
            }

            return await query.CountAsync();
        }


        public async Task<List<Comment>> GetAllAsync(CommentQueryParameters queryParams)
        {
            var query = _context.Comments
                .Include(c => c.Blog)
                .Include(c => c.User)
                .Include(c => c.ParentComment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var keyword = queryParams.Search.ToLower();
                query = query.Where(c =>
                    c.Content!.ToLower().Contains(keyword) ||
                    c.Blog!.Title!.ToLower().Contains(keyword) ||
                    c.User!.Username!.ToLower().Contains(keyword));
            }

            query = queryParams.SortBy.ToLower() switch
            {
                "updatedat" => queryParams.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(c => c.UpdatedAt)
                    : query.OrderByDescending(c => c.UpdatedAt),

                _ => queryParams.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(c => c.PostAt)
                    : query.OrderByDescending(c => c.PostAt)
            };

            return await query
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();
        }

        public async Task<int> CountAllAsync(string? search)
        {
            var query = _context.Comments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.ToLower();
                query = query
                    .Include(c => c.Blog)
                    .Include(c => c.User)
                    .Where(c =>
                        c.Content!.ToLower().Contains(keyword) ||
                        c.Blog!.Title!.ToLower().Contains(keyword) ||
                        c.User!.Username!.ToLower().Contains(keyword));
            }

            return await query.CountAsync();
        }


    }

}
