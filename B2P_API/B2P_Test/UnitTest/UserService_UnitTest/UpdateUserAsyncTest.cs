using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class UpdateUserAsyncTest
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

        [Fact(DisplayName = "UTCID01 - Null request returns 400")]
        public async Task UTCID01_NullRequest_Returns400()
        {
            var userService = CreateUserService();
            var result = await userService.UpdateUserAsync(1, null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Dữ liệu không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Invalid userId returns 400")]
        public async Task UTCID02_InvalidUserId_Returns400()
        {
            var userService = CreateUserService();
            var req = new UpdateUserRequest();
            var result = await userService.UpdateUserAsync(0, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("UserId không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Empty FullName returns 400")]
        public async Task UTCID03_EmptyFullName_Returns400()
        {
            var userService = CreateUserService();
            var req = new UpdateUserRequest { FullName = "   " };
            var result = await userService.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên người dùng không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - FullName too long returns 400")]
        public async Task UTCID04_FullNameTooLong_Returns400()
        {
            var userService = CreateUserService();
            var req = new UpdateUserRequest { FullName = new string('a', 51) };
            var result = await userService.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên người dùng không được vượt quá 50 ký tự", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Empty email returns 400")]
        public async Task UTCID05_EmptyEmail_Returns400()
        {
            var userService = CreateUserService();
            var req = new UpdateUserRequest { FullName = "User", Email = "" };
            var result = await userService.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Invalid email returns 400")]
        public async Task UTCID06_InvalidEmail_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(false);

            var req = new UpdateUserRequest { FullName = "User", Email = "a@b.com" };
            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ Email không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID07 - Empty address returns 400")]
        public async Task UTCID07_EmptyAddress_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            var req = new UpdateUserRequest { FullName = "User", Email = "a@b.com", Address = "   " };
            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID08 - Address too long returns 400")]
        public async Task UTCID08_AddressTooLong_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            var req = new UpdateUserRequest { FullName = "User", Email = "a@b.com", Address = new string('a', 256) };
            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ không được vượt quá 255 ký tự", result.Message);
        }

        [Fact(DisplayName = "UTCID09 - Null Dob returns 400")]
        public async Task UTCID09_NullDob_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            var req = new UpdateUserRequest { FullName = "User", Email = "a@b.com", Address = "A", Dob = null };
            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Ngày sinh không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID10 - Future Dob returns 400")]
        public async Task UTCID10_FutureDob_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var req = new UpdateUserRequest { FullName = "User", Email = "a@b.com", Address = "A", Dob = dob };
            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Ngày sinh không được là ngày tương lai", result.Message);
        }

        [Fact(DisplayName = "UTCID11 - Age under 15 returns 400")]
        public async Task UTCID11_AgeUnder15_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-14));
            var req = new UpdateUserRequest { FullName = "User", Email = "a@b.com", Address = "A", Dob = dob };
            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Người dùng phải từ 15 tuổi trở lên", result.Message);
        }

        [Fact(DisplayName = "UTCID12 - User not found returns 404")]
        public async Task UTCID12_UserNotFound_Returns404()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest { FullName = "User", Email = "a@b.com", Address = "A", Dob = dob };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync((User)null);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_65, result.Message);
        }

        [Fact(DisplayName = "UTCID13 - Email already exists returns 400")]
        public async Task UTCID13_EmailExists_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest { FullName = "User", Email = "a@b.com", Address = "A", Dob = dob };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync(new User());

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email đã được sử dụng", result.Message);
        }

        [Fact(DisplayName = "UTCID14 - Invalid bank account number returns 400")]
        public async Task UTCID14_InvalidBankAccountNumber_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(false);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = dob,
                AccountNumber = "123",
                AccountHolder = "Holder",
                BankTypeId = 1
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số tài khoản không hợp lệ,chỉ chứa từ 9-16 ký tự", result.Message);
        }

        [Fact(DisplayName = "UTCID15 - Empty AccountHolder returns 400")]
        public async Task UTCID15_EmptyAccountHolder_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = dob,
                AccountNumber = "123456789",
                AccountHolder = "   ",
                BankTypeId = 1
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên chủ tài khoản không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID16 - AccountHolder too long returns 400")]
        public async Task UTCID16_AccountHolderTooLong_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = dob,
                AccountNumber = "123456789",
                AccountHolder = new string('a', 51),
                BankTypeId = 1
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên chủ tài khoản không được vượt quá 50 ký tự", result.Message);
        }

        [Fact(DisplayName = "UTCID17 - Invalid BankTypeId (zero) returns 400")]
        public async Task UTCID17_InvalidBankTypeIdZero_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = dob,
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 0
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Loại ngân hàng không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID18 - BankType not found returns 400")]
        public async Task UTCID18_BankTypeNotFound_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = dob,
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 1
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);
            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(1)).ReturnsAsync((BankType)null);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không tìm thấy kiểu ngân hàng đã chọn", result.Message);
        }

        [Fact(DisplayName = "UTCID19 - Update user fails returns 500")]
        public async Task UTCID19_UpdateUserFails_Returns500()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = dob,
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 1
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);
            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(1)).ReturnsAsync(new BankType());
            _bankAccountRepositoryMock.Setup(x => x.GetBankAccountsByUserIdAsync(1)).ReturnsAsync(new BankAccount());
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(false);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thất bại", result.Message);
        }

        [Fact(DisplayName = "UTCID20 - Success returns 200")]
        public async Task UTCID20_Success_Returns200()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));
            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = dob,
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 1
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);
            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(1)).ReturnsAsync(new BankType());
            _bankAccountRepositoryMock.Setup(x => x.GetBankAccountsByUserIdAsync(1)).ReturnsAsync(new BankAccount());
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thành công", result.Message);
        }

        [Fact(DisplayName = "UTCID21 - Exception returns 500")]
        public async Task UTCID21_Exception_Returns500()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(It.IsAny<int?>())).ReturnsAsync(new BankType());
            _bankAccountRepositoryMock.Setup(x => x.GetBankAccountsByUserIdAsync(It.IsAny<int>())).ReturnsAsync(new BankAccount());

            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 1,
                IsMale = true
            };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ThrowsAsync(new Exception("fail"));

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("fail", result.Message);
        }

        [Fact(DisplayName = "UTCID22 - Create new bank account when user has no bank account")]
        public async Task UTCID22_CreateNewBankAccount_WhenNotExist()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            // Setup các dependency để qua hết validate
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, It.IsAny<string>())).ReturnsAsync((User)null);
            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(It.IsAny<int?>())).ReturnsAsync(new BankType());
            _bankAccountRepositoryMock.Setup(x => x.GetBankAccountsByUserIdAsync(1)).ReturnsAsync((BankAccount)null); // Không có bank account
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);
            _bankAccountRepositoryMock.Setup(x => x.AddBankAccountAsync(It.IsAny<BankAccount>())).ReturnsAsync(true);

            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 1,
                IsMale = true
            };

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            // Đảm bảo hàm AddBankAccountAsync đã được gọi
            _bankAccountRepositoryMock.Verify(x => x.AddBankAccountAsync(It.IsAny<BankAccount>()), Times.Once);
            // Đảm bảo hàm UpdateBankAccountAsync không được gọi
            _bankAccountRepositoryMock.Verify(x => x.UpdateBankAccountAsync(It.IsAny<BankAccount>()), Times.Never);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thành công", result.Message);
        }

        [Fact(DisplayName = "UTCID23 - Update existing bank account when user has bank account")]
        public async Task UTCID23_UpdateExistingBankAccount_WhenExist()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            // Setup các dependency để qua hết validate
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, It.IsAny<string>())).ReturnsAsync((User)null);
            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(It.IsAny<int?>())).ReturnsAsync(new BankType());
            _bankAccountRepositoryMock.Setup(x => x.GetBankAccountsByUserIdAsync(1)).ReturnsAsync(new BankAccount
            {
                UserId = 1,
                AccountNumber = "987654321",
                AccountHolder = "OldHolder",
                BankTypeId = 1
            }); // Đã có bank account
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);
            _bankAccountRepositoryMock.Setup(x => x.AddBankAccountAsync(It.IsAny<BankAccount>())).ReturnsAsync(true);

            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 1,
                IsMale = true
            };

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            // Đảm bảo hàm UpdateBankAccountAsync đã được gọi
            _bankAccountRepositoryMock.Verify(x => x.UpdateBankAccountAsync(It.IsAny<BankAccount>()), Times.Once);
            // Đảm bảo hàm AddBankAccountAsync không được gọi
            _bankAccountRepositoryMock.Verify(x => x.AddBankAccountAsync(It.IsAny<BankAccount>()), Times.Never);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thành công", result.Message);
        }

        [Fact(DisplayName = "UTCID24 - Null address returns 400")]
        public async Task UTCID24_NullAddress_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, It.IsAny<string>())).ReturnsAsync((User)null);
            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(It.IsAny<int?>())).ReturnsAsync(new BankType());
            _bankAccountRepositoryMock.Setup(x => x.GetBankAccountsByUserIdAsync(1)).ReturnsAsync(new BankAccount());
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = null,
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 1,
                IsMale = true
            };

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID25 - Null BankTypeId returns 400")]
        public async Task UTCID25_NullBankTypeId_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = null, // <--- nhánh này chưa test
                IsMale = true
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Loại ngân hàng không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID26 - Dob with birthday not yet this year triggers age-- branch")]
        public async Task UTCID26_DobBirthdayNotYetThisYear_PassesAgeCalculation()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, It.IsAny<string>())).ReturnsAsync((User)null);
            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(It.IsAny<int?>())).ReturnsAsync(new BankType());
            _bankAccountRepositoryMock.Setup(x => x.GetBankAccountsByUserIdAsync(1)).ReturnsAsync(new BankAccount());
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            // Today is 2025-07-30, birthday is 2005-12-01 (not yet reached this year)
            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = new DateOnly(2005, 12, 1),
                AccountNumber = "123456789",
                AccountHolder = "Holder",
                BankTypeId = 1,
                IsMale = true
            };

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
        }

        [Fact(DisplayName = "UTCID27 - AccountHolder with leading/trailing spaces is trimmed")]
        public async Task UTCID27_AccountHolderWithSpaces_IsTrimmed()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            userServiceMock.Protected()
                .Setup<bool>("IsValidBankAccount", ItExpr.IsAny<string>())
                .Returns(true);

            var req = new UpdateUserRequest
            {
                FullName = "User",
                Email = "a@b.com",
                Address = "A",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "   Holder   ", // leading/trailing spaces
                BankTypeId = 1,
                IsMale = true
            };
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(new User());
            _userRepositoryMock.Setup(x => x.CheckEmailExistedByUserId(1, req.Email)).ReturnsAsync((User)null);
            _bankAccountRepositoryMock.Setup(x => x.GetBankTypeByIdAsync(1)).ReturnsAsync(new BankType());
            _bankAccountRepositoryMock.Setup(x => x.GetBankAccountsByUserIdAsync(1)).ReturnsAsync(new BankAccount());
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            var result = await userServiceMock.Object.UpdateUserAsync(1, req);

            // Bạn có thể verify giá trị .AccountHolder đã được trim hoặc chỉ assert kết quả thành công
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
        }
    }
}