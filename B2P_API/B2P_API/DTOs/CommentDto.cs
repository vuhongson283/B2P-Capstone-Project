using System.ComponentModel.DataAnnotations;

namespace B2P_API.DTOs
{
    public class CommentDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "UserId không hợp lệ.")]
        public int UserId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "BlogId không hợp lệ.")]
        public int BlogId { get; set; }

        public int? ParentCommentId { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        [MaxLength(300, ErrorMessage = "Nội dung không vượt quá 300 ký tự.")]
        public string Content { get; set; } = null!;
    }

    public class CommentResponseDto
    {
        public int CommentId { get; set; }
        public int BlogId { get; set; }
        public string BlogTitle { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime? PostAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? ParentCommentId { get; set; }
    }



    public class CommentUpdateResponseDto
    {
        public int CommentId { get; set; }
        public int BlogId { get; set; }
        public int UserId { get; set; }
        public string? Content { get; set; }
        public DateTime? PostAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? ParentCommentId { get; set; }
        public string? UserName { get; set; } // nếu cần hiện tên người dùng
    }

    public class CommentQueryParameters
    {
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Sắp xếp theo thời gian đăng (postAt) hoặc chỉnh sửa (updatedAt)
        public string SortBy { get; set; } = "postAt";
        public string SortDirection { get; set; } = "desc"; // asc | desc
    }


}
