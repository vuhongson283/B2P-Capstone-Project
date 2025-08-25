using B2P_API.DTOs.RatingDTO;
using B2P_API.DTOs.UserDTO;
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
    public class UpdateAsyncTest
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
            var result = await service.UpdateAsync(99, new CreateRatingDto
            {
                BookingId = 1,
                Comment = "Test",
                Stars = 5
            });

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy đánh giá.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Update success returns 200")]
        public async Task UTCID02_UpdateSuccess_Returns200()
        {
            // Arrange
            var rating = new Rating
            {
                RatingId = 2,
                BookingId = 222,
                Comment = "Old comment",
                Stars = 2,
            };
            _repoMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(rating);
            _repoMock.Setup(x => x.UpdateAsync(It.IsAny<Rating>())).Returns(Task.CompletedTask);

            var service = CreateService();

            var dto = new CreateRatingDto
            {
                BookingId = 3,
                Comment = "Updated!",
                Stars = 4
            };

            // Act
            var result = await service.UpdateAsync(2, dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật đánh giá thành công.", result.Message);
            Assert.Equal("OK", result.Data);

            Assert.Equal(dto.BookingId, rating.BookingId);
            Assert.Equal(dto.Comment, rating.Comment);
            Assert.Equal(dto.Stars, rating.Stars);
        }

    }
}