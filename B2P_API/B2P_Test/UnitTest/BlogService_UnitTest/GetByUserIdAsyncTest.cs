using B2P_API.DTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BlogService_UnitTest
{
    public class GetByUserIdAsyncTest
    {
        private readonly Mock<IBlogRepository> _blogRepositoryMock = new();
        private readonly Mock<IAccountManagementRepository> _accRepoMock = new();

        private BlogService CreateBlogService()
        {
            return new BlogService(_blogRepositoryMock.Object, _accRepoMock.Object);
        }

        [Theory(DisplayName = "UTCID01 - Invalid page or pageSize returns 400")]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        public async Task UTCID01_InvalidPaging_Returns400(int page, int pageSize)
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = page, PageSize = pageSize, SortBy = "postat", SortDirection = "asc" };

            var result = await blogService.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Page/Size không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Invalid sortBy returns 400")]
        public async Task UTCID02_InvalidSortBy_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "invalid", SortDirection = "asc" };

            var result = await blogService.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("SortBy không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Invalid sortDirection returns 400")]
        public async Task UTCID03_InvalidSortDirection_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "postat", SortDirection = "wrong" };

            var result = await blogService.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("SortDirection không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Page > totalPages returns 400")]
        public async Task UTCID04_PageOutOfRange_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 3, PageSize = 2, SortBy = "postat", SortDirection = "asc" };

            _blogRepositoryMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(4); // totalPages = 2
            _blogRepositoryMock.Setup(x => x.GetByUserIdAsync(1, qp)).ReturnsAsync(new List<Blog>());

            var result = await blogService.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Số trang vượt quá tổng số trang (2).", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Success returns 200 and correct data")]
        public async Task UTCID05_Success_Returns200()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 2, SortBy = "postat", SortDirection = "desc" };

            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, UserId = 1, Title = "A", Content = "A", PostAt = DateTime.Now, UpdatedAt = DateTime.Now, Comments = new List<Comment> { new Comment() } },
                new Blog { BlogId = 2, UserId = 1, Title = "B", Content = "B", PostAt = DateTime.Now, UpdatedAt = DateTime.Now, Comments = new List<Comment>() }
            };

            _blogRepositoryMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(2);
            _blogRepositoryMock.Setup(x => x.GetByUserIdAsync(1, qp)).ReturnsAsync(blogs);

            var result = await blogService.GetByUserIdAsync(1, qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy blog theo UserId thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.ItemsPerPage);
            Assert.Equal(1, result.Data.TotalPages);
            Assert.Equal(2, result.Data.Items.Count());
            Assert.Equal(1, result.Data.Items.First().UserId);
        }

        // BỔ SUNG: UTCID02b - SortBy null/empty returns 400
        [Theory(DisplayName = "UTCID02b - SortBy null/empty returns 400")]
        [InlineData(null)]
        public async Task UTCID02b_SortByNullOrEmpty_Returns400(string sortBy)
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = sortBy, SortDirection = "asc" };

            var result = await blogService.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("SortBy không hợp lệ", result.Message);
        }

        // BỔ SUNG: UTCID03b - SortDirection null/empty returns 400
        [Theory(DisplayName = "UTCID03b - SortDirection null/empty returns 400")]
        [InlineData(null)]
        public async Task UTCID03b_SortDirectionNullOrEmpty_Returns400(string sortDirection)
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "postat", SortDirection = sortDirection };

            var result = await blogService.GetByUserIdAsync(1, qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("SortDirection không hợp lệ", result.Message);
        }
        // BỔ SUNG: UTCID06 - Page > totalPages nhưng totalPages == 0 returns 200 (empty result)
        [Fact(DisplayName = "UTCID06 - Page > totalPages and totalPages == 0 returns empty result")]
        public async Task UTCID06_PageGreaterThanTotalPages_AndTotalPagesIsZero_ReturnsEmptyResult()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 2, PageSize = 10, SortBy = "postat", SortDirection = "asc" };

            // totalItems = 0 -> totalPages = 0
            _blogRepositoryMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(0);
            _blogRepositoryMock.Setup(x => x.GetByUserIdAsync(1, qp)).ReturnsAsync(new List<Blog>());

            var result = await blogService.GetByUserIdAsync(1, qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy blog theo UserId thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
            Assert.Empty(result.Data.Items);
        }
    }
}