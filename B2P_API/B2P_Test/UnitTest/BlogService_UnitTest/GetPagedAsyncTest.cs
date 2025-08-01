using B2P_API.DTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using MockQueryable.Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BlogService_UnitTest
{
    public class GetPagedAsyncTest
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
        [InlineData(1, 101)]
        public async Task UTCID01_InvalidPageOrPageSize_Returns400(int page, int pageSize)
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = page, PageSize = pageSize, SortBy = "postat", SortDirection = "asc" };

            var result = await blogService.GetPagedAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tham số phân trang không hợp lệ.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Invalid sortBy returns 400")]
        public async Task UTCID02_InvalidSortBy_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "invalid", SortDirection = "asc" };

            var blogs = new List<Blog>();
            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trường sắp xếp không hợp lệ.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Page > totalPages returns 400")]
        public async Task UTCID03_PageGreaterThanTotalPages_Returns400()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 3, PageSize = 2, SortBy = "postat", SortDirection = "asc" };

            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, PostAt = DateTime.Now, UpdatedAt = DateTime.Now, Comments = new List<Comment>() },
                new Blog { BlogId = 2, PostAt = DateTime.Now, UpdatedAt = DateTime.Now, Comments = new List<Comment>() },
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Page 3 vượt quá số trang tối đa (1).", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - No blogs found returns 200 with empty data")]
        public async Task UTCID04_NoBlogsFound_Returns200()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "postat", SortDirection = "asc" };

            var blogs = new List<Blog>();
            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Không có blog nào.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
            Assert.Empty(result.Data.Items);
        }

        [Theory(DisplayName = "UTCID05 - Test sorting returns 200")]
        [InlineData("postat", "asc")]
        [InlineData("updatedat", "desc")]
        [InlineData("lastcomment", "asc")]
        public async Task UTCID05_ValidSorting_Returns200(string sortBy, string sortDirection)
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 2, SortBy = sortBy, SortDirection = sortDirection };

            var now = DateTime.Now;
            var blogs = new List<Blog>
            {
                new Blog
                {
                    BlogId = 1,
                    PostAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1),
                    Comments = new List<Comment> { new Comment { PostAt = now.AddDays(-2) } }
                },
                new Blog
                {
                    BlogId = 2,
                    PostAt = now,
                    UpdatedAt = now,
                    Comments = new List<Comment> { new Comment { PostAt = now.AddDays(-1) } }
                },
                new Blog
                {
                    BlogId = 3,
                    PostAt = now.AddDays(1),
                    UpdatedAt = now.AddDays(1),
                    Comments = new List<Comment>()
                }
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách blog thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.TotalItems);
            Assert.Equal(2, result.Data.ItemsPerPage);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.TotalPages);
            Assert.Equal(2, result.Data.Items.Count());
        }

        [Fact(DisplayName = "UTCID06 - Test pagination returns 200")]
        public async Task UTCID06_Pagination_Returns200()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 2, PageSize = 2, SortBy = "postat", SortDirection = "asc" };

            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, PostAt = DateTime.Now.AddDays(-2), UpdatedAt = DateTime.Now, Comments = new List<Comment>() },
                new Blog { BlogId = 2, PostAt = DateTime.Now.AddDays(-1), UpdatedAt = DateTime.Now, Comments = new List<Comment>() },
                new Blog { BlogId = 3, PostAt = DateTime.Now, UpdatedAt = DateTime.Now, Comments = new List<Comment>() }
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal(3, result.Data.TotalItems);
            Assert.Equal(2, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.TotalPages);
            Assert.Equal(1, result.Data.Items.Count());
        }

        [Fact(DisplayName = "UTCID07 - Test null UserId returns 200")]
        public async Task UTCID07_NullUserId_Returns200()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 10, SortBy = "postat", SortDirection = "asc" };

            var blogs = new List<Blog>
            {
                new Blog
                {
                    BlogId = 1,
                    UserId = null,
                    Title = "Test",
                    Content = "Content",
                    PostAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Comments = new List<Comment>()
                }
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data.Items.First().UserId);
        }

        [Fact(DisplayName = "UTCID08 - Default sorting by postat desc returns correct order")]
        public async Task UTCID08_DefaultSorting_PostAtDesc_ReturnsCorrectOrder()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 3, SortBy = "postat", SortDirection = "desc" };

            var now = DateTime.Now;
            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, PostAt = now.AddDays(-2), UpdatedAt = now, Comments = new List<Comment>() },
                new Blog { BlogId = 2, PostAt = now.AddDays(-1), UpdatedAt = now, Comments = new List<Comment>() },
                new Blog { BlogId = 3, PostAt = now, UpdatedAt = now, Comments = new List<Comment>() }
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            var items = result.Data.Items.ToList();
            Assert.Equal(3, items.Count);
            // Kết quả phải là BlogId 3, 2, 1 (postAt giảm dần)
            Assert.Equal(3, items[0].BlogId);
            Assert.Equal(2, items[1].BlogId);
            Assert.Equal(1, items[2].BlogId);
        }

        [Fact(DisplayName = "UTCID09 - Default sorting by postat asc returns correct order")]
        public async Task UTCID09_DefaultSorting_PostAtAsc_ReturnsCorrectOrder()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 3, SortBy = "postat", SortDirection = "asc" };

            var now = DateTime.Now;
            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, PostAt = now.AddDays(-2), UpdatedAt = now, Comments = new List<Comment>() },
                new Blog { BlogId = 2, PostAt = now.AddDays(-1), UpdatedAt = now, Comments = new List<Comment>() },
                new Blog { BlogId = 3, PostAt = now, UpdatedAt = now, Comments = new List<Comment>() }
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            var items = result.Data.Items.ToList();
            Assert.Equal(3, items.Count);
            // Kết quả phải là BlogId 1, 2, 3 (postAt tăng dần)
            Assert.Equal(1, items[0].BlogId);
            Assert.Equal(2, items[1].BlogId);
            Assert.Equal(3, items[2].BlogId);
        }

        [Fact(DisplayName = "UTCID10 - Sorting by updatedat asc returns correct order")]
        public async Task UTCID10_Sorting_UpdatedAtAsc_ReturnsCorrectOrder()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 3, SortBy = "updatedat", SortDirection = "asc" };

            var now = DateTime.Now;
            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, UpdatedAt = now.AddDays(-2), PostAt = now, Comments = new List<Comment>() },
                new Blog { BlogId = 2, UpdatedAt = now.AddDays(-1), PostAt = now, Comments = new List<Comment>() },
                new Blog { BlogId = 3, UpdatedAt = now, PostAt = now, Comments = new List<Comment>() }
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            var items = result.Data.Items.ToList();
            Assert.Equal(3, items.Count);
            // UpdatedAt tăng dần: 1,2,3
            Assert.Equal(1, items[0].BlogId);
            Assert.Equal(2, items[1].BlogId);
            Assert.Equal(3, items[2].BlogId);
        }

        [Fact(DisplayName = "UTCID11 - Sorting by updatedat desc returns correct order")]
        public async Task UTCID11_Sorting_UpdatedAtDesc_ReturnsCorrectOrder()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 3, SortBy = "updatedat", SortDirection = "desc" };

            var now = DateTime.Now;
            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, UpdatedAt = now.AddDays(-2), PostAt = now, Comments = new List<Comment>() },
                new Blog { BlogId = 2, UpdatedAt = now.AddDays(-1), PostAt = now, Comments = new List<Comment>() },
                new Blog { BlogId = 3, UpdatedAt = now, PostAt = now, Comments = new List<Comment>() }
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            var items = result.Data.Items.ToList();
            Assert.Equal(3, items.Count);
            // UpdatedAt giảm dần: 3,2,1
            Assert.Equal(3, items[0].BlogId);
            Assert.Equal(2, items[1].BlogId);
            Assert.Equal(1, items[2].BlogId);
        }

        [Fact(DisplayName = "UTCID12 - Sorting by lastcomment desc returns correct order")]
        public async Task UTCID12_Sorting_LastCommentDesc_ReturnsCorrectOrder()
        {
            var blogService = CreateBlogService();
            var qp = new BlogQueryParameters { Page = 1, PageSize = 3, SortBy = "lastcomment", SortDirection = "desc" };

            var now = DateTime.Now;
            var blogs = new List<Blog>
            {
                new Blog { BlogId = 1, Comments = new List<Comment> { new Comment { PostAt = now.AddDays(-5) }, new Comment { PostAt = now.AddDays(-4) } }, PostAt = now, UpdatedAt = now },
                new Blog { BlogId = 2, Comments = new List<Comment> { new Comment { PostAt = now.AddDays(-2) } }, PostAt = now, UpdatedAt = now },
                new Blog { BlogId = 3, Comments = new List<Comment> { new Comment { PostAt = now.AddDays(-1) }, new Comment { PostAt = now.AddDays(-3) } }, PostAt = now, UpdatedAt = now }
            };

            var mockQueryable = blogs.AsQueryable().BuildMock();
            _blogRepositoryMock.Setup(x => x.GetAllWithRelated()).Returns(mockQueryable);

            var result = await blogService.GetPagedAsync(qp);

            Assert.True(result.Success);
            var items = result.Data.Items.ToList();
            Assert.Equal(3, items.Count);
            // lastcomment giảm dần: BlogId 3 (-1), BlogId 2 (-2), BlogId 1 (-4)
            Assert.Equal(3, items[0].BlogId);
            Assert.Equal(2, items[1].BlogId);
            Assert.Equal(1, items[2].BlogId);
        }


    }
}