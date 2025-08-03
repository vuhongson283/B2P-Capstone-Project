using B2P_API.DTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BlogService_UnitTest
{
    public class CreateAsyncTest
    {
        private readonly Mock<IBlogRepository> _blogRepositoryMock = new();
        private readonly Mock<IAccountManagementRepository> _accRepoMock = new();

        private BlogService CreateBlogService()
        {
            return new BlogService(_blogRepositoryMock.Object, _accRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - User not found returns 404")]
        public async Task UTCID01_UserNotFound_Returns404()
        {
            // Arrange
            var blogService = CreateBlogService();
            _accRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User)null);

            var dto = new CreateBlogDTO
            {
                UserId = 1,
                Title = "Tiêu đề",
                Content = "Nội dung"
            };

            // Act
            var result = await blogService.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("User không tồn tại.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Create blog success returns 201")]
        public async Task UTCID02_CreateBlogSuccess_Returns201()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var blogService = CreateBlogService();
            var user = new User { UserId = 2 };
            var blog = new Blog
            {
                BlogId = 10,
                UserId = 2,
                Title = "Tiêu đề",
                Content = "Nội dung",
                PostAt = now
            };

            _accRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(user);
            _blogRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Blog>())).ReturnsAsync(blog);

            var dto = new CreateBlogDTO
            {
                UserId = 2,
                Title = "Tiêu đề",
                Content = "Nội dung"
            };

            // Act
            var result = await blogService.CreateAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Tạo blog thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(blog.BlogId, result.Data.BlogId);
            Assert.Equal(blog.UserId, result.Data.UserId);
            Assert.Equal(blog.Title, result.Data.Title);
            Assert.Equal(blog.Content, result.Data.Content);
            Assert.Equal(blog.PostAt, result.Data.PostAt);
        }
    }
}