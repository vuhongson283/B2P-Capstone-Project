using B2P_API.DTOs;
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

  

    public IQueryable<Blog> GetAllWithRelated()
    {
        return _context.Blogs
            .Include(b => b.Comments);
    }


    public async Task<Blog?> GetByIdAsync(int id)
    {
        return await _context.Blogs
            .Include(b => b.Comments)
                .ThenInclude(c => c.User) // Lấy thêm thông tin người comment
                                          //.Include(b => b.Images)    // nếu cần
                                          //.Include(b => b.User)      // nếu muốn lấy thông tin người viết blog
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

    

    public async Task<List<Blog>> GetAllAsync(BlogQueryParameters queryParams)
    {
        var query = _context.Blogs
            .Include(b => b.Comments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            query = query.Where(b => b.Title.Contains(queryParams.Search) || b.Content.Contains(queryParams.Search));
        }

        // Sắp xếp
        query = queryParams.SortBy?.ToLower() switch
        {
            "commenttime" => queryParams.SortDirection.ToLower() == "asc"
                ? query.OrderBy(b => b.Comments.Max(c => c.PostAt))
                : query.OrderByDescending(b => b.Comments.Max(c => c.PostAt)),

            _ => queryParams.SortDirection.ToLower() == "asc"
                ? query.OrderBy(b => b.PostAt)
                : query.OrderByDescending(b => b.PostAt)
        };

        // Phân trang
        return await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(string? search)
    {
        var query = _context.Blogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));
        }

        return await query.CountAsync();
    }

    public async Task<List<Blog>> GetByUserIdAsync(int userId, BlogQueryParameters queryParams)
    {
        var query = _context.Blogs
            .Include(b => b.Comments)
            .Where(b => b.UserId == userId);

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            query = query.Where(b =>
                b.Title.Contains(queryParams.Search) ||
                b.Content.Contains(queryParams.Search));
        }

        query = queryParams.SortBy?.ToLower() switch
        {
            "commenttime" => queryParams.SortDirection.ToLower() == "asc"
                ? query.OrderBy(b => b.Comments.Max(c => c.PostAt))
                : query.OrderByDescending(b => b.Comments.Max(c => c.PostAt)),

            _ => queryParams.SortDirection.ToLower() == "asc"
                ? query.OrderBy(b => b.PostAt)
                : query.OrderByDescending(b => b.PostAt)
        };

        return await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId, string? search)
    {
        var query = _context.Blogs.Where(b => b.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));
        }

        return await query.CountAsync();
    }




}
