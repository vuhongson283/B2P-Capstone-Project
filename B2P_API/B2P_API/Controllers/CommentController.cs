using B2P_API.DTOs;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CommentController : ControllerBase
	{
		private readonly CommentService _service;
		private readonly BlogService _blogService;
		private readonly UserService _userService;
		private readonly IBookingNotificationService _notificationService;

		public CommentController(
			CommentService service,
			BlogService blogService,
			UserService userService,
			IBookingNotificationService notificationService)
		{
			_service = service;
			_blogService = blogService;
			_userService = userService;
			_notificationService = notificationService;
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CommentDto dto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(new ApiResponse<string>
				{
					Success = false,
					Message = "Dữ liệu không hợp lệ.",
					Status = 400,
					Data = string.Join(" | ", ModelState.Values
						.SelectMany(v => v.Errors)
						.Select(e => e.ErrorMessage))
				});
			}

			try
			{
				Console.WriteLine($"🔄 [DEBUG] Creating comment for BlogId: {dto.BlogId}, UserId: {dto.UserId}");

				var result = await _service.CreateAsync(dto);
				Console.WriteLine($"📝 [DEBUG] Comment creation result: Success={result?.Success}, Status={result?.Status}");

				// ✅ Test: Gửi notification ngay lập tức để test
				if (result?.Success == true)
				{
					Console.WriteLine($"✅ [DEBUG] Comment created successfully, sending test notification...");

					try
					{
						// Test notification đơn giản
						var testNotification = new
						{
							commentId = 999,
							userId = dto.UserId,
							userName = "Test User",
							userAvatar = "https://ui-avatars.com/api/?name=Test&background=27ae60&color=fff&size=200",
							blogId = dto.BlogId,
							blogTitle = "Test Blog",
							blogAuthorId = 26, // Current user ID
							content = dto.Content,
							isReply = false,
							parentCommentId = dto.ParentCommentId,
							timestamp = DateTime.UtcNow.ToString("O"),
							action = "comment_created"
						};

						Console.WriteLine($"📤 [DEBUG] Sending test notification: {System.Text.Json.JsonSerializer.Serialize(testNotification)}");

						// Gửi đến tất cả clients để test
						await _notificationService.SendCommentNotification(testNotification);

						Console.WriteLine($"✅ [DEBUG] Test notification sent successfully");
					}
					catch (Exception notificationEx)
					{
						Console.WriteLine($"❌ [DEBUG] Error sending notification: {notificationEx.Message}");
						Console.WriteLine($"📝 [DEBUG] Notification stack trace: {notificationEx.StackTrace}");
					}
				}

				return StatusCode(result.Status, result);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ [DEBUG] Error in Create comment: {ex.Message}");
				return StatusCode(500, new ApiResponse<string>
				{
					Success = false,
					Message = "Có lỗi xảy ra khi tạo bình luận.",
					Status = 500,
					Data = ex.Message
				});
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, [FromBody] CommentDto dto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(new ApiResponse<string>
				{
					Success = false,
					Message = "Dữ liệu không hợp lệ.",
					Status = 400,
					Data = string.Join(" | ", ModelState.Values
						.SelectMany(v => v.Errors)
						.Select(e => e.ErrorMessage))
				});
			}

			var result = await _service.UpdateAsync(id, dto);
			return StatusCode(result.Status, result);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id, [FromQuery] int userId, [FromQuery] int roleId)
		{
			var result = await _service.DeleteAsync(id, userId, roleId);
			return StatusCode(result.Status, result);
		}

		[HttpGet("user/{userId}")]
		public async Task<IActionResult> GetByUserId(int userId, [FromQuery] CommentQueryParameters queryParams)
		{
			var result = await _service.GetByUserIdAsync(userId, queryParams);
			return StatusCode(result.Status, result);
		}

		[HttpGet]
		public async Task<IActionResult> GetAll([FromQuery] CommentQueryParameters queryParams)
		{
			var result = await _service.GetAllAsync(queryParams);
			return StatusCode(result.Status, result);
		}

		// ✅ Private method để gửi notification
		private async Task SendCommentNotificationAsync(CommentDto dto, object commentResult)
		{
			try
			{
				Console.WriteLine($"🔔 Starting notification process for BlogId: {dto.BlogId}");

				// Lấy thông tin blog
				var blogResult = await _blogService.GetByIdAsync(dto.BlogId);
				if (blogResult?.Success != true || blogResult.Data == null)
				{
					Console.WriteLine($"⚠️ Blog not found or access failed for BlogId: {dto.BlogId}");
					return;
				}

				var blog = blogResult.Data;

				// Chỉ gửi notification nếu không phải comment của chính blog author
				if (blog.UserId == dto.UserId)
				{
					Console.WriteLine($"⏭️ Skipping notification - User commenting on own blog");
					return;
				}

				// Lấy thông tin user comment
				var userResult = await _userService.GetUserByIdAsync(dto.UserId);
				var commenterName = userResult?.Success == true && userResult.Data != null
					? (userResult.Data.FullName ?? userResult.Data.FullName ?? $"User {dto.UserId}")
					: $"User {dto.UserId}";

				// Lấy parent comment info nếu là reply
				string parentComment = null;
				if (dto.ParentCommentId.HasValue)
				{
					try
					{
						// Bạn có thể implement method GetCommentById nếu cần
						parentComment = "Bình luận trước..."; // Placeholder
					}
					catch
					{
						parentComment = "Bình luận trước...";
					}
				}

				var notificationData = new
				{
					commentId = GetCommentIdFromResult(commentResult), // Helper method
					userId = dto.UserId,
					userName = commenterName,
					userAvatar = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(commenterName)}&background=27ae60&color=fff&size=200",
					blogId = dto.BlogId,
					blogTitle = blog.Title ?? "Untitled Blog",
					blogAuthorId = blog.UserId,
					content = dto.Content ?? "",
					isReply = dto.ParentCommentId.HasValue,
					parentCommentId = dto.ParentCommentId,
					parentComment = parentComment,
					timestamp = DateTime.UtcNow.ToString("O"),
					action = dto.ParentCommentId.HasValue ? "comment_reply" : "comment_created"
				};

				// Gửi SignalR notification
				await _notificationService.NotifyCommentCreated(blog.UserId, notificationData);
				Console.WriteLine($"✅ SignalR notification sent to user {blog.UserId} for comment on blog {dto.BlogId}");

			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error sending comment notification: {ex.Message}");
				Console.WriteLine($"📝 Notification error stack: {ex.StackTrace}");
				// Không throw exception để không ảnh hưởng main process
			}
		}

		// ✅ Helper method để extract comment ID từ result
		private int GetCommentIdFromResult(object result)
		{
			try
			{
				// Dựa vào cấu trúc ApiResponse của bạn
				if (result is ApiResponse<object> apiResponse && apiResponse.Data != null)
				{
					var data = apiResponse.Data;
					var commentIdProperty = data.GetType().GetProperty("CommentId") ??
										  data.GetType().GetProperty("Id") ??
										  data.GetType().GetProperty("commentId");

					if (commentIdProperty != null)
					{
						var value = commentIdProperty.GetValue(data);
						if (value is int intValue)
							return intValue;
					}
				}

				return 0; // Fallback
			}
			catch
			{
				return 0;
			}
		}
	}
}