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
    public class CreateAsyncTest
    {
        private readonly Mock<IMerchantPaymentRepository> _repoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;

        public CreateAsyncTest()
        {
            _repoMock = new Mock<IMerchantPaymentRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
        }

        [Fact(DisplayName = "UTCID01 - CreateAsync returns success when valid data")]
        public async Task CreateAsync_ReturnsSuccess_WhenValid()
        {
            // Arrange
            var dto = new MerchantPaymentCreateDto
            {
                UserId = 5,
                PaymentMethodId = 2,
                PaymentKey = "payment-key-123",
                StatusId = 1
            };

            _accRepoMock.Setup(x => x.GetByIdAsync(dto.UserId))
                .ReturnsAsync(new User { UserId = dto.UserId });

            var now = DateTime.Now;
            var created = new MerchantPayment
            {
                MerchantPaymentId = 7,
                UserId = dto.UserId,
                PaymentMethodId = dto.PaymentMethodId,
                PaymentKey = dto.PaymentKey,
                StatusId = dto.StatusId,
                CreatedAt = now
            };
            _repoMock.Setup(x => x.AddAsync(It.IsAny<MerchantPayment>()))
                .ReturnsAsync(created);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Created successfully", result.Message);
            Assert.NotNull(result.Data);

            var response = Assert.IsType<MerchantPaymentResponseDto>(result.Data);
            Assert.Equal(7, response.MerchantPaymentId);
            Assert.Equal(dto.UserId, response.UserId);
            Assert.Equal(dto.PaymentMethodId, response.PaymentMethodId);
            Assert.Equal(dto.PaymentKey, response.PaymentKey);
            Assert.Equal(dto.StatusId, response.StatusId);
            Assert.Equal(now, response.CreatedAt);
        }

        [Theory(DisplayName = "UTCID02 - CreateAsync returns 400 if invalid UserId")]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task CreateAsync_ReturnsBadRequest_WhenInvalidUserId(int userId)
        {
            // Arrange
            var dto = new MerchantPaymentCreateDto
            {
                UserId = userId,
                PaymentMethodId = 2,
                PaymentKey = "abc",
                StatusId = 1
            };

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("UserId không tồn tại", result.Message);
        }

        [Theory(DisplayName = "UTCID03 - CreateAsync returns 400 if invalid PaymentMethodId")]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(5)]
        public async Task CreateAsync_ReturnsBadRequest_WhenInvalidPaymentMethodId(int paymentMethodId)
        {
            // Arrange
            var dto = new MerchantPaymentCreateDto
            {
                UserId = 2,
                PaymentMethodId = paymentMethodId,
                PaymentKey = "abc",
                StatusId = 1
            };

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("PaymentMethod không tồn tại", result.Message);
        }

        [Theory(DisplayName = "UTCID04 - CreateAsync returns 400 if PaymentKey is empty or whitespace")]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_ReturnsBadRequest_WhenPaymentKeyEmpty(string paymentKey)
        {
            // Arrange
            var dto = new MerchantPaymentCreateDto
            {
                UserId = 2,
                PaymentMethodId = 1,
                PaymentKey = paymentKey,
                StatusId = 1
            };

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("PaymentKey không được để trống", result.Message);
        }

        [Theory(DisplayName = "UTCID05 - CreateAsync returns 400 if invalid StatusId")]
        [InlineData(0)]
        [InlineData(-2)]
        public async Task CreateAsync_ReturnsBadRequest_WhenInvalidStatusId(int statusId)
        {
            // Arrange
            var dto = new MerchantPaymentCreateDto
            {
                UserId = 2,
                PaymentMethodId = 1,
                PaymentKey = "abc",
                StatusId = statusId
            };

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Status không tồn tại", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - CreateAsync returns 404 if user does not exist")]
        public async Task CreateAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var dto = new MerchantPaymentCreateDto
            {
                UserId = 9,
                PaymentMethodId = 2,
                PaymentKey = "abc",
                StatusId = 1
            };

            _accRepoMock.Setup(x => x.GetByIdAsync(dto.UserId)).ReturnsAsync((User)null);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Người dùng không tồn tại", result.Message);
        }
    }
}