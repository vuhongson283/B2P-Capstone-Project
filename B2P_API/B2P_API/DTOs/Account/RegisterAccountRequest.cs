using B2P_API.Utils;
using System.ComponentModel.DataAnnotations;

namespace B2P_API.DTOs.Account
{
	public class RegisterAccountRequest
	{
		[Required(ErrorMessage = MessagesCodes.MSG_07)]
		[EmailAddress(ErrorMessage = MessagesCodes.MSG_06)]
		// Bắt buộc phải có phần mở rộng (ví dụ .com, .vn…)
		[RegularExpression(
			@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
			ErrorMessage = MessagesCodes.MSG_06)]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = MessagesCodes.MSG_07)]
		[MinLength(8, ErrorMessage = MessagesCodes.MSG_13)]
		public string Password { get; set; } = string.Empty;

		[Required(ErrorMessage = MessagesCodes.MSG_07)]
		[Compare("Password", ErrorMessage = MessagesCodes.MSG_14)]
		public string ConfirmPassword { get; set; } = string.Empty;

		[Required(ErrorMessage = MessagesCodes.MSG_07)]
		[StringLength(100, ErrorMessage = MessagesCodes.MSG_26)]
		// Chỉ cho phép ký tự chữ và khoảng trắng
		[RegularExpression(
			@"^[\p{L} ]+$",
			ErrorMessage = "Tên không được chứa số hoặc ký tự đặc biệt")]
		public string FullName { get; set; } = string.Empty;

		[Required(ErrorMessage = MessagesCodes.MSG_07)]
		// Phải đúng 12 chữ số từ 0–9
		[RegularExpression(
			@"^\d{12}$",
			ErrorMessage = "Số điện thoại phải gồm đúng 12 chữ số")]
		public string PhoneNumber { get; set; } = string.Empty;

		[Required(ErrorMessage = MessagesCodes.MSG_07)]
		public bool? IsMale { get; set; }

		[StringLength(200, ErrorMessage = MessagesCodes.MSG_26)]
		public string? Address { get; set; }
	}
}
