using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class ChangePasswordAsyncTest
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IMemoryCache> _cacheMock = new();
        private readonly Mock<ISMSService> _smsServiceMock = new();
        private readonly Mock<IBankAccountRepository> _bankAccountRepositoryMock = new();
        private readonly Mock<IImageRepository> _imageRepositoryMock = new();

        private UserService CreateUserService()
        {
            return new UserService(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            );
        }

        [Fact(DisplayName = "UTCID01 - Request is null returns 400")]
        public async Task UTCID01_RequestIsNull_Returns400()
        {
            var userService = CreateUserService();
            var result = await userService.ChangePasswordAsync(null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Dữ liệu không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - UserId invalid returns 400")]
        public async Task UTCID02_UserIdInvalid_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 0, NewPassword = "pass12345", ConfirmPassword = "pass12345" };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("UserId không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - NewPassword is empty returns 400")]
        public async Task UTCID03_NewPasswordEmpty_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "   ", ConfirmPassword = "   " };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu mới không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - NewPassword too short returns 400")]
        public async Task UTCID04_NewPasswordTooShort_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "short", ConfirmPassword = "short" };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu có ít nhất 8 ký tự", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - ConfirmPassword is empty returns 400")]
        public async Task UTCID05_ConfirmPasswordEmpty_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "   " };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Xác nhận mật khẩu không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Passwords not match returns 400")]
        public async Task UTCID06_PasswordsNotMatch_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "password456" };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_14, result.Message);
        }

        [Fact(DisplayName = "UTCID07 - User not found returns 404")]
        public async Task UTCID07_UserNotFound_Returns404()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "password123" };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync((User)null);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_65, result.Message);
        }

        [Fact(DisplayName = "UTCID08 - Has password, old password empty returns 400")]
        public async Task UTCID08_HasPassword_OldPasswordEmpty_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "password123", OldPassword = "   " };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("oldpass") };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu cũ không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID09 - Has password, old password wrong returns 400")]
        public async Task UTCID09_HasPassword_OldPasswordWrong_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "password123", OldPassword = "wrongoldpass" };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("oldpass") };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_15, result.Message);
        }

        [Fact(DisplayName = "UTCID10 - Has password, update fail returns 500")]
        public async Task UTCID10_HasPassword_UpdateFail_Returns500()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "password123", OldPassword = "oldpass" };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("oldpass") };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(false);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật mật khẩu thất bại", result.Message);
        }

        [Fact(DisplayName = "UTCID11 - Has password, success returns 200")]
        public async Task UTCID11_HasPassword_Success_Returns200()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "password123", OldPassword = "oldpass" };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("oldpass") };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            var result = await userService.ChangePasswordAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Đổi mật khẩu thành công", result.Message);
            Assert.NotNull(result.Data);
            var property = result.Data.GetType().GetProperty("IsFirstTimeSetup");
            Assert.NotNull(property);
            Assert.False((bool)property.GetValue(result.Data));
        }

        [Fact(DisplayName = "UTCID12 - First time setup, update fail returns 500")]
        public async Task UTCID12_FirstTimeSetup_UpdateFail_Returns500()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 2, NewPassword = "password123", ConfirmPassword = "password123" };
            var user = new User { UserId = 2, Password = null };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(false);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật mật khẩu thất bại", result.Message);
        }

        [Fact(DisplayName = "UTCID13 - First time setup, success returns 200")]
        public async Task UTCID13_FirstTimeSetup_Success_Returns200()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 2, NewPassword = "password123", ConfirmPassword = "password123" };
            var user = new User { UserId = 2, Password = null };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            var result = await userService.ChangePasswordAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Thiết lập mật khẩu thành công", result.Message);
            Assert.NotNull(result.Data);
            var property = result.Data.GetType().GetProperty("IsFirstTimeSetup");
            Assert.NotNull(property);
            Assert.True((bool)property.GetValue(result.Data));
        }

        [Fact(DisplayName = "UTCID14 - Exception returns 500")]
        public async Task UTCID14_Exception_Returns500()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "password123", OldPassword = "oldpass" };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ThrowsAsync(new System.Exception("fail"));

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("fail", result.Message);
        }

        [Fact(DisplayName = "UTCID15 - NewPassword is null returns 400")]
        public async Task UTCID15_NewPasswordNull_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = null, ConfirmPassword = "password123" };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu mới không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID16 - ConfirmPassword is null returns 400")]
        public async Task UTCID16_ConfirmPasswordNull_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = null };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Xác nhận mật khẩu không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID17 - OldPassword is null returns 400")]
        public async Task UTCID17_HasPassword_OldPasswordNull_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "password123", ConfirmPassword = "password123", OldPassword = null };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("oldpass") };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu cũ không được để trống", result.Message);
        }
    }
}