using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using B2P_API.Services;
using B2P_API.DTOs.MerchantPaymentDTO;
using B2P_API.Models;
using B2P_API.Interface;

namespace B2P_Test.UnitTest.MerchantPaymentService_UnitTest
{
    public class GetByUserIdAsyncTest
    {
        private readonly Mock<IMerchantPaymentRepository> _repoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;

        public GetByUserIdAsyncTest()
        {
            _repoMock = new Mock<IMerchantPaymentRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
        }

        [Fact(DisplayName = "UTCID01 - GetByUserIdAsync returns list of DTO when data exists")]
        public async Task GetByUserIdAsync_ReturnsList_WhenDataExists()
        {
            // Arrange
            int userId = 5;
            var list = new List<MerchantPayment>
            {
                new MerchantPayment
                {
                    MerchantPaymentId = 1,
                    UserId = userId,
                    PaymentMethodId = 2,
                    PaymentMethod = new PaymentMethod { PaymentMethodId = 2, Description = "Ví điện tử" },
                    PaymentKey = "key1",
                    StatusId = 1,
                    Status = new Status { StatusId = 1, StatusName = "Đã xác nhận" },
                    CreatedAt = new DateTime(2025, 8, 1, 10, 0, 0)
                },
                new MerchantPayment
                {
                    MerchantPaymentId = 2,
                    UserId = userId,
                    PaymentMethodId = 3,
                    PaymentMethod = new PaymentMethod { PaymentMethodId = 3, Description = "Ngân hàng" },
                    PaymentKey = "key2",
                    StatusId = 2,
                    Status = new Status { StatusId = 2, StatusName = "Đã huỷ" },
                    CreatedAt = new DateTime(2025, 8, 2, 11, 30, 0)
                }
            };

            _repoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(list);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.GetByUserIdAsync(userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Get by userId successfully", result.Message);
            Assert.NotNull(result.Data);

            var dtoList = result.Data.ToList();
            Assert.Equal(2, dtoList.Count);

            Assert.Equal(1, dtoList[0].MerchantPaymentId);
            Assert.Equal(userId, dtoList[0].UserId);
            Assert.Equal(2, dtoList[0].PaymentMethodId);
            Assert.Equal("Ví điện tử", dtoList[0].PaymentMethodName);
            Assert.Equal("key1", dtoList[0].PaymentKey);
            Assert.Equal(1, dtoList[0].StatusId);
            Assert.Equal("Đã xác nhận", dtoList[0].StatusName);
            Assert.Equal(new DateTime(2025, 8, 1, 10, 0, 0), dtoList[0].CreatedAt);

            Assert.Equal(2, dtoList[1].MerchantPaymentId);
            Assert.Equal(userId, dtoList[1].UserId);
            Assert.Equal(3, dtoList[1].PaymentMethodId);
            Assert.Equal("Ngân hàng", dtoList[1].PaymentMethodName);
            Assert.Equal("key2", dtoList[1].PaymentKey);
            Assert.Equal(2, dtoList[1].StatusId);
            Assert.Equal("Đã huỷ", dtoList[1].StatusName);
            Assert.Equal(new DateTime(2025, 8, 2, 11, 30, 0), dtoList[1].CreatedAt);
        }

        [Fact(DisplayName = "UTCID02 - GetByUserIdAsync returns empty list when no data")]
        public async Task GetByUserIdAsync_ReturnsEmptyList_WhenNoData()
        {
            // Arrange
            int userId = 10;
            _repoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(new List<MerchantPayment>());

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.GetByUserIdAsync(userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Get by userId successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }
    }
}