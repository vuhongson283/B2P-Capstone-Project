using B2P_API.DTOs;
using B2P_API.Models;
using B2P_API.Repositories;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Services;

public class BlogService
{
    private readonly BlogRepository _repository;
    private readonly SportBookingDbContext _context;

    public BlogService(BlogRepository repository, SportBookingDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<List<Blog>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<ApiResponse<PagedResponse<BlogResponseDto>>> GetPagedAsync(BlogQueryParameters query)
    {
        // ✅ Validate đầu vào
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
        {
            return new ApiResponse<PagedResponse<BlogResponseDto>>
            {
                Success = false,
                Message = "Tham số phân trang không hợp lệ.",
                Status = 400,
                Data = null
            };
        }

        var validSortBy = new[] { "postat", "updatedat", "lastcomment" };
        if (!validSortBy.Contains(query.SortBy.ToLower()))
        {
            return new ApiResponse<PagedResponse<BlogResponseDto>>
            {
                Success = false,
                Message = "Trường sắp xếp không hợp lệ.",
                Status = 400,
                Data = null
            };
        }

        var blogs = _repository.GetAllWithRelated();

        // ✅ Sắp xếp
        blogs = query.SortBy.ToLower() switch
        {
            "updatedat" => query.SortDirection == "asc"
                ? blogs.OrderBy(b => b.UpdatedAt)
                : blogs.OrderByDescending(b => b.UpdatedAt),

            "lastcomment" => query.SortDirection == "asc"
                ? blogs.OrderBy(b => b.Comments.Max(c => c.PostAt))
                : blogs.OrderByDescending(b => b.Comments.Max(c => c.PostAt)),

            _ => query.SortDirection == "asc"
                ? blogs.OrderBy(b => b.PostAt)
                : blogs.OrderByDescending(b => b.PostAt)
        };

        var totalItems = await blogs.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / query.PageSize);

        // ⚠️ Nếu page vượt quá số trang thì trả lỗi
        if (query.Page > totalPages && totalItems > 0)
        {
            return new ApiResponse<PagedResponse<BlogResponseDto>>
            {
                Success = false,
                Message = $"Page {query.Page} vượt quá số trang tối đa ({totalPages}).",
                Status = 400,
                Data = null
            };
        }

        if (totalItems == 0)
        {
            return new ApiResponse<PagedResponse<BlogResponseDto>>
            {
                Success = true,
                Message = "Không có blog nào.",
                Status = 200,
                Data = new PagedResponse<BlogResponseDto>
                {
                    CurrentPage = query.Page,
                    ItemsPerPage = query.PageSize,
                    TotalItems = 0,
                    TotalPages = 0,
                    Items = new List<BlogResponseDto>()
                }
            };
        }

        var items = await blogs
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BlogResponseDto
            {
                BlogId = b.BlogId,
                UserId = b.UserId ?? 0,
                Title = b.Title,
                Content = b.Content,
                PostAt = b.PostAt,
                UpdatedAt = b.UpdatedAt,
                TotalComments = b.Comments.Count
            })
            .ToListAsync();

        var paged = new PagedResponse<BlogResponseDto>
        {
            CurrentPage = query.Page,
            ItemsPerPage = query.PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            Items = items
        };

        return new ApiResponse<PagedResponse<BlogResponseDto>>
        {
            Success = true,
            Message = "Lấy danh sách blog thành công.",
            Status = 200,
            Data = paged
        };
    }


    public async Task<ApiResponse<BlogResponseDto>> GetByIdAsync(int id)
    {
        var blog = await _repository.GetByIdAsync(id);

        if (blog == null)
        {
            return new ApiResponse<BlogResponseDto>
            {
                Success = false,
                Message = "Không tìm thấy blog.",
                Status = 404,
                Data = null
            };
        }

        var dto = new BlogResponseDto
        {
            BlogId = blog.BlogId,
            UserId = blog.UserId ?? 0,
            Title = blog.Title,
            Content = blog.Content,
            PostAt = blog.PostAt,
            UpdatedAt = blog.UpdatedAt,
            TotalComments = blog.Comments.Count
        };

        return new ApiResponse<BlogResponseDto>
        {
            Success = true,
            Message = "Lấy blog thành công.",
            Status = 200,
            Data = dto
        };
    }


    public async Task<ApiResponse<Blog>> CreateAsync(CreateBlogDTO dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId); //chua co repo cua user nen goi db o day, se thay doi sau
        if (user == null)
        {
            return new ApiResponse<Blog>
            {
                Success = false,
                Message = "User không tồn tại.",
                Status = 404,
                Data = null
            };
        }

        var blog = new Blog
        {
            Title = dto.Title,
            Content = dto.Content,
            UserId = dto.UserId,
            PostAt = DateTime.UtcNow
        };

        var createdBlog = await _repository.AddAsync(blog);

        return new ApiResponse<Blog>
        {
            Success = true,
            Message = "Tạo blog thành công.",
            Status = 201,
            Data = createdBlog
        };
    }

    public async Task<ApiResponse<Blog>> UpdateAsync(int id, UpdateBlogDTO dto)
    {
        var existingBlog = await _repository.GetByIdAsync(id);
        if (existingBlog == null)
        {
            return new ApiResponse<Blog>
            {
                Success = false,
                Message = "Không tìm thấy blog.",
                Status = 404,
                Data = null
            };
        }

        // Kiểm tra quyền sở hữu
        if (existingBlog.UserId != dto.UserId)
        {
            return new ApiResponse<Blog>
            {
                Success = false,
                Message = "Bạn không có quyền sửa blog này.",
                Status = 403,
                Data = null
            };
        }

        // Kiểm tra có gì thay đổi không
        bool noChanges =
            existingBlog.Title == dto.Title &&
            existingBlog.Content == dto.Content;

        if (noChanges)
        {
            return new ApiResponse<Blog>
            {
                Success = false,
                Message = "Không có thay đổi nào để cập nhật.",
                Status = 400,
                Data = existingBlog
            };
        }

        // Cập nhật
        existingBlog.Title = dto.Title;
        existingBlog.Content = dto.Content;
        existingBlog.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveAsync();

        return new ApiResponse<Blog>
        {
            Success = true,
            Message = "Cập nhật blog thành công.",
            Status = 200,
            Data = existingBlog
        };
    }


    public async Task<ApiResponse<string>> DeleteAsync(int id, int userId)
    {
        var blog = await _repository.GetByIdAsync(id);
        if (blog == null)
        {
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Không tìm thấy blog.",
                Status = 404,
                Data = null
            };
        }

        if (blog.UserId != userId)
        {
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Bạn không có quyền xóa blog này.",
                Status = 403,
                Data = null
            };
        }

        await _repository.DeleteAsync(blog);

        return new ApiResponse<string>
        {
            Success = true,
            Message = "Xóa blog thành công.",
            Status = 200,
            Data = null
        };
    }

    public async Task<ApiResponse<PagedResponse<BlogResponseDto>>> GetByUserIdAsync(int userId, int page, int size, string sortBy, string sortDir)
    {
        var validSort = new[] { "postat", "updatedat" };
        if (!validSort.Contains(sortBy.ToLower()))
        {
            return new ApiResponse<PagedResponse<BlogResponseDto>>
            {
                Success = false,
                Message = "Trường sortBy không hợp lệ.",
                Status = 400
            };
        }

        var (blogs, totalItems) = await _repository.GetByUserIdAsync(userId, page, size, sortBy, sortDir);

        if (totalItems == 0)
        {
            return new ApiResponse<PagedResponse<BlogResponseDto>>
            {
                Success = false,
                Message = "Người dùng này chưa có blog nào.",
                Status = 404
            };
        }

        var totalPages = (int)Math.Ceiling((double)totalItems / size);
        if (page > totalPages)
        {
            return new ApiResponse<PagedResponse<BlogResponseDto>>
            {
                Success = false,
                Message = "Số trang vượt quá tổng số trang.",
                Status = 400
            };
        }

        var items = blogs.Select(b => new BlogResponseDto
        {
            BlogId = b.BlogId,
            UserId = b.UserId ?? 0,
            Title = b.Title,
            Content = b.Content,
            PostAt = b.PostAt,
            UpdatedAt = b.UpdatedAt,
            TotalComments = b.Comments?.Count ?? 0
        });


        return new ApiResponse<PagedResponse<BlogResponseDto>>
        {
            Success = true,
            Message = "Lấy danh sách blog theo người dùng thành công.",
            Status = 200,
            Data = new PagedResponse<BlogResponseDto>
            {
                CurrentPage = page,
                ItemsPerPage = size,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            }
        };
    }

}
