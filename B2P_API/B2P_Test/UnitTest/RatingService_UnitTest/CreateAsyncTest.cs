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
    public class CreateAsyncTest
    {
        private readonly Mock<IRatingRepository> _repoMock = new();

        private RatingService CreateService()
        {
            return new RatingService(_repoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Create rating success returns 201")]
        public async Task UTCID01_CreateRatingSuccess_Returns201()
        {
            // Arrange
            var dto = new CreateRatingDto
            {
                BookingId = 5,
                Comment = "Great court!",
                Stars = 4
            };

            // Fake repo: after AddAsync, RatingId is set (simulate DB)
            _repoMock.Setup(x => x.AddAsync(It.IsAny<Rating>()))
                .Callback<Rating>(r => r.RatingId = 123)
                .Returns(Task.CompletedTask);

            var service = CreateService();

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Tạo đánh giá thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(123, result.Data.RatingId);
            Assert.Equal(5, result.Data.BookingId);
            Assert.Equal("Great court!", result.Data.Comment);
            Assert.Equal(4, result.Data.Stars);
            Assert.True((DateTime.UtcNow - result.Data.CreateAt.ToUniversalTime()).TotalSeconds < 10);
        }

        [Fact(DisplayName = "UTCID02 - Exception returns 500")]
        public async Task UTCID02_Exception_Returns500()
        {
            // Arrange
            var dto = new CreateRatingDto
            {
                BookingId = 6,
                Comment = "Bad service",
                Stars = 1
            };

            _repoMock.Setup(x => x.AddAsync(It.IsAny<Rating>()))
                .ThrowsAsync(new Exception("db error"));

            var service = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateAsync(dto));
            Assert.Equal("db error", ex.Message);
        }
    }
}