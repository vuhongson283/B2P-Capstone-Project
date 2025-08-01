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
    public class GetAllAsyncTest
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

            var result = await service.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Giá trị 'page' và 'pageSize' phải lớn hơn 0.", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Invalid sortBy returns 400")]
        public async Task UTCID02_InvalidSortBy_Returns400()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 10, SortBy = "invalid", SortDirection = "asc" };

            var result = await service.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trường 'sortBy' không hợp lệ. Chỉ chấp nhận: 'postAt', 'updatedAt'.", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Invalid sortDirection returns 400")]
        public async Task UTCID03_InvalidSortDirection_Returns400()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 10, SortBy = "postat", SortDirection = "wrong" };

            var result = await service.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trường 'sortDirection' phải là 'asc' hoặc 'desc'.", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Page > totalPages returns 400")]
        public async Task UTCID04_PageOutOfRange_Returns400()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 3, PageSize = 2, SortBy = "postat", SortDirection = "asc" };

            _commentRepositoryMock.Setup(x => x.CountAllAsync(null)).ReturnsAsync(4); // totalPages = 2
            _commentRepositoryMock.Setup(x => x.GetAllAsync(qp)).ReturnsAsync(new List<Comment>());

            var result = await service.GetAllAsync(qp);

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
                new Comment { CommentId = 1, UserId = 100, User = new User { FullName = "UserA" }, BlogId = 1, Blog = new Blog { Title = "BlogA" }, Content = "A", PostAt = System.DateTime.Now, UpdatedAt = null, ParentCommentId = null },
                new Comment { CommentId = 2, UserId = 101, User = new User { FullName = "UserB" }, BlogId = 2, Blog = null, Content = "B", PostAt = System.DateTime.Now, UpdatedAt = System.DateTime.Now, ParentCommentId = 1 }
            };

            _commentRepositoryMock.Setup(x => x.CountAllAsync(null)).ReturnsAsync(2);
            _commentRepositoryMock.Setup(x => x.GetAllAsync(qp)).ReturnsAsync(comments);

            var result = await service.GetAllAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách bình luận thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.ItemsPerPage);
            Assert.Equal(1, result.Data.TotalPages);
            Assert.Equal(2, result.Data.Items.Count());
            var items = result.Data.Items;
            Assert.Equal("UserA", items.First().UserName);
            Assert.Equal("UserB", items.Last().UserName);
        }

        [Fact(DisplayName = "UTCID06 - Page > totalPages nhưng totalPages == 0 returns 200 with empty result")]
        public async Task UTCID06_PageGreaterThanTotalPages_AndTotalPagesIsZero_ReturnsEmptyResult()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 2, PageSize = 10, SortBy = "postat", SortDirection = "asc" };

            _commentRepositoryMock.Setup(x => x.CountAllAsync(null)).ReturnsAsync(0); // totalPages = 0
            _commentRepositoryMock.Setup(x => x.GetAllAsync(qp)).ReturnsAsync(new List<Comment>());

            var result = await service.GetAllAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách bình luận thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
            Assert.Empty(result.Data.Items);
        }

        [Fact(DisplayName = "UTCID07 - No comments returns 200 with empty items")]
        public async Task UTCID07_NoComments_Returns200EmptyItems()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 10, SortBy = "postat", SortDirection = "asc" };

            _commentRepositoryMock.Setup(x => x.CountAllAsync(null)).ReturnsAsync(0);
            _commentRepositoryMock.Setup(x => x.GetAllAsync(qp)).ReturnsAsync(new List<Comment>());

            var result = await service.GetAllAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Items);
        }

        [Fact(DisplayName = "UTCID08 - Comments with null Blog, null User, null Content")]
        public async Task UTCID08_CommentsWithNullProperties_ReturnsCorrectDto()
        {
            var service = CreateCommentService();
            var qp = new CommentQueryParameters { Page = 1, PageSize = 2, SortBy = "postat", SortDirection = "asc" };

            // Đảm bảo User và Blog không null, nhưng property có thể null
            var comments = new List<Comment>
            {
                new Comment { CommentId = 1, UserId = 100, User = new User { FullName = null }, BlogId = 1, Blog = new Blog { Title = null }, Content = null, PostAt = System.DateTime.Now, UpdatedAt = null, ParentCommentId = null },
                new Comment { CommentId = 2, UserId = null, User = new User { FullName = null }, BlogId = null, Blog = new Blog { Title = null }, Content = null, PostAt = null, UpdatedAt = null, ParentCommentId = null }
            };

            _commentRepositoryMock.Setup(x => x.CountAllAsync(null)).ReturnsAsync(2);
            _commentRepositoryMock.Setup(x => x.GetAllAsync(qp)).ReturnsAsync(comments);

            var result = await service.GetAllAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Items.Count());
            var items = result.Data.Items.ToList();

            Assert.Equal("(Không có tiêu đề)", items[0].BlogTitle);
            Assert.Equal("", items[0].Content);
            Assert.Null(items[0].UserName);

            Assert.Equal("(Không có tiêu đề)", items[1].BlogTitle);
            Assert.Equal("", items[1].Content);
            Assert.Null(items[1].UserName);
        }
    }
}