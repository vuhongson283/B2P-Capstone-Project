using B2P_API.DTOs;
using B2P_API.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Update.Internal;
using B2P_API.DTOs.ImageDTOs;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly ILogger<ImageController> _logger;

        public ImageController(IImageService imageService, ILogger<ImageController> logger)
        {
            _imageService = imageService;
            _logger = logger;
        }

        [HttpPost("upload-facility")]
        public async Task<IActionResult> UploadImageFacility(
            List<IFormFile> files,
            [FromForm] int entityId,
            [FromForm] string? caption = null)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "No files uploaded" });

            foreach (var file in files)
            {
                if (!IsValidImageFile(file))
                    return BadRequest(new { message = $"Invalid image format: {file.FileName}" });
            }

            var uploadResults = new List<ImageUploadResponse>();

            try
            {
                foreach (var file in files)
                {
                    var result = await _imageService.UploadImageAsync(file, "facility", entityId, caption);
                    uploadResults.Add(result);
                }

                return Ok(uploadResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading one or more images");
                return StatusCode(500, new { message = "Upload failed" });
            }
        }

        [HttpPost("upload-user")]
        public async Task<IActionResult> UploadImageUser(
            IFormFile file,
            [FromForm] int entityId,
            [FromForm] string? caption = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!IsValidImageFile(file))
                return BadRequest(new { message = "Invalid image format" });

            try
            {
                var result = await _imageService.UploadImageAsync(file, "user", entityId, caption);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new { message = "Upload failed" });
            }
        }

        [HttpPost("upload-blog")]
        public async Task<IActionResult> UploadImageBlog(
            IFormFile file,
            [FromForm] int entityId,
            [FromForm] string? caption = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!IsValidImageFile(file))
                return BadRequest(new { message = "Invalid image format" });

            try
            {
                var result = await _imageService.UploadImageAsync(file, "blog", entityId, caption);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new { message = "Upload failed" });
            }
        }

        [HttpPost("upload-slide")]
        public async Task<IActionResult> UploadImageSlide(
            IFormFile file,
            [FromForm] int entityId,
            [FromForm] string? caption = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!IsValidImageFile(file))
                return BadRequest(new { message = "Invalid image format" });

            try
            {
                var result = await _imageService.UploadImageAsync(file, "slide", entityId, caption);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new { message = "Upload failed" });
            }
        }

        [HttpGet("{imageType}/{entityId}")]
        public async Task<IActionResult> GetImages(string imageType, int entityId)
        {
            try
            {
                var images = await _imageService.GetImagesByTypeAndEntityAsync(imageType, entityId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting images for {ImageType} with ID {EntityId}", imageType, entityId);
                return StatusCode(500, new { message = "Failed to get images" });
            }
        }

        // Specific endpoints for backward compatibility
        [HttpGet("facility/{facilityId}")]
        public async Task<IActionResult> GetFacilityImages(int facilityId)
        {
            try
            {
                var images = await _imageService.GetFacilityImagesAsync(facilityId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting facility images");
                return StatusCode(500, new { message = "Failed to get images" });
            }
        }

        [HttpGet("blog/{blogId}")]
        public async Task<IActionResult> GetBlogImages(int blogId)
        {
            try
            {
                var images = await _imageService.GetBlogImagesAsync(blogId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blog images");
                return StatusCode(500, new { message = "Failed to get images" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserImages(int userId)
        {
            try
            {
                var images = await _imageService.GetUserImagesAsync(userId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user images");
                return StatusCode(500, new { message = "Failed to get images" });
            }
        }

        [HttpGet("slide/{slideId}")]
        public async Task<IActionResult> GetSlideImages(int slideId)
        {
            try
            {
                var images = await _imageService.GetSlideImagesAsync(slideId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slide images");
                return StatusCode(500, new { message = "Failed to get images" });
            }
        }

        [HttpDelete("{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            try
            {
                var result = await _imageService.DeleteImageAsync(imageId);
                if (result)
                    return Ok(new { message = "Image deleted successfully" });
                else
                    return NotFound(new { message = "Image not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image");
                return StatusCode(500, new { message = "Delete failed" });
            }
        }

        [HttpPut("update-image/{imageId}")]
        public async Task<IActionResult> UpdateImage(
            int imageId,
            IFormFile? file,
            [FromForm] int? order,
            [FromForm] string? caption)
        {
            try
            {
                var request = new UpdateImageRequest
                {
                    File = file,
                    Order = order,
                    Caption = caption
                };

                var result = await _imageService.UpdateImageAsync(imageId, request);

                if (result.Success)
                    return Ok(result);
                else
                    return StatusCode(result.Status, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating image {ImageId}", imageId);
                return StatusCode(500, new { message = "Update failed" });
            }
        }



        private bool IsValidImageFile(IFormFile file)
        {
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            return allowedTypes.Contains(file.ContentType.ToLower()) && file.Length <= 10 * 1024 * 1024; // Max 10MB
        }
    }
}
