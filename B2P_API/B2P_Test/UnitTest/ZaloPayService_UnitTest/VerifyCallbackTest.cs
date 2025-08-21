using B2P_API.DTOs.CommissionPaymentHistoryDTOs;
using B2P_API.Hubs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.ZaloPayService_UnitTest
{
    public class VerifyCallbackTest
    {
        private readonly Mock<ILogger<ZaloPayService>> _loggerMock = new();
        private readonly ZaloPayConfig _config = new()
        {
            AppId = "app_id_test",
            Key1 = "key1_test",
            Endpoint = "https://sandbox.zalopay.com"
        };

        private readonly Mock<IBookingRepository> _bookingRepoMock = new();
        private readonly Mock<IAccountManagementRepository> _accRepoMock = new();
        private readonly Mock<IAccountRepository> _accRepo2Mock = new();
        private readonly Mock<IHubContext<BookingHub>> _hubContextMock = new();

        private readonly Mock<ICommissionPaymentHistoryRepository> _commissionRepoMock = new();
        private readonly Mock<IAccountManagementRepository> _commissionAccRepoMock = new();

        private BookingService CreateBookingService()
        {
            return new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true }.Object;
        }

        private CommissionPaymentHistoryService CreateCommissionPaymentHistoryService()
        {
            return new Mock<CommissionPaymentHistoryService>(
                _commissionRepoMock.Object,
                _commissionAccRepoMock.Object
            )
            { CallBase = true }.Object;
        }

        [Fact(DisplayName = "UTCID01 - VerifyCallback returns success for valid booking callback")]
        public async Task UTCID01_VerifyCallback_ReturnsSuccess_ForBooking()
        {
            // Arrange
            var callbackDataObj = new CallbackData
            {
                AppId = 2553,
                AppTransId = "230820_123456",
                AppTime = 1692524800000,
                AppUser = "test_user",
                Amount = 10000,
                EmbedData = JsonSerializer.Serialize(new Dictionary<string, object> { { "bookingid", 123 } }),
                Item = "[]",
                ZpTransId = 987654321,
                ServerTime = 1692524800000,
                Channel = 1,
                MerchantUserId = "muser",
                ZpUserId = "zuser",
                UserFeeAmount = 0,
                DiscountAmount = 0
            };

            // Serialize object thành JSON
            var jsonString = JsonSerializer.Serialize(callbackDataObj);
            // Escape JSON string (như cách ZaloPay gửi)
            var escapedJsonString = JsonSerializer.Serialize(jsonString);
            // Remove quotes bao quanh để có được escaped string thuần túy
            var dataParameter = escapedJsonString.Trim('"');

            var bookingServiceMock = new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true };

            bookingServiceMock.Setup(x => x.MarkBookingPaidAsync(123, "987654321"))
                .ReturnsAsync(new ApiResponse<string> { Success = true })
                .Verifiable();

            var zaloPayService = new ZaloPayService(
                null,
                bookingServiceMock.Object,
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await zaloPayService.VerifyCallback(dataParameter, "any-mac");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Callback verified successfully", result.Message);
            Assert.NotNull(result.Data);
            var cbData = Assert.IsType<CallbackData>(result.Data);
            Assert.Equal("230820_123456", cbData.AppTransId);
            Assert.Equal(987654321, cbData.ZpTransId);
            bookingServiceMock.Verify(x => x.MarkBookingPaidAsync(123, "987654321"), Times.Once);
        }

        [Fact(DisplayName = "UTCID02 - VerifyCallback returns success for valid commission callback")]
        public async Task UTCID02_VerifyCallback_ReturnsSuccess_ForCommission()
        {
            // Arrange
            var callbackDataObj = new CallbackData
            {
                AppId = 2553,
                AppTransId = "230820_123457",
                AppTime = 1692524800000,
                AppUser = "test_user",
                Amount = 10000,
                EmbedData = JsonSerializer.Serialize(new Dictionary<string, object> { { "commissionid", 456 } }),
                Item = "[]",
                ZpTransId = 987654322,
                ServerTime = 1692524800000,
                Channel = 1,
                MerchantUserId = "muser",
                ZpUserId = "zuser",
                UserFeeAmount = 0,
                DiscountAmount = 0
            };

            var jsonString = JsonSerializer.Serialize(callbackDataObj);
            var escapedJsonString = JsonSerializer.Serialize(jsonString);
            var dataParameter = escapedJsonString.Trim('"');

            var commissionServiceMock = new Mock<CommissionPaymentHistoryService>(
                _commissionRepoMock.Object,
                _commissionAccRepoMock.Object
            )
            { CallBase = true };

            commissionServiceMock
                .Setup(x => x.UpdateAsync(456, It.Is<CommissionPaymentHistoryUpdateDto>(dto =>
                    dto.StatusId == 7 && dto.Note.Contains("987654322"))))
                .ReturnsAsync(new ApiResponse<object> { Success = true })
                .Verifiable();

            var zaloPayService = new ZaloPayService(
                null,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                commissionServiceMock.Object
            );

            // Act
            var result = await zaloPayService.VerifyCallback(dataParameter, "any-mac");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Callback verified successfully", result.Message);
            Assert.NotNull(result.Data);
            var cbData = Assert.IsType<CallbackData>(result.Data);
            Assert.Equal("230820_123457", cbData.AppTransId);
            Assert.Equal(987654322, cbData.ZpTransId);
            commissionServiceMock.Verify(x => x.UpdateAsync(456, It.IsAny<CommissionPaymentHistoryUpdateDto>()), Times.Once);
        }

        [Fact(DisplayName = "UTCID03 - VerifyCallback returns failed when JSON escape error")]
        public async Task UTCID03_VerifyCallback_ReturnsFailedOnJsonEscapeError()
        {
            // Arrange - JSON không hợp lệ khi unescape
            var invalidEscapedData = "invalid_json_without_proper_escaping";

            var zaloPayService = new ZaloPayService(
                null,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await zaloPayService.VerifyCallback(invalidEscapedData, "any-mac");

            // Assert
            Assert.False(result.Success);
            // Expect exception message, not specific "Invalid JSON format (escape issue)"
            Assert.Contains("Exception:", result.Message);
            Assert.Equal(-1, result.ErrorCode);
        }

        [Fact(DisplayName = "UTCID04 - VerifyCallback returns success when neither bookingid nor commissionid in embed_data")]
        public async Task UTCID04_VerifyCallback_ReturnsSuccess_WhenNoBookingOrCommissionId()
        {
            // Arrange
            var callbackDataObj = new CallbackData
            {
                AppId = 2553,
                AppTransId = "230820_123458",
                AppTime = 1692524800000,
                AppUser = "test_user",
                Amount = 10000,
                EmbedData = JsonSerializer.Serialize(new Dictionary<string, object> { { "other", 789 } }),
                Item = "[]",
                ZpTransId = 987654323,
                ServerTime = 1692524800000,
                Channel = 1,
                MerchantUserId = "muser",
                ZpUserId = "zuser",
                UserFeeAmount = 0,
                DiscountAmount = 0
            };

            var jsonString = JsonSerializer.Serialize(callbackDataObj);
            var escapedJsonString = JsonSerializer.Serialize(jsonString);
            var dataParameter = escapedJsonString.Trim('"');

            var zaloPayService = new ZaloPayService(
                null,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await zaloPayService.VerifyCallback(dataParameter, "any-mac");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Callback verified successfully", result.Message);
            Assert.NotNull(result.Data);
            var cbData = Assert.IsType<CallbackData>(result.Data);
            Assert.Equal("230820_123458", cbData.AppTransId);
            Assert.Equal(987654323, cbData.ZpTransId);
        }

        [Fact(DisplayName = "UTCID05 - VerifyCallback returns success for sample JSON from document")]
        public async Task UTCID05_VerifyCallback_ReturnsSuccess_ForSampleJsonFromDoc()
        {
            // Arrange - convert sample data thành escaped format
            var sampleDataJson = """{"app_id":2553,"app_trans_id":"250820_145187","app_time":1755703540632,"app_user":"string","amount":5000000,"embed_data":"{\"additionalProp1\":\"string\",\"additionalProp2\":\"string\",\"additionalProp3\":\"string\"}","item":"[{\"Name\":\"string\",\"Quantity\":0,\"Price\":0,\"Description\":\"string\"}]","zp_trans_id":250820000024753,"server_time":1755703587395,"channel":36,"merchant_user_id":"DXp3vtfyygtb2_XdP8yFeA","zp_user_id":"DXp3vtfyygtb2_XdP8yFeA","user_fee_amount":0,"discount_amount":0}""";

            // Escape the JSON string như cách ZaloPay gửi
            var escapedJsonString = JsonSerializer.Serialize(sampleDataJson);
            var dataParameter = escapedJsonString.Trim('"');

            var zaloPayService = new ZaloPayService(
                null,
                CreateBookingService(),
                Options.Create(_config),
                _loggerMock.Object,
                CreateCommissionPaymentHistoryService()
            );

            // Act
            var result = await zaloPayService.VerifyCallback(dataParameter, "33af7d21885ae5d8836c75a129b324974cf7ab94ffe5731f6467b55d0d00eabc");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Callback verified successfully", result.Message);
            Assert.NotNull(result.Data);
            var cbData = Assert.IsType<CallbackData>(result.Data);
            Assert.Equal("250820_145187", cbData.AppTransId);
            Assert.Equal(250820000024753, cbData.ZpTransId);
        }
    }
}