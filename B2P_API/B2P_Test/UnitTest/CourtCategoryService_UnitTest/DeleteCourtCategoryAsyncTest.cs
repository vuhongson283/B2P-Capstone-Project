using B2P_API.Interface;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;

namespace B2P_Test.UnitTest.CourtCategoryService_UnitTest
{
    public class DeleteCourtCategoryAsyncTest
    {
        private readonly Mock<ICourtCategoryRepository> _categoryRepoMock;
        private readonly CourtCategoryService _service;

        public DeleteCourtCategoryAsyncTest()
        {
            _categoryRepoMock = new Mock<ICourtCategoryRepository>();
            _service = new CourtCategoryService(_categoryRepoMock.Object);
        }

        [Theory(DisplayName = "UTCID01 - Should return 400 for invalid ID or 200 for success")]
        [InlineData(null, 400, MessagesCodes.MSG_81)]                        // Null ID
        [InlineData(0, 400, MessagesCodes.MSG_81)]                           // Zero ID
        [InlineData(-1, 400, MessagesCodes.MSG_81)]                          // Negative ID
        [InlineData(1, 200, MessagesCodes.MSG_85)]                           // Valid ID - Success
        public async Task UTCID01_DeleteScenarios_ReturnsCorrectResponse(
            int? id, int expectedStatus, string expectedMessage)
        {
            // Setup - Mock repository for success case
            if (expectedStatus == 200)
            {
                _categoryRepoMock.Setup(x => x.DeleteCourtCategoryAsync(id!.Value))
                    .ReturnsAsync(true); // Assuming Task<bool> return type
            }

            // Act
            var result = await _service.DeleteCourtCategoryAsync(id);

            // Assert
            Assert.Equal(expectedStatus == 200, result.Success);
            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Null(result.Data);

            // Verify repository calls
            if (expectedStatus == 200)
            {
                _categoryRepoMock.Verify(x => x.DeleteCourtCategoryAsync(id!.Value), Times.Once);
            }
            else
            {
                _categoryRepoMock.Verify(x => x.DeleteCourtCategoryAsync(It.IsAny<int>()), Times.Never);
            }
        }

        [Fact(DisplayName = "UTCID02 - Should return 500 when repository throws exception")]
        public async Task UTCID02_RepositoryException_Returns500()
        {
            // Setup
            var validId = 1;
            _categoryRepoMock.Setup(x => x.DeleteCourtCategoryAsync(validId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.DeleteCourtCategoryAsync(validId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
            Assert.Null(result.Data);

            // Verify repository was called
            _categoryRepoMock.Verify(x => x.DeleteCourtCategoryAsync(validId), Times.Once);
        }
    }
}