using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using B2P_API.Services;
using B2P_API.Models;
using B2P_API.Interface;

namespace B2P_Test.UnitTest.MerchantPaymentService_UnitTest
{
    public class DeleteAsyncTest
    {
        private readonly Mock<IMerchantPaymentRepository> _repoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;

        public DeleteAsyncTest()
        {
            _repoMock = new Mock<IMerchantPaymentRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
        }

        [Fact(DisplayName = "UTCID01 - DeleteAsync returns success when entity exists")]
        public async Task DeleteAsync_ReturnsSuccess_WhenEntityExists()
        {
            // Arrange
            int id = 10;
            var entity = new MerchantPayment
            {
                MerchantPaymentId = id
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _repoMock.Setup(x => x.DeleteAsync(entity)).Returns(Task.CompletedTask);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.DeleteAsync(id);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.True(result.Data);
            Assert.Equal("Deleted successfully", result.Message);

            // Kiểm tra repository gọi đúng method
            _repoMock.Verify(x => x.DeleteAsync(entity), Times.Once);
        }

        [Fact(DisplayName = "UTCID02 - DeleteAsync returns 404 when entity not found")]
        public async Task DeleteAsync_ReturnsNotFound_WhenEntityNotFound()
        {
            // Arrange
            int id = 99;
            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((MerchantPayment)null);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.DeleteAsync(id);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.False(result.Data);
            Assert.Equal("Not found", result.Message);
            _repoMock.Verify(x => x.DeleteAsync(It.IsAny<MerchantPayment>()), Times.Never);
        }
    }
}