using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.RatingService_UnitTest
{
    public class DeleteAsyncTest
    {
        private readonly Mock<IRatingRepository> _repoMock = new();

        private RatingService CreateService()
        {
            return new RatingService(_repoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Rating not found returns 404")]
        public async Task UTCID01_RatingNotFound_Returns404()
        {
            // Arrange
            _repoMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Rating)null);

            var service = CreateService();

            // Act
            var result = await service.DeleteAsync(99);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy đánh giá.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Delete success returns 200")]
        public async Task UTCID02_DeleteSuccess_Returns200()
        {
            // Arrange
            var rating = new Rating { RatingId = 10, Stars = 5, Comment = "Good", BookingId = 3 };
            _repoMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(rating);
            _repoMock.Setup(x => x.DeleteAsync(rating)).Returns(Task.CompletedTask);

            var service = CreateService();

            // Act
            var result = await service.DeleteAsync(10);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Xóa đánh giá thành công.", result.Message);
            Assert.Equal("OK", result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Exception returns 500")]
        public async Task UTCID03_Exception_Returns500()
        {
            // Arrange
            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

            var service = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => service.DeleteAsync(1));
            Assert.Equal("db error", ex.Message);
        }
    }
}