using B2P_API.Services;
using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace B2P_Test.UnitTest.ZaloPayService_UnitTest
{
    public class ZaloPayServiceHelperTest
    {
        [Fact(DisplayName = "GenerateAppTransId returns correct format")]
        public void GenerateAppTransId_ReturnsCorrectFormat()
        {
            // Arrange
            // Use reflection to access private method
            var service = typeof(ZaloPayService);
            var method = service.GetMethod("GenerateAppTransId", BindingFlags.NonPublic | BindingFlags.Instance);

            // Use a dummy ZaloPayService instance (constructor params can be default/null since we won't use them)
            var dummy = (ZaloPayService)FormatterServices.GetUninitializedObject(typeof(ZaloPayService));

            // Act
            var result = (string)method.Invoke(dummy, null);

            // Assert
            // Format: yyMMdd_XXXXXX
            Assert.Matches(@"^\d{6}_\d{6}$", result);

            var parts = result.Split('_');
            Assert.Equal(2, parts.Length);
            Assert.Equal(DateTime.Now.ToString("yyMMdd"), parts[0]);
            Assert.True(int.TryParse(parts[1], out int randomPart));
            Assert.InRange(randomPart, 100000, 999999);
        }

        [Fact(DisplayName = "CreateHmacSha256 returns correct hash")]
        public void CreateHmacSha256_ReturnsCorrectHash()
        {
            // Arrange
            var message = "test-message";
            var secret = "test-secret";
            var expected = ComputeHmacSha256(message, secret);

            var service = typeof(ZaloPayService);
            var method = service.GetMethod("CreateHmacSha256", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (string)method.Invoke(null, new object[] { message, secret });

            // Assert
            Assert.Equal(expected, result);
        }

        private string ComputeHmacSha256(string message, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}