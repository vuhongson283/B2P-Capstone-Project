using B2P_API.DTOs.ImageDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace B2P_Test.UnitTest.ImageService_UnitTest
{
    public class UpdateImageAsyncTests
    {
        private readonly Mock<IImageRepository> _imageRepoMock;
        private readonly Mock<IGoogleDriveService> _driveServiceMock;
        private readonly Mock<ILogger<ImageService>> _loggerMock;
        private readonly ImageService _service;

        public UpdateImageAsyncTests()
        {
            _imageRepoMock = new Mock<IImageRepository>();
            _driveServiceMock = new Mock<IGoogleDriveService>();
            _loggerMock = new Mock<ILogger<ImageService>>();
            _service = new ImageService(_imageRepoMock.Object, _driveServiceMock.Object, _loggerMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should update image metadata successfully without file")]
        public async Task UTCID01_UpdateMetadata_Success()
        {
            // Arrange
            int imageId = 1;
            var request = new UpdateImageRequest
            {
                Order = 2,
                Caption = "New caption"
            };

            var existingImage = new Image
            {
                ImageId = imageId,
                ImageUrl = "https://old-url.com",
                Order = 1,
                Caption = "Old caption"
            };

            _imageRepoMock.Setup(x => x.GetByIdAsync(imageId))
                .ReturnsAsync(existingImage);

            _imageRepoMock.Setup(x => x.UpdateAsync(existingImage))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateImageAsync(imageId, request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Update Image Successfully!!!", result.Message);

            // Kiểm tra dữ liệu đã được cập nhật trên entity
            Assert.Equal(2, existingImage.Order);
            Assert.Equal("New caption", existingImage.Caption);

            // Kiểm tra các trường không thay đổi
            Assert.Equal("https://old-url.com", existingImage.ImageUrl);

            _driveServiceMock.Verify(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
        }

        [Fact(DisplayName = "UTCID02 - Should update image with new file successfully")]
        public async Task UTCID02_UpdateWithFile_Success()
        {
            // Arrange
            int imageId = 1;
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("new-image.jpg");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var request = new UpdateImageRequest
            {
                File = mockFile.Object,
                Order = 3
            };

            var existingImage = new Image
            {
                ImageId = imageId,
                ImageUrl = "https://old-url.com"
            };

            _imageRepoMock.Setup(x => x.GetByIdAsync(imageId))
                .ReturnsAsync(existingImage);

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync("new-file-id");

            _driveServiceMock.Setup(x => x.CreatePublicLinkAsync("new-file-id"))
                .ReturnsAsync("https://new-url.com");

            _imageRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Image>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateImageAsync(imageId, request);

            // Assert
            Assert.True(result.Success);

            // Sử dụng Newtonsoft.Json để parse dynamic object
            var json = JsonConvert.SerializeObject(result.Data);
            var data = JsonConvert.DeserializeObject<dynamic>(json);

            Assert.Equal("https://new-url.com", data.imageUrl.ToString());
            Assert.Equal(3, (int)data.order);

            _driveServiceMock.Verify(x => x.UploadImageAsync(It.IsAny<byte[]>(),
                It.Is<string>(name => name.StartsWith($"updated_{imageId}_"))), Times.Once);
        }

        [Fact(DisplayName = "UTCID03 - Should return not found when image doesn't exist")]
        public async Task UTCID03_ImageNotFound_ReturnsNotFound()
        {
            // Arrange
            int imageId = 999;
            var request = new UpdateImageRequest();

            _imageRepoMock.Setup(x => x.GetByIdAsync(imageId))
                .ReturnsAsync((Image)null);

            // Act
            var result = await _service.UpdateImageAsync(imageId, request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Image not found", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Should handle Google Drive upload failure")]
        public async Task UTCID04_DriveUploadFails_ReturnsError()
        {
            // Arrange
            int imageId = 1;
            var mockFile = new Mock<IFormFile>();
            var request = new UpdateImageRequest { File = mockFile.Object };

            _imageRepoMock.Setup(x => x.GetByIdAsync(imageId))
                .ReturnsAsync(new Image());

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Drive upload failed"));

            // Act
            var result = await _service.UpdateImageAsync(imageId, request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Drive upload failed", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Should handle database update failure")]
        public async Task UTCID05_DatabaseUpdateFails_ReturnsError()
        {
            // Arrange
            int imageId = 1;
            var request = new UpdateImageRequest { Order = 2 };

            _imageRepoMock.Setup(x => x.GetByIdAsync(imageId))
                .ReturnsAsync(new Image());

            _imageRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Image>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateImageAsync(imageId, request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Update failed", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Should update only caption when provided")]
        public async Task UTCID06_UpdateCaptionOnly_Success()
        {
            // Arrange
            int imageId = 1;
            var request = new UpdateImageRequest
            {
                Caption = "Updated caption"
            };

            var existingImage = new Image
            {
                ImageId = imageId,
                Caption = "Original caption",
                Order = 1,
                ImageUrl = "https://existing.com"
            };

            _imageRepoMock.Setup(x => x.GetByIdAsync(imageId))
                .ReturnsAsync(existingImage);

            _imageRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Image>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateImageAsync(imageId, request);

            // Assert
            Assert.True(result.Success);
            var data = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Data));
            Assert.Equal("Updated caption", data.caption.ToString());
            Assert.Equal(1, (int)data.order);
            Assert.Equal("https://existing.com", data.imageUrl.ToString());
        }

        [Fact(DisplayName = "UTCID07 - Should handle public link creation failure")]
        public async Task UTCID07_LinkCreationFails_ReturnsError()
        {
            // Arrange
            int imageId = 1;
            var mockFile = new Mock<IFormFile>();
            var request = new UpdateImageRequest { File = mockFile.Object };

            _imageRepoMock.Setup(x => x.GetByIdAsync(imageId))
                .ReturnsAsync(new Image());

            _driveServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync("file-id");

            _driveServiceMock.Setup(x => x.CreatePublicLinkAsync("file-id"))
                .ThrowsAsync(new Exception("Link creation failed"));

            // Act
            var result = await _service.UpdateImageAsync(imageId, request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Link creation failed", result.Message);
        }

    }
}