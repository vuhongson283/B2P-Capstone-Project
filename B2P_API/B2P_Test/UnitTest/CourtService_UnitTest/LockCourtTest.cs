using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.CourtService_UnitTest
{
    public class LockCourtTest
    {
        private readonly Mock<ICourtRepository> _courtRepoMock;
        private readonly CourtServices _service;
        private readonly int _testUserId = 1;
        private readonly int _testCourtId = 1;
        private readonly int _testStatusId = 2; // 2 = locked status

        public LockCourtTest()
        {
            _courtRepoMock = new Mock<ICourtRepository>();
            _service = new CourtServices(_courtRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should lock court successfully")]
        public async Task UTCID01_LockCourtSuccess_ReturnsSuccess()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.LockCourt(_testCourtId, _testStatusId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.LockCourt(_testUserId, _testCourtId, _testStatusId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Khóa sân thành công", result.Message);
            Assert.NotNull(result.Data);

            _courtRepoMock.Verify(x => x.LockCourt(_testCourtId, _testStatusId), Times.Once());
        }

        [Fact(DisplayName = "UTCID02 - Should return error when user is not owner")]
        public async Task UTCID02_NotOwner_ReturnsError()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(false);

            // Act
            var result = await _service.LockCourt(_testUserId, _testCourtId, _testStatusId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Tài khoản không thể khóa sân này vì không phải tài khoản chủ sân.", result.Message);
            Assert.Null(result.Data);

            _courtRepoMock.Verify(x => x.LockCourt(It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }

        [Fact(DisplayName = "UTCID03 - Should return not found when court doesn't exist")]
        public async Task UTCID03_CourtNotFound_Returns404()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.LockCourt(_testCourtId, _testStatusId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.LockCourt(_testUserId, _testCourtId, _testStatusId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy sân.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Should handle database errors")]
        public async Task UTCID05_DatabaseError_Returns500()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.LockCourt(_testCourtId, _testStatusId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.LockCourt(_testUserId, _testCourtId, _testStatusId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Đã xảy ra lỗi trong quá trình khóa: Database connection failed", result.Message);
            Assert.Null(result.Data);
        }
    }
}