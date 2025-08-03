using B2P_API.DTOs;
using B2P_API.DTOs.UserDTO;
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
    public class GetAllAsyncTest
    {
        private readonly Mock<IBlogRepository> _blogRepositoryMock = new();
        private readonly Mock<IAccountManagementRepository> _accRepoMock = new();

        private BlogService CreateBlogService()
        {
            return new BlogService(_blogRepositoryMock.Object, _accRepoMock.Object);
        }

        // UTCID01: Invalid Page/PageSize
        [Fact(DisplayName = "UTCID01 - Invalid Page/PageSize returns 400")]
        public async Task UTCID01_Invalid_Page_Or_PageSize_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 0, PageSize = -1, SortBy = "postAt", SortDirection = "asc" };

            var result = await blogService.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Số trang và kích thước mỗi trang phải lớn hơn 0", result.Message);
        }

        // UTCID02: Invalid sortBy
        [Fact(DisplayName = "UTCID02 - Invalid sortBy returns 400")]
        public async Task UTCID02_Invalid_SortBy_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "invalid", SortDirection = "asc" };

            var result = await blogService.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Trường sortBy không hợp lệ", result.Message);
        }

        // UTCID03: Invalid sortDirection
        [Fact(DisplayName = "UTCID03 - Invalid sortDirection returns 400")]
        public async Task UTCID03_Invalid_SortDirection_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "postAt", SortDirection = "invalid" };

            var result = await blogService.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Trường sortDirection phải là", result.Message);
        }

        // UTCID04: Page > TotalPages
        [Fact(DisplayName = "UTCID04 - Page greater than totalPages returns 400")]
        public async Task UTCID04_PageGreaterThanTotalPages_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 5, PageSize = 2, SortBy = "postAt", SortDirection = "asc" };

            _blogRepositoryMock.Setup(x => x.CountAsync(qp.Search)).ReturnsAsync(4); // totalPages = 2
            // Không cần setup GetAllAsync vì branch này dừng trước

            var result = await blogService.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Số trang không hợp lệ", result.Message);
        }

        // UTCID05: No blogs found (empty)
        [Fact(DisplayName = "UTCID05 - No blogs found returns 200 with empty data")]
        public async Task UTCID05_NoBlogsFound_ReturnsEmptyResult()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "postAt", SortDirection = "asc" };

            _blogRepositoryMock.Setup(x => x.CountAsync(qp.Search)).ReturnsAsync(0);
            _blogRepositoryMock.Setup(x => x.GetAllAsync(qp)).ReturnsAsync(new List<Blog>());

            var result = await blogService.GetAllAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Không có blog nào phù hợp.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Empty(result.Data.Items);
        }

        // UTCID06: Blogs found (normal success)
        [Fact(DisplayName = "UTCID06 - Blogs found returns 200 with data")]
        public async Task UTCID06_BlogsFound_ReturnsPagedResult()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 2, SortBy = "postAt", SortDirection = "desc" };

            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, UserId = 10, Title = "A", Content = "Content A", PostAt = DateTime.Now, UpdatedAt = DateTime.Now, Comments = new List<Comment>{ new Comment(), new Comment() } },
                new Blog { BlogId = 2, UserId = 20, Title = "B", Content = "Content B", PostAt = DateTime.Now, UpdatedAt = DateTime.Now, Comments = new List<Comment>() }
            };

            _blogRepositoryMock.Setup(x => x.CountAsync(qp.Search)).ReturnsAsync(2);
            _blogRepositoryMock.Setup(x => x.GetAllAsync(qp)).ReturnsAsync(blogs);

            var result = await blogService.GetAllAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách blog thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.ItemsPerPage);
            Assert.Equal(1, result.Data.TotalPages);
            Assert.Equal(2, result.Data.Items.Count());
            var blogDtos = result.Data.Items.ToList();
            Assert.Equal(1, blogDtos[0].BlogId);
            Assert.Equal(2, blogDtos[1].BlogId);
            Assert.Equal(2, blogDtos[0].TotalComments);
            Assert.Equal(0, blogDtos[1].TotalComments);
        }

        [Theory(DisplayName = "UTCID02b - Null/empty sortBy returns 400")]
        [InlineData(null)]
        [InlineData("")]
        public async Task UTCID02b_NullOrEmptySortBy_Returns400(string sortBy)
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = sortBy, SortDirection = "asc" };

            var result = await blogService.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Trường sortBy không hợp lệ", result.Message);
        }

        // BỔ SUNG: UTCID03b: Null/empty sortDirection returns 400
        [Theory(DisplayName = "UTCID03b - Null/empty sortDirection returns 400")]
        [InlineData(null)]
        [InlineData("")]
        public async Task UTCID03b_NullOrEmptySortDirection_Returns400(string sortDirection)
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "postAt", SortDirection = sortDirection };

            var result = await blogService.GetAllAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Trường sortDirection phải là", result.Message);
        }
    }
}