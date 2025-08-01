using B2P_API.DTOs;
using B2P_API.Models;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.CommentService_UnitTest
{
    public class GetByUserIdAsyncTest
    {
        private readonly Mock<ICommentRepository> _commentRepositoryMock = new();

        private CommentService CreateCommentService()
        {
            return new CommentService(_commentRepositoryMock.Object);
        }

        [Theory(DisplayName = "UTCID01 - Invalid page or pageSize returns 400")]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        public async Task UTCID01_InvalidPaging_Returns400(int page, int pageSize)
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = page, PageSize = pageSize, SortBy = "postat", SortDirection = "asc" };

            var result = await service.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Giá trị 'page' và 'pageSize' phải lớn hơn 0.", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Invalid sortBy returns 400")]
        public async Task UTCID02_InvalidSortBy_Returns400()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 10, SortBy = "invalid", SortDirection = "asc" };

            var result = await service.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trường 'sortBy' không hợp lệ. Chỉ chấp nhận: 'postAt', 'updatedAt'.", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Invalid sortDirection returns 400")]
        public async Task UTCID03_InvalidSortDirection_Returns400()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 10, SortBy = "postat", SortDirection = "wrong" };

            var result = await service.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trường 'sortDirection' phải là 'asc' hoặc 'desc'.", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Page > totalPages returns 400")]
        public async Task UTCID04_PageOutOfRange_Returns400()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 3, PageSize = 2, SortBy = "postat", SortDirection = "asc" };

            _commentRepositoryMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(4); // totalPages = 2
            _commentRepositoryMock.Setup(x => x.GetByUserIdAsync(1, qp)).ReturnsAsync(new List<Comment>());

            var result = await service.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Số trang vượt quá tổng số trang hiện có (2).", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Success returns 200 and correct data")]
        public async Task UTCID05_Success_Returns200()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 2, SortBy = "postat", SortDirection = "desc" };

            var comments = new List<Comment>
            {
                new Comment { CommentId = 1, BlogId = 1, Blog = new Blog { Title = "BlogA" }, Content = "A", PostAt = System.DateTime.Now, UpdatedAt = null, ParentCommentId = null },
                new Comment { CommentId = 2, BlogId = 2, Blog = null, Content = "B", PostAt = System.DateTime.Now, UpdatedAt = System.DateTime.Now, ParentCommentId = 1 }
            };

            _commentRepositoryMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(2);
            _commentRepositoryMock.Setup(x => x.GetByUserIdAsync(1, qp)).ReturnsAsync(comments);

            var result = await service.GetByUserIdAsync(1, qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy bình luận theo UserId thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.ItemsPerPage);
            Assert.Equal(1, result.Data.TotalPages);
            Assert.Equal(2, result.Data.Items.Count());
            var items = result.Data.Items;
            Assert.Equal("BlogA", items.First().BlogTitle);
            Assert.Equal("(Không có tiêu đề)", items.Last().BlogTitle);
        }

        [Fact(DisplayName = "UTCID06 - Page > totalPages nhưng totalPages == 0 returns 200")]
        public async Task UTCID06_PageGreaterThanTotalPages_AndTotalPagesIsZero_ReturnsEmptyResult()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 2, PageSize = 10, SortBy = "postat", SortDirection = "asc" };

            // totalItems = 0 -> totalPages = 0
            _commentRepositoryMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(0);
            _commentRepositoryMock.Setup(x => x.GetByUserIdAsync(1, qp)).ReturnsAsync(new List<Comment>());

            var result = await service.GetByUserIdAsync(1, qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy bình luận theo UserId thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
            Assert.Empty(result.Data.Items);
        }

        [Fact(DisplayName = "UTCID07 - No comments returns 200 with empty data")]
        public async Task UTCID07_NoComments_Returns200()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 10, SortBy = "postat", SortDirection = "asc" };

            _commentRepositoryMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(0);
            _commentRepositoryMock.Setup(x => x.GetByUserIdAsync(1, qp)).ReturnsAsync(new List<Comment>());

            var result = await service.GetByUserIdAsync(1, qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy bình luận theo UserId thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
            Assert.Empty(result.Data.Items);
        }

        [Fact(DisplayName = "UTCID08 - Comments with null Blog, null Content")]
        public async Task UTCID08_CommentsWithNullBlogAndContent_ReturnsCorrectDto()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 2, SortBy = "postat", SortDirection = "asc" };

            var comments = new List<Comment>
            {
                // Blog = null, Content = null
                new Comment { CommentId = 1, BlogId = null, Blog = null, Content = null, PostAt = System.DateTime.Now, UpdatedAt = null, ParentCommentId = null },
                // Blog = có, Content = "", ParentCommentId = 5
                new Comment { CommentId = 2, BlogId = 2, Blog = new Blog { Title = "BlogB" }, Content = "", PostAt = System.DateTime.Now, UpdatedAt = System.DateTime.Now, ParentCommentId = 5 }
            };

            _commentRepositoryMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(2);
            _commentRepositoryMock.Setup(x => x.GetByUserIdAsync(1, qp)).ReturnsAsync(comments);

            var result = await service.GetByUserIdAsync(1, qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Items.Count());
            var items = result.Data.Items.ToList();

            // Kiểm tra mapping BlogTitle và Content fallback
            Assert.Equal("(Không có tiêu đề)", items[0].BlogTitle);
            Assert.Equal(0, items[0].BlogId); // vì BlogId ?? 0
            Assert.Equal("", items[0].Content);
            Assert.Null(items[0].ParentCommentId);

            Assert.Equal("BlogB", items[1].BlogTitle);
            Assert.Equal(2, items[1].BlogId);
            Assert.Equal("", items[1].Content);
            Assert.Equal(5, items[1].ParentCommentId);
        }

    }
}