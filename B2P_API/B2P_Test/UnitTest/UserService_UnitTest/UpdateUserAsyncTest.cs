using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class UpdateUserAsyncTest
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IMemoryCache> _cacheMock = new();
        private readonly Mock<ISMSService> _smsServiceMock = new();
        private readonly Mock<IBankAccountRepository> _bankRepoMock = new();
        private readonly Mock<IImageRepository> _imageRepoMock = new();

        private UserService CreateService()
            => new UserService(
                _userRepoMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankRepoMock.Object,
                _imageRepoMock.Object);

        // --------- Basic request validation ---------
        [Fact(DisplayName = "UTCID01 - Return 400 when updateUserDto is null")]
        public async Task UTCID01_Return400WhenUpdateUserDtoIsNull()
        {
            int userId = 1;
            UpdateUserRequest updateUserDto = null;
            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Dữ liệu không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Return 400 when userId is invalid")]
        public async Task UTCID02_Return400WhenUserIdIsInvalid()
        {
            int userId = 0;
            var updateUserDto = new UpdateUserRequest { FullName = "Test User" };
            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("UserId không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        // --------- User data validation ---------
        [Fact(DisplayName = "UTCID03 - Return 400 when FullName is empty")]
        public async Task UTCID03_Return400WhenFullNameIsEmpty()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest { FullName = "" };
            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên người dùng không được để trống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Return 400 when FullName exceeds 50 characters")]
        public async Task UTCID04_Return400WhenFullNameExceeds50Characters()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = new string('a', 51) // 51 characters
            };
            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên người dùng không được vượt quá 50 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Return 400 when phone is invalid")]
        public async Task UTCID05_Return400WhenPhoneIsInvalid()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Phone = "abc123" // invalid
            };
            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Return 400 when phone already exists")]
        public async Task UTCID06_Return400WhenPhoneAlreadyExists()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Phone = "0901234567"
            };
            var existingUser = new User { UserId = 2, Phone = "0901234567" };
            _userRepoMock.Setup(x => x.CheckPhoneExistedByUserId(userId, updateUserDto.Phone))
                         .ReturnsAsync(existingUser);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại đã được sử dụng", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID07 - Return 400 when email is invalid")]
        public async Task UTCID07_Return400WhenEmailIsInvalid()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "invalidemail"
            };
            _userRepoMock.Setup(x => x.CheckPhoneExistedByUserId(userId, It.IsAny<string>()))
                         .ReturnsAsync((User)null);
            // IsRealEmailAsync sẽ trả về false nếu không mock, giả sử code kiểm tra logic thật
            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ Email không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID08 - Return 400 when email already exists")]
        public async Task UTCID08_Return400WhenEmailAlreadyExists()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "existing@email.com"
            };
            var existingUser = new User { UserId = 2, Email = "existing@email.com" };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync(existingUser);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email đã được sử dụng", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID09 - Return 400 when address exceeds 255 characters")]
        public async Task UTCID09_Return400WhenAddressExceeds255Characters()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = new string('a', 256) // 256 characters
            };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ không được vượt quá 255 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID10 - Return 400 when Dob is future date")]
        public async Task UTCID10_Return400WhenDobIsFutureDate()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
            };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Ngày sinh không được là ngày tương lai", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID11 - Return 400 when user age is under 15")]
        public async Task UTCID11_Return400WhenUserAgeIsUnder15()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-14)) // 14 years old
            };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Người dùng phải từ 15 tuổi trở lên", result.Message);
            Assert.Null(result.Data);
        }

        // --------- User existence & update ---------
        [Fact(DisplayName = "UTCID12 - Return 404 when user not found")]
        public async Task UTCID12_Return404WhenUserNotFound()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20))
            };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync((User)null);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID13 - Return 500 when user update fails")]
        public async Task UTCID13_Return500WhenUserUpdateFails()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20))
            };
            var user = new User { UserId = userId };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync(user);
            _userRepoMock.Setup(x => x.UpdateUserAsync(user))
                        .ReturnsAsync(false);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thất bại", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID14 - Update user successfully without bank account")]
        public async Task UTCID14_UpdateUserSuccessfullyWithoutBankAccount()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                IsMale = true
            };
            var user = new User { UserId = userId };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync(user);
            _userRepoMock.Setup(x => x.UpdateUserAsync(user))
                        .ReturnsAsync(true);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thành công", result.Message);
            Assert.Null(result.Data);

            Assert.Equal("Valid Name", user.FullName);
            Assert.Equal("valid@email.com", user.Email);
            Assert.Equal("Valid Address", user.Address);
            Assert.True(user.IsMale);
        }

        [Fact(DisplayName = "UTCID15 - Return 500 when exception occurs")]
        public async Task UTCID15_Return500WhenExceptionOccurs()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20))
            };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ThrowsAsync(new Exception("Database error"));

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Database error", result.Message);
            Assert.Null(result.Data);
        }

        // --------- Bank account validation ---------
        [Fact(DisplayName = "UTCID16 - Return 400 when bank account update fails (bank type not found)")]
        public async Task UTCID16_Return400WhenBankTypeNotFound()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "Valid Holder",
                BankTypeId = 999 // Invalid bank type ID
            };
            var user = new User { UserId = userId };

            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync(user);
            _bankRepoMock.Setup(x => x.GetBankTypeByIdAsync(999))
                        .ReturnsAsync((BankType)null);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không tìm thấy kiểu ngân hàng đã chọn", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID17 - Return 400 when bank account number is invalid")]
        public async Task UTCID17_Return400WhenBankAccountNumberIsInvalid()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123", // Invalid account number
                AccountHolder = "Valid Holder",
                BankTypeId = 1
            };
            var user = new User { UserId = userId };

            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync(user);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số tài khoản không hợp lệ, chỉ chứa từ 9-16 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID18 - Return 400 when account holder name exceeds 50 characters")]
        public async Task UTCID18_Return400WhenAccountHolderNameExceeds50Characters()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = new string('a', 51), // Too long
                BankTypeId = 1
            };
            var user = new User { UserId = userId };

            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync(user);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên chủ tài khoản không được vượt quá 50 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        // --------- Coverage for bank account: skip update when missing one required field ---------
        [Fact(DisplayName = "UTCID19 - Skip bank account update when account number is missing")]
        public async Task UTCID19_SkipBankAccountUpdateWhenAccountNumberMissing()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                // AccountNumber missing
                AccountHolder = "Valid Holder",
                BankTypeId = 1
            };
            var user = new User { UserId = userId };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email)).ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thành công", result.Message);
        }

        [Fact(DisplayName = "UTCID20 - Skip bank account update when account holder is missing")]
        public async Task UTCID20_SkipBankAccountUpdateWhenAccountHolderMissing()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                // AccountHolder missing
                BankTypeId = 1
            };
            var user = new User { UserId = userId };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email)).ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thành công", result.Message);
        }

        [Fact(DisplayName = "UTCID21 - Skip bank account update when bank type is missing or invalid")]
        public async Task UTCID21_SkipBankAccountUpdateWhenBankTypeMissingOrInvalid()
        {
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "Valid Holder",
                BankTypeId = 0 // invalid
            };
            var user = new User { UserId = userId };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email)).ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.UpdateUserAsync(userId, updateUserDto);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thành công", result.Message);
        }
    }
}