using B2P_API.DTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BlogService_UnitTest
{
    public class GetByIdAsyncTest
    {
        private readonly Mock<IBlogRepository> _blogRepositoryMock = new();
        private readonly Mock<IAccountManagementRepository> _accRepoMock = new();

        private BlogService CreateBlogService()
        {
            return new BlogService(_blogRepositoryMock.Object, _accRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Blog not found returns 404")]
        public async Task UTCID01_BlogNotFound_Returns404()
        {
            // Arrange
            var blogService = CreateBlogService();
            _blogRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Blog)null);

            // Act
            var result = await blogService.GetByIdAsync(1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy blog.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Blog found returns 200 with correct data")]
        public async Task UTCID02_BlogFound_Returns200()
        {
            // Arrange
            var now = DateTime.Now;
            var blog = new Blog
            {
                BlogId = 123,
                UserId = 99,
                Title = "Sample Title",
                Content = "Sample Content",
                PostAt = now,
                UpdatedAt = now.AddMinutes(10),
                Comments = new List<Comment>
                {
                    new Comment(),
                    new Comment()
                }
            };
            var blogService = CreateBlogService();
            _blogRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(blog);

            // Act
            var result = await blogService.GetByIdAsync(123);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy blog thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(blog.BlogId, result.Data.BlogId);
            Assert.Equal(blog.UserId, result.Data.UserId);
            Assert.Equal(blog.Title, result.Data.Title);
            Assert.Equal(blog.Content, result.Data.Content);
            Assert.Equal(blog.PostAt, result.Data.PostAt);
            Assert.Equal(blog.UpdatedAt, result.Data.UpdatedAt);
            Assert.Equal(blog.Comments.Count, result.Data.TotalComments);
        }

    }
}