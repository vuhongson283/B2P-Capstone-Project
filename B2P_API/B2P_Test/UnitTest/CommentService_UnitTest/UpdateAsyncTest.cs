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
    public class UpdateAsyncTest
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

            var dto = new CommentDto { UserId = 1, BlogId = 2, Content = "content" };

            var result = await service.UpdateAsync(10, dto);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Comment không tồn tại.", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Not owner returns 403")]
        public async Task UTCID02_NotOwner_Returns403()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Comment { CommentId = 10, UserId = 99, Content = "old content" });

            var dto = new CommentDto { UserId = 1, BlogId = 2, Content = "content" };

            var result = await service.UpdateAsync(10, dto);

            Assert.False(result.Success);
            Assert.Equal(403, result.Status);
            Assert.Equal("Bạn không có quyền sửa comment này.", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - No content change returns 400")]
        public async Task UTCID03_NoContentChange_Returns400()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Comment { CommentId = 10, UserId = 1, Content = "content" });

            var dto = new CommentDto { UserId = 1, BlogId = 2, Content = "content" };

            var result = await service.UpdateAsync(10, dto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Nội dung không có thay đổi.", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Success returns 200")]
        public async Task UTCID04_Success_Returns200()
        {
            var service = CreateCommentService();
            var existing = new Comment { CommentId = 10, UserId = 1, Content = "old content" };
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(existing);
            _commentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Comment>())).ReturnsAsync(existing);

            var dto = new CommentDto { UserId = 1, BlogId = 2, Content = "new content" };

            var result = await service.UpdateAsync(10, dto);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật comment thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(10, result.Data.CommentId);
            Assert.Equal("new content", result.Data.Content);
        }

        [Fact(DisplayName = "UTCID05 - Success returns 200 with content needs trim")]
        public async Task UTCID05_SuccessWithTrimContent_Returns200()
        {
            var service = CreateCommentService();
            var existing = new Comment { CommentId = 10, UserId = 1, Content = "old content" };
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(existing);
            _commentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Comment>())).ReturnsAsync(
                (Comment c) => c
            );

            var dto = new CommentDto { UserId = 1, BlogId = 2, Content = "   new content   " };

            var result = await service.UpdateAsync(10, dto);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật comment thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(10, result.Data.CommentId);
            Assert.Equal("new content", result.Data.Content); // đã trim
        }
    }
}