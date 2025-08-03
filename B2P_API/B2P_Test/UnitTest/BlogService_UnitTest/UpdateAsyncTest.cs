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
    public class UpdateAsyncTest
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

            var dto = new UpdateBlogDTO
            {
                UserId = 1,
                Title = "New title",
                Content = "New content"
            };

            // Act
            var result = await blogService.UpdateAsync(1, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy blog.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - User not owner returns 403")]
        public async Task UTCID02_UserNotOwner_Returns403()
        {
            // Arrange
            var blog = new Blog
            {
                BlogId = 1,
                UserId = 2, // khác UserId với dto
                Title = "Old title",
                Content = "Old content"
            };

            var blogService = CreateBlogService();
            _blogRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(blog);

            var dto = new UpdateBlogDTO
            {
                UserId = 1, // không đúng chủ sở hữu
                Title = "New title",
                Content = "New content"
            };

            // Act
            var result = await blogService.UpdateAsync(1, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(403, result.Status);
            Assert.Equal("Bạn không có quyền sửa blog này.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - No changes returns 400")]
        public async Task UTCID03_NoChanges_Returns400()
        {
            // Arrange
            var blog = new Blog
            {
                BlogId = 1,
                UserId = 1,
                Title = "Same title",
                Content = "Same content"
            };

            var blogService = CreateBlogService();
            _blogRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(blog);

            var dto = new UpdateBlogDTO
            {
                UserId = 1,
                Title = "Same title",
                Content = "Same content"
            };

            // Act
            var result = await blogService.UpdateAsync(1, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không có thay đổi nào để cập nhật.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(blog.BlogId, result.Data.BlogId);
        }

        [Fact(DisplayName = "UTCID04 - Update success returns 200")]
        public async Task UTCID04_UpdateSuccess_Returns200()
        {
            // Arrange
            var oldDate = DateTime.UtcNow.AddDays(-1);
            var blog = new Blog
            {
                BlogId = 1,
                UserId = 1,
                Title = "Old title",
                Content = "Old content",
                UpdatedAt = oldDate
            };

            var blogService = CreateBlogService();
            _blogRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(blog);
            _blogRepositoryMock.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);

            var dto = new UpdateBlogDTO
            {
                UserId = 1,
                Title = "New title",
                Content = "New content"
            };

            // Act
            var result = await blogService.UpdateAsync(1, dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật blog thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(dto.Title, result.Data.Title);
            Assert.Equal(dto.Content, result.Data.Content);
            Assert.True(result.Data.UpdatedAt > oldDate); // UpdatedAt phải được cập nhật mới
        }
    }
}