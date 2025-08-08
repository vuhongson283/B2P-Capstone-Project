using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.ImageService_UnitTest
{
    public class UploadImageAsyncTests
    {
        private readonly Mock<IImageRepository> _imageRepoMock;
        private readonly Mock<IGoogleDriveService> _driveServiceMock;
        private readonly Mock<ILogger<ImageService>> _loggerMock;
        private readonly ImageService _service;

        public UploadImageAsyncTests()
        {
            _imageRepoMock = new Mock<IImageRepository>();
            _driveServiceMock = new Mock<IGoogleDriveService>();
            _loggerMock = new Mock<ILogger<ImageService>>();
            _service = new ImageService(_imageRepoMock.Object, _driveServiceMock.Object, _loggerMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should upload image successfully")]
        public async Task UTCID01_UploadImage_Success()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            const string imageType = "facility";
            const int entityId = 1;
            const string fileId = "drive_file_id";
            const string imageUrl = "https://drive.google.com/test.jpg";
            const int nextOrder = 1;

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(fileId);

            _driveServiceMock.Setup(x => x.CreatePublicLinkAsync(fileId))
                .ReturnsAsync(imageUrl);

            _imageRepoMock.Setup(x => x.GetNextOrderAsync(imageType, entityId))
                .ReturnsAsync(nextOrder);

            _imageRepoMock.Setup(x => x.CreateAsync(It.IsAny<Image>()))
                .ReturnsAsync(new Image
                {
                    ImageId = 1,
                    ImageUrl = imageUrl,
                    Order = nextOrder,
                    FacilityId = entityId
                });

            // Act
            var result = await _service.UploadImageAsync(mockFile.Object, imageType, entityId);

            // Assert
            Assert.Equal(1, result.ImageId);
            Assert.Equal(imageUrl, result.ImageUrl);
            Assert.Equal(nextOrder, result.Order);
            Assert.Equal("Image uploaded successfully", result.Message);

            _driveServiceMock.Verify(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
            _driveServiceMock.Verify(x => x.CreatePublicLinkAsync(fileId), Times.Once);
            _imageRepoMock.Verify(x => x.GetNextOrderAsync(imageType, entityId), Times.Once);
            _imageRepoMock.Verify(x => x.CreateAsync(It.IsAny<Image>()), Times.Once);
        }

        [Fact(DisplayName = "UTCID02 - Should handle file upload failure")]
        public async Task UTCID02_FileUploadFails_ThrowsException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("File copy failed"));

            const string imageType = "facility";
            const int entityId = 1;

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.UploadImageAsync(mockFile.Object, imageType, entityId));

            // Assert
            Assert.Equal("File copy failed", ex.Message);

            // Verify logging was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error uploading image")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact(DisplayName = "UTCID03 - Should handle Google Drive upload failure")]
        public async Task UTCID03_DriveUploadFails_ThrowsException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            const string imageType = "facility";
            const int entityId = 1;

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Drive upload failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _service.UploadImageAsync(mockFile.Object, imageType, entityId));

            // Verify logging was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error uploading image")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact(DisplayName = "UTCID04 - Should handle database save failure")]
        public async Task UTCID04_DatabaseSaveFails_ThrowsException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            const string imageType = "facility";
            const int entityId = 1;
            const string fileId = "drive_file_id";
            const string imageUrl = "https://drive.google.com/test.jpg";

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(fileId);

            _driveServiceMock.Setup(x => x.CreatePublicLinkAsync(fileId))
                .ReturnsAsync(imageUrl);

            _imageRepoMock.Setup(x => x.GetNextOrderAsync(imageType, entityId))
                .ReturnsAsync(1);

            _imageRepoMock.Setup(x => x.CreateAsync(It.IsAny<Image>()))
                .ThrowsAsync(new Exception("Database save failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _service.UploadImageAsync(mockFile.Object, imageType, entityId));

            // Verify logging was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error uploading image")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact(DisplayName = "UTCID05 - Should set caption when provided")]
        public async Task UTCID05_WithCaption_SetsCaption()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            const string imageType = "facility";
            const int entityId = 1;
            const string fileId = "drive_file_id";
            const string imageUrl = "https://drive.google.com/test.jpg";
            const string caption = "Test caption";

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(fileId);

            _driveServiceMock.Setup(x => x.CreatePublicLinkAsync(fileId))
                .ReturnsAsync(imageUrl);

            _imageRepoMock.Setup(x => x.GetNextOrderAsync(imageType, entityId))
                .ReturnsAsync(1);

            Image savedImage = null;
            _imageRepoMock.Setup(x => x.CreateAsync(It.IsAny<Image>()))
                .Callback<Image>(img => savedImage = img)
                .ReturnsAsync(new Image { ImageId = 1 });

            // Act
            await _service.UploadImageAsync(mockFile.Object, imageType, entityId, caption);

            // Assert
            Assert.Equal(caption, savedImage.Caption);
        }

        [Theory(DisplayName = "UTCID06 - Should handle different image types")]
        [InlineData("facility", nameof(Image.FacilityId))]
        [InlineData("blog", nameof(Image.BlogId))]
        [InlineData("user", nameof(Image.UserId))]
        [InlineData("slide", nameof(Image.SlideId))]
        public async Task UTCID06_HandleDifferentImageTypes_SetsCorrectEntityId(string imageType, string expectedPropertyName)
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            const int entityId = 1;
            const string fileId = "drive_file_id";
            const string imageUrl = "https://drive.google.com/test.jpg";
            const int nextOrder = 1;

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(fileId);

            _driveServiceMock.Setup(x => x.CreatePublicLinkAsync(fileId))
                .ReturnsAsync(imageUrl);

            _imageRepoMock.Setup(x => x.GetNextOrderAsync(imageType, entityId))
                .ReturnsAsync(nextOrder);

            Image createdImage = null;
            _imageRepoMock.Setup(x => x.CreateAsync(It.IsAny<Image>()))
                .Callback<Image>(img => createdImage = img)
                .ReturnsAsync(new Image { ImageId = 1 });

            // Act
            await _service.UploadImageAsync(mockFile.Object, imageType, entityId);

            // Assert
            Assert.NotNull(createdImage);

            // Kiểm tra property tương ứng được set
            var propertyInfo = typeof(Image).GetProperty(expectedPropertyName);
            Assert.NotNull(propertyInfo);
            Assert.Equal(entityId, (int)propertyInfo.GetValue(createdImage));

            // Kiểm tra các properties khác
            Assert.Equal(imageUrl, createdImage.ImageUrl);
            Assert.Equal(nextOrder, createdImage.Order);

            // Kiểm tra các properties khác null
            var otherProperties = new[] { "FacilityId", "BlogId", "UserId", "SlideId" }
                .Where(p => p != expectedPropertyName);

            foreach (var prop in otherProperties)
            {
                var otherPropInfo = typeof(Image).GetProperty(prop);
                Assert.Null(otherPropInfo.GetValue(createdImage));
            }
        }

        [Fact(DisplayName = "UTCID07 - Should throw exception for invalid image type")]
        public async Task UTCID07_InvalidImageType_ThrowsException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            const string invalidImageType = "invalid_type";
            const int entityId = 1;

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadImageAsync(mockFile.Object, invalidImageType, entityId));

            Assert.Contains("Invalid image type", ex.Message);

            // Verify logging was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error uploading image")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact(DisplayName = "UTCID08 - Should throw exception for invalid image type")]
        public async Task UTCID08_InvalidImageType_ThrowsException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            const string invalidImageType = "invalid_type";
            const int entityId = 1;

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadImageAsync(mockFile.Object, invalidImageType, entityId));

            // Kiểm tra message chứa cả loại không hợp lệ
            Assert.Contains($"Invalid image type: {invalidImageType}", ex.Message);

            // Verify logging was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error uploading image")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Theory(DisplayName = "UTCID09 - Should accept all valid image types")]
        [InlineData("facility")]
        [InlineData("blog")]
        [InlineData("user")]
        [InlineData("slide")]
        [InlineData("FACILITY")] // Test case sensitivity
        [InlineData("Blog")] // Test mixed case
        public async Task UTCID09_ValidImageTypes_ShouldNotThrow(string validImageType)
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            const int entityId = 1;
            const string fileId = "drive_file_id";
            const string imageUrl = "https://drive.google.com/test.jpg";

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(fileId);

            _driveServiceMock.Setup(x => x.CreatePublicLinkAsync(fileId))
                .ReturnsAsync(imageUrl);

            _imageRepoMock.Setup(x => x.GetNextOrderAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(1);

            _imageRepoMock.Setup(x => x.CreateAsync(It.IsAny<Image>()))
                .ReturnsAsync(new Image { ImageId = 1 });

            // Act
            var exception = await Record.ExceptionAsync(() =>
                _service.UploadImageAsync(mockFile.Object, validImageType, entityId));

            // Assert
            Assert.Null(exception); // Đảm bảo không có exception nào được throw
        }
    }
}