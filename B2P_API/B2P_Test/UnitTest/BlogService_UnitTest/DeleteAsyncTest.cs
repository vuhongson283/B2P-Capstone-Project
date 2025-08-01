using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BlogService_UnitTest
{
    public class DeleteAsyncTest
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
            var result = await blogService.DeleteAsync(1, 1);

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
                UserId = 2
            };
            var blogService = CreateBlogService();
            _blogRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(blog);

            // Act
            var result = await blogService.DeleteAsync(1, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(403, result.Status);
            Assert.Equal("Bạn không có quyền xóa blog này.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Delete success returns 200")]
        public async Task UTCID03_DeleteSuccess_Returns200()
        {
            // Arrange
            var blog = new Blog
            {
                BlogId = 1,
                UserId = 1
            };
            var blogService = CreateBlogService();
            _blogRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(blog);
            _blogRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<Blog>())).Returns(Task.CompletedTask);

            // Act
            var result = await blogService.DeleteAsync(1, 1);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Xóa blog thành công.", result.Message);
            Assert.Null(result.Data);
        }
    }
}