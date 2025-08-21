using System.Threading.Tasks;
using Xunit;
using Moq;
using B2P_API.Services;
using B2P_API.DTOs.MerchantPaymentDTO;
using B2P_API.Models;
using B2P_API.Interface;

namespace B2P_Test.UnitTest.MerchantPaymentService_UnitTest
{
    public class UpdateAsyncTest
    {
        private readonly Mock<IMerchantPaymentRepository> _repoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;

        public UpdateAsyncTest()
        {
            _repoMock = new Mock<IMerchantPaymentRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
        }

        [Fact(DisplayName = "UTCID01 - UpdateAsync returns success when valid data")]
        public async Task UpdateAsync_ReturnsSuccess_WhenValid()
        {
            // Arrange
            int id = 2;
            var entity = new MerchantPayment
            {
                MerchantPaymentId = id,
                PaymentKey = "old-key",
                StatusId = 1
            };
            var dto = new MerchantPaymentUpdateDto
            {
                PaymentKey = "new-key",
                StatusId = 5
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _repoMock.Setup(x => x.UpdateAsync(It.IsAny<MerchantPayment>())).Returns(Task.CompletedTask);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.UpdateAsync(id, dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Updated successfully", result.Message);
            Assert.True(result.Data);

            // Check that entity was updated
            Assert.Equal("new-key", entity.PaymentKey);
            Assert.Equal(5, entity.StatusId);

            _repoMock.Verify(x => x.UpdateAsync(entity), Times.Once);
        }

        [Fact(DisplayName = "UTCID02 - UpdateAsync returns 404 when entity not found")]
        public async Task UpdateAsync_ReturnsNotFound_WhenEntityNotFound()
        {
            // Arrange
            int id = 100;
            var dto = new MerchantPaymentUpdateDto
            {
                PaymentKey = "x",
                StatusId = 2
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((MerchantPayment)null);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.UpdateAsync(id, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("MerchantPayment không tồn tại", result.Message);
            Assert.False(result.Data);

            _repoMock.Verify(x => x.UpdateAsync(It.IsAny<MerchantPayment>()), Times.Never);
        }

        [Fact(DisplayName = "UTCID03 - UpdateAsync returns 400 if PaymentKey is empty or whitespace")]
        public async Task UpdateAsync_ReturnsBadRequest_WhenPaymentKeyInvalid()
        {
            // Arrange
            int id = 3;
            var entity = new MerchantPayment
            {
                MerchantPaymentId = id,
                PaymentKey = "old-key",
                StatusId = 1
            };
            var dto = new MerchantPaymentUpdateDto
            {
                PaymentKey = "   ", // Invalid
                StatusId = 3
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.UpdateAsync(id, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("PaymentKey không họưp lệ", result.Message);
            Assert.False(result.Data);

            _repoMock.Verify(x => x.UpdateAsync(It.IsAny<MerchantPayment>()), Times.Never);
        }

        [Theory(DisplayName = "UTCID04 - UpdateAsync returns 400 if StatusId invalid")]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(11)]
        public async Task UpdateAsync_ReturnsBadRequest_WhenStatusIdInvalid(int invalidStatusId)
        {
            // Arrange
            int id = 4;
            var entity = new MerchantPayment
            {
                MerchantPaymentId = id,
                PaymentKey = "some-key",
                StatusId = 1
            };
            var dto = new MerchantPaymentUpdateDto
            {
                PaymentKey = "new-key",
                StatusId = invalidStatusId
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.UpdateAsync(id, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("StatusId phải từ 1 đến 10", result.Message);
            Assert.False(result.Data);

            _repoMock.Verify(x => x.UpdateAsync(It.IsAny<MerchantPayment>()), Times.Never);
        }

        [Fact(DisplayName = "UTCID05 - UpdateAsync returns 400 if only PaymentKey is provided and StatusId is missing (default 0)")]
        public async Task UpdateAsync_ReturnsBadRequest_WhenOnlyPaymentKeyProvidedAndStatusIdDefault0()
        {
            // Arrange
            int id = 5;
            var entity = new MerchantPayment
            {
                MerchantPaymentId = id,
                PaymentKey = "old-key",
                StatusId = 4
            };
            var dto = new MerchantPaymentUpdateDto
            {
                PaymentKey = "updated-key"
                // StatusId mặc định là 0
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.UpdateAsync(id, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("StatusId phải từ 1 đến 10", result.Message);
            Assert.False(result.Data);

            _repoMock.Verify(x => x.UpdateAsync(It.IsAny<MerchantPayment>()), Times.Never);
        }

        [Fact(DisplayName = "UTCID06 - UpdateAsync updates only StatusId when only StatusId is provided")]
        public async Task UpdateAsync_UpdatesOnlyStatusId()
        {
            // Arrange
            int id = 6;
            var entity = new MerchantPayment
            {
                MerchantPaymentId = id,
                PaymentKey = "key-6",
                StatusId = 5
            };
            var dto = new MerchantPaymentUpdateDto
            {
                PaymentKey = null,
                StatusId = 7
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _repoMock.Setup(x => x.UpdateAsync(It.IsAny<MerchantPayment>())).Returns(Task.CompletedTask);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.UpdateAsync(id, dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.True(result.Data);

            Assert.Equal("key-6", entity.PaymentKey);
            Assert.Equal(7, entity.StatusId);

            _repoMock.Verify(x => x.UpdateAsync(entity), Times.Once);
        }
    }
}