using System.Threading.Tasks;
using Moq;
using Xunit;
using B2P_API.DTOs.SliderDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;

namespace B2P_Test.UnitTest.SliderManagementService_UnitTest
{
    public class CreateSliderAsyncTest
    {
        private readonly Mock<ISliderManagementRepository> _repoMock = new();

        private SliderManagementService CreateService()
            => new SliderManagementService(_repoMock.Object);

        [Fact(DisplayName = "UTCID01 - Create slider successfully returns 201 and slider id")]
        public async Task UTCID01_CreateSlider_Success()
        {
            // Arrange
            var request = new CreateSliderRequest
            {
                SlideUrl = "test-url",
                SlideDescription = "test description"
            };
            var createdSlider = new Slider
            {
                SlideId = 88,
                SlideUrl = "test-url",
                SlideDescription = "test description",
                StatusId = 1
            };
            _repoMock.Setup(x => x.CreateSliderAsync(It.IsAny<Slider>())).ReturnsAsync(createdSlider);

            var service = CreateService();

            // Act
            var result = await service.CreateSliderAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Tạo slider thành công.", result.Message);
            Assert.Equal("88", result.Data);
            _repoMock.Verify(x => x.CreateSliderAsync(It.Is<Slider>(
                s => s.SlideUrl == request.SlideUrl
                  && s.SlideDescription == request.SlideDescription
                  && s.StatusId == 1
            )), Times.Once);
        }
    }
}