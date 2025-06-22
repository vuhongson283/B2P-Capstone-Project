using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repositories;

public class BlogRepository
{
    private readonly SportBookingDbContext _context;

    public BlogRepository(SportBookingDbContext context)
    {
        _context = context;
    }

    public async Task<List<Blog>> GetAllAsync()
    {
        return await _context.Blogs.Include(b => b.User).ToListAsync();
    }

    public IQueryable<Blog> GetAllWithRelated()
    {
        return _context.Blogs
            .Include(b => b.Comments);
    }


    public async Task<Blog?> GetByIdAsync(int id)
    {
        return await _context.Blogs
            .Include(b => b.Comments)
          //  .Include(b => b.Images)    // nếu cần
          //  .Include(b => b.User)      // nếu cần
            .FirstOrDefaultAsync(b => b.BlogId == id);
    }


    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }


    public async Task<Blog> AddAsync(Blog blog)
    {
        blog.PostAt = DateTime.Now;
        _context.Blogs.Add(blog);
        await _context.SaveChangesAsync();
        return blog;
    }

    public async Task<Blog?> UpdateAsync(int id, Blog blogUpdate)
    {
        var blog = await _context.Blogs.FindAsync(id);
        if (blog == null) return null;

        blog.Title = blogUpdate.Title;
        blog.Content = blogUpdate.Content;
        blog.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return blog;
    }

    public async Task DeleteAsync(Blog blog)
    {
        _context.Blogs.Remove(blog);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<Blog>, int)> GetByUserIdAsync(int userId, int page, int size, string sortBy, string sortDir)
    {
        var query = _context.Blogs
            .Include(b => b.User)
            .Include(b => b.Comments)
            .Where(b => b.UserId == userId);

        var totalItems = await query.CountAsync();

        query = sortBy.ToLower() switch
        {
            "updatedat" => sortDir == "asc" ? query.OrderBy(b => b.UpdatedAt) : query.OrderByDescending(b => b.UpdatedAt),
            _ => sortDir == "asc" ? query.OrderBy(b => b.PostAt) : query.OrderByDescending(b => b.PostAt)
        };

        var result = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (result, totalItems);
    }


}
