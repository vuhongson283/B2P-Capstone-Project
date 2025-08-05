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

        [Fact(DisplayName = "UTCID01 - Return 400 when updateUserDto is null")]
        public async Task UTCID01_Return400WhenUpdateUserDtoIsNull()
        {
            // Arrange
            int userId = 1;
            UpdateUserRequest updateUserDto = null;
            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Dữ liệu không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Return 400 when userId is invalid")]
        public async Task UTCID02_Return400WhenUserIdIsInvalid()
        {
            // Arrange
            int userId = 0;
            var updateUserDto = new UpdateUserRequest { FullName = "Test User" };
            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("UserId không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Return 400 when FullName is empty")]
        public async Task UTCID03_Return400WhenFullNameIsEmpty()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest { FullName = "" };
            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên người dùng không được để trống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Return 400 when FullName exceeds 50 characters")]
        public async Task UTCID04_Return400WhenFullNameExceeds50Characters()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = new string('a', 51) // 51 characters
            };
            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên người dùng không được vượt quá 50 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Return 400 when Email is empty")]
        public async Task UTCID05_Return400WhenEmailIsEmpty()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = ""
            };
            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không được để trống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Return 400 when Email already exists")]
        public async Task UTCID06_Return400WhenEmailAlreadyExists()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "existing@email.com"
            };
            var existingUser = new User { UserId = 2, Email = "existing@email.com" };

            // Mock IsRealEmailAsync to return true (valid email format)
            // Note: You might need to make this method virtual or use interface for mocking

            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync(existingUser);
            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email đã được sử dụng", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID07 - Return 400 when Address is empty")]
        public async Task UTCID07_Return400WhenAddressIsEmpty()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = ""
            };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ không được để trống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID08 - Return 400 when Address exceeds 255 characters")]
        public async Task UTCID08_Return400WhenAddressExceeds255Characters()
        {
            // Arrange
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

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ không được vượt quá 255 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID09 - Return 400 when Dob is null")]
        public async Task UTCID09_Return400WhenDobIsNull()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = null
            };
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Ngày sinh không được để trống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID10 - Return 400 when Dob is future date")]
        public async Task UTCID10_Return400WhenDobIsFutureDate()
        {
            // Arrange
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

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Ngày sinh không được là ngày tương lai", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID11 - Return 400 when user age is under 15")]
        public async Task UTCID11_Return400WhenUserAgeIsUnder15()
        {
            // Arrange
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

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Người dùng phải từ 15 tuổi trở lên", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID12 - Return 404 when user not found")]
        public async Task UTCID12_Return404WhenUserNotFound()
        {
            // Arrange
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

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID13 - Return 500 when user update fails")]
        public async Task UTCID13_Return500WhenUserUpdateFails()
        {
            // Arrange
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

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thất bại", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID14 - Update user successfully without bank account")]
        public async Task UTCID14_UpdateUserSuccessfullyWithoutBankAccount()
        {
            // Arrange
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

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thông tin người dùng thành công", result.Message);
            Assert.Null(result.Data);

            // Verify user properties were updated
            Assert.Equal("Valid Name", user.FullName);
            Assert.Equal("valid@email.com", user.Email);
            Assert.Equal("Valid Address", user.Address);
            Assert.True(user.IsMale);
        }

        [Fact(DisplayName = "UTCID15 - Return 500 when exception occurs")]
        public async Task UTCID15_Return500WhenExceptionOccurs()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",  // Add required email
                Address = "Valid Address",  // Add required address
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)) // Add required DOB
            };

            // Setup mocks to pass all validations but throw exception during user retrieval
            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null); // Email validation passes
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ThrowsAsync(new Exception("Database error")); // Exception occurs here

            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Database error", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID16 - Return 400 when bank account update fails")]
        public async Task UTCID16_Return400WhenBankAccountUpdateFails()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = "Valid Holder",
                BankTypeId = 999 // Invalid bank type ID - will cause bank account validation to fail
            };
            var user = new User { UserId = userId };

            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync(user);
            _bankRepoMock.Setup(x => x.GetBankTypeByIdAsync(999))
                        .ReturnsAsync((BankType)null); // Bank type not found

            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không tìm thấy kiểu ngân hàng đã chọn", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID17 - Return 400 when bank account number is invalid")]
        public async Task UTCID17_Return400WhenBankAccountNumberIsInvalid()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123", // Invalid account number (too short)
                AccountHolder = "Valid Holder",
                BankTypeId = 1
            };
            var user = new User { UserId = userId };

            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync(user);

            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số tài khoản không hợp lệ, chỉ chứa từ 9-16 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID18 - Return 400 when account holder name exceeds 50 characters")]
        public async Task UTCID18_Return400WhenAccountHolderNameExceeds50Characters()
        {
            // Arrange
            int userId = 1;
            var updateUserDto = new UpdateUserRequest
            {
                FullName = "Valid Name",
                Email = "valid@email.com",
                Address = "Valid Address",
                Dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
                AccountNumber = "123456789",
                AccountHolder = new string('a', 51), // Invalid holder name (too long)
                BankTypeId = 1
            };
            var user = new User { UserId = userId };

            _userRepoMock.Setup(x => x.CheckEmailExistedByUserId(userId, updateUserDto.Email))
                        .ReturnsAsync((User)null);
            _userRepoMock.Setup(x => x.GetUserByIdAsync(userId))
                        .ReturnsAsync(user);

            var service = CreateService();

            // Act
            var result = await service.UpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên chủ tài khoản không được vượt quá 50 ký tự", result.Message);
            Assert.Null(result.Data);
        }
    }
}