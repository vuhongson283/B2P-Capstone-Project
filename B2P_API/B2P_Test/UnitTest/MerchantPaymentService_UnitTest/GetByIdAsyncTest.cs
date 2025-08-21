using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using B2P_API.Services;
using B2P_API.DTOs.MerchantPaymentDTO;
using B2P_API.Models;
using B2P_API.Interface;

namespace B2P_Test.UnitTest.MerchantPaymentService_UnitTest
{
    public class GetByIdAsyncTest
    {
        private readonly Mock<IMerchantPaymentRepository> _repoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;

        public GetByIdAsyncTest()
        {
            _repoMock = new Mock<IMerchantPaymentRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
        }

        [Fact(DisplayName = "UTCID01 - GetByIdAsync returns DTO when entity exists")]
        public async Task GetByIdAsync_ReturnsDto_WhenEntityExists()
        {
            // Arrange
            int id = 15;
            var entity = new MerchantPayment
            {
                MerchantPaymentId = id,
                UserId = 7,
                PaymentMethodId = 2,
                PaymentKey = "pm-key-xyz",
                StatusId = 1,
                CreatedAt = new DateTime(2025, 8, 20, 10, 0, 0)
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.GetByIdAsync(id);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Get by id successfully", result.Message);
            Assert.NotNull(result.Data);

            var dto = Assert.IsType<MerchantPaymentResponseDto>(result.Data);
            Assert.Equal(entity.MerchantPaymentId, dto.MerchantPaymentId);
            Assert.Equal(entity.UserId, dto.UserId);
            Assert.Equal(entity.PaymentMethodId, dto.PaymentMethodId);
            Assert.Equal(entity.PaymentKey, dto.PaymentKey);
            Assert.Equal(entity.StatusId, dto.StatusId);
            Assert.Equal(entity.CreatedAt, dto.CreatedAt);
        }

        [Fact(DisplayName = "UTCID02 - GetByIdAsync returns 404 when not found")]
        public async Task GetByIdAsync_ReturnsNotFound_WhenEntityNotFound()
        {
            // Arrange
            int id = 999;
            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((MerchantPayment)null);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.GetByIdAsync(id);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Not found", result.Message);
            Assert.Null(result.Data);
        }
    }
}