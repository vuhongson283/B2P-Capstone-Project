using B2P_API.DTOs;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;

namespace B2P_API.Services
{
    public class CommentService
    {
        private readonly CommentRepository _repository;

        public CommentService(CommentRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<Comment>> CreateAsync(CommentDto dto)
        {
            // Kiểm tra user và blog tồn tại
            if (!await _repository.UserExists(dto.UserId))
                return new ApiResponse<Comment>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại.",
                    Status = 404
                };

            if (!await _repository.BlogExists(dto.BlogId))
                return new ApiResponse<Comment>
                {
                    Success = false,
                    Message = "Bài viết không tồn tại.",
                    Status = 404
                };

            // Nếu có ParentCommentId → kiểm tra parent tồn tại và cùng blog
            if (dto.ParentCommentId.HasValue)
            {
                var parent = await _repository.GetByIdAsync(dto.ParentCommentId.Value);
                if (parent == null || parent.BlogId != dto.BlogId)
                {
                    return new ApiResponse<Comment>
                    {
                        Success = false,
                        Message = "Parent comment không hợp lệ.",
                        Status = 400
                    };
                }
            }

            var comment = new Comment
            {
                UserId = dto.UserId,
                BlogId = dto.BlogId,
                Content = dto.Content,
                ParentCommentId = dto.ParentCommentId,
                PostAt = DateTime.Now
            };

            var created = await _repository.AddAsync(comment);

            return new ApiResponse<Comment>
            {
                Success = true,
                Message = "Tạo comment thành công.",
                Status = 201,
                Data = created
            };
        }

        public async Task<ApiResponse<Comment>> UpdateAsync(int id, CommentDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return new ApiResponse<Comment>
                {
                    Success = false,
                    Message = "Comment không tồn tại.",
                    Status = 404
                };
            }

            if (existing.UserId != dto.UserId)
            {
                return new ApiResponse<Comment>
                {
                    Success = false,
                    Message = "Bạn không có quyền sửa comment này.",
                    Status = 403
                };
            }

            if (existing.Content == dto.Content?.Trim())
            {
                return new ApiResponse<Comment>
                {
                    Success = false,
                    Message = "Nội dung không có thay đổi.",
                    Status = 400
                };
            }

            existing.Content = dto.Content!.Trim();
            existing.UpdatedAt = DateTime.Now;

            var updated = await _repository.UpdateAsync(existing);

            return new ApiResponse<Comment>
            {
                Success = true,
                Message = "Cập nhật comment thành công.",
                Status = 200,
                Data = updated
            };
        }

        public async Task<ApiResponse<string>> DeleteAsync(int id, int userId, int roleId)
        {
            var comment = await _repository.GetByIdAsync(id);

            if (comment == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Comment không tồn tại.",
                    Status = 404
                };
            }

            // Chỉ chủ sở hữu hoặc admin (roleId == 1) mới được xóa
            if (comment.UserId != userId && roleId != 1)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Bạn không có quyền xóa comment này.",
                    Status = 403
                };
            }

            await _repository.DeleteAsync(comment);

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Xóa comment thành công.",
                Status = 200,
                Data = null
            };
        }

        public async Task<ApiResponse<PagedResponse<CommentResponseDto>>> GetByUserIdAsync(int userId, CommentQueryParameters queryParams)
        {
            if (queryParams.Page <= 0 || queryParams.PageSize <= 0)
            {
                return new ApiResponse<PagedResponse<CommentResponseDto>>
                {
                    Success = false,
                    Message = "Giá trị 'page' và 'pageSize' phải lớn hơn 0.",
                    Status = 400
                };
            }

            var validSortBy = new[] { "postat", "updatedat" };
            if (!validSortBy.Contains(queryParams.SortBy.ToLower()))
            {
                return new ApiResponse<PagedResponse<CommentResponseDto>>
                {
                    Success = false,
                    Message = "Trường 'sortBy' không hợp lệ. Chỉ chấp nhận: 'postAt', 'updatedAt'.",
                    Status = 400
                };
            }

            if (queryParams.SortDirection.ToLower() != "asc" && queryParams.SortDirection.ToLower() != "desc")
            {
                return new ApiResponse<PagedResponse<CommentResponseDto>>
                {
                    Success = false,
                    Message = "Trường 'sortDirection' phải là 'asc' hoặc 'desc'.",
                    Status = 400
                };
            }

            var totalItems = await _repository.CountByUserIdAsync(userId, queryParams.Search);
            var totalPages = (int)Math.Ceiling(totalItems / (double)queryParams.PageSize);

            if (totalPages > 0 && queryParams.Page > totalPages)
            {
                return new ApiResponse<PagedResponse<CommentResponseDto>>
                {
                    Success = false,
                    Message = $"Số trang vượt quá tổng số trang hiện có ({totalPages}).",
                    Status = 400
                };
            }

            var comments = await _repository.GetByUserIdAsync(userId, queryParams);

            var commentDtos = comments.Select(c => new CommentResponseDto
            {
                CommentId = c.CommentId,
                BlogId = c.BlogId ?? 0,
                BlogTitle = c.Blog?.Title ?? "(Không có tiêu đề)",
                Content = c.Content ?? "",
                PostAt = c.PostAt,
                UpdatedAt = c.UpdatedAt,
                ParentCommentId = c.ParentCommentId
            });

            var response = new PagedResponse<CommentResponseDto>
            {
                CurrentPage = queryParams.Page,
                ItemsPerPage = queryParams.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = commentDtos
            };

            return new ApiResponse<PagedResponse<CommentResponseDto>>
            {
                Success = true,
                Message = "Lấy bình luận theo UserId thành công.",
                Status = 200,
                Data = response
            };
        }


        public async Task<ApiResponse<PagedResponse<CommentResponseDto>>> GetAllAsync(CommentQueryParameters queryParams)
        {
            if (queryParams.Page <= 0 || queryParams.PageSize <= 0)
                return new ApiResponse<PagedResponse<CommentResponseDto>>
                {
                    Success = false,
                    Message = "Giá trị 'page' và 'pageSize' phải lớn hơn 0.",
                    Status = 400
                };

            var validSortBy = new[] { "postat", "updatedat" };
            if (!validSortBy.Contains(queryParams.SortBy.ToLower()))
                return new ApiResponse<PagedResponse<CommentResponseDto>>
                {
                    Success = false,
                    Message = "Trường 'sortBy' không hợp lệ. Chỉ chấp nhận: 'postAt', 'updatedAt'.",
                    Status = 400
                };

            if (queryParams.SortDirection.ToLower() != "asc" && queryParams.SortDirection.ToLower() != "desc")
                return new ApiResponse<PagedResponse<CommentResponseDto>>
                {
                    Success = false,
                    Message = "Trường 'sortDirection' phải là 'asc' hoặc 'desc'.",
                    Status = 400
                };

            var totalItems = await _repository.CountAllAsync(queryParams.Search);
            var totalPages = (int)Math.Ceiling(totalItems / (double)queryParams.PageSize);

            if (totalPages > 0 && queryParams.Page > totalPages)
                return new ApiResponse<PagedResponse<CommentResponseDto>>
                {
                    Success = false,
                    Message = $"Số trang vượt quá tổng số trang hiện có ({totalPages}).",
                    Status = 400
                };

            var comments = await _repository.GetAllAsync(queryParams);

            var commentDtos = comments.Select(c => new CommentResponseDto
            {
                UserId = c.UserId,
                UserName = c.User.FullName,
                CommentId = c.CommentId,
                BlogId = c.BlogId ?? 0,
                BlogTitle = c.Blog?.Title ?? "(Không có tiêu đề)",
                Content = c.Content ?? "",
                PostAt = c.PostAt,
                UpdatedAt = c.UpdatedAt,
                ParentCommentId = c.ParentCommentId
            });

            var response = new PagedResponse<CommentResponseDto>
            {
                CurrentPage = queryParams.Page,
                ItemsPerPage = queryParams.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = commentDtos
            };

            return new ApiResponse<PagedResponse<CommentResponseDto>>
            {
                Success = true,
                Message = "Lấy danh sách bình luận thành công.",
                Status = 200,
                Data = response
            };
        }


    }


}
