using B2P_API.Models;
using B2P_API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq.Protected;
using Microsoft.AspNetCore.SignalR;
using B2P_API.Interface;
using B2P_API.Hubs;

namespace B2P_Test.UnitTest.ZaloPayService_UnitTest
{
    public class QueryOrderAsyncTest
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<ZaloPayService>> _loggerMock;
        private readonly ZaloPayConfig _config;

        // BookingService dependencies
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly Mock<IHubContext<BookingHub>> _hubContextMock;

        // CommissionPaymentHistoryService dependencies
        private readonly Mock<ICommissionPaymentHistoryRepository> _commissionRepoMock;
        private readonly Mock<IAccountManagementRepository> _commissionAccRepoMock;

        public QueryOrderAsyncTest()
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

        [Fact(DisplayName = "UTCID01 - QueryOrderAsync returns success with valid response")]
        public async Task UTCID01_QueryOrderAsync_ReturnsSuccess()
        {
            // Arrange
            string appTransId = "230820_123456";
            var queryResponse = new QueryOrderResponse
            {
                ReturnCode = 1,
                ReturnMessage = "Success",
                IsProcessing = false,
                Amount = 10000,
                ZpTransId = 987654321,
                DiscountAmount = 0
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().Contains("/v2/query")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(queryResponse))
                });

            var zaloPayService = new ZaloPayService(
                _httpClient,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await zaloPayService.QueryOrderAsync(appTransId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ErrorCode);
            Assert.Equal("Success", result.Message);
            Assert.NotNull(result.Data);
            var resp = Assert.IsType<QueryOrderResponse>(result.Data);
            // Không kiểm tra AppTransId vì không tồn tại
            Assert.Equal(queryResponse.ZpTransId, resp.ZpTransId);
            Assert.Equal(queryResponse.Amount, resp.Amount);
            Assert.Equal(queryResponse.ReturnCode, resp.ReturnCode);
            Assert.Equal(queryResponse.ReturnMessage, resp.ReturnMessage);
            Assert.Equal(queryResponse.IsProcessing, resp.IsProcessing);
            Assert.Equal(queryResponse.DiscountAmount, resp.DiscountAmount);
        }

        [Fact(DisplayName = "UTCID02 - QueryOrderAsync returns failed when HTTP error")]
        public async Task UTCID02_QueryOrderAsync_ReturnsFailedOnHttpError()
        {
            // Arrange
            string appTransId = "230820_123456";
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("")
                });

            var zaloPayService = new ZaloPayService(
                _httpClient,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await zaloPayService.QueryOrderAsync(appTransId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal((int)HttpStatusCode.BadRequest, result.ErrorCode);
            Assert.Contains("HTTP Error", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - QueryOrderAsync returns failed when exception occurs")]
        public async Task UTCID03_QueryOrderAsync_ReturnsFailedOnException()
        {
            // Arrange
            string appTransId = "230820_123456";
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new Exception("Network failure"));

            var zaloPayService = new ZaloPayService(
                _httpClient,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await zaloPayService.QueryOrderAsync(appTransId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(-1, result.ErrorCode);
            Assert.Contains("Exception: Network failure", result.Message);
        }
    }
}