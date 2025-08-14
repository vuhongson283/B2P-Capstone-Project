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
    public class GetByIdAsyncTest
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
            var result = await service.GetByIdAsync(99);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy đánh giá.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - GetById returns rating successfully")]
        public async Task UTCID02_GetById_ReturnsRatingSuccessfully()
        {
            // Arrange
            var rating = new Rating
            {
                RatingId = 1,
                BookingId = 10,
                Comment = "Nice place",
                CreateAt = DateTime.Now.AddMinutes(-30),
                Stars = 5
            };
            _repoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(rating);
            var service = CreateService();

            // Act
            var result = await service.GetByIdAsync(1);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.RatingId);
            Assert.Equal(10, result.Data.BookingId);
            Assert.Equal("Nice place", result.Data.Comment);
            Assert.Equal(5, result.Data.Stars);
            Assert.Equal(rating.CreateAt.Value, result.Data.CreateAt);
        }

        [Fact(DisplayName = "UTCID03 - Exception returns 500")]
        public async Task UTCID03_Exception_Returns500()
        {
            // Arrange
            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.GetByIdAsync(1));
        }
    }
}