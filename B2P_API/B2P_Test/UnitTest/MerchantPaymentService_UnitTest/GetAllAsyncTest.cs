using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using B2P_API.Services;
using B2P_API.Models;
using B2P_API.Interface;

namespace B2P_Test.UnitTest.MerchantPaymentService_UnitTest
{
    public class GetAllAsyncTest
    {
        private readonly Mock<IMerchantPaymentRepository> _repoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;

        public GetAllAsyncTest()
        {
            _repoMock = new Mock<IMerchantPaymentRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
        }

        [Fact(DisplayName = "UTCID01 - GetAllAsync returns all merchant payments as DTOs")]
        public async Task GetAllAsync_ReturnsAllMerchantPaymentsAsDtos()
        {
            // Arrange
            var entities = new List<MerchantPayment>
            {
                new MerchantPayment
                {
                    MerchantPaymentId = 1,
                    UserId = 5,
                    PaymentMethodId = 2,
                    PaymentKey = "key1",
                    StatusId = 1,
                    CreatedAt = new DateTime(2024, 8, 1)
                },
                new MerchantPayment
                {
                    MerchantPaymentId = 2,
                    UserId = 7,
                    PaymentMethodId = 3,
                    PaymentKey = "key2",
                    StatusId = 2,
                    CreatedAt = new DateTime(2024, 8, 2)
                }
            };

            _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(entities);

            var service = new MerchantPaymentService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var response = await service.GetAllAsync();

            // Assert
            Assert.True(response.Success);
            Assert.Equal(200, response.Status);
            Assert.Equal("Get all successfully", response.Message);
            Assert.NotNull(response.Data);

            var list = response.Data.ToList();
            Assert.Equal(2, list.Count);

            Assert.Equal(1, list[0].MerchantPaymentId);
            Assert.Equal(5, list[0].UserId);
            Assert.Equal(2, list[0].PaymentMethodId);
            Assert.Equal("key1", list[0].PaymentKey);
            Assert.Equal(1, list[0].StatusId);
            Assert.Equal(new DateTime(2024, 8, 1), list[0].CreatedAt);

            Assert.Equal(2, list[1].MerchantPaymentId);
            Assert.Equal(7, list[1].UserId);
            Assert.Equal(3, list[1].PaymentMethodId);
            Assert.Equal("key2", list[1].PaymentKey);
            Assert.Equal(2, list[1].StatusId);
            Assert.Equal(new DateTime(2024, 8, 2), list[1].CreatedAt);
        }

    }
}