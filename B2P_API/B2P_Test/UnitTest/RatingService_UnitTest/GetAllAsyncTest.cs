using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.RatingService_UnitTest
{
    public class GetAllAsyncTest
    {
        private readonly Mock<IRatingRepository> _repoMock = new();

        private RatingService CreateService()
        {
            return new RatingService(_repoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - GetAll returns list of ratings successfully")]
        public async Task UTCID01_GetAll_ReturnsListOfRatingsSuccessfully()
        {
            // Arrange
            var ratings = new List<Rating>
            {
                new Rating
                {
                    RatingId = 1,
                    BookingId = 10,
                    Comment = "Great",
                    CreateAt = DateTime.Now.AddMinutes(-10),
                    Stars = 5
                },
                new Rating
                {
                    RatingId = 2,
                    BookingId = 20,
                    Comment = "Not bad",
                    CreateAt = DateTime.Now.AddMinutes(-5),
                    Stars = 4
                }
            };
            _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(ratings);

            var service = CreateService();

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách đánh giá thành công.", result.Message);
            Assert.NotNull(result.Data);

            var list = result.Data.ToList();
            Assert.Equal(2, list.Count);

            Assert.Equal(1, list[0].RatingId);
            Assert.Equal(10, list[0].BookingId);
            Assert.Equal("Great", list[0].Comment);
            Assert.Equal(5, list[0].Stars);

            Assert.Equal(2, list[1].RatingId);
            Assert.Equal(20, list[1].BookingId);
            Assert.Equal("Not bad", list[1].Comment);
            Assert.Equal(4, list[1].Stars);
        }

        [Fact(DisplayName = "UTCID02 - GetAll returns empty list when no ratings")]
        public async Task UTCID02_GetAll_ReturnsEmptyList()
        {
            // Arrange
            _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Rating>());

            var service = CreateService();

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách đánh giá thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Exception returns 500")]
        public async Task UTCID03_Exception_Returns500()
        {
            // Arrange
            _repoMock.Setup(x => x.GetAllAsync()).ThrowsAsync(new Exception("db error"));
            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.GetAllAsync());
        }
    }
}