using System.ComponentModel.DataAnnotations;

namespace B2P_API.DTOs;

public class BlogDto
{
    [Required(ErrorMessage = "UserId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "UserId phải là số nguyên dương.")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    [MaxLength(30, ErrorMessage = "Tiêu đề không được vượt quá 30 ký tự.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung không được để trống.")]
    [MaxLength(300, ErrorMessage = "Nội dung không được vượt quá 300 ký tự.")]
    public string Content { get; set; } = null!;
}


public class CreateBlogDTO
{
    [Required(ErrorMessage = "UserId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "UserId phải là số nguyên dương.")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    [MaxLength(30, ErrorMessage = "Tiêu đề không được vượt quá 30 ký tự.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung không được để trống.")]
    [MaxLength(300, ErrorMessage = "Nội dung không được vượt quá 300 ký tự.")]
    public string Content { get; set; } = null!;
}

public class UpdateBlogDTO
{
    [Required(ErrorMessage = "UserId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "UserId phải là số nguyên dương.")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    [MaxLength(30, ErrorMessage = "Tiêu đề không được vượt quá 30 ký tự.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung không được để trống.")]
    [MaxLength(300, ErrorMessage = "Nội dung không được vượt quá 300 ký tự.")]
    public string Content { get; set; } = null!;
}

public class BlogResponseDto
{
    public int BlogId { get; set; }
    public int UserId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime? PostAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int TotalComments { get; set; }
}


public class BlogQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "postAt"; // postAt, updatedAt, lastComment
    public string SortDirection { get; set; } = "desc"; // asc | desc
}
