using B2P_API.Models;
using B2P_API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Text.Json;
using Moq.Protected;
using Microsoft.AspNetCore.SignalR;
using B2P_API.Interface;
using B2P_API.Hubs;

namespace B2P_Test.UnitTest.ZaloPayService_UnitTest
{
    public class CreateOrderAsyncTest
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<ZaloPayService>> _loggerMock;
        private readonly ZaloPayConfig _config;

        // For BookingService dependencies
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly Mock<IHubContext<BookingHub>> _hubContextMock;

        // For CommissionPaymentHistoryService dependencies
        private readonly Mock<ICommissionPaymentHistoryRepository> _commissionRepoMock;
        private readonly Mock<IAccountManagementRepository> _commissionAccRepoMock;

        public CreateOrderAsyncTest()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _loggerMock = new Mock<ILogger<ZaloPayService>>();
            _config = new ZaloPayConfig
            {
                AppId = "app_id_test",
                Key1 = "key1_test",
                Endpoint = "https://sandbox.zalopay.com"
            };

            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _hubContextMock = new Mock<IHubContext<BookingHub>>();

            _commissionRepoMock = new Mock<ICommissionPaymentHistoryRepository>();
            _commissionAccRepoMock = new Mock<IAccountManagementRepository>();
        }

        private BookingService CreateBookingService()
        {
            return new BookingService(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            );
        }

        private CommissionPaymentHistoryService CreateCommissionPaymentHistoryService()
        {
            return new CommissionPaymentHistoryService(
                _commissionRepoMock.Object,
                _commissionAccRepoMock.Object
            );
        }

        [Fact(DisplayName = "UTCID01 - CreateOrderAsync returns success with valid response")]
        public async Task UTCID01_CreateOrderAsync_ReturnsSuccess()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                AppUser = "test_user",
                Amount = 10000,
                Description = "Test order",
                Items = new List<OrderItem> { new OrderItem { Name = "item1", Quantity = 1, Price = 10000 } },
                EmbedData = new Dictionary<string, object> { { "bookingid", 123 } },
                CallbackUrl = "https://callback",
                RedirectUrl = "https://redirect"
            };

            var zaloPayResponse = new ZaloPayOrderResponse
            {
                ReturnCode = 1,
                ReturnMessage = "Success",
                OrderUrl = "https://pay.zalopay.vn/order"
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(zaloPayResponse))
                });

            var service = new ZaloPayService(
                _httpClient,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await service.CreateOrderAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ErrorCode);
            Assert.Equal("Success", result.Message);
            Assert.NotNull(result.Data);
            var resp = Assert.IsType<ZaloPayOrderResponse>(result.Data);
            Assert.Equal(zaloPayResponse.OrderUrl, resp.OrderUrl);
        }

        [Fact(DisplayName = "UTCID02 - CreateOrderAsync returns failed when HTTP error")]
        public async Task UTCID02_CreateOrderAsync_ReturnsFailedOnHttpError()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                AppUser = "test_user",
                Amount = 10000,
                Description = "Test order",
                Items = new List<OrderItem> { new OrderItem { Name = "item1", Quantity = 1, Price = 10000 } },
                EmbedData = new Dictionary<string, object> { { "bookingid", 123 } },
                CallbackUrl = "https://callback",
                RedirectUrl = "https://redirect"
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("")
                });

            var service = new ZaloPayService(
                _httpClient,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await service.CreateOrderAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal((int)HttpStatusCode.BadRequest, result.ErrorCode);
            Assert.Contains("HTTP Error", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - CreateOrderAsync returns failed when exception occurs")]
        public async Task UTCID03_CreateOrderAsync_ReturnsFailedOnException()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                AppUser = "test_user",
                Amount = 10000,
                Description = "Test order",
                Items = new List<OrderItem> { new OrderItem { Name = "item1", Quantity = 1, Price = 10000 } },
                EmbedData = new Dictionary<string, object> { { "bookingid", 123 } },
                CallbackUrl = "https://callback",
                RedirectUrl = "https://redirect"
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Network failure"));

            var service = new ZaloPayService(
                _httpClient,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await service.CreateOrderAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(-1, result.ErrorCode);
            Assert.Contains("Exception: Network failure", result.Message);
        }
    }
}