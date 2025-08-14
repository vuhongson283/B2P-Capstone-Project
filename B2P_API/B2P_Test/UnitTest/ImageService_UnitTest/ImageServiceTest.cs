using B2P_API.DTOs;
using B2P_API.DTOs.ImageDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.ImageService_UnitTest
{
    public class ImageServiceTest
    {
        private readonly Mock<IImageRepository> _imageRepoMock;
        private readonly Mock<IGoogleDriveService> _driveServiceMock;
        private readonly Mock<ILogger<ImageService>> _loggerMock;
        private readonly ImageService _service;

        public ImageServiceTest()
        {
            _imageRepoMock = new Mock<IImageRepository>();
            _driveServiceMock = new Mock<IGoogleDriveService>();
            _loggerMock = new Mock<ILogger<ImageService>>();

            // Khởi tạo service với đầy đủ dependencies
            _service = new ImageService(
                _imageRepoMock.Object,
                _driveServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetImagesByTypeAndEntityAsync_ShouldReturnImages()
        {
            // Arrange
            var expected = new List<ImageResponseDto>
            {
                new ImageResponseDto { ImageId = 1, ImageUrl = "test.jpg" }
            };

            _imageRepoMock.Setup(x => x.GetByTypeAndEntityIdAsync("facility", 1))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetImagesByTypeAndEntityAsync("facility", 1);

            // Assert
            Assert.Equal(expected, result);
            _imageRepoMock.Verify(x => x.GetByTypeAndEntityIdAsync("facility", 1), Times.Once);
        }

        [Fact]
        public async Task GetFacilityImagesAsync_ShouldReturnImages()
        {
            // Arrange
            var expected = new List<ImageResponseDto>
            {
                new ImageResponseDto { ImageId = 1, ImageUrl = "facility.jpg" }
            };

            _imageRepoMock.Setup(x => x.GetByFacilityIdAsync(1))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetFacilityImagesAsync(1);

            // Assert
            Assert.Equal(expected, result);
            _imageRepoMock.Verify(x => x.GetByFacilityIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetBlogImagesAsync_ShouldReturnImages()
        {
            // Arrange
            var expected = new List<ImageResponseDto>
            {
                new ImageResponseDto { ImageId = 1, ImageUrl = "blog.jpg" }
            };

            _imageRepoMock.Setup(x => x.GetByBlogIdAsync(1))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetBlogImagesAsync(1);

            // Assert
            Assert.Equal(expected, result);
            _imageRepoMock.Verify(x => x.GetByBlogIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetUserImagesAsync_ShouldReturnImages()
        {
            // Arrange
            var expected = new List<ImageResponseDto>
            {
                new ImageResponseDto { ImageId = 1, ImageUrl = "user.jpg" }
            };

            _imageRepoMock.Setup(x => x.GetByUserIdAsync(1))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetUserImagesAsync(1);

            // Assert
            Assert.Equal(expected, result);
            _imageRepoMock.Verify(x => x.GetByUserIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetSlideImagesAsync_ShouldReturnImages()
        {
            // Arrange
            var expected = new List<ImageResponseDto>
            {
                new ImageResponseDto { ImageId = 1, ImageUrl = "slide.jpg" }
            };

            _imageRepoMock.Setup(x => x.GetBySlideIdAsync(1))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetSlideImagesAsync(1);

            // Assert
            Assert.Equal(expected, result);
            _imageRepoMock.Verify(x => x.GetBySlideIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteImageAsync_ShouldReturnTrue_WhenSuccess()
        {
            // Arrange
            _imageRepoMock.Setup(x => x.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteImageAsync(1);

            // Assert
            Assert.True(result);
            _imageRepoMock.Verify(x => x.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteImageAsync_ShouldReturnFalse_WhenFail()
        {
            // Arrange
            _imageRepoMock.Setup(x => x.DeleteAsync(1))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteImageAsync(1);

            // Assert
            Assert.False(result);
            _imageRepoMock.Verify(x => x.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetImagesByTypeAndEntityAsync_ShouldReturnEmpty_WhenNoData()
        {
            // Arrange
            _imageRepoMock.Setup(x => x.GetByTypeAndEntityIdAsync("facility", 1))
                .ReturnsAsync(new List<ImageResponseDto>());

            // Act
            var result = await _service.GetImagesByTypeAndEntityAsync("facility", 1);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFacilityImagesAsync_ShouldThrow_WhenRepoFails()
        {
            // Arrange
            _imageRepoMock.Setup(x => x.GetByFacilityIdAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetFacilityImagesAsync(1));
        }
    }
}