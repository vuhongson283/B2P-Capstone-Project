using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.CourtService_UnitTest
{
    public class DeleteCourtTest
    {
        private readonly Mock<ICourtRepository> _courtRepoMock;
        private readonly CourtServices _service;
        private readonly int _testUserId = 1;
        private readonly int _testCourtId = 1;

        public DeleteCourtTest()
        {
            _courtRepoMock = new Mock<ICourtRepository>();
            _service = new CourtServices(_courtRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should delete court successfully")]
        public async Task UTCID01_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.DeleteCourt(_testCourtId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteCourt(_testUserId, _testCourtId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Xóa sân thành công!", result.Message);
            Assert.True(result.Data);

            // Verify repository was called
            _courtRepoMock.Verify(x => x.DeleteCourt(_testCourtId), Times.Once());
        }

        [Fact(DisplayName = "UTCID02 - Should return error when user is not owner")]
        public async Task UTCID02_NotOwner_ReturnsError()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(false);

            // Act
            var result = await _service.DeleteCourt(_testUserId, _testCourtId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(403, result.Status);
            Assert.Equal("Tài khoản không thể xóa sân này vì không phải tài khoản chủ sân.", result.Message);
            Assert.False(result.Data);

            // Verify delete was not attempted
            _courtRepoMock.Verify(x => x.DeleteCourt(It.IsAny<int>()), Times.Never());
        }

        [Fact(DisplayName = "UTCID03 - Should return not found when court doesn't exist")]
        public async Task UTCID03_CourtNotFound_Returns404()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.DeleteCourt(_testCourtId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteCourt(_testUserId, _testCourtId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy sân.", result.Message);
            Assert.False(result.Data);
        }


        [Fact(DisplayName = "UTCID04 - Should call delete only once")]
        public async Task UTCID04_DeleteCalledOnce()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.DeleteCourt(_testCourtId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteCourt(_testUserId, _testCourtId);

            // Assert
            _courtRepoMock.Verify(x => x.DeleteCourt(_testCourtId), Times.Once());
        }
    }
}