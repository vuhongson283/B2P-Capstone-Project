using B2P_API.DTOs;
using B2P_API.Models;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.CommentService_UnitTest
{
    public class DeleteAsyncTest
    {
        private readonly Mock<ICommentRepository> _commentRepositoryMock = new();

        private CommentService CreateCommentService()
        {
            return new CommentService(_commentRepositoryMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Comment not exists returns 404")]
        public async Task UTCID01_CommentNotExists_Returns404()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Comment)null);

            var result = await service.DeleteAsync(10, 1, 2);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Comment không tồn tại.", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Not owner and not admin returns 403")]
        public async Task UTCID02_NotOwnerOrAdmin_Returns403()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Comment { CommentId = 10, UserId = 99 });

            var result = await service.DeleteAsync(10, 1, 2);

            Assert.False(result.Success);
            Assert.Equal(403, result.Status);
            Assert.Equal("Bạn không có quyền xóa comment này.", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Owner can delete, returns 200")]
        public async Task UTCID03_OwnerCanDelete_Returns200()
        {
            var service = CreateCommentService();
            var comment = new Comment { CommentId = 10, UserId = 1 };
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.DeleteAsync(comment)).Returns(Task.CompletedTask);

            var result = await service.DeleteAsync(10, 1, 2);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Xóa comment thành công.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Admin can delete, returns 200")]
        public async Task UTCID04_AdminCanDelete_Returns200()
        {
            var service = CreateCommentService();
            var comment = new Comment { CommentId = 10, UserId = 99 };
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.DeleteAsync(comment)).Returns(Task.CompletedTask);

            var result = await service.DeleteAsync(10, 1, 1); // roleId == 1 (admin)

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Xóa comment thành công.", result.Message);
            Assert.Null(result.Data);
        }
    }
}