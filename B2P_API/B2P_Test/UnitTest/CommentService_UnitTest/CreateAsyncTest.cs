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
    public class CreateAsyncTest
    {
        private readonly Mock<ICommentRepository> _commentRepositoryMock = new();

        private CommentService CreateCommentService()
        {
            return new CommentService(_commentRepositoryMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - User not exists returns 404")]
        public async Task UTCID01_UserNotExists_Returns404()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.UserExists(It.IsAny<int>())).ReturnsAsync(false);

            var dto = new CommentDto { UserId = 1, BlogId = 2, Content = "content" };

            var result = await service.CreateAsync(dto);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Người dùng không tồn tại.", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Blog not exists returns 404")]
        public async Task UTCID02_BlogNotExists_Returns404()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.UserExists(It.IsAny<int>())).ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.BlogExists(It.IsAny<int>())).ReturnsAsync(false);

            var dto = new CommentDto { UserId = 1, BlogId = 2, Content = "content" };

            var result = await service.CreateAsync(dto);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Bài viết không tồn tại.", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Parent comment not exists returns 400")]
        public async Task UTCID03_ParentCommentNotExists_Returns400()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.UserExists(It.IsAny<int>())).ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.BlogExists(It.IsAny<int>())).ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Comment)null);

            var dto = new CommentDto { UserId = 1, BlogId = 2, ParentCommentId = 99, Content = "content" };

            var result = await service.CreateAsync(dto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Parent comment không hợp lệ.", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Parent comment of different blog returns 400")]
        public async Task UTCID04_ParentCommentDiffBlog_Returns400()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.UserExists(It.IsAny<int>())).ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.BlogExists(It.IsAny<int>())).ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Comment { CommentId = 1, BlogId = 99 });

            var dto = new CommentDto { UserId = 1, BlogId = 2, ParentCommentId = 1, Content = "content" };

            var result = await service.CreateAsync(dto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Parent comment không hợp lệ.", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Success returns 201")]
        public async Task UTCID05_Success_Returns201()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.UserExists(It.IsAny<int>())).ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.BlogExists(It.IsAny<int>())).ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Comment>())).ReturnsAsync(new Comment { CommentId = 123, BlogId = 2, UserId = 1, Content = "content" });

            var dto = new CommentDto { UserId = 1, BlogId = 2, Content = "content" };

            var result = await service.CreateAsync(dto);

            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Tạo comment thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(123, result.Data.CommentId);
        }

        [Fact(DisplayName = "UTCID06 - Success with ParentCommentId returns 201")]
        public async Task UTCID06_SuccessWithParentCommentId_Returns201()
        {
            var service = CreateCommentService();
            _commentRepositoryMock.Setup(x => x.UserExists(It.IsAny<int>())).ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.BlogExists(It.IsAny<int>())).ReturnsAsync(true);

            // parent tồn tại, cùng BlogId
            var parentComment = new Comment { CommentId = 50, BlogId = 2 };
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(50)).ReturnsAsync(parentComment);
            _commentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Comment>())).ReturnsAsync(
                (Comment c) => { c.CommentId = 999; return c; }
            );

            var dto = new CommentDto { UserId = 1, BlogId = 2, ParentCommentId = 50, Content = "content" };

            var result = await service.CreateAsync(dto);

            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Tạo comment thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(999, result.Data.CommentId);
            Assert.Equal(50, result.Data.ParentCommentId);
        }
    }
}