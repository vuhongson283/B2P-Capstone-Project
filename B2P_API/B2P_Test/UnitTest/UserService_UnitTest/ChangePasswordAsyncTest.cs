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
        private readonly Mock<IImageRepository> _imageRepositoryMock = new();

        private UserService CreateUserService()
        {
            return new UserService(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
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
            var req = new ChangePasswordRequest { UserId = 0, NewPassword = "Pass123", ConfirmPassword = "Pass123" };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("UserId không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - NewPassword does not meet regex returns 400")]
        public async Task UTCID03_NewPasswordRegexFail_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "abc", ConfirmPassword = "abc" };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - ConfirmPassword is empty returns 400")]
        public async Task UTCID04_ConfirmPasswordEmpty_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = "   " };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Xác nhận mật khẩu không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Passwords not match returns 400")]
        public async Task UTCID05_PasswordsNotMatch_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = "Password2" };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_14, result.Message);
        }

        [Fact(DisplayName = "UTCID06 - User not found returns 404")]
        public async Task UTCID06_UserNotFound_Returns404()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = "Password1" };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync((User)null);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_65, result.Message);
        }

        [Fact(DisplayName = "UTCID07 - Has password, old password empty returns 400")]
        public async Task UTCID07_HasPassword_OldPasswordEmpty_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = "Password1", OldPassword = "   " };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("OldPassword") };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu cũ không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID08 - Has password, old password wrong returns 400")]
        public async Task UTCID08_HasPassword_OldPasswordWrong_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = "Password1", OldPassword = "WrongPassword" };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("OldPassword") };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_15, result.Message);
        }

        [Fact(DisplayName = "UTCID09 - Has password, update fail returns 500")]
        public async Task UTCID09_HasPassword_UpdateFail_Returns500()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = "Password1", OldPassword = "OldPassword" };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("OldPassword") };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(false);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật mật khẩu thất bại", result.Message);
        }

        [Fact(DisplayName = "UTCID10 - Has password, success returns 200")]
        public async Task UTCID10_HasPassword_Success_Returns200()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = "Password1", OldPassword = "OldPassword" };
            var user = new User { UserId = 1, Password = BCrypt.Net.BCrypt.HashPassword("OldPassword") };

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

        [Fact(DisplayName = "UTCID11 - First time setup, update fail returns 500")]
        public async Task UTCID11_FirstTimeSetup_UpdateFail_Returns500()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 2, NewPassword = "Password1", ConfirmPassword = "Password1" };
            var user = new User { UserId = 2, Password = null };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(false);

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật mật khẩu thất bại", result.Message);
        }

        [Fact(DisplayName = "UTCID12 - First time setup, success returns 200")]
        public async Task UTCID12_FirstTimeSetup_Success_Returns200()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 2, NewPassword = "Password1", ConfirmPassword = "Password1" };
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

        [Fact(DisplayName = "UTCID13 - Exception returns 500")]
        public async Task UTCID13_Exception_Returns500()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = "Password1", OldPassword = "OldPassword" };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(req.UserId)).ThrowsAsync(new System.Exception("fail"));

            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("fail", result.Message);
        }

        [Fact(DisplayName = "UTCID14 - ConfirmPassword is null returns 400")]
        public async Task UTCID14_ConfirmPasswordNull_Returns400()
        {
            var userService = CreateUserService();
            var req = new ChangePasswordRequest { UserId = 1, NewPassword = "Password1", ConfirmPassword = null };
            var result = await userService.ChangePasswordAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Xác nhận mật khẩu không được để trống", result.Message);
        }
    }
}